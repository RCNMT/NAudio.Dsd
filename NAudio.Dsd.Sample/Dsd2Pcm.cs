using NAudio.CoreAudioApi;
using NAudio.Dsd.Dsd2Pcm;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.Dsd.Sample
{
    internal class Dsd2Pcm
    {
        public static void Run(string path)
        {
            using var dsd = new DsdReader(path);
            using var pcm = new PcmProvider(dsd, PcmFormat.PCM352_8, 32, DitherType.FWeighted);

            using var wasapi = new WasapiOut(AudioClientShareMode.Shared, 200);

            wasapi.Init(pcm);
            wasapi.Play();
            wasapi.Volume = 0.02f;

            int dsdSampleRate = dsd.WaveFormat.SampleRate;
            int pcmSampleRate = pcm.WaveFormat.SampleRate;
            string inputName = DsdFormatExtensions.FromSamplingFrequency(dsdSampleRate).ToFriendlyString();

            Console.WriteLine($"DSD Sample Rate: {dsdSampleRate} Hz ({inputName})");
            Console.WriteLine($"PCM Sample Rate: {pcmSampleRate} Hz");
            Console.WriteLine($"PCM Bit Dept: {pcm.WaveFormat.BitsPerSample}-bit");
            Console.WriteLine();

            int t = Console.CursorTop;
            int l = Console.CursorLeft;
            bool seeking = true;
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan time = TimeSpan.FromSeconds(10);
            TimeSpan target = TimeSpan.FromSeconds(60 * 3 + 2); // 3 minutes

            while (wasapi.PlaybackState == PlaybackState.Playing)
            {
                // This if condition is for demonstration purposes.
                // Set seeking to true to enable seeking.
                // Seek to target after 10 seconds.
                if (seeking && stopwatch.Elapsed > time)
                {
                    pcm.CurrentTime = target;
                    seeking = false;
                }

                Console.SetCursorPosition(l, t);
                Console.WriteLine($"Stopwatch: {stopwatch.Elapsed:m\\:ss}");
                Console.WriteLine($"Buffer Size: {pcm.BufferSize.TotalMilliseconds} ms");
                Console.WriteLine($"DSD Time: {dsd.CurrentTime:m\\:ss} / {dsd.TotalTime:m\\:ss}");
                Console.WriteLine($"PCM Time: {pcm.CurrentTime:m\\:ss} / {pcm.TotalTime:m\\:ss}");
                Console.WriteLine();
                Console.WriteLine($"DSD Position: {dsd.Position} / {dsd.Length}");
                Console.WriteLine($"PCM Position: {pcm.Position} / {pcm.Length}");
                Thread.Sleep(200);
            }
        }
    }
}
