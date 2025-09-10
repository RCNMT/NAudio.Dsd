using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.Dsd.Sample
{
    internal class Dsd2Dop
    {
        public static void Run(string path)
        {
            using DsdReader reader = new(path);

            // Required DoP sample rates in Hz.
            // These are DoP sample rates that are dependent on the audio device or DACs capabilities.
            int[] samples =
            [
                //1411200, // DSD512 in DoP
                //705600,  // DSD256 in DoP
                352800,  // DSD128 in DoP
                176400,  // DSD64  in DoP
            ];

            int ratio = 1;
            int supported = 0;

            MMDeviceEnumerator enumerator = new();
            MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            foreach (var sample in samples)
            {
                if (device.AudioClient.IsFormatSupported(AudioClientShareMode.Exclusive, new WaveFormat(sample, 24, 2)))
                {
                    Console.WriteLine($"Supported: {sample} Hz");
                    supported = sample;
                    break;
                }
            }

            int dsdSampleRate = (int)reader.Header.SamplingFrequency;

            // DoP sample rate is 1/16 of the DSD sampling frequency.
            int dopSampleRate = (int)reader.Header.SamplingFrequency / 16;

            // Adjust ratio if the file's sampling frequency is higher than the supported frequency.
            // For example, if the file is DSD256 (705600 Hz in DoP) and the supported is 176400 Hz, ratio will be 705600 / 176400 = 4.
            if (dopSampleRate > supported)
            {
                ratio = dopSampleRate / supported;
            }

            dopSampleRate /= ratio;

            string inputName = DsdFormatExtensions.FromSamplingFrequency(dsdSampleRate).ToFriendlyString();
            string outputName = DsdFormatExtensions.FromSamplingFrequency(dsdSampleRate / ratio).ToFriendlyString();

            Console.WriteLine($"DSD Sample Rate: {dsdSampleRate} Hz ({inputName})");
            Console.WriteLine($"DoP Sample Rate: {dopSampleRate} Hz ({outputName})");
            Console.WriteLine();
            Console.WriteLine($"Conversion Ratio: {ratio}");
            Console.WriteLine($"Conversion Info: {inputName} to {outputName} {(ratio == 1 ? "(Skip)" : "")}");
            Console.WriteLine();

            using DopProvider dop = new(reader, ratio: ratio);
            using var wasapi = new WasapiOut(AudioClientShareMode.Exclusive, 200);
            wasapi.Init(dop);
            wasapi.Play();
            wasapi.Volume = 0.01f;

            int t = Console.CursorTop;
            int l = Console.CursorLeft;
            bool seeking = false;
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan time = TimeSpan.FromSeconds(10);

            while (wasapi.PlaybackState == PlaybackState.Playing)
            {
                // Commented out seeking for demonstration purposes.
                // Set seeking to true to enable seeking.
                // Seek to 4:30 after 10 seconds.
                if (seeking && stopwatch.Elapsed > time)
                {
                    dop.CurrentTime = TimeSpan.FromSeconds(60 * 4 - 30);
                    seeking = false;
                }

                Console.SetCursorPosition(l, t);
                Console.WriteLine($"Stopwatch: {stopwatch.Elapsed:m\\:ss}");
                Console.WriteLine($"DSD Time: {reader.CurrentTime:m\\:ss} / {reader.TotalTime:m\\:ss}");
                Console.WriteLine($"DoP Time: {dop.CurrentTime:m\\:ss} / {dop.TotalTime:m\\:ss}");
                Console.WriteLine();
                Console.WriteLine($"DSD Position: {reader.Position} / {reader.Length}");
                Console.WriteLine($"DoP Position: {dop.Position} / {dop.Length}");
                Thread.Sleep(200);
            }
        }
    }
}
