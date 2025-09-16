using NAudio.Dsd.Dsd2Pcm;

namespace NAudio.Dsd
{
    public class PcmProvider : WaveStream, IDisposable
    {
        private readonly Dither _dither;
        private readonly InputContext _inCtx;
        private readonly OutputContext _outCtx;
        private readonly bool _own;
        private readonly object _obj;
        private readonly DsdReader _source;
        private readonly AutoResetEvent _readyEvent;
        private readonly Dsd2PcmConversion[] _conversions;
        private readonly BufferedWaveProvider _buffered;
        private readonly int _frameSize;
        private readonly int _decimation;
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
                    var backtrack = TimeSpan.FromMilliseconds(500); // Back off a little to ensure proper filtering after seeking
                    _discardUntil = value;
                    _seekRequested = true;
                    _position = (long)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
                    _source.CurrentTime = value - backtrack;
                    Dsd2PcmConversion.Reset(_conversions);
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
            get => TimeSpan.FromMilliseconds(_buffered.BufferedBytes / _buffered.WaveFormat.AverageBytesPerSecond * 1000.0);
        }

        public PcmProvider(string path, PcmFormat format, int bits = 24, DitherType dither = DitherType.TriangularPDF) : this(new DsdReader(path), format, bits, true, dither)
        {
        }

        public PcmProvider(Stream stream, PcmFormat format, int bits = 24, DitherType dither = DitherType.TriangularPDF) : this(new DsdReader(stream), format, bits, false, dither)
        {
        }

        public PcmProvider(DsdReader source, PcmFormat format, int bits = 24, DitherType dither = DitherType.TriangularPDF) : this(source, format, bits, false, dither)
        {
        }

        private PcmProvider(DsdReader source, PcmFormat format, int bits, bool own, DitherType dither)
        {
            int dsdRate = source.Format.GetDsdRate();
            int channels = source.WaveFormat.Channels;
            int outputRate = format.GetSamplingFrequency();
            _own = own;
            _obj = new();
            _source = source;
            _frameSize = _source.Header.BlockSizePerChannel * _source.Header.ChannelCount;
            _readyEvent = new(false);
            _decimation = _source.WaveFormat.SampleRate / outputRate;
            _waveFormat = new WaveFormat(outputRate, bits, source.WaveFormat.Channels);

            _length = (long)(source.TotalTime.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            _inCtx = new InputContext(_source.IsLSBF, dsdRate, channels, _source.Header.BlockSizePerChannel, _source.Length, _source.Header.Interleaved);
            _outCtx = new OutputContext(_waveFormat.SampleRate, bits, _decimation, _inCtx.BlockSize, _inCtx.Channels, FilterType.Chebyshev);
            _dither = new Dither(dither, bits);
            _buffered = new BufferedWaveProvider(_waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
            };
            _conversions = Dsd2PcmConversion.CreateConversions(_inCtx, _outCtx);
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
            _fillBufferTask = Task.Run(FillBuffer);
        }

        private void FillBuffer()
        {
            byte[] buffer = new byte[_frameSize];
            int fltSize = _outCtx.PcmBlockSize;
            int pcmSize = _outCtx.PcmBlockSize * _outCtx.Channels * _outCtx.BytesPerSample;
            int channels = _outCtx.Channels;
            int bytesPerSample = _outCtx.BytesPerSample;

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

                    var fltData = new double[fltSize];
                    var pcmData = new byte[pcmSize];
                    var discard = _source.CurrentTime < _discardUntil;

                    for (int i = 0; i < channels; ++i)
                    {
                        _conversions[i].Translate(buffer.Length / channels, buffer, _inCtx.DsdStride, i * _inCtx.DsdChannelOffset, fltData, 1, _outCtx.Decimation);

                        if (discard) continue; // Skip after conversion to keep filter state intact

                        for (int j = 0; j < fltSize; ++j)
                        {
                            double sample = fltData[j];
                            sample = _dither.ApplyDither(sample);

                            int val = (int)Math.Clamp(sample * 2147483647f, int.MinValue, int.MaxValue);
                            int pos = j * channels * bytesPerSample + i * bytesPerSample;

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

                    if (discard)
                    {
                        continue;
                    }

                    while (_buffered.BufferedBytes + pcmData.Length > _buffered.BufferLength)
                    {
                        _token.ThrowIfCancellationRequested();
                        _readyEvent.WaitOne(200);
                    }

                    _buffered.AddSamples(pcmData, 0, pcmData.Length);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful exit
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
            lock (_obj)
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
