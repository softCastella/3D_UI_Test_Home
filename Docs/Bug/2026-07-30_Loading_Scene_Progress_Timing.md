# Loading Scene Progress Timing Reversion

Date: 2026-07-30

## Scope

This report covers the scene transition and loading progress behavior between the intro scene and the PPE room.

The intended application flow is:

`0_App -> 1_Title -> 2_Intro -> 6_LoadingScene -> 3_PPE_Room`

## Symptom

The adaptive loading experiment completed too quickly when `3_PPE_Room` reached Unity's scene-ready state almost immediately. Although the first-load estimate was authored as four seconds, the adaptive completion path could begin as soon as `AsyncOperation.progress` reached `0.9` and finish after the shorter minimum-display and completion-animation durations.

This made the percentage and segmented loading bar advance too quickly to be useful as a transition screen.

## Cause

Unity does not provide an exact total loading time before a scene load begins. The adaptive implementation estimated the total duration from elapsed time and `LoadSceneAsync.progress`, then switched to a short completion animation when the operation became ready for activation.

For a locally cached or quickly loaded scene, the ready signal arrived before the estimate could stabilize. Learning a previous duration also did not guarantee a consistent presentation because device state, cache state, and scene initialization time can vary between runs.

## Resolution

The adaptive estimate and PlayerPrefs-based duration learning were removed. The loading bar now uses a deterministic, Inspector-authored fill duration:

- `LoadingSceneController.LoadTarget("3_PPE_Room")` opens `6_LoadingScene` from the intro transition.
- `6_LoadingScene` starts `3_PPE_Room` with `SceneManager.LoadSceneAsync` and keeps `allowSceneActivation` disabled.
- The displayed percentage follows Unity's normalized scene progress but cannot advance faster than `1 / Progress Fill Duration` per second.
- When the PPE room is ready early, the bar still takes the authored fill duration to reach `100%`.
- Scene activation is allowed only after the bar reaches `100%` and the completion hold has elapsed.

## Inspector Configuration

The authoritative timing values are serialized on:

`6_LoadingScene > Canvas > LoadingSceneController > Progress Timing`

- `Minimum Display Duration`: `1` second
- `Progress Fill Duration`: `6` seconds
- `Completion Hold Duration`: `0.15` seconds

`6` is the current scene-authored value and produces a six-second minimum fill when the target scene is already ready. Increase `Progress Fill Duration` to make the transition slower. The C# field initializer remains a four-second fallback for a newly added or otherwise unconfigured component; it does not replace the value serialized in `6_LoadingScene`.

The scene also serializes `Prewarm Frames = 2` under `Scene Loading`. During these two frames the root loading `CanvasGroup` remains hidden while the XR Canvas resolves its render size. The percentage and bar timing begin after the complete loading UI is revealed.

## Validation

Verify the following after entering Play Mode from `0_App` or `2_Intro`:

- the intro scene fades out and opens `6_LoadingScene` instead of loading `3_PPE_Room` directly;
- the percentage begins at `0%` and uses no leading zero from `0%` through `99%`;
- the segmented gradient bar does not complete faster than the authored `Progress Fill Duration` when the PPE room is ready early;
- the eighteenth and final segment appears in the same frame that the percentage changes to `100%`;
- the percentage reaches `100%` before `3_PPE_Room` activates;
- `Progress Fill Duration` changes made in the Inspector remain authoritative after entering Play Mode.

The current progress source measures Unity scene and included-resource loading. Any future network, database, or application-specific initialization that runs after scene activation must expose its own progress source before it can be represented by this loading bar.

## Status

Code compilation and serialized-reference checks pass. Final timing, final-segment synchronization, and first-frame appearance still require a Quest/OpenXR headset run after Unity finishes script compilation.
