using NAudio.Dsd.Utilities;

namespace NAudio.Dsd
{
    public class DopProvider : WaveStream, IDisposable
    {
        private const byte DOP_MARKER_05 = 0x05;
        private const byte DOP_MARKER_FA = 0xFA;

        private readonly object _lock = new();
        private readonly bool _own;
        private readonly int _frameSize;
        private readonly DsdReader _source;
        private readonly BufferedWaveProvider _buffered;
        private readonly AutoResetEvent _readyEvent = new(false);
        private Task _fillBufferTask;
        private CancellationTokenSource _cts = new();
        private CancellationToken _token;
        private bool _seekRequested = false;

        private readonly WaveFormat _waveFormat;
        public override WaveFormat WaveFormat
        {
            get => _waveFormat;
        }

        private readonly long _length;
        public override long Length
        {
            get => _length;
        }

        private long _position;
        public override long Position
        {
            get => _position;
            set => CurrentTime = TimeSpan.FromSeconds(value / (float)WaveFormat.AverageBytesPerSecond);
        }

        public override TimeSpan TotalTime
        {
            get => _source.TotalTime;
        }

        public override TimeSpan CurrentTime
        {
            get => TimeSpan.FromSeconds(_position / (float)WaveFormat.AverageBytesPerSecond);
            set
            {
                lock (_lock)
                {
                    _position = (long)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
                    _seekRequested = true;
                    _source.CurrentTime = value;

                    if (_fillBufferTask == null || _fillBufferTask.IsCompleted)
                    {
                        _cts = new CancellationTokenSource();
                        _token = _cts.Token;
                        _fillBufferTask = Task.Run(FillBuffer);
                    }
                    _readyEvent.Set();
                }
            }
        }

        /// <summary>
        /// Current size of buffer
        /// </summary>
        public TimeSpan BufferSize
        {
            get => TimeSpan.FromMilliseconds(_buffered.BufferedBytes / (float)_buffered.WaveFormat.AverageBytesPerSecond * 1000.0f);
        }

        /// <summary>
        /// The maximum amount of audio data that buffer can hold as a TimeSpan (default: 5 seconds)
        /// </summary>
        public TimeSpan BufferDuration
        {
            get => _buffered.BufferDuration;
            set => _buffered.BufferDuration = value;
        }

        /// <summary>
        /// Creates a new DoP (DSD over PCM) provider from a DSD file path.
        /// </summary>
        /// <param name="path">The path to the DSD file to read.</param>
        public DopProvider(string path) : this(new DsdReader(path), true)
        {
        }

        /// <summary>
        /// Creates a new DoP (DSD over PCM) provider from an existing DsdReader instance.
        /// </summary>
        /// <param name="source">DsdReader instance to read DSD data from.</param>
        public DopProvider(DsdReader source) : this(source, false)
        {
        }

        private DopProvider(DsdReader source, bool own)
        {
            int inputRate = source.WaveFormat.SampleRate;
            _own = own;
            _source = source ?? throw new ArgumentNullException(nameof(source));

            _frameSize = _source.Header.FrameSize;
            _length = (long)(source.Length / 1.5f); // DoP is 1.5 times the size of DSD
            _waveFormat = new WaveFormat(_source.WaveFormat.SampleRate / 16, 24, _source.WaveFormat.Channels);
            _buffered = new BufferedWaveProvider(_waveFormat);
            _token = _cts.Token;
            _fillBufferTask = Task.Run(FillBuffer);
        }

        private void FillBuffer()
        {
            byte[] dsdBuffer = new byte[_frameSize];
            int channels = _source.Header.ChannelCount;
            bool isClear = false;

            try
            {
                while (_source.Position < _source.Length)
                {
                    if (_seekRequested)
                    {
                        lock (_lock)
                        {
                            _seekRequested = false;
                            isClear = true;
                        }
                    }

                    int read = _source.Read(dsdBuffer, 0, _frameSize);
                    if (read == 0) break;

                    byte[] dopBuffer = DSDToDoP(dsdBuffer, channels, read / channels);

                    while (_buffered.BufferedBytes + dopBuffer.Length > _buffered.BufferLength)
                    {
                        _token.ThrowIfCancellationRequested();
                        _readyEvent.WaitOne(200);
                    }

                    if (isClear)
                    {
                        lock (_lock) _buffered.ClearBuffer();
                        isClear = false;
                    }

                    _buffered.AddSamples(dopBuffer, 0, dopBuffer.Length);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            finally
            {
                _buffered.ReadFully = false;
                _readyEvent.Reset();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _readyEvent.Set();
            lock (_lock)
            {
                int read = _buffered.Read(buffer, offset, count);
                _position += read;
                return read;
            }
        }

        private byte[] DSDToDoP(byte[] dsdBuffer, int channels, int numBytesPerChannel)
        {
            if (channels != 2)
                throw new NotSupportedException("Only stereo DSD is supported for DoP conversion.");

            int dopFrameCount = numBytesPerChannel / 2;
            byte[] dopBuffer = new byte[dopFrameCount * 6];

            int dopIndex = 0;
            bool isArchLE = BitConverter.IsLittleEndian;     // Indicates the "endianness" of the architecture. True for little-endian, false for big-endian.
            bool isSourceLE = _source.Header.IsLittleEndian; // Indicates the "endianness" of the source DSD data. True for DSD LSB first, false for DSD MSB first.
            bool interleaved = _source.Header.Interleaved;

            for (int i = 0; i < dopFrameCount; i++)
            {
                int baseOffset;
                byte LMSB, LLSB, RMSB, RLSB;

                if (interleaved)
                {
                    baseOffset = i * 4;
                    LMSB = dsdBuffer[baseOffset];
                    RMSB = dsdBuffer[baseOffset + 1];
                    LLSB = dsdBuffer[baseOffset + 2];
                    RLSB = dsdBuffer[baseOffset + 3];
                }
                else
                {
                    baseOffset = i * 2;
                    LMSB = dsdBuffer[baseOffset];
                    LLSB = dsdBuffer[baseOffset + 1];
                    RMSB = dsdBuffer[numBytesPerChannel + baseOffset];
                    RLSB = dsdBuffer[numBytesPerChannel + baseOffset + 1];
                }

                byte marker = ((i & 1) == 0) ? DOP_MARKER_05 : DOP_MARKER_FA;

                if (isSourceLE)
                {
                    LMSB = LookupTable.BitReversal[LMSB];
                    LLSB = LookupTable.BitReversal[LLSB];
                    RMSB = LookupTable.BitReversal[RMSB];
                    RLSB = LookupTable.BitReversal[RLSB];
                }

                if (isArchLE)
                {
                    dopBuffer[dopIndex++] = LLSB;
                    dopBuffer[dopIndex++] = LMSB;
                    dopBuffer[dopIndex++] = marker;
                    dopBuffer[dopIndex++] = RLSB;
                    dopBuffer[dopIndex++] = RMSB;
                    dopBuffer[dopIndex++] = marker;
                }
                else
                {
                    dopBuffer[dopIndex++] = LMSB;
                    dopBuffer[dopIndex++] = LLSB;
                    dopBuffer[dopIndex++] = marker;
                    dopBuffer[dopIndex++] = RMSB;
                    dopBuffer[dopIndex++] = RLSB;
                    dopBuffer[dopIndex++] = marker;
                }
            }

            return dopBuffer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _own && _source != null)
            {
                _source.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
