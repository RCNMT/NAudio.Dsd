using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.Dsd.Sample
{
    internal class Dsd2Dop
    {
        public static void Run(string path)
        {
            using var dsd = new DsdReader(path);
            using var dop = new DopProvider(dsd);
            
            using var wasapi = new WasapiOut(AudioClientShareMode.Exclusive, 200);
            
            wasapi.Init(dop);
            wasapi.Play();
            wasapi.Volume = 0.02f;

            int dsdSampleRate = dsd.Header.SamplingFrequency;
            int dopSampleRate = dop.WaveFormat.SampleRate;
            string inputName = $"DSD{(dsdSampleRate % 44100 == 0 ? dsdSampleRate / 44100 : dsdSampleRate / 48000)}";

            Console.WriteLine($"DSD Sample Rate: {dsdSampleRate} Hz ({inputName})");
            Console.WriteLine($"DoP Sample Rate: {dopSampleRate} Hz");
            Console.WriteLine();

            int t = Console.CursorTop;
            int l = Console.CursorLeft;
            bool seeking = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan from = TimeSpan.FromSeconds(10);
            TimeSpan to = TimeSpan.FromSeconds(60 * 3);

            while (wasapi.PlaybackState == PlaybackState.Playing)
            {
                // This if condition is for demonstration purposes.
                // Set seeking to true to enable seeking.
                // Seek to target after 10 seconds.
                if (seeking && dop.CurrentTime > from)
                {
                    dop.CurrentTime = to;
                    seeking = false;
                }

                Console.SetCursorPosition(l, t);
                Console.WriteLine($"Stopwatch: {stopwatch.Elapsed:m\\:ss\\.ff}");
                Console.WriteLine($"Buffer Size: {dop.BufferSize.TotalMilliseconds:0.00} ms");
                Console.WriteLine($"DSD Time: {dsd.CurrentTime:m\\:ss\\.ff} / {dsd.TotalTime:m\\:ss\\.ff}");
                Console.WriteLine($"PCM Time: {dop.CurrentTime:m\\:ss\\.ff} / {dop.TotalTime:m\\:ss\\.ff}");
                Console.WriteLine();
                Console.WriteLine($"DSD Position: {dsd.Position} / {dsd.Length}");
                Console.WriteLine($"DoP Position: {dop.Position} / {dop.Length}");
                Thread.Sleep(50);
            }
        }
    }
}
