namespace NAudio.Dsd
{
    public class DopProvider : WaveStream, IDisposable
    {
        private const byte DOP_MARKER_05 = 0x05;
        private const byte DOP_MARKER_FA = 0xFA;

        private readonly object _obj = new();
        private delegate byte[] ConversionDelegate(byte[] dsdBuffer);
        private readonly bool _own;
        private readonly bool _isLittleEndian;
        private readonly int _ratio;
        private readonly int _frameSize;
        private readonly ConversionDelegate _conversion;
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
            set => CurrentTime = TimeSpan.FromSeconds(value / WaveFormat.AverageBytesPerSecond);
        }

        public override TimeSpan TotalTime
        {
            get => _source.TotalTime;
        }

        public override TimeSpan CurrentTime
        {
            get => TimeSpan.FromSeconds(_position / WaveFormat.AverageBytesPerSecond);
            set
            {
                lock (_obj)
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
        /// Creates a new DoP (DSD over PCM) provider from a DSD file path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ratio"></param>
        public DopProvider(string path, int ratio = 1) : this(new DsdReader(path), true, ratio)
        {
        }

        /// <summary>
        /// Creates a new DoP (DSD over PCM) provider from a stream containing DSD data.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ratio"></param>
        public DopProvider(Stream stream, int ratio = 1) : this(new DsdReader(stream), false, ratio)
        {
        }

        /// <summary>
        /// Creates a new DoP (DSD over PCM) provider from an existing DsdReader instance.
        /// </summary>
        /// <param name="source">DsdReader instance to read DSD data from.</param>
        /// <param name="ratio"></param>
        public DopProvider(DsdReader source, int ratio = 1) : this(source, false, ratio)
        {
        }

        private DopProvider(DsdReader source, bool own, int ratio)
        {
            _own = own;
            _ratio = ratio;
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _length = (long)(source.Length / _ratio * 1.5); // DoP is 1.5 times the size of DSD
            _frameSize = (int)(_source.Header.BlockSizePerChannel * _source.Header.ChannelCount);
            _waveFormat = new WaveFormat(_source.WaveFormat.SampleRate / (_ratio * 16), 24, _source.WaveFormat.Channels);
            _isLittleEndian = _source.Header.BitsPerSample == 1;
            _conversion = ratio switch
            {
                1 => dsdBuffer => dsdBuffer, // No conversion needed
                2 => DsdConversion.DSD2xToDSD1x_FIR2nd,
                4 => DsdConversion.DSD4xToDSD1x_FIR2nd,
                8 => DsdConversion.DSD8xToDSD1x_FIR2nd,
                16 => DsdConversion.DSD16xToDSD1x,
                _ => throw new ArgumentOutOfRangeException(nameof(ratio), "Ratio must be 1, 2, 4, 8, or 16")
            };
            _buffered = new BufferedWaveProvider(_waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(5),
            };
            _token = _cts.Token;
            _fillBufferTask = Task.Run(FillBuffer);
        }

        private void FillBuffer()
        {
            byte[] buffer = new byte[_frameSize];
            try
            {
                while (_source.Position + _frameSize <= _source.Length)
                {
                    if (_seekRequested)
                    {
                        lock (_obj)
                        {
                            _buffered.ClearBuffer();
                            _seekRequested = false;
                        }
                    }
                    int read = _source.Read(buffer, 0, _frameSize);
                    if (read == 0) break;

                    byte[] dsdBuffer = _conversion(buffer);
                    byte[] dopBuffer = DSDToDoP(dsdBuffer, (int)_source.Header.ChannelCount, dsdBuffer.Length / (int)_source.Header.ChannelCount);

                    while (_buffered.BufferedBytes + dopBuffer.Length > _buffered.BufferLength)
                    {
                        _token.ThrowIfCancellationRequested();
                        _readyEvent.WaitOne(200);
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
            lock (_obj)
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
            bool isArchLE = BitConverter.IsLittleEndian;    // Indicates the "endianness" of the architecture. True for little-endian, false for big-endian.
            bool isSourceLE = _isLittleEndian;              // Indicates the "endianness" of the source DSD data. True for DSD LSB first, false for DSD MSB first.

            for (int i = 0; i < dopFrameCount; i++)
            {
                int baseOffset = i * 2;

                byte LMSB = dsdBuffer[baseOffset];
                byte LLSB = dsdBuffer[baseOffset + 1];
                byte RMSB = dsdBuffer[numBytesPerChannel + baseOffset];
                byte RLSM = dsdBuffer[numBytesPerChannel + baseOffset + 1];

                byte marker = ((i & 1) == 0) ? DOP_MARKER_05 : DOP_MARKER_FA;

                if (isSourceLE)
                {
                    LMSB = ReverseByteWithLookup(LMSB);
                    LLSB = ReverseByteWithLookup(LLSB);
                    RMSB = ReverseByteWithLookup(RMSB);
                    RLSM = ReverseByteWithLookup(RLSM);
                }

                if (isArchLE)
                {
                    dopBuffer[dopIndex++] = LLSB;
                    dopBuffer[dopIndex++] = LMSB;
                    dopBuffer[dopIndex++] = marker;
                    dopBuffer[dopIndex++] = RLSM;
                    dopBuffer[dopIndex++] = RMSB;
                    dopBuffer[dopIndex++] = marker;
                }
                else
                {
                    dopBuffer[dopIndex++] = LMSB;
                    dopBuffer[dopIndex++] = LLSB;
                    dopBuffer[dopIndex++] = marker;
                    dopBuffer[dopIndex++] = RMSB;
                    dopBuffer[dopIndex++] = RLSM;
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

        public static readonly byte[] ReverseByteTable =
            [.. Enumerable.Range(0, 256).Select(x => (byte)((x * 0x0202020202 & 0x010884422010) % 1023))];

        public static byte ReverseByteWithLookup(byte b)
        {
            return ReverseByteTable[b];
        }
    }
}
