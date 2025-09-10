#pragma warning disable CS0219
#pragma warning disable IDE0059

using NAudio.Dsd.Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.CursorVisible = false;

        const string file = "DSD file path here";

        Dsd2Dop.Run(file);
    }
}





