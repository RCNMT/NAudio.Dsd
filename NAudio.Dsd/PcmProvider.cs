using NAudio.Dsd.Dsd2Pcm;
using System.Diagnostics;

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
        private readonly MultiStageResampler[] _resamplers;
        private readonly BufferedWaveProvider _buffered;
        private readonly int _frameSize;
        private readonly int _decimation;
        private readonly int _targetChannels;
        private readonly int _sourceChannels;
        private readonly Task _fillBufferTask;
        private CancellationTokenSource _cts;
        private CancellationToken _token;
        private TimeSpan _discardUntil;
        private Stopwatch? _delayTimer;
        private bool _seekRequested = false;
        private bool _delayNeeded = true;
        private int _delayMilliseconds;

        private delegate void Float64ToInt(byte[] buffer, int position, double value);
        private readonly Float64ToInt FloatToInt;

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
            set => CurrentTime = TimeSpan.FromSeconds(value / (double)WaveFormat.AverageBytesPerSecond);
        }

        public override TimeSpan TotalTime
        {
            get => _source.TotalTime;
        }

        public override TimeSpan CurrentTime
        {
            get => TimeSpan.FromSeconds(_position / (double)WaveFormat.AverageBytesPerSecond);
            set
            {
                lock (_lock)
                {
                    var warmupTime = TimeSpan.FromSeconds(_conversions[0].Size / (double)_source.WaveFormat.SampleRate);
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
            get => TimeSpan.FromMilliseconds(_buffered.BufferedBytes / (double)_buffered.WaveFormat.AverageBytesPerSecond * 1000.0);
        }

        public List<string> ConversionSteps { get; } = [];

        public int DelayMilliseconds
        {
            get { return _delayMilliseconds; }
            set { _delayMilliseconds = Math.Max(value, _decimation * ConversionSteps.Count); }
        }

        public PcmProvider(string path, WaveFormat format, DitherType dither = DitherType.TriangularPDF, FilterType filter = FilterType.Kaiser, double[]? coeff = null) :
            this(new DsdReader(path), format, true, dither, filter, coeff)
        {
        }

        public PcmProvider(DsdReader source, WaveFormat format, DitherType dither = DitherType.TriangularPDF, FilterType filter = FilterType.Kaiser, double[]? coeff = null) :
            this(source, format, false, dither, filter, coeff)
        {
        }

        public PcmProvider(string path, WaveFormat format, DitherType dither = DitherType.TriangularPDF, double[]? coeff = null) :
            this(new DsdReader(path), format, true, dither, FilterType.Custom, coeff: coeff)
        {
        }

        public PcmProvider(DsdReader source, WaveFormat format, DitherType dither = DitherType.TriangularPDF, double[]? coeff = null) :
            this(source, format, false, dither, FilterType.Custom, coeff: coeff)
        {
        }

        private PcmProvider(DsdReader source, WaveFormat format, bool own, DitherType dither, FilterType filter, double[]? coeff)
        {
            if (format.Encoding != WaveFormatEncoding.Pcm)
            {
                throw new ArgumentException("Must be PCM encoding");
            }

            _targetChannels = format.Channels;
            _sourceChannels = source.WaveFormat.Channels;

            if (_targetChannels > _sourceChannels)
            {
                throw new ArgumentException("Target channels cannot be greater than source channels");
            }

            int bits = format.BitsPerSample;
            int targetRate = format.SampleRate;
            int sourceRate = source.WaveFormat.SampleRate;
            int outputRate = sourceRate % 11025 == 0 ? 44100 * 8 : 48000 * 8;

            while (outputRate < targetRate)
            {
                outputRate *= 2;
            }

            var stages = MultiStageResampler.GetIntermediates(outputRate, targetRate, 2);

            while (stages.Count > 3) // lower Dsd2Pcm output
            {
                outputRate /= 2;
                stages = MultiStageResampler.GetIntermediates(outputRate, targetRate, 2);
            }

            _own = own;
            _lock = new();
            _source = source;
            _frameSize = _source.Header.FrameSize;
            _readyEvent = new(false);
            _decimation = sourceRate / outputRate;
            _waveFormat = new WaveFormat(targetRate, bits, _targetChannels);
            _discardUntil = TimeSpan.Zero;
            _length = (long)(source.TotalTime.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            _inCtx = new InputContext(_source.IsLSBF, sourceRate, _sourceChannels, _source.Header.BlockSizePerChannel, _source.Length, _source.Header.Interleaved);
            _outCtx = new OutputContext(outputRate, bits, _decimation, _inCtx.BlockSize, _inCtx.Channels, filter);
            _dither = new Dither(dither, bits);
            _buffered = new BufferedWaveProvider(_waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
            };
            _resamplers = MultiStageResampler.CreateMultiStageResamplers(stages, _targetChannels);
            _conversions = Dsd2PcmConversion.CreateConversions(_inCtx, _outCtx, coeff);
            FloatToInt = bits switch
            {
                16 => Float64ToInt16,
                24 => Float64ToInt24,
                32 => Float64ToInt32,
                _ => throw new NotImplementedException(),
            };

            _cts = new CancellationTokenSource();
            _token = _cts.Token;
            _fillBufferTask = Task.Run(FillBuffer);

            ConversionSteps.Add($"DSD {sourceRate}Hz");
            ConversionSteps.Add($"PCM {outputRate}Hz");
            if (_resamplers.Length > 0)
            {
                foreach (var item in _resamplers[0].ConversionSteps)
                {
                    ConversionSteps.Add($"PCM {item.Item2}Hz");
                }
            }

            _delayMilliseconds = _decimation * ConversionSteps.Count;
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
                            MultiStageResampler.Reset(_resamplers);
                            isDiscard = _source.CurrentTime < _discardUntil;
                            isClear = true;
                        }
                    }

                    int read = _source.Read(dsdData, 0, _frameSize);
                    if (read == 0) break;

                    if (isDiscard)
                    {
                        for (int i = 0; i < sourceChannels; ++i) // Warmup filter
                        {
                            _conversions[i].Translate(read / sourceChannels, dsdData, i * _inCtx.DsdChannelOffset, fltData);
                            _ = _resamplers[i].Resample(fltData);
                        }
                        isDiscard = _source.CurrentTime < _discardUntil;
                        continue;
                    }

                    for (int i = 0; i < targetChannels; ++i)
                    {
                        _conversions[i].Translate(read / sourceChannels, dsdData, i * _inCtx.DsdChannelOffset, fltData);
                        rspData = _resamplers[i].Resample(fltData);

                        if (pcmData.Length != rspData.Length * targetChannels * bytesPerSample)
                        {
                            Array.Resize(ref pcmData, rspData.Length * targetChannels * bytesPerSample);
                        }

                        for (int j = 0; j < rspData.Length; ++j)
                        {
                            double sample = rspData[j];
                            sample = _dither.ApplyDither(sample);

                            int pos = j * targetChannels * bytesPerSample + i * bytesPerSample;
                            FloatToInt(pcmData, pos, sample);
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
            if (_delayNeeded)
            {
                _delayTimer ??= Stopwatch.StartNew();
                if (_delayMilliseconds <= _delayTimer.ElapsedMilliseconds)
                {
                    _delayNeeded = false;
                    _delayTimer = null;
                }

                Array.Clear(buffer, offset, count);
                return count;
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

        #region Float64ToInteger
        private static void Float64ToInt16(byte[] buffer, int position, double value)
        {
            short sample16 = (short)Math.Clamp(value * 32767f, short.MinValue, short.MaxValue);
            byte[] bytes = BitConverter.GetBytes(sample16);
            buffer[position] = bytes[0];
            buffer[position + 1] = bytes[1];
        }

        private static void Float64ToInt24(byte[] buffer, int position, double value)
        {
            int sample24 = (int)Math.Clamp(value * 8388607f, -8388608, 8388607);
            buffer[position] = (byte)(sample24 & 0xFF);
            buffer[position + 1] = (byte)((sample24 >> 8) & 0xFF);
            buffer[position + 2] = (byte)((sample24 >> 16) & 0xFF);
        }

        private static void Float64ToInt32(byte[] buffer, int position, double value)
        {
            int sample32 = (int)Math.Clamp(value * 2147483647f, int.MinValue, int.MaxValue);
            byte[] bytes = BitConverter.GetBytes(sample32);
            buffer[position] = bytes[0];
            buffer[position + 1] = bytes[1];
            buffer[position + 2] = bytes[2];
            buffer[position + 3] = bytes[3];
        }
        #endregion
    }
}
