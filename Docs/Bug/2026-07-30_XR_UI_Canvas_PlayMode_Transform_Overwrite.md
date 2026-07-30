# XR UI Canvas Play Mode Transform Overwrite

Date: 2026-07-30

## Symptom

The scene-authored position of `XR UI Canvas` appeared to be replaced after entering Play Mode.

## Cause

- The root `RectTransform` serialized different X/Y values in `m_LocalPosition` and `m_AnchoredPosition`.
- `XRWorldCanvasPlacement` performed an unconditional camera-relative placement from `Start` whenever attached.
- `ScenarioDetailModal` also changed the active state of the entire parent Canvas while showing or hiding only its modal content.

## Fix

- Synchronized the root Canvas local and anchored positions in `3_PPE_Room_Loco.unity`.
- Changed `XRWorldCanvasPlacement` to run only through the explicit `PlaceFromCamera` method.
- Limited `ScenarioDetailModal` visibility changes to its serialized modal root.
- Added `XRUiCanvasPlayModeValidator` to compare the Canvas transform immediately before and after entering Play Mode.

## Validation

Run `Tools > UI > Validate XR UI Canvas Play Mode Transform`, then enter Play Mode. The Console should report that the transform remained unchanged.
