# Changelog

All notable changes to this package will be documented in this file.

## [0.1.2] - 2026-05-13
- [CHANGED] Save naming updated across all Adjust tools (`Image Adjust`, `Image Adjust (Batch)`, `Image Adjust Blend`, `Audio Adjust`, `Audio Adjust (Batch)`).
- [CHANGED] `Save As Copy` now uses `_CddMMMyyyyHHmmss` (example: `RGBColorReplaceTest_C25May2026085705`).
- [CHANGED] `Backup Original` now uses `_OddMMMyyyyHHmmss` (example: `RGBColorReplaceTest_O25May2026085705`).
- [CHANGED] Image preview rendering in Image Adjust tools is now pixel-accurate (nearest-neighbor preview scaling with point/clamp preview textures).
- [CHANGED] Color replace matching in Image Adjust tools now uses corrected saturation/brightness difference handling.
- [CHANGED] `Effect Blend` in Image Adjust tools now supports stronger replacement (`0..2`) while preserving existing behavior at `1`.
- [CHANGED] UI wording updated: `Tone Equalization` is now `Flatten Exposure`.

## [0.1.1] - 2026-03-12
- [ADDED] Demo audio and image assets for quick evaluation
- [ADDED] Per-tool quick start documentation
- [CHANGED] Export menu ordering

## [0.1.0] - 2026-01-28
- [ADDED] Initial package release
