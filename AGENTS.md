# 3D_UI_Test_home Project Context

## Project purpose

This is a Unity XR prototype for testing spatial 3D UI in an industrial confined-space environment. The project combines an industrial tank and pipeline scene with head tracking, hand tracking, and experimental sci-fi card UI.

## Environment

- Unity: 6000.4.8f1
- Render pipeline: Universal Render Pipeline (URP) 17.4.0
- XR runtime: OpenXR
- XR Interaction Toolkit: 3.4.1
- XR Hands: 1.7.3
- Input System: 1.19.0
- Primary targets: Meta Quest/Android and PC OpenXR

## Primary scene

- Build scene: `Assets/Scenes/Confined Space Scene_half.unity`
- Related scene variants:
  - `Assets/Scenes/Confined Space Scene_Blue.unity`
  - `Assets/Scenes/Confined Space Scene_white.unity`
  - `Assets/Scenes/close change 1.unity`

The build scene contains the industrial environment, tanks, ladders, railings, tracked hand objects, background gradient, and the XR camera.

## Project-owned runtime code

- `Assets/Scripts/XRHeadTrackedCamera.cs`
  - Applies the center-eye XR pose to its camera.
  - Currently referenced by the primary build scene.
- `Assets/Scripts/SciFiCardVisual.cs`
  - Procedurally builds layered rounded-card meshes, frame, glass, glow, shadow, corner markers, and collider geometry.
- `Assets/Scripts/SciFiCardDepthResponse.cs`
  - Adds camera-relative parallax to card background, content, and foreground layers.

The sci-fi card scripts exist but were not referenced by the primary build scene at the time this file was created. Verify current scene references before assuming otherwise.

## Project-owned editor tools

- `Assets/Editor/SciFiCardMaker.cs`
- `Assets/Editor/PlaneMaker.cs`
- `Assets/Editor/XRHandMaterialMaker.cs`
- `Assets/Editor/BackgroundGradientShaderGenerator.cs`
- `Tools/generate_background_shadergraph.ps1`
- `Tools/generate_background_shadergraph.py`

## Assets and samples

Large parts of `Assets` are imported industrial models or Unity package samples. Treat these as third-party/sample content unless the task explicitly targets them:

- `Assets/3D Models`
- `Assets/AdventureForge`
- `Assets/FreeIndustrialModels`
- `Assets/Samples`
- `Assets/TextMesh Pro`
- `Assets/TutorialInfo`

Prefer adding project-specific code under `Assets/Scripts` and editor-only code under `Assets/Editor`.

## Working rules

- Preserve `.meta` files and Unity asset GUIDs.
- Do not manually edit generated folders: `Library`, `Temp`, `Logs`, or `UserSettings`.
- Do not treat generated `.csproj` and `.slnx` files as authoritative project configuration.
- Check `ProjectSettings/EditorBuildSettings.asset` before changing assumptions about the startup scene.
- Keep runtime code out of `Assets/Editor`.
- When modifying a Unity YAML scene or prefab directly, make small changes and verify file IDs, GUIDs, and serialized references carefully.
- Prefer Unity-compatible C# APIs supported by the configured Unity version.
- For XR changes, account for both Android/Quest and Standalone OpenXR unless the requested target is explicit.
- Do not modify imported packages or samples when a project-owned wrapper or component is sufficient.

## UI authoring rules

Apply these rules to every UI element and UI system in the project, not only to a specific scene, modal, or component:

- When creating or modifying UI, treat scene, prefab, and Inspector-serialized values as the authoritative UI configuration.
- Do not hardcode UI presentation values in runtime code. This includes positions, sizes, anchors, pivots, scale, spacing, colors, fonts, font sizes, labels, sorting order, and other visual or layout values.
- Expose UI configuration through serialized fields when runtime access is required, and preserve the values authored in the Inspector, scene, or prefab.
- Runtime code may change UI state or dynamic content when required, but it must not silently replace authored presentation or layout values.
- Whenever a UI element is created, modified, or diagnosed, inspect all related runtime assignment code together with the scene or prefab changes. Do not complete a UI change after editing only the serialized asset while leaving conflicting runtime assignments unexamined.
- When diagnosing or changing UI, trace the complete runtime assignment path, including `Awake`, `OnEnable`, `Start`, scene-load callbacks, initialization helpers, instantiated clones, layout components, animation systems, and editor-generated setup code.
- Explicitly check whether entering Play Mode overwrites scene, prefab, RectTransform, Canvas, TMP, material, or component values. Remove or redesign unintended runtime overwrites instead of compensating for them with additional hardcoded assignments.
- Prefer scene- or prefab-authored UI objects with serialized references over runtime-created UI. If runtime creation is genuinely required, use a serialized prefab or serialized configuration as its source of truth.
- Editor builders and migration tools may provide initial defaults when creating an object, but must not repeatedly overwrite existing authored UI values. After creation, the serialized scene or prefab values are authoritative.

## Validation

For code changes, check for C# compilation errors and inspect Unity logs when available. For UI changes, compare relevant serialized values before and after entering Play Mode and verify that no unintended runtime assignment changes them. For scene, prefab, shader, XR, or rendering changes, explain any verification that still requires opening Unity or testing on a headset.

## Session startup

At the beginning of a task, use this file as orientation, then inspect the files directly relevant to the request. Do not rescan `Library`, `Temp`, or the full imported asset collection unless necessary.
