# PPE Room Teleportation Provider Regression

Date: 2026-07-30

## Symptom

In `Assets/Scenes/3_PPE_Room.unity`, both PPE location markers could be reached by the controller ray, but pulling Trigger no longer moved the XR Origin. The same markers had worked before the teleport implementation was migrated to XRI's provider pipeline.

## Cause

Two independent parts of the previous custom path were removed at the same time:

- `XRLocationTeleportTarget` stopped changing the XR Origin transform directly and began queueing a `TeleportRequest` through `TeleportationProvider`.
- The active hand-authored Near-Far Interactors, which allowed Activate events on hovered targets, were replaced by Starter Assets prefab instances whose inherited `Allow Hovered Activate` value was disabled.

`3_PPE_Room` had no `TeleportationProvider`, `LocomotionMediator`, or `XRBodyTransformer`, so a request could not be executed even if the Trigger event reached the marker.

## Resolution

The scene now contains a teleport-only XRI locomotion stack under `XR Origin (VR)`:

- `PPE Teleport-Only Locomotion`
  - `XRBodyTransformer`, explicitly referencing the scene XR Origin
  - `LocomotionMediator`
  - `Teleportation Provider`
    - `TeleportationProvider`, explicitly referencing the mediator

No continuous move, turn, jump, climb, or gravity provider is present. Both active controller Near-Far Interactor prefab instances author `Allow Hovered Activate = true` as scene overrides, and both PPE location markers explicitly reference the scene TeleportationProvider.

Each marker now uses a separate scene-authored arrival anchor. The visible marker remains rotated flat on the floor, while its arrival anchor faces the PPE stands along world `-Z`. The existing positive `Arrival Forward Offset` therefore places the user 0.7 m beyond the marker on the PPE-stand side and aligns the view toward the stands. The left and right Near-Far Interactors retain their distinct left/right Trigger Activate action references.

## Validation

Run `Tools > PPE > Validate Teleport-Only Locomotion` or invoke `PPERoomTeleportationSetup.Validate` in Unity batch mode. The harness checks the complete provider chain, both marker references, both active controller interactor overrides, and the absence of other active locomotion providers.

Headset validation must still confirm:

- left and right controller rays hover both PPE markers;
- Trigger teleports while Grip remains available for Select/Grab;
- current head height is preserved;
- destination forward alignment and arrival offset match the authored marker values;
- both eyes render the ray, reticle, and markers consistently on Quest/OpenXR.
