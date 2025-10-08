namespace NAudio.Dsd.Dsd2Pcm
{
    public struct OutputContext
    {
        public int Bits;
        public int Channels;
        public int BlockSize;
        public int SampleRate;
        public int Decimation;
        public int PcmBlockSize;
        public int OutBlockSize;
        public int BytesPerSample;
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
        }
    }
}
