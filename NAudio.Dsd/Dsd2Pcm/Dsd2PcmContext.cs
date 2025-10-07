namespace NAudio.Dsd.Dsd2Pcm
{
    public class Dsd2PcmContext
    {
        public int Decimation;
        public int FifoPosition;
        public bool IsLSBFirst;
        public byte[] Fifo = null!;
        public double[][] Tables = null!;

        public void Reset()
        {
            Array.Clear(Fifo, 0, Fifo.Length);
            FifoPosition = 0;
        }
    }
}
