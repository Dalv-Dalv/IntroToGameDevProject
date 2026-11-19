# GAME_API.md — Lumens in a Dangerous Space

Team + AI reference for how the game is wired. Every field, default, layer and signature below was checked against the
actual files on 2026-11-19. **"Default"** = the C# default in the script; prefab/scene values are given where they differ.

**Contents:** [1. Game & scene flow](#1-game--scene-flow) · [2. Layers & physics](#2-layers--physics2d) ·
[3. Component guide](#3-component--inspector-guide) · [4. Recipes](#4-quick-recipes) ·
[5. Gotchas & pending hooks](#5-known-integration-gotchas--pending-hooks)

**Project facts that bite:** Unity 6 (use `linearVelocity`, `linearDamping`, `FindAnyObjectByType`) · 2D gravity
**(0, −24)** · fixed timestep **0.02** · **domain reload disabled** (Enter Play Mode Options = 1 → statics survive Play
sessions; reset them with `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`) · Input System with
`PointersAndKeyboardsRespectGameViewFocus` (keyboard only reaches the game while the Game view is focused).

---

## 1. Game & scene flow

```
MainMenu ──Play──► PlayerJoinScreen ──any joined player presses Interact──► MapScene ◄─────────────┐
(MainMenuController)  (PlayerJoinManager,                (RunManager.StartNewRun)                  │
                       JoinScreenController)                                                       │
MapScene ──pick Level/Boss node──► SampleScene (gameplay) ──Complete()──► next map node ───────────┤
          (MapCursor → RunManager.SelectNode)   │                     (boss won → new run) ────────┤
          Items/Discard nodes: no content yet,  └──Fail()──► RunManager.FailRun() → new run ───────┘
          marked visited instantly                            (map shows RUN COMPLETE / RUN FAILED)
```

| Scene (build order) | Key objects |
|---|---|
| `MainMenu` | `MainMenuController` (UI Toolkit): Play → `PlayerJoinScreen`; Controls = keyboard rebinding |
| `PlayerJoinScreen` | `PlayerJoinManager` (maxPlayers 4), `JoinScreenController` |
| `SampleScene` (= `SceneNames.Gameplay`) | Spaceship prefab, `LevelManager` + `LevelBootstrap`, `PlayerJoinManager` (maxPlayers 4), `LevelDebugKeys`, camera `SecondOrderFollower` |
| `MapScene` | `MapView`, `MapCursor`, `MapCamera`, `MapPlayerSpawner`, `RunResultScreen`, `MapDebugView` |
| `EnemyTestScene` (not in build) | Enemy sandbox: ship with test-only overrides, sample enemies, spawners (see §5) |

**Run state (static, survives scene loads):** `RunManager` (current `RunState`, `RunConfig` from `Resources/RunConfig`),
`PlayerRoster` (joined device + control scheme pairs). Players are **not** kept between scenes; each scene re-spawns them
from the roster (`PlayerJoinManager.Start` / `MapPlayerSpawner.Start`).

**Gameplay scene start:** `LevelBootstrap.Awake` (order −100) instantiates `RunManager.State.PendingNode.level.layoutPrefab`
and moves the ship to `LevelLayout.shipStart`. Play-in-editor works because `DevBootstrap` (editor only, BeforeSceneLoad)
creates a run + a `KeyboardP1` player when the active scene is `MapScene` or `SampleScene`.

**Level end:** `LevelManager.Fail()` / `Complete()` → `State` = Lost/Won, ship frozen (`linearVelocity = 0`,
`simulated = false`), big text via `OnGUI`, then after `resultDelay` (2 s, realtime) → `RunManager.FailRun()` or
`CompletePendingNode()`.

**RunConfig** (`Assets/Resources/RunConfig.asset`): rows 7, nodes/row 2–3, columns 4, weights Level 6 / Items 2 / Discard 1,
seed 0 (random). Levels: Open Space, Narrow Gap, Zigzag, Pillars; boss: The Gauntlet (`Layout_Gauntlet`).

### PlayerAndSpaceshipSync (players inside a moving ship)
- `Awake`: `shipInstanceCounter++` (static, never reset → grows every Play session because domain reload is off), clones
  `interiorRoot` to **(0, −500·n, 0)** as `"<ship>_PhysicsWallsOnly"`, strips its SpriteRenderers (and any
  SpaceshipControllers), then **destroys every Collider2D under the live ship's `interiorRoot`**.
- On `PlayerJoinManager.OnPlayerJoined`: creates `<player>_PhysicsProxy` at the static room's spawn point (same layer,
  copied Rigidbody2D settings + CircleCollider2D). The real player becomes **Kinematic**, is parented to the ship, and
  `PlayerMovement`/`SquashAndStretch` are pointed at the proxy body.
- `LateUpdate`: proxy position relative to the static room → visual player `localPosition` inside the ship.
- Consoles (`*Console`, Default-layer triggers) sit **outside** `Interior` on the live ship, so visual players can still
  interact with them.
- **Rule: keep all gameplay (enemies, spawns, pickups, projectiles) above Y = −400.**

---

## 2. Layers & Physics2D

| # | Layer | Used for |
|---|---|---|
| 0 | Default | Level obstacles/walls (solid), Goal (trigger), ship consoles (trigger). **Only crash layer.** |
| 1/2/4/5 | TransparentFX / Ignore Raycast / Water / UI | Unity built-ins (unused by gameplay) |
| 6 | Ground | Ship interior floors/walls (live in the hidden static copy at runtime) |
| 7 | Player | Players (visual Kinematic body on the ship + proxy body in the static room) |
| 8 | Spaceship | Ship root: Dynamic RB (mass 10.36, damping 2.01, gravity 0, freeze rot), solid `CircleCollider2D` r **9.65** = the hull |
| 9 | InnerSpaceship | Not used by Spaceship.prefab or Player.prefab (purpose unclear) |
| 10 | ProxySpaceship | Interior colliders that only players collide with |
| 11 | OneWayPlatform | Interior platforms (`PlatformEffector2D`) |
| 12 | Ladder | Interior ladders (triggers) |
| 13 | Enemy | Enemy bodies (solid Dynamic; Kinematic trigger while hull-attached) |
| 14 | EnemyProjectile | Enemy shots (Kinematic triggers) |
| 15 | ShipProjectile | **Reserved** for ship weapons (Bullet.prefab is still on Default) |
| 16 | Pickup | Drops (Kinematic triggers) |

**Collision matrix (layers each one ignores):**

| Layer | Ignores |
|---|---|
| 0 Default | 10, 16 |
| 6 Ground | 10, 16 |
| 7 Player | 7, 8, 9, 13, 14, 15, 16 |
| 8 Spaceship | 7, 8, 9, 15 |
| 10 ProxySpaceship | 0, 1, 2, 4, 5, 6, 13–16 (collides with Player, Spaceship, 9–12) |
| 11 OneWayPlatform / 12 Ladder | 13–16 |
| 13 Enemy | 7, 9, 10, 11, 12, 14, 16 → collides with **0, 6, 8, 13, 15** (+ built-ins) |
| 14 EnemyProjectile | everything except **0, 6, 8** |
| 15 ShipProjectile | everything except **0, 6, 13** |
| 16 Pickup | everything except **8** |

**Rules**
- `ShipCrashDetector.crashLayers` on `Spaceship.prefab` = **1 (Default only)**. Any *solid* (non-trigger) Default collider
  touching the hull after the 0.5 s grace period → `LevelManager.Fail()`. (Script default is `~0` = everything — a new
  ship that keeps the default would also crash on enemies.)
- Triggers never crash the ship (only `OnCollisionEnter2D` is checked).
- Never put projectiles, pickups or attached enemies on Default. Projectiles/pickups are always triggers.
- Kinematic triggers that must hit static walls need `Rigidbody2D.useFullKinematicContacts = true` (see `EnemyProjectile_Basic`).
- `Physics2D.queriesHitTriggers = true` project-wide; enemy line-of-sight explicitly ignores triggers.

---

## 3. Component & Inspector guide

Format: **Attach to** · **Inspector** (field = default → effect) · **API**. `RC` = `[RequireComponent]`.

### Combat — `Assets/Scripts/Combat/` (universal; no enemy types)

**`DamageType`** — `[Flags] enum { None=0, Generic=1, Projectile=2, Contact=4, Module=8, Explosion=16 }`. Additive only.
Module = special ship-module weapons (not plain turret bullets).

**`DamageInfo`** — plain struct: `float amount; DamageType type; Vector2? sourcePos; GameObject source;`
ctor `DamageInfo(float amount, DamageType type = Generic, Vector2? sourcePos = null, GameObject source = null)`.
`sourcePos == null` → no knockback.

**`IDamageable`** — `void DealDamage(DamageInfo hit);`

**`DamageUtil`** — `static bool TryGetDamageable(Collider2D col, out IDamageable target)`: checks
`col.attachedRigidbody`'s GameObject first, then `GetComponentInParent`. Every damage dealer uses this.

**`Damageable`** (IDamageable, `[DisallowMultipleComponent]`) — universal HP for enemies, crates, the ship.
- Attach to: the object's root (next to its Rigidbody2D, so `DamageUtil` finds it).
- Inspector: `maxHealth` = 30 · `acceptedTypes` = Everything (−1) → other types are blocked · `destroyOnDeath` = true
  (false for the ship) · `invulnerabilityTime` = 0 → seconds of blocked hits after each hit.
- API: `float Health`, `MaxHealth`, `bool IsDead`, `DamageType AcceptedTypes` · `bool Accepts(DamageType)` ·
  `DealDamage(DamageInfo)` · `Heal(float)` (clamped, no-op when dead) · `Kill()` (ignores filter/i-frames).
- Events: `Action<DamageInfo> OnDamaged` · `Action<DamageInfo> OnDamageBlocked` (wrong type / invulnerable; nothing
  fires once dead) · `Action<float,float> OnHealthChanged` (current, max) · `Action OnDied` (once, before Destroy).
- Order per hit: Health −= amount → OnHealthChanged → OnDamaged → (0 HP) OnDied → Destroy.

**`DamageFlash`** (RC Damageable) — tints sprites on hit.
- Inspector: `renderers[]` = empty → all child SpriteRenderers · `flashColor` = white · `flashDuration` = 0.1 ·
  `flashOnBlocked` = true · `blockedColor` = (0.4, 0.8, 1). Restores colors captured in Awake.

**`FailLevelOnDeath`** (RC Damageable) — `OnDied` → `LevelManager.Instance.Fail()` (null-safe). For the ship.

**`LevelGate`** (static) — `bool IsPlaying` (LevelManager exists and State == Playing) · `Rigidbody2D Ship` (null unless
Playing). Everything that moves/attacks/spawns/collects halts when `!IsPlaying`.

**`ContactDamage`** — damages what it touches (solid or trigger, Enter + Stay), once per cooldown **per target**.
- Inspector: `damage` = 10 · `damageType` = Contact · `targetLayers` = 256 (Spaceship) · `cooldown` = 0.5.
- Skips: disabled component, `!LevelGate.IsPlaying`, other layers, its own Rigidbody2D. Cooldown starts even if the
  target blocks the hit. `sourcePos` = own position.
- API: `float Damage`, `Cooldown` · event `Action<IDamageable, DamageInfo> OnHit`.

**`Projectile`** (RC Rigidbody2D) — straight-flying shot.
- Prefab setup: trigger collider, **Kinematic** RB, gravity 0, `useFullKinematicContacts` on, layer 14 or 15.
- Inspector: `speed` = 12 · `damage` = 10 · `damageType` = Projectile · `hitLayers` = 256 (Spaceship) ·
  `obstacleLayers` = 65 (Default + Ground) · `lifetime` = 4 · `rotateToDirection` = true.
- Behaviour: forces own colliders to trigger; `MovePosition` each FixedUpdate; frozen while not Playing. On trigger:
  owner ignored → `hitLayers`: damage (if damageable) + despawn → `obstacleLayers`: despawn on **non-trigger** only.
- API: `Launch(Vector2 direction, GameObject owner = null)` (call right after Instantiate) · `GameObject Owner`,
  `Vector2 Velocity`, `float Damage` · event `Action<Projectile, Collider2D> OnImpact` (not on lifetime expiry).

**`ProjectileLauncher`** — fires `Projectile` prefabs.
- Inspector: `projectilePrefab` · `muzzle` = this transform if empty · `spawnOffset` = 0.7 · `cooldown` = 1.2 ·
  `spreadDegrees` = 0. Owner = GameObject of the parent Rigidbody2D.
- API: `bool TryFire(Vector2 direction)` (false: cooldown / no prefab / not Playing / zero dir) · `bool IsReady`,
  `float Cooldown`, `Vector2 MuzzlePosition` · event `Action<Projectile> OnFired`.

### Enemy — `Assets/Scripts/Enemy/`

Standard enemy root (see `Enemy_Basic.prefab`): layer 13, SpriteRenderer, CircleCollider2D (solid, r 0.5, scale 1.2),
Rigidbody2D (Dynamic, mass 1, damping 1, gravity 0, freeze Z, interpolate) + the components below.
**Target is always the ship** (`LevelManager.Ship`); distances are measured to the **hull** (`ship.ClosestPoint`).

**`EnemyMover`** (RC Rigidbody2D) — dumb steering motor; knows nothing about AI.
- Inspector: `acceleration` = 6 (used when a command passes no accel) · `arriveDistance` = 0.4 · `slowDownDistance` = 1.5.
- API: `Stop(float accel = -1)` · `SetDesiredVelocity(Vector2 v, float accel = -1)` · `MoveTo(Vector2 point, float speed,
  float accel = -1)` · `PauseSteering(float seconds)` · `Rigidbody2D Body`, `bool HasArrived`, `IsMovingToPoint`,
  `Vector2 TargetPoint`, `bool IsSteeringPaused`. (`accel < 0` = default.)
- Zeroes velocity when not Playing. Disable it to hand the body to something else (latching).

**`EnemyBrain`** (RC EnemyMover, execution order −10) — perception + mode: `enum State { Idle, Patrol, Engaged }`.
- Inspector: `detectRadius` = 8 · `loseRadius` = 13 · `useLineOfSight` = true · `lineOfSightMask` = 321 (Default, Ground,
  Spaceship) · `idleTimeRange` = (1, 2.5) · `patrolRadius` = 4 (around spawn) · `patrolSpeed` = 2.5 · `maxPatrolTime` = 6 ·
  `logStateChanges` = false.
- Flow: Idle ⇄ Patrol; ship hull within detect (+ LOS) → Engaged → calls the `EnemyBehavior`; hull beyond lose (unless
  `HoldAggro`) or level not Playing → Patrol.
- API: `State CurrentState` · event `Action<State,State> OnStateChanged` (from, to) · `Rigidbody2D Ship`,
  `float HullDistance`, `Vector2 ClosestHullPoint` (refreshed each FixedUpdate) · `Vector2 Home` · `float DetectRadius`,
  `LoseRadius` · `EnemyMover Mover` · `bool HasLineOfSight()`.
- Uses the **first** `EnemyBehavior` on the GameObject — variants that change behavior must remove the inherited one.

**`EnemyBehavior`** (abstract, RC EnemyBrain) — the plug-in slot for "what this enemy does when engaged".
- Protected: `Brain`, `Mover`, `Body` (Rigidbody2D), `Damageable` (may be null) — set in `protected virtual Awake()`
  (override → call `base.Awake()`).
- Overrides: `virtual OnEngage()` · `abstract TickEngaged()` (every FixedUpdate, only while Playing) ·
  `virtual OnDisengage()` · `virtual bool HoldAggro => false`.

**`ApproachBehavior`** (`Behaviors/`) — fly at the ship, hover near the hull. `speed` = 6 · `acceleration` = 10 ·
`stopDistance` = 1 (0 = ram) · `slowDownDistance` = 3.

**`LatchBehavior`** (`Behaviors/`, RC HullAttacher) — fly in and latch. `speed` = 6 · `acceleration` = 12 ·
`attachReach` = 0.25 · `waitDistance` = 2.5 (hover when refused) · `retryInterval` = 1 · `reattachDelay` = 2.
`HoldAggro` = attached.

**`EnemyKnockback`** (RC Damageable, Rigidbody2D) — on a hit with `sourcePos`: impulse away + `mover.PauseSteering`.
`knockbackImpulse` = 7 · `steeringPause` = 0.3. Skips non-Dynamic bodies.

**`HullAttacher`** (RC Rigidbody2D) — latch onto the hull and ride along (never reparented).
- Inspector: `maxAttachedPerShip` = 4 (0 = unlimited; counts all attached HullAttachers on that ship) ·
  `stackOutward` = false (true: pile on top of an occupied spot; false: slide to the nearest open spot) ·
  `maxSearchSteps` = 24 · `maxStackHeight` = 6 · `occupancyMask` = Enemy · `shakeOffSpeed` = 0 (off) ·
  `shakeOffTime` = 1 · `detachSpeed` = 4.
- While attached: EnemyMover disabled, RB Kinematic, own colliders `isTrigger = true`, position = ship-relative anchor
  using the ship's predicted pose each FixedUpdate. Stays attached when the level ends.
- API: `bool TryAttach(Rigidbody2D ship)` · `Detach()` · `bool IsAttached`, `Rigidbody2D AttachedShip`,
  `Vector2 LocalAnchor`, `bool StackOutward`, `float BodyRadius` · events `Action OnAttached`, `OnDetached`.

**`EnemySpawner`** — keeps up to `maxAlive` of **its own** spawns alive (no manager, no pooling).
- Inspector: `entries[]` {`prefab`, `weight` = 1} · `maxAlive` = 4 · `totalToSpawn` = 0 (endless) · `initialDelay` = 1 ·
  `spawnInterval` = 3 (also the delay after a slot frees) · `aroundShip` = false · `spawnRadius` = 6 (around the
  spawner) · `shipRingDistance` = (18, 24) from the hull (ring mode) · `minDistanceFromHull` = 8 ·
  `activationDistance` = 0 (always; else only while the hull is this close) · `clearance` = 0.8 ·
  `blockingLayers` = Default, Ground, Spaceship, Enemy · `placementTries` = 10 · `container` (optional parent).
- Rejects spots at Y ≤ −400, too close to the hull, or overlapping solid colliders. Paused while not Playing.
- API: `int AliveCount`, `SpawnedTotal`, `bool IsFinished` · event `Action<GameObject> OnSpawned`.

**Prefabs** — `Prefabs/Enemies/Enemy_Basic` (30 HP, Approach, DamageFlash, EnemyKnockback, drops 2–3 Light orbs).
Samples in `Prefabs/Enemies/Samples/` (variants of Enemy_Basic, **demo only, not final mobs**): `Enemy_Sample_Rammer`
(ContactDamage 5 / 0.75 s), `Enemy_Sample_Shooter` (DebugShooterBehavior + ProjectileLauncher), `Enemy_Sample_Latcher`
(LatchBehavior + HullAttacher + ContactDamage 3 / 1 s).

**Test-only (`Enemy/Debug/`, EnemyTestScene only):** `DebugDamager` (`damageKey` K, `damage` 10, `radius` 6,
`damageType` Generic; includes triggers, skips own body) · `DebugSelfDamage` (H, 25, Projectile → own Damageable) ·
`DebugResourceReceiver` (IResourceReceiver stub, `capacity` 10, on-screen counter) · `DebugShipDriver` (WASD →
`MovementModule.SetMoveInput`) · `DebugShooterBehavior` (`preferredDistance` 6, `speed` 4, `acceleration` 8,
`requireLineOfSight` true).

### Pickups — `Assets/Scripts/Pickups/`

**`ResourceType`** — `enum { Light = 0 }` (additive only).

**`IResourceReceiver`** — `float Receive(ResourceType type, float amount)` → amount accepted (0 = full / unwanted).
Must live on the ship root (the GameObject with the ship's Rigidbody2D). **No real implementation yet** (§5).

**`Pickup`** (RC Rigidbody2D) — collectible drop.
- Prefab setup: trigger collider, Kinematic RB, gravity 0, layer 16.
- Inspector: `resourceType` = Light · `amount` = 1 · `driftDeceleration` = 6 · `magnetRadius` = 5 (to the hull) ·
  `magnetSpeed` = 16 · `magnetAcceleration` = 40 · `lifetime` = 15 (0 = forever) · `blinkTime` = 3 · `spriteRenderer`.
- Collects on trigger Enter/Stay with the ship → `Receive`; keeps any rejected remainder; destroyed at 0. No receiver →
  one warning, stays. Frozen while not Playing.
- API: `ResourceType ResourceType`, `float Amount`, `Vector2 Velocity` · `Launch(Vector2 initialVelocity)`.

**`DropOnDeath`** (RC Damageable) — on `OnDied` spawns `drops[]` {`prefab`, `chance` = 1, `count` = (1, 1) min–max
inclusive} with `scatterSpeed` = 4 (each 50–100%). Calls `Pickup.Launch` or sets a Dynamic body's velocity.

Prefab: `Prefabs/Pickups/Pickup_Light` (yellow orb, Light ×1). Projectile prefab: `Prefabs/Projectiles/EnemyProjectile_Basic`.

### Spaceship — `Assets/Scripts/Spaceship/`

**`SpaceshipModule`** (abstract) — a ship system driven by a console.
- Protected: `bool currentInteractInput`, `Vector2 currentMoveInput`.
- API: `SetMoveInput(Vector2)` (clamped to length 1) · `SetInteractInput(bool)`.

**`MovementModule : SpaceshipModule`** (RC Rigidbody2D, on the ship root) — `maxSpeed` = 8 (**prefab: 1000**). Each
FixedUpdate: `AddForce(input · maxSpeed · mass · fixedDeltaTime)`. It is really a force scale: 1000 with damping 2.01 →
top speed ≈ 10 u/s.

**`WeaponFire : SpaceshipModule`** (on each `ShipTurret`) — aims and fires `bullet`.
- Inspector (default → `ShipTurret.prefab`): `cooldown` 2 → 0.1 · `force` 0 → 20.1 · `rotationSpeed` 10 → 102.8 (deg/s) ·
  `centerPoint` (spawn + aim transform) · `minRotation` −50 → −74.5 · `maxRotation` 50 → 74.3 (each turret instance
  overrides these) · `gizmoRadius` 2 · `bullet` (Bullet.prefab).
- Fire: while interact is held and the timer > cooldown → `Instantiate(bullet, centerPoint.position)` +
  `AddForce(-centerPoint.right · force, Impulse)`. Aim: move input (in parent space) → clamped local Z angle.

**`SpaceshipController`** — a console the player operates.
- Attach to: a console GameObject on the ship with a **trigger** collider (found by `PlayerInteract`'s overlap).
- Inspector: `spaceshipController` (the SpaceshipModule; auto `GetComponentInParent` if empty) · `standPoint`
  (player is snapped to its X).
- API: `PlayerInteract CurrentOperator`, `bool IsOccupied`, `Transform VisualTransform` · `bool TryOccupy(PlayerInteract)`
  · `Release()` · `virtual SetMoveInput(Vector2)`, `SetInteractInput(bool)` · `virtual Initialize(SpaceshipModule,
  Transform)` · protected `virtual OnOperatorEntered/OnOperatorExited` (exit zeroes the module's inputs).

**`PlayerAndSpaceshipSync`** (ship root) — see §1. Inspector: `interiorRoot`, `localSpawnPoint`.
API: `HandlePlayerJoined(GameObject)`.

**Spaceship.prefab wiring:** SteeringConsole → `MovementModule`; Left/Right/Top/Bottom TurretConsole → that turret's
`WeaponFire`. Turrets sit at local (±9.61, 0) / (0, ±9.61).

### Level — `Assets/Scripts/Level/`

**`LevelManager`** — one per gameplay scene. Inspector: `ship` (Rigidbody2D) · `resultDelay` = 2 · `startGracePeriod` = 0.5.
API: `static Instance` · `Rigidbody2D Ship` · `LevelState State` (`Playing, Won, Lost`) · `bool IsInGracePeriod` ·
`Complete()` · `Fail()` (both ignored once not Playing).

**`LevelBootstrap`** (RC LevelManager, order −100) — `nameShowTime` = 2. Spawns the pending node's layout and places the ship.
Needs `RunManager.State.PendingNode` (no run → exception).

**`LevelConfig`** (ScriptableObject, menu *Run/Level Config*) — `string displayName`, `GameObject layoutPrefab`.

**`LevelLayout`** — on a layout prefab root: `public Transform shipStart`.

**`GoalZone`** (RC Collider2D) — forces its collider to trigger; the ship's Rigidbody2D entering → `Complete()`.

**`ShipCrashDetector`** (RC Rigidbody2D, ship root) — `crashLayers` = ~0 (**prefab: Default only**). Solid collision on a
crash layer after the grace period → `Fail()`.

**`LevelDebugKeys`** — debug builds only: F9 = Complete, F10 = Fail.

Prefabs: `Levels/Obstacle` (Default, solid BoxCollider2D, red sprite) · `Levels/Goal` (Default, trigger box, GoalZone) ·
`Levels/Layout_*` (LevelLayout + ShipStart + Obstacles + Goal).

### Player — `Assets/Scripts/Player/`

**`PlayerMovement`** (RC PlayerInput, Rigidbody2D) — platforming (coyote time, jump buffer, ladders, one-way drop-through).
Inspector (default → `Player.prefab`): `playerInput`, `rb` · `moveSpeed` 0 → 9 · `jumpForce` 0 → 10 · `dynamics` (2, 0.5, 2) ·
`groundCheck` · `groundCheckRadius` 0 → 0.69 · `groundMask` → Ground · `oneWayPlatformMask` → OneWayPlatform ·
`dropThroughDuration` 0.25 · `ladderMask` → Ladder · `climbSpeed` 5 → 8 · `ladderHorizontalMultiplier` 0.6 → 0.9 ·
`ladderSnapSpeed` 14 · `hangTime` 0 → 0.15 · `jumpBufferDuration` 0 → 0.15.
API: `SetUsingModule(bool)` · `SetPhysicsRigidbody(Rigidbody2D proxy)`.

**`PlayerInteract`** — `playerInput`, `playerMovement` · `interactRadius` 1.5 → **1.96** · `interactableLayer` → Everything.
Interact = occupy the nearest free `SpaceshipController`; Back or moving beyond 1.5 × radius = release.
API: `ReleaseCurrentModule()`.

**`PlayerJoinManager`** — `playerPrefab`, `maxPlayers` (scenes: 4). Join keys: Gamepad Start, Space (P1), Enter (P2),
Right Shift (P3), Numpad Enter (P4). API: `public Action<GameObject> OnPlayerJoined` (a field, not an `event`).

**`JoinScreenController`** — any joined player's Interact → `RunManager.StartNewRun()`.

**`KeyboardBindingPreferences`** (static) — `Apply(InputActionAsset)`, `Save(InputActionAsset)`; PlayerPrefs key
`KeyboardBindingOverrides.v1`.

`Player.prefab`: layer 7, PlayerInput, PlayerMovement, CircleCollider2D r 0.5 (solid), Rigidbody2D Dynamic (mass 1,
gravity 1, freeze rot), PlayerInteract, child `Visuals` with SquashAndStretch (0.1 / 0.15).

### Map — `Assets/Scripts/Map/`
- **Data:** `NodeType { Level, Items, Discard, Boss }` · `MapNode` {id, row, column, type, `List<int> next`, `LevelConfig
  level`} · `MapData` {nodes, rows; `Get(id)`, `Row(r)`, `RowCount`} · `MapGenerator.Generate(RunConfig, seed)`
  (seeded; first row and the row before the boss are always Level; no two non-Level nodes in a row; throws without
  levels/boss).
- **View:** `MapView` (`xSpacing` 2.5, `ySpacing` 2, `jitter` 0.3, `dashSpacing` 0.4, `visuals`, `backgroundColor`,
  `edgeColor`, `tokenMoveTime` 0.5; `Refresh(MapNode)`, `MoveToken(MapNode)`) · `MapNodeView` (`NodeState { Locked,
  Visited, Available, Selected }`) · `MapEdge` · `MapSprites.Circle()/Square()` · `NodeTypeVisuals` (SO: icon + tint per
  type) · `MapCamera` (`size` 5, `smoothTime` 0.3, `lookAhead` 3; `SetTarget`).
- **Input:** `MapCursor` (`Move(int)`, `Confirm()`) · `MapPlayerSpawner` (`mapPlayerPrefab`) · `MapPlayerInput` (on
  `MapPlayer.prefab`: Move left/right + Interact) · `MapDebugView` (F1, debug builds).

### Run — `Assets/Scripts/Run/`
- **`RunManager`** (static): `RunState State` · `RunConfig Config` · event `Action StateChanged` · `StartNewRun()` ·
  `CreateRun()` · `bool CanSelect(MapNode)` · `SelectNode(MapNode)` · `CompletePendingNode()` · `FailRun()` ·
  `bool? TakeLastResult()`.
- **`RunState`**: `seed`, `map`, `currentNodeId` (−1 = start), `visitedNodeIds`, `MapNode PendingNode` ·
  `AvailableNodes()` · `Visit(MapNode)`.
- **`RunConfig`** (SO, *Run/Run Config*): `rows` 7, `minNodesPerRow` 2, `maxNodesPerRow` 3, `columns` 4, `levelWeight` 6,
  `itemsWeight` 2, `discardWeight` 1, `seed` 0, `levels`, `bossLevel`.
- **`PlayerRoster`** (static): `Entries`, `Count`, `Add`, `UsesDevice`, `UsesScheme`, `Spawn`, `SpawnAll`.
  `RosterEntry` {controlScheme, device}.
- **`SceneNames`**: `PlayerJoin` = "PlayerJoinScreen", `Map` = "MapScene", `Gameplay` = "SampleScene".
- **`RunResultScreen`** (`showTime` 3) · **`DevBootstrap`** (editor only).

### Utilities / Editor / UI
- `DebugText.Draw(string text, int size, Color color, TextAnchor anchor)` — call from `OnGUI` only.
- `DetachOnAwake` — `keepWorldTransform` = true; unparents on Awake.
- `DynamicsParams` — struct `f` [0.001–12], `z` [0.001–3], `r` [−5–5]; ctor default (2, 0.5, 2). Inspector shows a
  response graph (`Editor/DynamicsParamsDrawer`).
- `V2SecondOrderDynamics` / `V3SecondOrderDynamics` — `(f, z, r, x0)`; `Update(dt, x, xd)`; `ChangeParameters(f, z, r)`.
- `SecondOrderFollower` — `target`, `dynamics` (3, 0.6, 0); smooth-follows in LateUpdate (gameplay camera → ship).
- `SquashAndStretch` — `rb`, `deformationMultiplier` 0.05, `maxDeformation` 0.5, `dynamics` (5, 0.25, 0);
  `SetPhysicsRigidbody(rb)`.
- `UI/MainMenu/MainMenuController` (RC UIDocument) — `playerControls`; Play → PlayerJoinScreen; rebinds the
  `KeyboardP1–4` groups of the `Gameplay` action map; saves via `KeyboardBindingPreferences`. `SpaceBackdrop` draws the background.

---

## 4. Quick recipes

**Make something damageable / destructible**
1. Root: collider (solid if it should block; layer Default = also crashes the ship on touch, Enemy = doesn't).
2. Add `Damageable` (set `maxHealth`, optional `acceptedTypes`, `destroyOnDeath`) + `DamageFlash`.
3. Optional: `DropOnDeath`. Anything calling `DamageUtil` / `IDamageable` can now hurt it.

**Make a new enemy (no core code changes)**
1. In `Prefabs/Enemies/`, create a **prefab variant** of `Enemy_Basic` (keeps layer 13, RB, Damageable, Mover, Brain…).
2. Different engaged behavior? Remove `ApproachBehavior` and add one `EnemyBehavior` subclass (only one per enemy):
   ```csharp
   public class MyBehavior : EnemyBehavior {
       public override void TickEngaged() {
           Vector2 toShip = Brain.Ship.position - Body.position;
           Mover.SetDesiredVelocity(toShip.normalized * 5f);
       }
   }
   ```
3. Add actuators as needed: `ContactDamage` (melee), `ProjectileLauncher` + a projectile prefab (ranged),
   `HullAttacher` + `LatchBehavior` (latcher). Tune `Damageable.acceptedTypes` for armored variants (e.g. Module only).

**Add drops to any Damageable**
`DropOnDeath` → entry {`Pickup_Light`, chance, count}. New resource: add a `ResourceType` value and make a variant of
`Pickup_Light` (trigger, Kinematic, layer 16, `resourceType` set). The ship needs an `IResourceReceiver` to collect.

**Build a new spaceship console/module**
1. `class MyModule : SpaceshipModule` using `currentMoveInput` / `currentInteractInput` in Update/FixedUpdate.
2. Put it on the ship (or a child like a turret).
3. Add a console GameObject on the ship **outside `Interior`** with a trigger collider + `SpaceshipController`
   (`spaceshipController` → MyModule, optional `standPoint`).
4. Weapons that hit enemies: use `ProjectileLauncher` + a projectile on layer 15 (`hitLayers` = Enemy) or
   `DamageUtil` overlaps **with triggers on** (attached enemies are triggers). Use `DamageType.Module` for anti-latcher tools.

**Build a level layout**
1. Prefab root + `LevelLayout`; child `ShipStart` (assign `shipStart`).
2. Add `Obstacle` instances (Default, solid → crash on touch) and one `Goal` (trigger + `GoalZone`).
3. Optional enemies: drop `EnemySpawner` objects (area mode, `activationDistance` so they wake up as the ship
   approaches) or enemy prefab instances. Keep everything above Y −400.
4. Create a `LevelConfig` (displayName + layoutPrefab) and add it to `RunConfig.levels` (or `bossLevel`).

---

## 5. Known integration gotchas & pending hooks

| # | Issue | Status / what to do |
|---|---|---|
| 1 | **Turret bullets vs. the hull:** `WeaponFire` spawns `Bullet.prefab` at the turret pivot (9.61 from ship centre, inside the hull circle r 9.65). Bullet = solid Dynamic body on **Default** = crash layer → after the grace period it should trigger `ShipCrashDetector` → `Fail()`, and its impulse pushes the ship. | Found in the files, **not play-tested**. Fix options: spawn outside the hull, or make bullets Kinematic triggers on layer 15 with a `Projectile`. |
| 2 | `Bullet.prefab` has no script: no damage, no lifetime (never destroyed; 10 shots/s per turret at cooldown 0.1), Discrete collision at ~20 u/s. | Add `Projectile` (`hitLayers` = Enemy, `damageType` Projectile) + lifetime. |
| 3 | **Ship has no HP in real levels:** `Spaceship.prefab` has no `Damageable`, so enemy damage hits nothing outside `EnemyTestScene`. | Add `Damageable` (destroyOnDeath off, accepted Projectile/Contact/Explosion, i-frames 0) + `FailLevelOnDeath` + `DamageFlash` to the ship root when the team is ready. |
| 4 | **Light resource:** no `IResourceReceiver` on the ship (no ShipLight script yet); pickups stay uncollected and warn once. | Teammate's Light system implements `IResourceReceiver` on the ship root. |
| 5 | **Attached latchers are triggers:** weapons/modules that use non-trigger queries miss them. | Use `ContactFilter2D { useTriggers = true }`; projectiles already use trigger events. Trigger-vs-trigger hits from ship projectiles on attached latchers are **untested**. |
| 6 | **After level end the ship is `simulated = false`.** | Don't query ship physics unless `LevelGate.IsPlaying` / `State == Playing`. All enemy/pickup/spawner code already halts. |
| 7 | **Y ≤ −400 is the hidden player-physics zone.** | Never spawn or move gameplay objects there. `EnemySpawner` rejects such spots. |
| 8 | **Domain reload disabled:** statics persist between Play sessions. `PlayerAndSpaceshipSync.shipInstanceCounter` is never reset, so the hidden interior goes to −500, −1000, … −5500… | Harmless now; reset new statics with `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`. |
| 9 | `ShipCrashDetector.crashLayers` script default is `~0`; only the prefab value (Default) makes enemies/pickups safe. | Keep the prefab override; check it on any new ship. |
| 10 | `GoalZone.OnTriggerEnter2D` and `LevelDebugKeys` use `LevelManager.Instance` without a null check. | NRE in scenes without a LevelManager. |
| 11 | Gameplay scene needs a run (`LevelBootstrap` reads `PendingNode`); works in-editor only via `DevBootstrap`. `Fail()`/`Complete()` in any scene end in a scene load after 2 s (EnemyTestScene → MapScene). | Expected; for sandboxes stop Play Mode before 2 s. |
| 12 | Items/Discard map nodes have no content (marked visited instantly). Run end immediately starts a new run on the map. | Pending design. |
| 13 | **Editor input focus:** keyboard input only reaches the game while the Game view is focused (also for scripted key presses). | Click the Game view before testing. |
| 14 | README out of date: hidden interior is at `Y = −500·n` (README says −5000+); the field is `interiorRoot` (README: `interiorCollidersRoot`); `ShipCrashDetector` only fails on `crashLayers` (README: any non-trigger collision). | Update README when convenient. |
| 15 | Misc: `MovementModule.maxSpeed` is a force scale, not a speed · `PlayerJoinManager.OnPlayerJoined` is a public field (can be overwritten with `=`) · the ship's `ShipCrashDetector` component is named "WeaponConsole" in the prefab (cosmetic) · `PlayerAndSpaceshipSync` strips SpaceshipControllers from the interior copy, but consoles live outside `Interior` (no-op; if a console is ever moved under `Interior`, its collider is destroyed on the live ship and it becomes unusable). | Cleanup candidates. |

### Test sandbox: `EnemyTestScene`
Not in the build. Its ship instance has **scene-only** overrides (Spaceship.prefab is untouched): Damageable (100 HP,
Projectile/Contact/Explosion, i-frames 0), FailLevelOnDeath, DamageFlash, DebugShipDriver (WASD), DebugDamager K
(Generic, r 12) + M (Module, r 18), DebugSelfDamage H, DebugResourceReceiver. Contains the sample enemies, 5 latchers and
two spawners (`EnemySpawner_Area`; `EnemySpawner_ShipRing` inactive). Enemy-system planning notes live in `.plans/` (local, gitignored).
