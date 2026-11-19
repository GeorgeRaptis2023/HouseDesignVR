# Project Overview
- Game Title: House Design Together (project folder name; working title).
- High-Level Concept: An asymmetric collaborative house-design prototype. A PC host observes a room while VR clients select colors and recolor individual wall faces using their controllers.
- Players: Networked multiplayer; existing AsymmetricSpawner assigns PC to host and VR to clients.
- Inspiration / Reference Games: None specified; use a familiar paint-palette interaction rather than introducing unrelated mechanics.
- Tone / Art Direction: Preserve the current simple 3D room; compact, readable wrist palette with neutral framing.
- Target Platform: Current StandaloneWindows64 target; preserve existing OpenXR configuration and do not add platform targets.
- Screen Orientation / Resolution: Existing PC landscape output and headset stereo display; wrist UI uses world-space dimensions, not a fixed monitor resolution.
- Render Pipeline: URP, package 17.3.0; Unity 6000.3.27f2.

# Game Mechanics
## Core Gameplay Loop
1. Point the right controller at the color wheel on the left wrist and press/drag the trigger to select hue and saturation.
2. Optionally adjust a brightness slider; inspect the selected-color swatch.
3. Aim at a wall face and press the right trigger once to apply the selected color to that face.
4. PC and VR players see the server-confirmed result. Repeat to explore room color combinations.

User-selected scope: one wall face per application, left-wrist palette, shared networked colors. This supports creative autonomy and low-friction experimentation (SDT), with immediate and unambiguous feedback (MDA). Brush strokes and whole-wall recoloring are not included.

## Controls and Input Methods
- Use the active New Input System (activeInputHandler=1), existing InputActionManager, and XR Interaction Toolkit 3.3.2.
- Reuse the right NearFarInteractor's UI ray and existing UI Press action for the wrist wheel.
- Subscribe to an appropriate existing right-hand trigger action for painting; verify its exact binding and API during implementation. No legacy Input polling.
- Painting is blocked while the ray is over UI, selecting/grabbing an object, or in teleport mode. UI selection must never paint a wall behind the palette.
- Palette visible only for the owning VR player and attached to the tracked left controller. Remote avatars must not run painting input or show another player's personal palette.
- Default palette size approximately 0.22–0.28 m wide, oriented above the left wrist for right-ray use. Tune after headset or simulator visual verification. Keep it out of the user's normal forward sightline.
- Maximum paint reach configurable, initially 8 m; one accepted application per trigger press, not a continuous brush.

# UI
Use uGUI world-space Canvas for XR controller ray compatibility. Existing scene EventSystem currently has InputSystemUIInputModule; the NearFarInteractor has UI interaction enabled but requires the appropriate XR UI input module and tracked-device graphic raycaster.

Wireframe (left wrist):
```text
+--------------------------+
|      COLOR PALETTE       |
|                          |
|     [ HSV color disk ]   |
|       selection dot      |
|                          |
| Brightness [----------]  |
| Selected [color swatch]  |
+--------------------------+
```
- Disk angle maps to hue; radial distance maps to saturation; slider controls value. This includes white, gray, and black without a second palette.
- Ignore pointer positions outside the disk rather than changing color unexpectedly.
- Selection marker has contrasting outlines and the swatch updates locally immediately.
- Right-ray target feedback should distinguish valid wall hits from empty space; reuse existing ray visuals where possible and avoid creating conflicting controller rays.
- Decorative UI graphics do not intercept raycasts; wheel/slider are the interactive targets.
- Use existing project-compatible labels; no new font package or font asset work is necessary.

# Key Asset & Context
## Verified current state
- Assets/Scripts/SpawnWalls.cs: SpawnCuboid(Vector3 position, Vector3 scale, Quaternion rotation, string objectTag = "") creates primitive cubes locally in Start(). Four walls and the PCInvisible ceiling are created on every peer; they are not network-spawned objects.
- Assets/Scripts/AsymmetricSpawner.cs: host gets Assets/Prefabs/Capsule.prefab; clients get Assets/Prefabs/XR Origin (XR Rig).prefab.
- Assets/Scripts/PlayerLocalSetup.cs: owner-only camera/input pattern with componentsToDisable for remote avatars.
- VR prefab: tracked left/right controllers, NearFarInteractor with UI interaction enabled, InputActionManager, NetworkObject and NetworkTransforms.
- Assets/Samples/XR Interaction Toolkit/3.3.2/Starter Assets/XRI Default Input Actions.inputactions: Select, Activate, and UI Press action references already assigned to controllers. Read rather than modify sample assets.
- Assets/Scenes/SampleScene.unity: GameManager carries SpawnWalls; EventSystem uses InputSystemUIInputModule; NetworkManager.EnableSceneManagement is true.
- Versions: Input System 1.20.1, Netcode for GameObjects 2.13.3, uGUI 2.0.0, OpenXR 1.16.1.
- Existing light prefab and NetworkLightSource.cs remain unchanged.
- No existing plan file or Assets/Plans directory was present when planning started.

## Implementation approach
Two viable face-rendering approaches:
1. Separate child face renderers: simple visual targeting but adds geometry and can produce z-fighting against existing cube faces.
2. A six-submesh cuboid with per-face material slots: slightly more mesh setup but preserves the original wall shape/collider and makes each face independently colorable without overlays. Recommended implementation.

Keep existing local room generation, assign deterministic wall IDs and local-face IDs, and replicate face-color state through one scene-placed room painting NetworkBehaviour. This avoids replacing all generated geometry with network prefabs. Server-owned state must be retained for late joiners; RPC-only broadcast is insufficient.

## Create
- Assets/Scripts/VRWallPainter.cs: owner-only NetworkBehaviour on the VR player; selected color, ray targeting, input subscription lifecycle, server paint request, and UI/interaction suppression.
- Assets/Scripts/VRColorWheel.cs: uGUI pointer/drag color conversion and selected-color event; wheel texture generated once and cleaned up if generated at runtime.
- Assets/Scripts/PaintableWall.cs: stable wall ID, six local face IDs, hit-normal-to-face mapping, per-face renderer updates.
- Assets/Scripts/WallFaceMeshUtility.cs: build a cube mesh with six independently assigned submeshes, correct UVs, normals, winding, and bounds.
- Assets/Scripts/NetworkWallPaintState.cs: server-authoritative retained color state keyed by wall ID and face ID, plus local generated-wall registration and replay.
- Assets/Prefabs/VRColorPalette.prefab: world-space Canvas, disk, marker, brightness slider and swatch.
- Assets/Materials/WallPaint.mat: URP Lit base material compatible with existing wall rendering.
- Assets/Tests/EditMode/WallPaintingTests.cs and suitable test assembly configuration if needed; Play Mode integration tests where supported.

## Modify
- Assets/Scripts/SpawnWalls.cs: explicitly identify/register the four walls with stable IDs, attach PaintableWall, and replace their rendering mesh with six-submesh geometry while preserving transforms and BoxColliders. Do not make the ceiling or floor paintable by default; preserve PCInvisible behavior.
- Assets/Prefabs/XR Origin (XR Rig).prefab: attach/wire VRWallPainter and palette under the left controller, reference right interactor/trigger input, and integrate owner-only visibility/input.
- Assets/Scenes/SampleScene.unity: scene-placed NetworkObject and NetworkWallPaintState on a dedicated object; configure the existing EventSystem for XR UI with exactly one active input module while retaining desktop mouse behavior required by current UI.
- Assets/Scripts/PlayerLocalSetup.cs only if existing serialized component controls cannot handle palette visibility; prefer self-contained owner checks in VRWallPainter and keep changes minimal.

Dependency analysis: AssetDatabase.GetDependencies was used for the scene and VR prefab because a dedicated dependency tool was not available in this planning toolset. Before implementation edits, repeat dependency/dependent impact checks for each modified asset with the available dependency tools or AssetDatabase APIs, including scene references and network prefab lists. Do not alter Packages or Starter Assets source.

# Implementation Steps
1. Verify exact APIs and interaction bindings against installed package code/documentation; inspect prefab dependents, EventSystem behavior, input action ownership, controller ray hit APIs, UI press dispatch timing, and network prefab registration. Confirm mouse support in XRUIInputModule before replacing the current module. Dependencies: none.
2. Implement WallFaceMeshUtility.cs and PaintableWall.cs; define stable local-face ordering (+X, -X, +Y, -Y, +Z, -Z) and wall IDs 0–3. Preserve existing shared material assets; use controlled per-wall material instances or a fixed per-face material set with proper cleanup, never mutate shared assets. Verify outward winding and face-only color isolation. Dependencies: Step 1.
3. Extend SpawnWalls.cs to register four walls after creation and exclude ceiling/floor. Preserve the public SpawnCuboid signature and existing callers; use explicit room registration so arbitrary future cuboids are not paintable automatically. Dependencies: Step 2.
4. Implement retained network room state. Initialize server state for 24 faces using a NetworkList or equivalent serializable retained collection. Subscribe on spawn and replay state when local wall generation is ready; handle either startup order. Clear subscriptions on despawn and manage network collection lifetime. Dependencies: Steps 2–3.
5. Implement VRWallPainter.cs. Gate actions to spawned owning VR players. Route paint requests through the owning player NetworkBehaviour using supported NGO RPCs to the server; validate sender ownership, VR role, wall/face IDs, finite opaque color channels, reach, and plausible hit data against server room geometry. Use server-side ray validation and tracked-pose tolerance appropriate to this project's actual transform authority, not blind trust in client wall IDs. Rate-limit applications; server orders simultaneous edits, latest accepted edit wins. Dependencies: Steps 1 and 4.
6. Implement VRColorWheel.cs and VRColorPalette.prefab. Generate/calculate HSV disk consistently, support pointer press/drag and brightness changes, place marker/swatches, wire selection to the painter. Hide personal palette and disable input on nonowners. Dependencies: Steps 1 and 5.
7. Wire VR prefab and scene. Configure tracked-device raycasting and XR UI input with exactly one active module. Ensure painting checks UI consumption after input dispatch so a shared trigger cannot paint through UI; use a deferred per-frame request gate if needed. Preserve locomotion, teleport, grabbing, network UI, PC camera, and existing light controls. Dependencies: Steps 4–6.
8. Compile, run unit/integration tests, capture and inspect runtime wrist UI, and validate with PC host plus VR client/simulator and late joiner. Fix only observed issues; distinguish automated input-test failures from actual controller failures. Dependencies: Steps 1–7.

# Verification & Testing
- EditMode: HSV samples and boundary handling; hue wrap; brightness=0 yields black, saturation=0 yields grayscale; color alpha stays 1.
- Geometry: six face slots; normals/UVs/bounds correct; exactly one face changes; adjacent wall, reverse side, and edges remain independently colored. Repeat with rotated/scaled cuboids.
- Targeting: ignore ceiling, floor, UI, nonpaintable surfaces and empty space; obey maximum reach; block painting during grab/teleport and while pointer is over palette.
- Ownership: owner palette is visible and functional; PC/remote VR avatars cannot operate another player's painter; no action subscription leaks after reconnect/despawn.
- Network: host and VR see identical accepted colors; initial state applies whether geometry or network state arrives first; a late joiner receives all existing colors; simultaneous edits are ordered by server; invalid IDs/colors/range and unauthorized sender are rejected.
- Material lifecycle: painting does not recolor shared materials or unrelated walls; bounded mesh/material/texture allocation, proper destruction, no per-frame material instantiation.
- UI: wheel can be selected and dragged by actual XR ray; slider works; selection never paints behind the UI; comfortable wrist placement and readable swatch in headset stereo. Capture runtime Game View and obtain hardware confirmation where simulator cannot establish comfort.
- Regression: room dimensions/colliders, PCInvisible ceiling, existing host/client controls, teleport/grab/locomotion, and network light remain operational.
- Compile in the current editor; run tests using Unity Test Runner. Perform PC standalone development build smoke check if build tools and runtime environment are available. Explicitly report any untested hardware or multi-process behavior.

Out of scope: brush textures, decals, undo/history, saving colors across sessions, painting ceiling/floor, persistence services, new networking topology, package upgrades, and changing existing lighting.
