# Weather audio

- **Rain ambience:** Ove Melaa, “Rain Ambient (Not Loopable) (2 versions available)”.
  https://lpc.opengameart.org/content/rain-ambient-not-loopable-2-versions-available
  CC0 1.0: https://creativecommons.org/publicdomain/zero/1.0/
  Original long recording preserved in `source/rain-recording.ogg`.
  Playback excerpt is filtered, normalized and given an eight-second circular crossfade.
- **Thunder:** Jerimee, “Thunder”. https://opengameart.org/content/thunder
  CC BY 3.0: https://creativecommons.org/licenses/by/3.0/
  Original `thunderclap_0.ogg` preserved as `source/thunderclap.ogg`.
  Based on René Nyffenegger's cSound instrument, with modifications by Jerimee.
  Petalfell playback adds filtering, fades, normalization and runtime pitch/level variation.

Rebuild playback Ogg files with `python3 tools/build-weather-audio.py`.
Attribution must accompany redistributed builds. Original recordings are not rewritten.
