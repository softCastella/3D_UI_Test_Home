# PPE Room Card Ray Selection and Short Ray

Date: 2026-07-30

## Symptom

- `XR UI Canvas (1)` disappeared while merely moving a controller before an intentional click.
- Card selection accepted input paths other than a controller ray plus trigger.
- After selection, the Canvas did not consistently hide and the controller ray appeared cut short before the PPE location markers.

## Cause

- `ScenarioCardSelectProxy` created an `XRSimpleInteractable` at runtime and listened to its generic Select event in addition to mouse and UI pointer input.
- Only one of the three serialized card proxies referenced `XR UI Canvas (1)` as its post-selection hide target.
- The controller Near-Far prefabs cast and rendered only 10 m, while the PPE markers are roughly 12-15 m from the initial rig position.
- `CurveVisualController` used a 0.25 m resting line and did not extend the line when no valid hit was available, producing the cut-ray appearance.

## Fix

- Accept card selection only from an XR `TrackedDeviceEventData` produced by a `NearFarInteractor` ray click.
- Remove the mouse, generic pointer, and runtime-created `XRSimpleInteractable` selection paths.
- Disable the mouse `GraphicRaycaster` and mouse physics fallback on `XR UI Canvas (1)`.
- Point all three card proxies at `XR UI Canvas (1)` for post-selection hiding.
- Author both controller rays to cast/render for 40 m and extend to an empty hit.

## Validation

Run `Tools > PPE > Validate Card Ray Selection` or invoke
`PPERoomCardRaySelectionHarness.Validate` in Unity batch mode. Headset validation must still confirm both controllers, both eyes, trigger-only card selection, Canvas persistence before selection, Canvas hiding after selection, and ray/reticle contact with both PPE location markers.
