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
            string inputName = DsdFormatExtensions.FromSamplingFrequency(dsdSampleRate).ToFriendlyString();

            Console.WriteLine($"DSD Sample Rate: {dsdSampleRate} Hz ({inputName})");
            Console.WriteLine($"DoP Sample Rate: {dopSampleRate} Hz");
            Console.WriteLine();

            int t = Console.CursorTop;
            int l = Console.CursorLeft;
            bool seeking = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan time = TimeSpan.FromSeconds(10);
            TimeSpan target = TimeSpan.FromSeconds(60 * 3); // 3 minutes

            while (wasapi.PlaybackState == PlaybackState.Playing)
            {
                // This if condition is for demonstration purposes.
                // Set seeking to true to enable seeking.
                // Seek to target after 10 seconds.
                if (seeking && stopwatch.Elapsed > time)
                {
                    dop.CurrentTime = target;
                    seeking = false;
                }

                Console.SetCursorPosition(l, t);
                Console.WriteLine($"Stopwatch: {stopwatch.Elapsed:m\\:ss}");
                Console.WriteLine($"Buffer Size: {dop.BufferSize.TotalMilliseconds} ms");
                Console.WriteLine($"DSD Time: {dsd.CurrentTime:m\\:ss} / {dsd.TotalTime:m\\:ss}");
                Console.WriteLine($"DoP Time: {dop.CurrentTime:m\\:ss} / {dop.TotalTime:m\\:ss}");
                Console.WriteLine();
                Console.WriteLine($"DSD Position: {dsd.Position} / {dsd.Length}");
                Console.WriteLine($"DoP Position: {dop.Position} / {dop.Length}");
                Thread.Sleep(200);
            }
        }
    }
}
