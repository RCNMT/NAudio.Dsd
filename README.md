# NAudio.Dsd
[![GitHub](https://img.shields.io/github/license/rcnmt/NAudio.Dsd)](https://github.com/RCNMT/NAudio.Dsd/blob/main/LICENSE) 
[![Nuget](https://img.shields.io/nuget/v/NAudio.Dsd)](https://www.nuget.org/packages/NAudio.Dsd/)

This is a DSD decoder library support to [NAudio](https://github.com/naudio/NAudio).

It provides a `DsdReader` class for reading DSD files, a `DopProvider` class that encapsulate DSD into DoP (DSD over PCM), 
and a `PcmProvider` class that convert DSD to PCM (Pulse Code Modulation).

## Usage

Coding examples are available in the 
[NAudio.Dsd.Sample](https://github.com/RCNMT/NAudio.Dsd/tree/main/NAudio.Dsd.Sample)
project.

### Examples

```cs
using NAudio.CoreAudioApi;
using NAudio.Dsd;
using NAudio.Dsd.Dsd2Pcm;
using NAudio.Wave;

// Native DSD (.dsf, .dff)
using var dsd = new DsdReader("path-to-dsd-file");

// DoP encapsulation
using var dop = new DopProvider("path-to-dsd-file", ratio: 1);
using var wasapi = new WasapiOut(AudioClientShareMode.Exclusive, 200);    // Using Exclusive mode
wasapi.Init(dop);
wasapi.Play();
while (wasapi.PlaybackState == PlaybackState.Playing) Thread.Sleep(200);

// PCM conversion
using var pcm = new PcmProvider("path-to-dsd-file", PcmFormat.PCM352_8);  // Output PCM 352.8 kHz
using var wasapi = new WasapiOut(AudioClientShareMode.Shared, 200);
wasapi.Init(pcm);
wasapi.Play();
while (wasapi.PlaybackState == PlaybackState.Playing) Thread.Sleep(200);
```
## License

NAudio.Dsd's code are released under the MIT License. See [LICENSE](https://github.com/RCNMT/NAudio.Dsd/blob/main/LICENSE) for further details.