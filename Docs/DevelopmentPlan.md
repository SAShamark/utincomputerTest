# Development Plan — Ball Shooter (test task)

Stage 1 = **primitives only** (Unity sphere/capsule/cube, flat URP Lit materials, no VFX/DOTween polish).

## 1. Balance model

### Size = "mass"
- Player state is **mass `M`**, radius `r = cbrt(M) * RADIUS_SCALE` (volume conservation: shot grows as player shrinks, visually honest).
- Start `M0 = 0.8` → radius ≈ 0.93 m. Critical `M_MIN = 0.03` → radius ≈ 0.31 m.
- Hold: mass flows player → shot with **accelerating rate** `rate = BASE_RATE + ACCEL * holdTime` (BASE 0.08/s, ACCEL 0.25/s²). Short tap ≈ 0.03 (small shot), ~1 s ≈ 0.2 (big shot). Accelerating rate makes greed risky — "чим довше, тим більше".
- If `M` drops below `M_MIN` while holding → **lose** (shot is cancelled/absorbed).

### Shot power
- Shot radius `rs = cbrt(m) * RADIUS_SCALE`.
- **Infection radius** `R_inf = rs * INFECT_MULTIPLIER` (2.5).
- Shot flies straight toward the target (GDD); first obstacle contact = impact point. Corridor-edge obstacles are cleared by the infection radius + chain.

### Anti-spam (shot economy)
- **Infection budget**: a shot can infect at most `max(1, round(380 · m^1.5))` obstacles (nearest first, chain included). Superlinear → big shots are more mass-efficient on clusters; a 0.02 tap kills exactly one obstacle.
- **Launch cost**: every release burns an extra 0.01 mass from the ball. Many small shots pay it many times.
- Bot check (start mass 0.9): planned shots win with 0.183 left (reserve ≈ 1.21); spam 0.02 ×28, spam 0.05 ×14 and greedy (+50%) all lose.

### Chain infection ("ближче — простіше")
1. `OverlapSphere(impact, R_inf)` → infected set, each gets **energy** `E = R_inf`.
2. Every infected obstacle, after `CHAIN_DELAY` (0.08 s), tries neighbours within `LINK_DISTANCE` (0.9 m); neighbour gets `E_parent - distance * HOP_COST` (HOP_COST 0.5, tuned). Spreads while `E > 0`.
3. Gap > `LINK_DISTANCE` breaks the chain → sparse/single obstacles need their own (small) shots; dense clusters let a medium shot clear a lot.
4. Infected obstacle flashes, then explodes (destroy/return to pool).

### Path and movement
- Path = straight line player-start → door. **Width = player diameter** (PathView stretched quad, rescaled every frame with radius).
- `PathChecker`: `SphereCast(radius = playerRadius)` along path → distance to first blocking obstacle.
- Player stays at the start while clearing. After explosions settle, if the whole corridor is clear the player **hops** (1.2 m arcs) all the way to the door.
- Since the path shrinks with the ball, spending mass also *narrows* what must be cleared — a nice self-balancing loop.
- Door opens when player within **5 m** → player hops through → **win**.

### Lose conditions
- `M < M_MIN` during charge.
- Path still blocked and `M - M_MIN < MIN_SHOT_MASS` (0.02) → cannot shoot anymore.

### 20% reserve
- `LevelConfig` keeps per-zone **reference shot masses** (designer intent). `OnValidate` logs `reserve = (M0 - M_MIN) / Σ referenceShots` and warns if `< 1.2`.

## 2. Level layout (v2: ≈ 25 m path, whole level on screen)

Obstacles = capsule prefab (r 0.3, h 1.2), placed **as prefab instances in the scene**, grouped under `Zone_X` objects. Field outside the corridor (spacing 1.15 > link distance, so no chains) is decorative-but-real: an oversized shot wastes mass on it.

Player never aims — every shot hits the nearest obstacle on the line, so the only decision is **shot size**. Each zone is built around one size decision:

| Zone | z (m) | Layout | Decision / what it teaches | Reference |
|---|---|---|---|---|
| Z1 Warm-up | 3.5–4.7 | dense 4×3 block, spacing 0.6 | dense = cheap: one medium shot + chain clears all | 0.10 |
| Z2 Snipers | 6.6–9.4 | 3 singles, 1.4 m apart | sparse = expensive: three short taps, a big shot is waste | 3 × 0.02 |
| Z3 Sentry | 11.0 | 1 single in front of the nest | tap it, don't mix it with the nest | 0.02 |
| Z4 Nest | 12.0–13.1 | dense 3×3, 1 m behind the sentry (gap > link) | medium shot | 0.10 |
| Z5 Wall | 15.0–16.1 | 9-wide band, 3 staggered rows | the big brave shot; accelerating charge punishes over-holding | 0.18 |
| Z6 Broken bridge | 18.2–20.6 | two 3×2 clusters, 1.2 m gap | two small shots beat one big (gap breaks the chain) | 2 × 0.05 |
| Z7 Door guard | 22.0–23.0 | diamond before the door | last precise shot with a small ball | 0.06 |

Door at z 25.5 (opens at 5 m).

### Verification (scripted bot, exact masses)
- **Optimal play** (reference shots): 10 shots, wins with mass 0.174 left (radius 0.56 m). Reserve = (0.8 − 0.03) / 0.62 ≈ **1.24** (≥ 1.2 required).
- **Greedy play** (≈ +50% per shot): runs dry on the Wall → lose. So size choice matters.
- Tuning history: infection ×3 → ×2.5 and start mass 1.0 → 0.8 (v2 was too forgiving: optimal play ended with 0.37 left ≈ 1.8 reserve).

All numbers live in `LevelConfig` / `BalanceConfig` (ScriptableObjects) and get tuned in Play Mode.

## 3. Architecture (user code style, see CLAUDE.md)

```
Assets/
  Scripts/
    ApplicationStart.cs, ServicesManager.cs          (from Desktop\Scripts, trimmed)
    Services/  MonoSingleton, ValueConstants, ObjectPool/*  (copied)
    Game/
      EntryPoint.cs, GameplayController.cs, LevelConfig.cs, BalanceConfig.cs
      Entities/
        Player/   PlayerBallControl.cs  (+ [Serializable] BallSizeLogic, ChargeLogic, HopMovementLogic)
        Shot/     ShotProjectile.cs
        Obstacles/ ObstacleControl.cs, InfectionLogic.cs (chain), IInfectable.cs
        Path/     PathChecker.cs, PathView.cs
        Door/     DoorControl.cs
      Input/      TapInput.cs  (Input System: touch + mouse)
      CameraControl.cs
    UI/  UIManager, PopupsManager, BasePopup, ResultPopup, LevelCompletedPopup, GameplayScreen (copied/trimmed)
  Prefabs/  PlayerBall, ShotProjectile, Obstacle, Door, UI/*
  Configs/  LevelConfig.asset, BalanceConfig.asset, WindowsConfig.asset
  Materials/ (flat colors)
  Scenes/   Bootstrap.unity, Game.unity
```

Flow: `EntryPoint.Start` → Init PlayerBall(config, input, pathChecker) → Init Obstacles → Init Door → `GameplayController` subscribes `OnWin / OnLose` → popups with Restart.

## 4. Iterations

1. **Setup** — folders, copy Services/ObjectPool/MonoSingleton/ValueConstants, Bootstrap + Game scenes, Android/portrait player settings, camera.
2. **Charge & shoot** — PlayerBall mass logic, TapInput, ShotProjectile grow/fly; mass loss lose condition. Test in Play Mode (logs).
3. **Obstacles & infection** — Obstacle prefab, overlap + chain infection, explosion (destroy/pool).
4. **Path & hopping** — PathView width = diameter, PathChecker, hop movement.
5. **Level** — place zones Z1–Z5 as prefab instances, Door with 5 m trigger, win/lose detection, LevelConfig reserve check.
6. **UI** — copy UIManager/Popups, Result/LevelCompleted with Restart, simple size indicator.
7. **Balance pass** — tune numbers in Play Mode, verify 20% reserve and both lose cases.
8. **(Later) Visual** — DOTween hops/squash, explosion particles, colors like the GDD art, camera follow.
9. **Delivery** — Android APK build, README (rules, decisions, balance), gameplay video (user).
