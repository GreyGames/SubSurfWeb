# Menu + Camera Handover (Lightweight)

This project uses a minimal menu/camera setup for small WebGL builds.

## Current Implementation

### Scripts
- `Assets/Scripts/LightweightMainMenu.cs`
- `Assets/Scripts/CameraSideShift.cs`
- `Assets/Scripts/EndlessTrackLooper.cs`
- `Assets/Scripts/RunnerLaneController.cs`

### Menu Behavior
- Uses `OnGUI` only (no Animator controllers, no tween packages, no TMP requirement).
- Main panel has `Play` and `Options`.
- `Play`:
  - Starts gameplay immediately.
  - Slides menu upward and hides it.
- `Options`:
  - Slides main panel down.
  - Shows options panel from below.

### Gameplay Gating
- Menu open:
  - `EndlessTrackLooper.PauseWorld(true)`
  - `RunnerLaneController.LockInput(true)`
- `Play` pressed:
  - `PauseWorld(false)`
  - `LockInput(false)`

## Intro Camera Flow

### Goal
- Before game starts, camera shows the runner from an angled intro view.
- After `Play`, camera rotates back to normal gameplay view.

### How it works
- `CameraSideShift` has an intro mode:
  - `introYawDegrees = 225`
  - `SetIntroActive(true/false)`
- `LightweightMainMenu` controls this:
  - On startup with paused menu: intro ON.
  - On `Play`: intro OFF.

## Camera Shake Fix (Important)

The Main Camera is parented under `Player`.  
To avoid jitter during lane movement, inactive follow now uses local-space smoothing when camera is a direct child of target.

If camera shake returns, check:
- There is only one `CameraSideShift` on Main Camera.
- Camera is still parented as expected (or update follow logic accordingly).
- `transitionSpeed` is not extremely low.

## Inspector Tuning

### `CameraSideShift`
- `transitionSpeed`: smoothing speed of camera transitions.
- `introYawDegrees`: intro angle around player (default `225`).
- `introHeightOffset`: vertical offset during intro angle.
- `sideYawDegrees`, `orbitDegreesPerSecond`: used by slow-mo cinematic zones.

### `LightweightMainMenu`
- `slidePixelsPerSecond`: menu slide speed.
- `startPaused`: should stay enabled for normal flow.
- `disableScriptAfterPlay`: can be enabled for tiny runtime overhead savings.

## Future UI Polish (Without Bloat)

### Safe now (still `OnGUI`)
- Add dark backdrop behind panel.
- Custom `GUIStyle` for title/buttons (hover/pressed colors).
- Add eased motion curve for panel slide.

### Later (still lightweight)
- Move menu to one simple `uGUI` Canvas + two Buttons.
- Keep no Animator/tween package.
- Drive panel motion with one script and `RectTransform`.

## Developer Checklist

- If changing menu start flow, keep world pause/input lock and intro camera toggles in sync.
- If changing camera hierarchy, re-test local follow behavior.
- If adding visual polish, avoid package dependencies unless absolutely required.
