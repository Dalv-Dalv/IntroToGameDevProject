# 2D Co-Op Spaceship Project

A local multiplayer 2D cooperative spaceship game built in Unity. Up to 4 players crew a single spacecraft, navigating its interior platforms and operating consoles to pilot the ship.

---

## Table of Contents
1. [Getting Started](#getting-started)
2. [Controls & Player Spawning](#controls--player-spawning)
3. [Gameplay Overview](#gameplay-overview)
4. [Architecture & Systems](#architecture--systems)
5. [Script Overview](#script-overview)

---

## Getting Started

1. Open the project in Unity (requires Universal Render Pipeline and the Unity Input System package).
2. In the Project window, open **`SampleScene`**.
3. Press **Play** in the Editor.
4. The scene starts with no active players. Press **`Space`** (Keyboard P1) or **`Start`** (Gamepad) to spawn a character inside the ship.

---

## Controls & Player Spawning

The game supports drop-in local multiplayer for up to **4 players** using any combination of gamepads or a single shared keyboard.

### 1. Control Schemes (`PlayerControls`)
Press the assigned **Spawn / Join** key while the game is running to instantiate a player:

| Scheme | Spawn / Join | Move / Steer (Up, Left, Down, Right) | Jump | Interact (Use Console) | Back (Exit Console) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Gamepad** | `Start` | `Left Stick` | `South Button` (`A` / `Cross`) | `Right Trigger` (`RT` / `R2`) | `Left Trigger` (`LT` / `L2`) |
| **Keyboard P1** | `Space` | `W`, `A`, `S`, `D` | `W` | `E` | `Q` |
| **Keyboard P2** | `Enter` | `Arrow Keys` (`Up`, `Left`, `Down`, `Right`) | `Up Arrow` | `/` (Slash) | `'` (Quote) |
| **Keyboard P3** | `Right Shift` | `Y`, `G`, `H`, `J` | `Y` | `U` | `T` |
| **Keyboard P4** | `Numpad Enter` | `O`, `K`, `L`, `;` | `O` | `P` | `I` |

### 2. Context-Sensitive Actions
Inputs change behavior depending on whether the player is moving on foot or operating a ship console:

| Action | On-Foot (Inside Ship) | Operating Console |
| :--- | :--- | :--- |
| **Move** | Horizontal movement (`Left` / `Right`) | 2D directional input (`Up`, `Down`, `Left`, `Right`) sent to the active module |
| **Jump** | Vertical jump (includes Coyote Time and Jump Buffering) | Disabled |
| **Interact** | Occupy the nearest unoccupied console within range | Held/Pressed state forwarded to the active module |
| **Back** | None | Release the current console and restore on-foot movement |

---

## Gameplay Overview

1. **Spawn:** Press a join key to spawn at the ship's interior spawn point.
2. **Navigate:** Move and jump across the interior layout to reach control stations.
3. **Operate Consoles:** Stand near an available console and press **Interact**. The player character aligns with the station's `standPoint`, and on-foot movement is locked.
4. **Pilot the Ship:** While occupying the movement console, directional inputs apply physics forces to the spaceship's `Rigidbody2D`.
5. **Release Consoles:** Press **Back** to leave the station and return to platforming. Moving beyond `1.5x` the interaction radius also forces an automatic release.

---

## Architecture & Systems

### 1. Spaceship Modules (`SpaceshipModule` & `SpaceshipController`)
Interactive stations and ship behaviors are split into two classes:
* **`SpaceshipController`**: Attached to console objects inside the ship. Manages player proximity checks, occupancy state, player alignment, and input forwarding.
* **`SpaceshipModule`**: Abstract base class attached to the main Spaceship object. Receives `currentMoveInput` (`Vector2`) and `currentInteractInput` (`bool`) from its linked `SpaceshipController`.

**Adding a new module:**
1. Create a class inheriting from `SpaceshipModule` (see `MovementModule.cs`).
2. Implement module behavior in `Update()` or `FixedUpdate()` using `currentMoveInput` and `currentInteractInput`.
3. Attach the component to the Spaceship and assign it to a console's `SpaceshipController`.

### 2. Static Proxy Physics Sync (`PlayerAndSpaceshipSync`)
To avoid 2D physics instability when platforming inside a moving and rotating `Rigidbody2D`, player physics are simulated separately from the moving ship:
* On `Awake()`, `PlayerAndSpaceshipSync` duplicates the `interiorCollidersRoot` hierarchy at a distant static position (`Y = -5000+`).
* Players physically spawn, collide, and interact with consoles inside this stationary copy.
* For each player, a visual dummy (`SpriteRenderer`) is parented to the moving spaceship and synced in `LateUpdate()` to match the physics player's relative position, rotation, scale, and sprite state.

### 3. Second-Order Dynamics
Movement smoothing, camera tracking, and sprite deformation use a second-order system (`V2SecondOrderDynamics`, `V3SecondOrderDynamics`) controlled by three parameters:
* **`f` (Frequency):** Response speed in Hz.
* **`z` (Damping):** Damping ratio (values `< 1` oscillate/overshoot; `>= 1` settle critically/overdamped).
* **`r` (Initial Response):** Immediate velocity reaction to target changes.

`DynamicsParamsDrawer.cs` renders a live step-response curve directly in the Unity Inspector wherever `DynamicsParams` is serialized.

---

## Script Overview

| Script | Purpose |
| :--- | :--- |
| `PlayerJoinManager.cs` | Detects device join inputs and instantiates player prefabs with paired control schemes. |
| `PlayerMovement.cs` | Handles 2D horizontal movement, jumping, coyote time, jump buffering, and velocity smoothing. |
| `PlayerInteract.cs` | Performs overlap checks for `SpaceshipController` stations and routes input while occupying a module. |
| `PlayerAndSpaceshipSync.cs` | Clones the static physics interior and syncs visual player transforms onto the moving ship. |
| `SpaceshipController.cs` | Represents an interactable station inside the ship; forwards operator input to a `SpaceshipModule`. |
| `SpaceshipModule.cs` | Abstract base class for ship systems controlled via consoles. |
| `MovementModule.cs` | Applies directional forces to the spaceship `Rigidbody2D` based on operator input. |
| `SecondOrderFollower.cs` | Smoothly interpolates a transform toward a target using 3D second-order dynamics (used on the Main Camera). |
| `SquashAndStretch.cs` | Deforms local scale procedurally based on frame-to-frame `Rigidbody2D` velocity deltas. |
| `V2/V3SecondOrderDynamics.cs` | Solvers for 2D and 3D second-order differential equations. |
| `DynamicsParams.cs` | Serializable struct holding `f`, `z`, and `r` parameters. |
| `DynamicsParamsDrawer.cs` | Custom `PropertyDrawer` that previews the second-order response graph in the Inspector. |
| `DetachOnAwake.cs` | Unparents a GameObject on `Awake()`. |
