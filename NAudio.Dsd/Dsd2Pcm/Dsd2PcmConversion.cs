namespace NAudio.Dsd.Dsd2Pcm
{
    public class Dsd2PcmConversion
    {
        private static readonly byte[] _bitReverse = [
            0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0, 0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
            0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8, 0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
            0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4, 0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
            0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC, 0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
            0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2, 0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
            0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA, 0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
            0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6, 0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
            0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE, 0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
            0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1, 0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
            0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9, 0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
            0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5, 0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
            0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED, 0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
            0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3, 0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
            0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB, 0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
            0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7, 0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
            0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF, 0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF
        ];

        private const int FIFOSIZE = 1024;
        private const int FIFOMASK = FIFOSIZE - 1;

        public FilterType FilterType { get; }
        public bool IsLSBFirst { get; }
        public int Decimation { get; }
        public int DsdRate { get; }

        private readonly Dsd2PcmContext _dsd2dxdCtx;

        /// <summary>
        /// Create a DSD to PCM conversion instance
        /// </summary>
        /// <param name="inputCtx">DSD input context</param>
        /// <param name="outputCtx">DXD output context</param>
        public Dsd2PcmConversion(InputContext inputCtx, OutputContext outputCtx)
        {
            DsdRate = inputCtx.DsdRate;
            IsLSBFirst = inputCtx.IsLSBFirst;
            FilterType = outputCtx.FilterType;
            Decimation = outputCtx.Decimation;

            _dsd2dxdCtx = Init(FilterType, IsLSBFirst, Decimation, DsdRate);
        }

        /// <summary>
        /// Translate DSD data to PCM data
        /// </summary>
        /// <param name="blockSize">Number of DSD bytes to process</param>
        /// <param name="dsdData">DSD input data</param>
        /// <param name="dsdStride">DSD input stride</param>
        /// <param name="dsdOffset">DSD input offset</param>
        /// <param name="pcmData">PCM output data</param>
        /// <param name="pcmStride">PCM output stride</param>
        /// <param name="decimation">Decimation factor (e.g. 64 for DSD64 → 44.1kHz)</param>
        public void Translate(int blockSize, byte[] dsdData, int dsdStride, int dsdOffset, double[] pcmData, int pcmStride, int decimation)
        {
            int fifoPosition = _dsd2dxdCtx.FifoPosition;
            int numFilterTables = _dsd2dxdCtx.NumTables;
            int tableOffset = numFilterTables * 2 - 1;
            int dsdIndex = 0;
            int pcmIndex = 0;

            int samplesPerOutput = decimation / 8;
            int outputCount = samplesPerOutput;

            while (blockSize-- > 0)
            {
                int dsdByte = dsdData[dsdIndex + dsdOffset];
                dsdIndex += dsdStride;

                _dsd2dxdCtx.Fifo[fifoPosition] = (byte)dsdByte;

                int fifoReadIndex = fifoPosition - numFilterTables & FIFOMASK;
                byte original = _dsd2dxdCtx.Fifo[fifoReadIndex];
                _dsd2dxdCtx.Fifo[fifoReadIndex] = _bitReverse[original & 0xFF];

                if (--outputCount == 0)
                {
                    outputCount = samplesPerOutput;

                    double accumulator = 0.0;

                    for (int i = 0; i < numFilterTables; ++i)
                    {
                        int fifoIdx1 = fifoPosition - i & FIFOMASK;
                        int fifoIdx2 = fifoPosition - tableOffset + i & FIFOMASK;

                        int val1 = _dsd2dxdCtx.Fifo[fifoIdx1];
                        int val2 = _dsd2dxdCtx.Fifo[fifoIdx2];

                        accumulator += _dsd2dxdCtx.Tables[i][val1] + _dsd2dxdCtx.Tables[i][val2];
                    }

                    if (_dsd2dxdCtx.Delay2 > 0)
                    {
                        _dsd2dxdCtx.Delay2--;
                    }
                    else
                    {
                        pcmData[pcmIndex] = accumulator;
                        pcmIndex += pcmStride;
                    }
                }

                fifoPosition = fifoPosition + 1 & FIFOMASK;
            }

            _dsd2dxdCtx.FifoPosition = fifoPosition;
        }

        /// <summary>
        /// Create a DSD to PCM conversion instance
        /// </summary>
        /// <param name="inputCtx">DSD input context</param>
        /// <param name="outputCtx">DXD output context</param>
        /// <returns></returns>
        public static Dsd2PcmConversion CreateConversion(InputContext inputCtx, OutputContext outputCtx)
        {
            return new Dsd2PcmConversion(inputCtx, outputCtx);
        }

        /// <summary>
        /// Create multiple DSD to PCM conversion instances for multichannel processing
        /// </summary>
        /// <param name="inputCtx">DSD input context</param>
        /// <param name="outputCtx">DXD output context</param>
        /// <returns></returns>
        public static Dsd2PcmConversion[] CreateConversions(InputContext inputCtx, OutputContext outputCtx)
        {
            Dsd2PcmConversion[] conversions = new Dsd2PcmConversion[outputCtx.Channels];
            for (int i = 0; i < inputCtx.Channels; ++i)
            {
                conversions[i] = new Dsd2PcmConversion(inputCtx, outputCtx);
            }
            return conversions;
        }

        private static void Precalc(Dsd2PcmContext ctx, double[] htaps, int numCoeffs)
        {
            int k;
            double acc;
            bool lsbf = ctx.IsLSBFirst;

            for (int i = 0; i < ctx.NumTables; ++i)
            {
                k = numCoeffs - i * 8;
                k = k > 8 ? 8 : k;

                int tableIdx = ctx.NumTables - 1 - i;

                for (int dsdSeq = 0; dsdSeq < 256; ++dsdSeq)
                {
                    acc = 0.0;
                    for (int bit = 0; bit < k; ++bit)
                    {
                        acc += lsbf
                            ? ((dsdSeq >> bit & 1) * 2 - 1) * htaps[i * 8 + bit]
                            : ((dsdSeq >> 7 - bit & 1) * 2 - 1) * htaps[i * 8 + bit];
                    }
                    ctx.Tables[tableIdx][dsdSeq] = acc;
                }
            }
        }

        private static Dsd2PcmContext Init(FilterType filtType, bool lsbf, int decimation, int dsdRate)
        {
            Dsd2PcmContext ctx = new();
            int numCoeffs;
            double[] htaps;

            if (dsdRate >= 2)
            {
                if (decimation == 8)
                {
                    htaps = Htaps.HtapsXld;
                    ctx.Delay1 = 6;
                }
                else if (decimation == 16)
                {
                    if (filtType == FilterType.Chebyshev)
                    {
                        htaps = Htaps.HtapsDDR16To1Cheb;
                        ctx.Delay1 = 6;
                    }
                    else
                    {
                        htaps = Htaps.HtapsDDR16To1EQ;
                        ctx.Delay1 = 6;
                    }
                }
                else if (decimation == 32)
                {
                    if (filtType == FilterType.Chebyshev)
                    {
                        htaps = Htaps.HtapsDDR32To1Cheb;
                        ctx.Delay1 = 8;
                    }
                    else
                    {
                        htaps = Htaps.HtapsDDR32To1EQ;
                        ctx.Delay1 = 8;
                    }
                }
                else if (decimation == 64)
                {
                    htaps = Htaps.HtapsDDR64To1Cheb;
                    ctx.Delay1 = 8;
                }
                else
                {
                    throw new ArgumentException("Unsupported decimation more than 64.");
                }
            }
            else if (decimation == 8)
            {
                if (filtType == FilterType.XLD)
                {
                    htaps = Htaps.HtapsXld;
                    ctx.Delay1 = 6;
                }
                else
                {
                    htaps = Htaps.HtapsD2P;
                    ctx.Delay1 = 0;
                }
            }
            else if (decimation == 16)
            {
                htaps = Htaps.Htaps16To1Xld;
                ctx.Delay1 = 6;
            }
            else if (decimation == 32)
            {
                htaps = Htaps.Htaps32To1Xld;
                ctx.Delay1 = 8;
            }
            else
            {
                throw new ArgumentException("Unsupported combination of decimation and filter type.");
            }

            numCoeffs = htaps.Length;
            ctx.Fifo = new byte[FIFOSIZE];
            ctx.NumTables = (numCoeffs + 7) / 8;
            ctx.IsLSBFirst = lsbf;
            ctx.Decimation = decimation;
            ctx.Tables = new double[ctx.NumTables][];

            for (int i = 0; i < ctx.NumTables; ++i)
            {
                ctx.Tables[i] = new double[256];
            }

            Precalc(ctx, htaps, numCoeffs);
            Dsd2PcmContext.Reset(ctx);

            return ctx;
        }
    }
}