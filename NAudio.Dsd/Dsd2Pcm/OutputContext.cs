namespace NAudio.Dsd.Dsd2Pcm
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
        public float ScaleFactor;
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

        public void SetGain(float gain)
        {
            ScaleFactor = 1.0f;
            float volume = MathF.Pow(10.0f, gain / 20.0f);

            if (Bits != 32)
            {
                ScaleFactor = MathF.Pow(2.0f, Bits - 1);
            }

            PeakLevel = (int)MathF.Floor(ScaleFactor);
            ScaleFactor *= volume;
        }
    }
}
