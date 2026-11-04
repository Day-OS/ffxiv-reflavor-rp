# SS14 speech notification

`speech-notification.ogg` is `Resources/Audio/Voice/Talk/speak_1.ogg` from
[Space Station 14](https://github.com/space-wizards/space-station-14/blob/master/Resources/Audio/Voice/Talk/speak_1.ogg),
used there as the Baritone speech sound. Retrieved on 2026-11-04.

Original Ogg SHA-256: `6fcb92338169219ecefaae9d8864727efd2845d0bc511fa44a39d7aef99defb4`.

**Creators:** Goonstation, modified by Whisper.

**License:** [Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported](https://creativecommons.org/licenses/by-nc-sa/3.0/).
The full license is included in [CC-BY-NC-SA-3.0.txt](CC-BY-NC-SA-3.0.txt).
This sound may only be reused on the terms of that license, including its non-commercial restriction.
The plugin's source code remains separately licensed under MPL 2.0.

**Upstream attribution:**
[SS14 audio metadata](https://github.com/space-wizards/space-station-14/blob/master/Resources/Audio/Voice/Talk/attributions.yml).

**Original source:**
[Goonstation talk sounds](https://github.com/goonstation/goonstation/tree/eb3e7df6292d23f6af2f18b4372d3a8ba4b0fda7/sound/misc/talk).

`speech-notification.pcm` is the same sound decoded to mono, 16-bit little-endian PCM at 44100 Hz,
with its amplitude multiplied by 2 (+6.02 dB). Timing, pitch and equalization are unchanged.
It carries the same asset license.
It is embedded in the plugin so playback needs no runtime Ogg decoder.

Reproduce the conversion with FFmpeg:

```powershell
ffmpeg -i speech-notification.ogg -af "volume=2" -ac 1 -ar 44100 -f s16le -acodec pcm_s16le speech-notification.pcm
```
