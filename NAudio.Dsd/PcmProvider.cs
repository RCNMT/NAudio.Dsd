using NAudio.Dsd.Dsd2Pcm;

namespace NAudio.Dsd
{
    public class PcmProvider : WaveStream, IDisposable
    {
        private readonly Dither _dither;
        private readonly InputContext _inCtx;
        private readonly OutputContext _outCtx;
        private readonly bool _own;
        private readonly object _lock;
        private readonly DsdReader _source;
        private readonly AutoResetEvent _readyEvent;
        private readonly Dsd2PcmConversion[] _conversions;
        private readonly MultiStageResampler[] _resampler;
        private readonly BufferedWaveProvider _buffered;
        private readonly int _frameSize;
        private readonly int _decimation;
        private readonly int _targetChannels;
        private readonly int _sourceChannels;
        private readonly Task _fillBufferTask;
        private CancellationTokenSource _cts;
        private CancellationToken _token;
        private TimeSpan _discardUntil;
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
                    var warmupTime = TimeSpan.FromSeconds(_conversions[0].Size / (float)_source.WaveFormat.SampleRate);
                    var backtrack = warmupTime + TimeSpan.FromMilliseconds(10);
                    _discardUntil = value;
                    _seekRequested = true;
                    _position = (long)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
                    _source.CurrentTime = value - backtrack;
                    if (_fillBufferTask == null || _fillBufferTask.IsCompleted)
                    {
                        _cts = new CancellationTokenSource();
                        _token = _cts.Token;
                        Task.Run(FillBuffer, _token);
                    }
                    _readyEvent.Set();
                }
            }
        }

        public TimeSpan BufferSize
        {
            get => TimeSpan.FromMilliseconds(_buffered.BufferedBytes / (float)_buffered.WaveFormat.AverageBytesPerSecond * 1000.0);
        }

        public List<string> ConversionSteps { get; } = [];

        public PcmProvider(string path, WaveFormat format, int latency, DitherType dither = DitherType.TriangularPDF, FilterType filter = FilterType.Kaiser, double[]? coeff = null) :
            this(new DsdReader(path), format, latency, true, dither, filter, coeff)
        {
        }

        public PcmProvider(DsdReader source, WaveFormat format, int latency, DitherType dither = DitherType.TriangularPDF, FilterType filter = FilterType.Kaiser, double[]? coeff = null) :
            this(source, format, latency, false, dither, filter, coeff)
        {
        }

        readonly int _latency;
        readonly int _bytesDelay;

        private PcmProvider(DsdReader source, WaveFormat format, int latency, bool own, DitherType dither, FilterType filter, double[]? coeff)
        {
            if (format.Encoding != WaveFormatEncoding.Pcm)
            {
                throw new ArgumentException("Must be PCM encoding");
            }

            _latency = latency;
            _targetChannels = format.Channels;
            _sourceChannels = source.WaveFormat.Channels;

            if (_targetChannels > _sourceChannels)
            {
                throw new ArgumentException("Target channels cannot be greater than source channels");
            }

            int bits = format.BitsPerSample;
            int targetRate = format.SampleRate;
            int sourceRate = source.WaveFormat.SampleRate;
            int resampleRate = sourceRate % 11025 == 0 ? 44100 * 8 : 48000 * 8;

            while (resampleRate < targetRate)
            {
                resampleRate *= 2;
            }

            _own = own;
            _lock = new();
            _source = source;
            _frameSize = _source.Header.FrameSize;
            _readyEvent = new(false);
            _decimation = _source.WaveFormat.SampleRate / resampleRate;
            _waveFormat = new WaveFormat(targetRate, bits, _targetChannels);
            _discardUntil = TimeSpan.Zero;
            _length = (long)(source.TotalTime.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            _inCtx = new InputContext(_source.IsLSBF, sourceRate, _sourceChannels, _source.Header.BlockSizePerChannel, _source.Length, _source.Header.Interleaved);
            _outCtx = new OutputContext(resampleRate, bits, _decimation, _inCtx.BlockSize, _inCtx.Channels, filter);
            _dither = new Dither(dither, bits);
            _buffered = new BufferedWaveProvider(_waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
            };
            _bytesDelay = _latency * (_buffered.WaveFormat.AverageBytesPerSecond / 1000);
            _resampler = MultiStageResampler.CreateMultiStageResamplers(resampleRate, targetRate, _targetChannels);
            _conversions = Dsd2PcmConversion.CreateConversions(_inCtx, _outCtx, coeff);
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
            _fillBufferTask = Task.Run(FillBuffer);

            ConversionSteps.Add($"DSD {sourceRate}Hz");
            ConversionSteps.Add($"PCM {resampleRate}Hz");
            if (_resampler.Length > 0)
            {
                foreach (var item in _resampler[0].ConversionSteps)
                {
                    ConversionSteps.Add($"PCM {item.Item2}Hz");
                }
            }
        }

        private void FillBuffer()
        {
            int fltSize = _outCtx.PcmBlockSize;
            int pcmSize = _outCtx.PcmBlockSize * _outCtx.Channels * _outCtx.BytesPerSample;
            int sourceChannels = _sourceChannels;
            int targetChannels = _targetChannels;
            int bytesPerSample = _outCtx.BytesPerSample;
            bool isDiscard = false;
            bool isClear = false;
            byte[] dsdData = new byte[_frameSize];
            byte[] pcmData = [];
            double[] fltData = new double[fltSize];
            double[] rspData;

            try
            {
                while (_source.Position < _source.Length)
                {
                    if (_seekRequested)
                    {
                        lock (_lock)
                        {
                            _seekRequested = false;
                            Dsd2PcmConversion.Reset(_conversions);
                            isDiscard = _source.CurrentTime < _discardUntil;
                            isClear = true;
                        }
                    }

                    int read = _source.Read(dsdData, 0, _frameSize);
                    if (read == 0) break;

                    if (isDiscard)
                    {
                        for (int i = 0; i < sourceChannels; ++i) // Warmup filter
                            _conversions[i].Translate(read / sourceChannels, dsdData, i * _inCtx.DsdChannelOffset, fltData);

                        isDiscard = _source.CurrentTime < _discardUntil;
                        continue;
                    }

                    for (int i = 0; i < targetChannels; ++i)
                    {
                        _conversions[i].Translate(read / sourceChannels, dsdData, i * _inCtx.DsdChannelOffset, fltData);
                        rspData = _resampler[i].Resample(fltData);

                        if (pcmData.Length != rspData.Length * targetChannels * bytesPerSample)
                        {
                            Array.Resize(ref pcmData, rspData.Length * targetChannels * bytesPerSample);
                        }

                        for (int j = 0; j < rspData.Length; ++j)
                        {
                            double sample = rspData[j];
                            sample = _dither.ApplyDither(sample);

                            int pos = j * targetChannels * bytesPerSample + i * bytesPerSample;
                            if (bytesPerSample == 2)        // Float to 16-bit
                            {
                                short sample16 = (short)Math.Clamp(sample * 32767f, short.MinValue, short.MaxValue);
                                byte[] bytes = BitConverter.GetBytes(sample16);
                                pcmData[pos] = bytes[0];
                                pcmData[pos + 1] = bytes[1];
                            }
                            else if (bytesPerSample == 3)   // Float to 24-bit
                            {
                                int sample24 = (int)Math.Clamp(sample * 8388607f, -8388608, 8388607);
                                pcmData[pos] = (byte)(sample24 & 0xFF);
                                pcmData[pos + 1] = (byte)((sample24 >> 8) & 0xFF);
                                pcmData[pos + 2] = (byte)((sample24 >> 16) & 0xFF);
                            }
                            else if (bytesPerSample == 4)   // Float to 32-bit
                            {
                                int sample32 = (int)Math.Clamp(sample * 2147483647f, int.MinValue, int.MaxValue);
                                byte[] bytes = BitConverter.GetBytes(sample32);
                                pcmData[pos] = bytes[0];
                                pcmData[pos + 1] = bytes[1];
                                pcmData[pos + 2] = bytes[2];
                                pcmData[pos + 3] = bytes[3];
                            }
                        }
                    }

                    while (_buffered.BufferedBytes + pcmData.Length > _buffered.BufferLength)
                    {
                        _token.ThrowIfCancellationRequested();
                        _readyEvent.WaitOne(200);
                    }

                    if (isClear)
                    {
                        lock (_lock) _buffered.ClearBuffer();
                        isClear = false;
                    }

                    _buffered.AddSamples(pcmData, 0, pcmData.Length);
                }
            }
            catch (OperationCanceledException)
            {
                // Exit
            }
            catch (Exception)
            {
                throw;
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
            while (_buffered.BufferedBytes < _bytesDelay)
            {
                Thread.Sleep(_latency);
            }
            lock (_lock)
            {

                int read = _buffered.Read(buffer, offset, count);
                _position += read;
                return read;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_own && _source != null)
                {
                    _source.Dispose();
                }
                if (_cts != null)
                {
                    _cts.Cancel();
                    _fillBufferTask.Wait();
                }
                _cts?.Dispose();
                _readyEvent?.Dispose();
                _fillBufferTask?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
