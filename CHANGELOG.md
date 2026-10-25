# Changelog

## 1.1.0 — 2026-10-25

- Removed the Harmony prerequisite, assembly reference, and all runtime patches.
- Uses official GameComponent/Window GUI callbacks and native layout bounds.
- TPS now follows the native tick-processing-time calculation, with immediate speed/pause/resume updates even at a five-second refresh interval.
- Retained display settings, native debug counter deduplication, and numeric pause display.

## 1.0.1 — 2026-10-25

- TPS always uses numbers; pause shows `0 (0)` and speed changes retain the last measured value until refreshed.
- Removed overlay hover explanations.
- Simplified Chinese/English README pages with centered language links.
- Release downloads contain the complete Mod ZIP only.

## 1.0.0 — 2026-10-25

- Initial independent RimWorld 1.7 / .NET 10 implementation.
- FPS and actual TPS integrated into native map/world time controls.
- Detailed settings, bilingual UI, presets, target/percentage/timing details, layout and color options.
- Cached formatting/measurement, no simulation-loop patches, explicit pause/loading/speed reset behavior.
- Reproducible build, core regression/allocation checks, complete installable release ZIP and SHA-256.
