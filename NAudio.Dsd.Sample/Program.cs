using NAudio.Dsd.Sample;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.CursorVisible = false;

        const string file = "DSD file path here";

        // Convert DSD to DoP and play it using WASAPI in exclusive mode.
        Dsd2Dop.Run(file);

        // Convert DSD to PCM and play it using WASAPI in shared mode.
        //Dsd2Pcm.Run(file);
    }
}





