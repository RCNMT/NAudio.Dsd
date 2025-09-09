namespace NAudio.Dsd.Dsd2Dxd
{
    public struct OutputContext
    {
        public int Bits;
        public int Channels;
        public int PeakLevel;
        public int BlockSize;
        public int SampleRate;
        public int Decimation;
        public int PcmBlockSize;
        public int OutBlockSize;
        public int BytesPerSample;
        public double ScaleFactor;
        public FilterType FilterType;

        public OutputContext(int rate, int bits, int decimation, int blockSize, int channels, FilterType filterType)
        {
            Bits = bits;
            FilterType = filterType;
            Channels = channels;
            BlockSize = blockSize;
            Decimation = decimation;
            SampleRate = rate;
            BytesPerSample = bits / 8;
            PcmBlockSize = BlockSize / (Decimation / 8);
            OutBlockSize = PcmBlockSize * Channels * BytesPerSample;
            SetGain(1);
        }

        public void SetGain(double gain)
        {
            ScaleFactor = 1.0;
            double volume = Math.Pow(10.0, gain / 20.0);

            if (Bits != 32)
            {
                ScaleFactor = Math.Pow(2.0, Bits - 1);
            }

            PeakLevel = (int)Math.Floor(ScaleFactor);
            ScaleFactor *= volume;
        }
    }
}
