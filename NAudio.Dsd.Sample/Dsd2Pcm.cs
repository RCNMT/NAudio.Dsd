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
            using var pcm = new PcmProvider(dsd, new WaveFormat(352800, 32, 2), DitherType.FWeighted, FilterType.BlackmanHarris, 3);

            using var wasapi = new WasapiOut(AudioClientShareMode.Shared, 200);

            wasapi.Init(pcm);
            wasapi.Play();
            wasapi.Volume = 0.02f;

            int dsdSampleRate = dsd.WaveFormat.SampleRate;
            int pcmSampleRate = pcm.WaveFormat.SampleRate;
            string inputName = $"DSD{(dsdSampleRate % 44100 == 0 ? dsdSampleRate / 44100 : dsdSampleRate / 48000)}";

            Console.WriteLine($"DSD Sample Rate: {dsdSampleRate} Hz ({inputName})");
            Console.WriteLine($"PCM Sample Rate: {pcmSampleRate} Hz");
            Console.WriteLine($"PCM Bit Dept: {pcm.WaveFormat.BitsPerSample}-bit");
            Console.WriteLine();
            Console.WriteLine($"Decimation: {dsdSampleRate / (double)pcmSampleRate}");
            Console.WriteLine();

            int max = pcm.ConversionSteps.Max(s => s.Length);
            for (int i = 0, j = 1; i <= pcm.ConversionSteps.Count && j < pcm.ConversionSteps.Count; i++, j++)
            {
                Console.WriteLine($"{pcm.ConversionSteps[i]} {new string(' ', max - pcm.ConversionSteps[i].Length)}-> {pcm.ConversionSteps[j]}");
            }
            Console.WriteLine();

            int t = Console.CursorTop;
            int l = Console.CursorLeft;
            bool seeking = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan from = TimeSpan.FromSeconds(10);
            TimeSpan to = TimeSpan.FromMinutes(2);

            while (wasapi.PlaybackState == PlaybackState.Playing)
            {
                // This if condition is for demonstration purposes.
                // Set seeking to true to enable seeking.
                // Seek to target after 10 seconds.
                if (seeking && pcm.CurrentTime > from)
                {
                    pcm.CurrentTime = to;
                    seeking = false;
                }

                Console.SetCursorPosition(l, t);
                Console.WriteLine($"Stopwatch: {stopwatch.Elapsed:m\\:ss\\.ff}");
                Console.WriteLine($"Buffer Size: {pcm.BufferSize.TotalMilliseconds:0.00} ms");
                Console.WriteLine($"DSD Time: {dsd.CurrentTime:m\\:ss\\.ff} / {dsd.TotalTime:m\\:ss\\.ff}");
                Console.WriteLine($"PCM Time: {pcm.CurrentTime:m\\:ss\\.ff} / {pcm.TotalTime:m\\:ss\\.ff}");
                Console.WriteLine();
                Console.WriteLine($"DSD Position: {dsd.Position} / {dsd.Length}");
                Console.WriteLine($"PCM Position: {pcm.Position} / {pcm.Length}");
                Thread.Sleep(50);
            }
        }
    }
}
