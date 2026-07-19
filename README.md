# CrossHop

An endless *Crossy Road / Frogger*-style arcade hopper for mobile (iOS/Android), built in **Unity 6 LTS** with **C#** and **URP**. The retention hook is a large, data-driven **collectible character roster** (gacha prize machine) — most characters cosmetic, a few with small passive abilities.

> Status: engineering scaffold. Core gameplay/meta systems are written and organized; scenes, art, and prefabs are assembled inside the Unity Editor.

---

## Getting started

### Prerequisites
- **Unity 6 LTS** (`6000.0.x`) via Unity Hub. The exact version is pinned in `ProjectSettings/ProjectVersion.txt`.
- **Git LFS** — required before committing any binary art/audio:
  ```bash
  brew install git-lfs   # macOS
  git lfs install
  ```
  `.gitattributes` is already configured to route models/textures/audio through LFS.

### Open the project
1. Unity Hub → **Add project from disk** → select this folder.
2. Unity generates `Library/`, `.meta` files and the remaining `ProjectSettings/` on first import (these are git-ignored).
3. Packages in `Packages/manifest.json` (URP, Input System, Cinemachine, TextMeshPro) restore automatically.

### Build the gray-box prototype (M1)
The fastest path to pressing **Play** — three menu clicks, no manual wiring:

1. **Tools ▸ CrossHop ▸ Build Full Roster** (or *Create Sample Content*) — generates the worlds/characters so there's a world to load.
2. **Tools ▸ CrossHop ▸ Build Gray-box Scene** in a new empty scene — spawns the player, systems, camera, light, generated gray-box prefabs (lane/obstacle/coin), a HUD (score, coins, game-over + retry), and a bootstrap that auto-starts a run. Everything is wired.
3. Press **Play** — hop with WASD/arrows in the editor. Lanes are colour-coded by type, coins spawn on grass, and the game-over screen shows score/best/coins with a Retry button.

> If HUD text is invisible on first run, accept Unity's **Import TMP Essentials** prompt.

---

## Architecture

Everything spawned is **object-pooled** (no `Instantiate`/`Destroy` mid-run). All content — characters, lanes, difficulty — is **authored as ScriptableObjects**, so designers add content without touching code. Systems communicate via **C# events**, not direct references.

```
Assets/CrossHop/Scripts/
├─ Core/         GridSettings (tunable grid) · ObjectPool
├─ Input/        InputReader (swipe on device, WASD in editor → hop events)
├─ Gameplay/     LaneType · LaneDefinition · DifficultyCurve · WorldTheme
│                MovingObstacle · Lane · LaneGenerator (world-driven, streams & recycles)
│                PlayerController (grid hops, death, log-riding)
│                CameraFollow (forward death-scroll) · GameManager (run flow)
├─ Characters/   CharacterData (→ modelPrefab, thumbnail, defaultWorld) · CharacterDB
│                Rarity · CharacterAbility (+ AbilityContext) · Abilities/DoubleCoinsAbility
├─ Economy/      SaveData · SaveSystem (atomic local JSON) · EconomyManager
└─ Meta/         GachaController (rolls) · CharacterSelectionController (select/buy/roll flow)

Assets/CrossHop/Editor/
├─ RosterBuilder           Tools ▸ CrossHop ▸ Build Full Roster (20 characters)
├─ GrayboxSceneBuilder     Tools ▸ CrossHop ▸ Build Gray-box Scene
├─ SampleContentBuilder    Tools ▸ CrossHop ▸ Create Sample Content (Farmland + Cluck)
├─ CharacterThumbnailBaker Tools ▸ CrossHop ▸ Bake Character Icons
└─ VoxelAssetPostprocessor auto-applies crisp-voxel import settings to Art/Models

Assets/CrossHop/Art/
├─ Models/       voxel sources (.vox/.fbx, LFS)   ├─ Characters/  CharacterData + CharacterDB
├─ Prefabs/      3D character prefabs             └─ Worlds/       WorldTheme + LaneDefinitions
└─ Thumbnails/   baked 2D menu sprites (generated)
```

Two assemblies (`CrossHop.Runtime`, `CrossHop.Editor`) keep compile times low and editor-only code out of builds.

### Data flow (source of truth → run)
`SaveData` → **`EconomyManager`** (runtime authority for wallet / collection / selection) → events.
Selecting a character stores its id; at run start `GameManager` reads the selected `CharacterData`,
follows `defaultWorld` to a **`WorldTheme`**, and calls `LaneGenerator.Configure(world)`. The generator
streams that world's lane set. **The character you pick decides the world you play** — no code branches per world.

### User flows
`CharacterSelectionController` exposes the three player actions over the data layer — **select** (equip an
owned character), **buy** (spend coins to unlock), **roll** (prize machine; new unlocks, duplicates refund) —
each raising events the menu UI binds to. `GameManager` drives run state (Menu → Playing → GameOver) and
`Restart()`. UI never touches save data directly.

### Abilities & field coins
Abilities follow the **definition ↔ runtime** split: `CharacterAbility` is a read-only ScriptableObject
(config + `CreateRuntime()`), and per-run mutable state lives in a fresh `AbilityRuntime` created each run
and discarded on death — never on the shared asset. Runtimes hook `Tick`, `ModifyCoinReward`,
`TryAbsorbDeath` (player survives with brief invulnerability) and `ModifyHopProfile`. Shipping abilities:
**Low-G Hop** (Eileen), **Fireproof** (Blaze), **Coin Magnet** (Pixel), plus a Double Coins sample.
`CoinField` spawns pooled collectible `Coin`s on safe lanes; the magnet pulls them via `PullToward`.

### Character asset pipeline (3D voxel → 2D sprite)
Each character is one `CharacterData` asset holding both representations:
`modelPrefab` (3D voxel prefab, in-game) and `thumbnail` (2D sprite, menu). **Don't hand-draw the menu
icons** — drop the prefab in, run **Bake Character Icons**, and the sprite is rendered from the prefab at a
fixed ¾ angle and assigned automatically, so the two never drift. Voxel models imported into `Art/Models`
get Point-filter / uncompressed settings applied automatically.

## Adding a character
1. `Create ▸ CrossHop ▸ Character`, set a **stable `id`** (never change once shipped), name, rarity, model, cost.
2. (Optional) attach an ability asset for a gameplay twist — keep it small, passive, readable.
3. Add it to the `CharacterDB` roster asset.

## Roadmap
- **M1 — Gray-box:** cube player, one road lane, hop → die → restart. *(Is the feel right?)*
- **M2 — Vertical slice:** all lane types, streaming, coins, 3 characters, one gacha roll. *(Is it fun?)*
- **M3 — Content + meta:** scale roster, tune difficulty, ads/IAP, analytics.
- **M4 — Soft launch:** one small market → tune retention → global.

## Tech decisions
| Concern | Choice |
|---|---|
| Engine / language | Unity 6 LTS · C# · URP |
| Art | Voxel/low-poly (MagicaVoxel / Blender) |
| Persistence | Local JSON (`SaveSystem`) — no backend at launch |
| Ads / IAP | Unity LevelPlay / AdMob + Unity IAP *(M3)* |
| Analytics | GameAnalytics or Unity Analytics *(M3)* |
| Source control | Git + Git LFS |
