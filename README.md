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
The fastest path to pressing **Play**:

1. Open a new empty scene (`File ▸ New Scene ▸ Basic`).
2. Run **Tools ▸ CrossHop ▸ Build Gray-box Scene**. This spawns a cube player, the lane generator, camera and managers, and wires all serialized references.
3. Create the content assets it needs (right-click in Project ▸ *Create ▸ CrossHop ▸ …*):
   - one **Lane Definition** of type *Safe* and one of type *Road*,
   - a **Difficulty Curve**,
   - a **Lane** prefab (empty GameObject + `Lane` component),
   - a **MovingObstacle** prefab (cube + `MovingObstacle` component) referenced by the Road lane.
4. Assign those on the `LaneGenerator`, then trigger `GameManager.StartRun()` (a temporary UI button or bootstrap script).

---

## Architecture

Everything spawned is **object-pooled** (no `Instantiate`/`Destroy` mid-run). All content — characters, lanes, difficulty — is **authored as ScriptableObjects**, so designers add content without touching code. Systems communicate via **C# events**, not direct references.

```
Assets/CrossHop/Scripts/
├─ Core/         GridSettings (tunable grid) · ObjectPool
├─ Input/        InputReader (swipe on device, WASD in editor → hop events)
├─ Gameplay/     LaneType · LaneDefinition · DifficultyCurve
│                MovingObstacle · Lane · LaneGenerator (streams & recycles)
│                PlayerController (grid hops, death, log-riding)
│                CameraFollow (forward death-scroll) · GameManager (run flow)
├─ Characters/   CharacterData · CharacterDB · Rarity
│                CharacterAbility (+ AbilityContext) · Abilities/DoubleCoinsAbility
├─ Economy/      SaveData · SaveSystem (atomic local JSON) · EconomyManager
└─ Meta/         GachaController (rarity-weighted rolls)

Assets/CrossHop/Editor/
└─ GrayboxSceneBuilder (Tools ▸ CrossHop ▸ Build Gray-box Scene)
```

Two assemblies (`CrossHop.Runtime`, `CrossHop.Editor`) keep compile times low and editor-only code out of builds.

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
