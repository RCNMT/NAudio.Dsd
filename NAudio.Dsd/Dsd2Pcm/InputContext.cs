namespace NAudio.Dsd.Dsd2Pcm
{
    public struct InputContext
    {
        public int DsdRate;
        public int Channels;
        public int DsdStride;
        public int BlockSize;
        public int DsdChannelOffset;
        public bool IsLSBFirst;
        public bool Interleaved;
        public long DataLength;

        public InputContext() { }

        public InputContext(bool isLSBFirst, int dsdRate, int channels, int blockSize, long length, bool interleaved)
        {
            IsLSBFirst = isLSBFirst;
            DsdRate = dsdRate;
            Channels = channels;
            DataLength = length;
            Interleaved = interleaved;
            SetBlockSize(blockSize);
        }

        public void SetBlockSize(int blockSize)
        {
            BlockSize = blockSize;
            DsdChannelOffset = Interleaved ? 1 : BlockSize;
            DsdStride = Interleaved ? Channels : 1;
        }
    }
}
