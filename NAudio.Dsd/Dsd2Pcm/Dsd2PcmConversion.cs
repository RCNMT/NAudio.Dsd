using NAudio.Dsd.Utilities;

namespace NAudio.Dsd.Dsd2Pcm
{
    public class Dsd2PcmConversion
    {
        private const int FIFOSIZE = 1024;
        private const int FIFOMASK = FIFOSIZE - 1;

        private readonly object _lock;
        private readonly int _size;
        private readonly int _decimation;
        private readonly InputContext _inputCtx;
        private readonly OutputContext _outputCtx;
        private readonly Dsd2PcmContext _dsd2dxdCtx;

        public int Size
        {
            get { return _size; }
        }

        public int Decimation
        {
            get { return _decimation; }
        }

        public FilterType FilterType
        {
            get { return _outputCtx.FilterType; }
        }

        /// <summary>
        /// Create a DSD to PCM conversion instance
        /// </summary>
        /// <param name="inputCtx">DSD input context</param>
        /// <param name="outputCtx">DXD output context</param>
        /// <param name="coeff">Array of FIR coefficients</param>
        public Dsd2PcmConversion(InputContext inputCtx, OutputContext outputCtx, float[]? coeff = null)
        {
            _lock = new object();
            _inputCtx = inputCtx;
            _outputCtx = outputCtx;
            _decimation = outputCtx.Decimation;

            float[] h;
            if (coeff != null)
            {
                h = coeff;
                _outputCtx.FilterType = FilterType.Custom;
            }
            else
            {
                var taps = _inputCtx.SampleRate switch
                {
                    2822400 => 96,
                    5644800 => 128,
                    11289600 => 192,
                    22579200 => 256,
                    45158400 => 384,
                    _ => throw new ArgumentException("Invalid DSD sample rate")
                };
                var factor = _outputCtx.SampleRate switch
                {
                    <= 44100 => 0.49f,
                    <= 88200 => 0.48f,
                    <= 176400 => 0.46f,
                    <= 352800 => 0.46f,
                    <= 705600 => 0.40f,
                    _ => throw new ArgumentException("Invalid PCM sample rate")
                };
                h = _outputCtx.FilterType switch
                {
                    FilterType.BlackmanHarris => Filter.DesignBlackmanHarrisFir(_inputCtx.SampleRate, _outputCtx.SampleRate, taps, factor),
                    FilterType.Chebyshev => Filter.DesignChebyshevFir(_inputCtx.SampleRate, _outputCtx.SampleRate, taps, 100, factor),
                    FilterType.Blackman => Filter.DesignBlackmanFir(_inputCtx.SampleRate, _outputCtx.SampleRate, taps, factor),
                    FilterType.Hamming => Filter.DesignHammingFir(_inputCtx.SampleRate, _outputCtx.SampleRate, taps, factor),
                    FilterType.Kaiser => Filter.DesignKaiserFir(_inputCtx.SampleRate, _outputCtx.SampleRate, taps, 80, factor),
                    FilterType.Custom => throw new ArgumentException($"{nameof(coeff)} can't be null with {nameof(FilterType.Custom)} filter type"),
                    _ => throw new ArgumentException("Invalid FIR coefficients")
                };
            }

            _size = (h.Length + 7) / 8;
            _dsd2dxdCtx = new Dsd2PcmContext
            {
                Fifo = new byte[FIFOSIZE],
                Tables = new float[_size][],
                IsLSBFirst = _inputCtx.IsLSBFirst,
                Decimation = _outputCtx.Decimation,
            };

            for (int i = 0; i < _size; ++i)
                _dsd2dxdCtx.Tables[i] = new float[256];

            Precalc(_dsd2dxdCtx, h, h.Length);
        }

        /// <summary>
        /// Translate DSD data to PCM data
        /// </summary>
        /// <param name="blockSize">Number of DSD bytes to process</param>
        /// <param name="dsdData">DSD input data</param>
        /// <param name="dsdOffset">DSD input offset</param>
        /// <param name="pcmData">PCM output data</param>
        public void Translate(int blockSize, byte[] dsdData, int dsdOffset, float[] pcmData)
        {
            lock (_lock)
            {
                int fifoPosition = _dsd2dxdCtx.FifoPosition;
                int numFilterTables = _dsd2dxdCtx.Tables.Length;
                int tableOffset = numFilterTables * 2 - 1;
                int dsdIndex = 0;
                int pcmIndex = 0;

                int samplesPerOutput = _decimation / 8;
                int outputCount = samplesPerOutput;

                while (blockSize-- > 0)
                {
                    int dsdByte = dsdData[dsdIndex + dsdOffset];
                    dsdIndex += _inputCtx.DsdStride;

                    _dsd2dxdCtx.Fifo[fifoPosition] = (byte)dsdByte;

                    int fifoReadIndex = fifoPosition - numFilterTables & FIFOMASK;
                    byte original = _dsd2dxdCtx.Fifo[fifoReadIndex];
                    _dsd2dxdCtx.Fifo[fifoReadIndex] = LookupTable.BitReversal[original & 0xFF];

                    if (--outputCount == 0)
                    {
                        outputCount = samplesPerOutput;

                        float accumulator = 0.0f;

                        for (int i = 0; i < numFilterTables; ++i)
                        {
                            int fifoIdx1 = fifoPosition - i & FIFOMASK;
                            int fifoIdx2 = fifoPosition - tableOffset + i & FIFOMASK;

                            int val1 = _dsd2dxdCtx.Fifo[fifoIdx1];
                            int val2 = _dsd2dxdCtx.Fifo[fifoIdx2];

                            accumulator += _dsd2dxdCtx.Tables[i][val1] + _dsd2dxdCtx.Tables[i][val2];
                        }

                        pcmData[pcmIndex] = accumulator;
                        pcmIndex += 1;
                    }

                    fifoPosition = fifoPosition + 1 & FIFOMASK;
                }

                _dsd2dxdCtx.FifoPosition = fifoPosition;
            }
        }

        /// <summary>
        /// Reset the internal state of the conversion instance
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                Array.Clear(_dsd2dxdCtx.Fifo, 0, _dsd2dxdCtx.Fifo.Length);
                _dsd2dxdCtx.FifoPosition = 0;
            }
        }

        /// <summary>
        /// Create multiple DSD to PCM conversion instances for multichannel processing
        /// </summary>
        /// <param name="inputCtx">DSD input context</param>
        /// <param name="outputCtx">DXD output context</param>
        /// <returns></returns>
        public static Dsd2PcmConversion[] CreateConversions(InputContext inputCtx, OutputContext outputCtx, float[]? coeff = null)
        {
            Dsd2PcmConversion[] conversions = new Dsd2PcmConversion[outputCtx.Channels];
            for (int i = 0; i < inputCtx.Channels; ++i)
            {
                conversions[i] = new Dsd2PcmConversion(inputCtx, outputCtx, coeff);
            }
            return conversions;
        }

        /// <summary>
        /// Reset multiple DSD to PCM conversion instances for multichannel processing
        /// </summary>
        /// <param name="conversions"></param>
        public static void Reset(Dsd2PcmConversion[] conversions)
        {
            foreach (var conv in conversions)
            {
                conv.Reset();
            }
        }

        private static void Precalc(Dsd2PcmContext ctx, float[] htaps, int numCoeffs)
        {
            int numTables = ctx.Tables.Length;
            bool lsbf = ctx.IsLSBFirst;

            for (int phase = 0; phase < numTables; ++phase)
            {
                int validTaps = Math.Min(8, numCoeffs - phase * 8);
                int tableIndex = numTables - 1 - phase;

                for (int dsdSeq = 0; dsdSeq < 256; ++dsdSeq)
                {
                    float acc = 0.0f;

                    for (int bit = 0; bit < validTaps; ++bit)
                    {
                        int tapIndex = phase * 8 + bit;
                        if (tapIndex >= numCoeffs) break;

                        int bitVal = lsbf ? (dsdSeq >> bit) & 1 : (dsdSeq >> (7 - bit)) & 1;

                        acc += (bitVal * 2 - 1) * htaps[tapIndex];
                    }

                    ctx.Tables[tableIndex][dsdSeq] = acc;
                }
            }

            ctx.Reset();
        }
    }
}