namespace NAudio.Dsd.Dsd2Dxd
{
    public class Dsd2DxdContext
    {
        public int Delay1;
        public int Delay2;
        public int NumTables;
        public int Decimation;
        public int FifoPosition;
        public bool IsLSBFirst;
        public byte[] Fifo = null!;
        public double[][] Tables = null!;

        public static void Reset(Dsd2DxdContext ctx)
        {
            for (int i = 0; i < ctx.Fifo.Length; ++i)
                ctx.Fifo[i] = 0x69; // silence pattern

            ctx.FifoPosition = 0;
            ctx.Delay2 = ctx.Delay1;
        }

        public static Dsd2DxdContext Clone(Dsd2DxdContext ctx)
        {
            Dsd2DxdContext p2 = new()
            {
                NumTables = ctx.NumTables,
                IsLSBFirst = ctx.IsLSBFirst,
                Decimation = ctx.Decimation,
                Delay1 = ctx.Delay1,
                Fifo = (byte[])ctx.Fifo.Clone(),
                FifoPosition = ctx.FifoPosition,
                Delay2 = ctx.Delay2,
                Tables = new double[ctx.NumTables][]
            };
            for (int i = 0; i < ctx.NumTables; ++i)
            {
                p2.Tables[i] = (double[])ctx.Tables[i].Clone();
            }

            return p2;
        }
    }
}
