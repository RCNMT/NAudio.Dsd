namespace NAudio.Dsd.Dsd2Dxd
{
    public struct InputContext
    {
        public int DsdRate;
        public int Channels;
        public int DsdStride;
        public int BlockSize;
        public int DsdChannelOffset;
        public bool IsLSBFirst;
        public long DataLength;

        public InputContext() { }

        public InputContext(bool isLSBFirst, int dsdRate, int channels, int blockSize, long length)
        {
            IsLSBFirst = isLSBFirst;
            DsdRate = dsdRate;
            Channels = channels;
            DataLength = length;
            SetBlockSize(blockSize);
        }

        public void SetBlockSize(int blockSize)
        {
            BlockSize = blockSize;
            bool interleaved = !(Channels > 1);
            DsdChannelOffset = interleaved ? 1 : BlockSize;
            DsdStride = interleaved ? Channels : 1;
        }
    }
}
