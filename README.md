# Lumens in a Dangerous Space

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
2. In the Project window, open **`Assets/Scenes/MainMenu.unity`** and enter Play Mode. Builds start here automatically.
3. Select **Play** to open the crew lobby. Press a join key from the table below, then press **Interact** as a joined player to start the run.
4. To change keys, select **Controls** and choose one of the four keyboard layouts. Click a key and press its replacement, or press **Esc** to cancel. Changes save automatically; **Reset to defaults** restores only the selected layout. Join keys and gamepad controls are unchanged.

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
| `PlayerJoinManager.cs` | Detects device join inputs, instantiates player prefabs with paired control schemes, records them in `PlayerRoster` and respawns roster players on scene start. |
| `PlayerMovement.cs` | Handles 2D horizontal movement, jumping, coyote time, jump buffering, and velocity smoothing. |
| `PlayerInteract.cs` | Performs overlap checks for `SpaceshipController` stations and routes input while occupying a module. |
| `PlayerAndSpaceshipSync.cs` | Clones the static physics interior and syncs visual player transforms onto the moving ship. |
| `LevelManager.cs` | Per-scene win/lose state. `Complete()` / `Fail()` freeze the ship, show a result, then hand it to `RunManager`. |
| `ShipCrashDetector.cs` | On the Spaceship root: any non-trigger collision calls `LevelManager.Fail()`. |
| `GoalZone.cs` | Green trigger zone: the ship entering it calls `LevelManager.Complete()`. |
| `LevelDebugKeys.cs` | Editor/dev only: F9 = win, F10 = lose. |
| `PlayerRoster.cs` | Static list of joined device/scheme pairs, so players survive scene loads. `SpawnAll` recreates the players. |
| `JoinScreenController.cs` | Join screen UI; any joined player pressing Interact starts a run and loads the map. |
| `SceneNames.cs` | Scene name constants. |
| `RunManager.cs` | Static class owning the current run: starts runs, selects map nodes, loads the map/gameplay scenes and ends runs. |
| `RunState.cs` | Current run data: map, visited nodes, pending node and the nodes that can be picked next. |
| `RunConfig.cs` | ScriptableObject with run/map tuning (`Assets/Resources/RunConfig.asset`). |
| `RunResultScreen.cs` | Shows "RUN COMPLETE" / "RUN FAILED" on the map after a run ends. |
| `LevelConfig.cs` | ScriptableObject describing a level: display name and layout prefab. |
| `LevelLayout.cs` | On the root of a layout prefab (obstacles + goal), points to where the ship starts. |
| `LevelBootstrap.cs` | In the gameplay scene: spawns the picked level's layout and moves the ship to its start. |
| `MapGenerator.cs` | Deterministic (seeded) generator for the branching node map. |
| `MapNode.cs`, `MapData.cs`, `NodeType.cs` | Map data model. |
| `MapDebugView.cs` | Text-only map for debugging (F1 toggles, Editor/dev only).
| `DevBootstrap.cs` | Editor only: makes a run and a keyboard player so the map or level scene can be played directly. |
| `DebugText.cs` | Draws big placeholder text over the screen (results, prompts). | |
| `MapView.cs` | Builds the flat 2D map at runtime: nodes, dashed edges, ship token and background. |
| `MapNodeView.cs` | One map node: state-based look (locked, visited, available, selected). |
| `NodeTypeVisuals.cs` | ScriptableObject with an icon and tint per node type. |
| `MapCursor.cs` | Highlights available nodes, confirms a pick and animates the token. |
| `MapPlayerSpawner.cs` | Spawns a `MapPlayer` prefab (with `PlayerInput`) for every player in the roster. |
| `MapPlayerInput.cs` | On the `MapPlayer` prefab: reads Move/Interact and drives `MapCursor`, so any player can pick. |
| `MapEdge.cs` | A dashed line between two nodes. |
| `MapCamera.cs` | Pans the map camera vertically to follow the ship token. |
| `MapSprites.cs` | Runtime-generated placeholder circle and square sprites. |
| `SpaceshipController.cs` | Represents an interactable station inside the ship; forwards operator input to a `SpaceshipModule`. |
| `SpaceshipModule.cs` | Abstract base class for ship systems controlled via consoles. |
| `MovementModule.cs` | Applies directional forces to the spaceship `Rigidbody2D` based on operator input. |
| `SecondOrderFollower.cs` | Smoothly interpolates a transform toward a target using 3D second-order dynamics (used on the Main Camera). |
| `SquashAndStretch.cs` | Deforms local scale procedurally based on frame-to-frame `Rigidbody2D` velocity deltas. |
| `V2/V3SecondOrderDynamics.cs` | Solvers for 2D and 3D second-order differential equations. |
| `DynamicsParams.cs` | Serializable struct holding `f`, `z`, and `r` parameters. |
| `DynamicsParamsDrawer.cs` | Custom `PropertyDrawer` that previews the second-order response graph in the Inspector. |
| `DetachOnAwake.cs` | Unparents a GameObject on `Awake()`. |
