# NAudio.Dsd
[![GitHub](https://img.shields.io/github/license/rcnmt/NAudio.Dsd)](https://github.com/RCNMT/NAudio.Dsd/blob/main/LICENSE) 
[![Nuget](https://img.shields.io/nuget/v/NAudio.Dsd)](https://www.nuget.org/packages/NAudio.Dsd/)

This is a DSD decoder library support to [NAudio](https://github.com/naudio/NAudio).

- `DsdReader` class for reading DSD files
- `DopProvider` class that encapsulate DSD into PCM container
- `PcmProvider` class that convert DSD to PCM (Pulse Code Modulation).

## Usage

Coding examples are available in the [NAudio.Dsd.Sample](https://github.com/RCNMT/NAudio.Dsd/tree/main/NAudio.Dsd.Sample) project.

### Examples

```cs
// DSD from files (.dsf, .dff)
using var dsd = new DsdReader("path-to-dsd-file");

// DoP encapsulation
using var dop = new DopProvider("path-to-dsd-file");

// PCM conversion
// Output PCM 44.1 kHz, 24-bit (default), Dither TriangularPDF (default), Filter Kaiser (default)
using var pcm = new PcmProvider("path-to-dsd-file", new WaveFormat(44100, 24, 2));
// Output PCM 176.4 kHz, 32-bit, Dither FWeighted, Filter BlackmanHarris
using var pcm = new PcmProvider("path-to-dsd-file", new WaveFormat(176400, 32, 2), DitherType.FWeighted, FilterType.BlackmanHarris);
// Output PCM 352.8 kHz, 32-bit, Dither FWeighted, Filter BlackmanHarris
using var pcm = new PcmProvider("path-to-dsd-file", new WaveFormat(352800, 32, 2), DitherType.FWeighted, FilterType.BlackmanHarris, 4);
// Output PCM 352.8 kHz, 16-bit, No dither, Custom filter
double[]  FIR = [0.001971638744376920, 0.004940751393740672, -0.010902272070274959, -0.022955081891054344, ...];
using var pcm = new PcmProvider("path-to-dsd-file", new WaveFormat(352800, 16, 2), FIR, DitherType.None);

using var wasapi = new WasapiOut();
wasapi.Init(pcm); // pcm or dop from provider
wasapi.Play();
while (wasapi.PlaybackState == PlaybackState.Playing) Thread.Sleep(200);
```
## License

NAudio.Dsd's code are released under the MIT License. See [LICENSE](https://github.com/RCNMT/NAudio.Dsd/blob/main/LICENSE) for further details.