#pragma warning disable CS0219
#pragma warning disable IDE0059

using NAudio.CoreAudioApi;
using NAudio.Dsd;
using NAudio.Wave;
using System.Diagnostics;

const string file = "DSD file path here";

using DsdReader reader = new(file);

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

Console.WriteLine($"DSD Sample Rate: {dsdSampleRate} Hz");
Console.WriteLine($"DoP Sample Rate: {dopSampleRate} Hz");
Console.WriteLine($"Conversion Ratio: {ratio}");

if (ratio != 1)
{
    Console.WriteLine($"Conversion: " +
        $"{DsdFormatExtensions.FromSamplingFrequency(dsdSampleRate).ToFriendlyString()}" +
        $" to " +
        $"{DsdFormatExtensions.FromSamplingFrequency(dsdSampleRate / ratio).ToFriendlyString()}");
}

using DopProvider dop = new(reader, ratio: ratio);
using var wasapi = new WasapiOut(AudioClientShareMode.Exclusive, 200);
wasapi.Init(dop);
wasapi.Play();
wasapi.Volume = 0.02f;


int t = Console.CursorTop;
int l = Console.CursorLeft;
bool seeked = false;
Stopwatch stopwatch = Stopwatch.StartNew();
TimeSpan time = TimeSpan.FromSeconds(10);

Console.CursorVisible = false;

while (wasapi.PlaybackState == PlaybackState.Playing)
{
    // Commented out seeking for demonstration purposes.
    // Seek to 4:30 after 10 seconds.
    //if (!seeked && stopwatch.Elapsed > time)
    //{
    //    dop.CurrentTime = TimeSpan.FromSeconds(60 * 4 - 30);
    //    seeked = true;
    //}

    Console.SetCursorPosition(l, t);
    Console.WriteLine($"Stopwatch: {stopwatch.Elapsed:m\\:ss}");
    Console.WriteLine($"DSP Time: {reader.CurrentTime:m\\:ss} / {reader.TotalTime:m\\:ss}");
    Console.WriteLine($"DoP Time: {dop.CurrentTime:m\\:ss} / {dop.TotalTime:m\\:ss}");
    Console.WriteLine($"DSD Position: {reader.Position} / {reader.Length}");
    Console.WriteLine($"DoP Position: {dop.Position} / {dop.Length}");
    Thread.Sleep(200);
}
