# utincomputerTest — Unity Developer test task

## Environment
- Unity 6000.3.23f1, URP 17.3, Input System 1.20, `com.unity.pipeline` installed.
- Live Editor control via Unity CLI (`unity status`, `unity command ... --caller plugin --skill unity-cli`, `--project-path C:/Repositories/utincomputerTest`). Prefer it over hand-editing scene/prefab YAML.
- CLI tips: `export UNITY_PROJECT_PATH=C:/Repositories/utincomputerTest`; flags `--caller plugin --skill unity-cli --result-only`. After writing .cs run `script eval 'UnityEditor.AssetDatabase.Refresh()'` then poll `script recompile_status`. Multi-line C# via `script eval --file <tmp.cs>`. Play: `editor set_auto_tick` then `editor.playmode start` / `editor.playmode stop` (editor unfocused otherwise freezes). Also once per session: `editor.playmode set_run_in_background` (else frames stuck at 1).
- Simulated mouse input doesn't reach unfocused Game view → test gameplay by invoking handlers via reflection in `script eval`.
- After `AssetDatabase.CreateAsset` + `OpenScene`, re-load assets with `LoadAssetAtPath` before assigning refs (otherwise ref saved as null). Always verify refs after wiring. Assign asset refs AFTER OpenScene (load asset again after opening).
- Balance bot: invoke private `PlayerBallControl.HandlePressed`, loop `_shootingLogic.Tick(0.005f)` until `_currentShot.Mass` reaches target, then `HandleReleased` (reflection); pick mass by first blocker's zone.
- Folders: `Assets/Scripts` (namespaces = folders), `Assets/Prefabs`, `Assets/Configs`, `Assets/Materials`, `Assets/Scenes` (Bootstrap = build 0, Game = build 1). Build target: Android, portrait.

## Scene assembly rule
Build scenes manually-style: real prefabs, MonoBehaviours placed in the scene, `[SerializeField]` refs wired in the inspector (done via Unity CLI in the live Editor). No LevelBuilder / editor generator scripts.

## GDD (summary)
Source: `C:\Users\sasha\Downloads\Тестове завдання на позицію Unity Developer.pdf`
Reference video: youtube.com/shorts/jDmxAjDl7Co ("Block Breaker — Gameplay Trailer": portrait, angled top-down camera, big ball bottom, castle with door top, pink path between cube obstacles).

- Player ball bottom-left, target (door) top-right, obstacles in between.
- **Hold tap** → shot ball separates from player and grows; player shrinks proportionally (longer hold = bigger shot).
- **Release** → shot flies toward target; on first obstacle hit it "infects" obstacles within radius ∝ shot size → they explode. Dense clusters are easier to infect (chain reaction); single obstacles need small shots.
- After area is cleared, player **hops forward along path center** toward target.
- **Path width = player diameter**, shrinks with the ball. At the end ball is smaller but must still pass freely.
- **Door** at the end opens when ball is within **5 m**.
- **Lose:** ball shrank below minimal critical size (held too long); or not enough size/shots to clear path.
- **Balance:** starting size must be enough with **20% reserve**.
- **Deliverables:** Android build (APK), YouTube gameplay video, Unity project source.

## Design decisions (not in GDD — document in README)
- Path blocked check: SphereCast/OverlapCapsule along path with player radius.
- Player STAYS at start while clearing (per GDD: clear path first). After each shot's explosions settle, if the whole corridor to the door is clear → ball hops all the way to the door (door opens at 5 m) → win.
- Lose "not enough": path still blocked and `currentSize - minSize < minShotSize`.
- Camera fits start→door into the viewport for any aspect (whole level on screen); shake + zoom punch on impact; follows the ball only on the final run to the door.
- Visual theme: bright "cozy garden" like the GDD art (stylized low-poly grass ground `Land1.png` from h8games, purple jelly-slime enemies with eyes + idle bob (built from primitives), pink path, orange slime ball with eyes looking forward, farm house + garden gate, hedges/trees/flower beds only on the frame). Field obstacles removed (only zones on the path). Keep the play field clean — no decor between obstacles. Models from user's polyperfect pack (private task, license OK).
- Look (per user references): camera pitch 46°/yaw -20°/FOV 40 (CameraControl fits distance), warm low sun (34°,-55°) soft shadows, `Assets/Settings/GameVolumeProfile.asset` (Neutral tonemap, color adjust, white balance, bloom, vignette), path = raised pink Lit slab from 6 m behind start to door. Use `capture game_view --source camera` if screen capture comes out blank.
- Shot always flies straight toward the target (GDD). Edge obstacles must be cleared by infection radius/chain, level is designed for that. Shot also checks start overlap.
- Obstacles: hand-placed prefab instances in zones on the path; shot economy = infection budget ∝ m^1.5 + launch cost 0.01; start mass 0.9 gives ~21% reserve for planned play.
- Infection: OverlapSphere(radius from shot size), then infected obstacles infect neighbours in small radius with delay (chain).

## Main classes
`EntryPoint`, `GameplayController`, `LevelConfig`/`BalanceConfig` (SO), `CameraControl` (+ `CameraParts/` Fit/Impact/Follow), `PlayerBallControl` (+ parts `BallSizeLogic`, `ShootingLogic` (uses `ChargeLogic`), `HopMovementLogic`, `PlayerBallFeedback`), `ShotProjectile`, `InfectionPreview`, `ObstacleControl` (+ `ObstacleFeedback`), `InfectionController` (+ `InfectionEffects`), `PathChecker`, `PathView`, `BlockersHighlight`, `DoorControl`, UI `GameplayScreen`.

## Code style (follow strictly)
Reference code: `C:\Users\sasha\Desktop\Scripts` — copy infrastructure from there (allowed by user):
`MonoSingleton`, `ObjectPool` + `BasePoolDestroyable`, `ValueConstants`, `ApplicationStart`, `ServicesManager`, `EntryPoint` pattern, `GameplayController`, `UIManager` + `ScreensManager`/`PopupsManager` + `ResultPopup`/`LevelCompletedPopup`, `SafeAreaFitter`.

- Fields `_camelCase`, `[SerializeField] private`; expose via `public X X => _x;` or `{ get; private set; }`.
- Constants `UPPER_SNAKE_CASE`; shared in `public struct ValueConstants`.
- Allman braces, braces always (even one-line `if`), early-return guards.
- Explicit types preferred; target-typed `new()` for fields.
- Handlers `HandleXxx`; events `public event Action OnXxx`, invoke `?.Invoke()`.
- No comments / regions / XML docs.
- Namespaces mirror folders (`Game.Entities...`, `Services.*`, `UI.*`).
- Manual DI via `Init(...)`; no DI framework. Plain C# controllers `: IDisposable` with ctor injection, `Init()` subscribes, `Dispose()` unsubscribes.
- Entity = `XxxControl` MonoBehaviour composed of `[Serializable]` plain parts with `Init()` / `Tick(deltaTime)`; `_isGameplayActive` gate.
- Configs: `[CreateAssetMenu(fileName = "X", menuName = "ScriptableObjects/X")]`.
- Libraries: DOTween, Newtonsoft.Json, TextMeshPro. Unity 6 API (`linearVelocity`).
