# SG Pathway → Unity port plan

Scope: **the v9.4 → v9.14.1 walkthrough cinematic only.** Campaigns,
exams/OSCE, branching cases, vitals, analytics, peer review, etc. are
out of scope for this milestone (see `docs/SUMMARY.md` in the source
repo for the full feature inventory).

## Source → target mapping

| Source (TS / React / Phaser)                       | Target (Unity 6 / C#)                                            |
| ---                                                | ---                                                              |
| `src/lib/walkthrough.ts` (types + traversal)       | `Runtime/Data/*SO.cs` + `Runtime/Engine/BeatsCalculator.cs`      |
| `src/lib/walkthrough-stemi.ts` (chapter data)      | `Content/Walkthroughs/Stemi/*.asset` (baked from JSON)           |
| `src/lib/walkthrough-stroke.ts`                    | `Content/Walkthroughs/Stroke/*.asset`                            |
| `src/lib/walkthrough-staging.ts`                   | `Runtime/Staging/StageComputer.cs` (1:1 port, already in place)  |
| `src/lib/scenery.tsx` (stage geom + 11 SVG scenes) | `Runtime/Staging/Stage.cs` (constants) + pre-baked PNG backdrops |
| `src/lib/showpieces.tsx`                           | `Runtime/Renderer/ShowpieceView.cs` (TODO) + URP overlay shader  |
| `src/lib/sprite-generator.tsx` (deterministic SVG) | Sprite atlas baked once + `Runtime/Renderer/ActorView.cs`        |
| `src/game/walkthrough/WalkthroughScene.ts` (Phaser)| `Runtime/Renderer/WalkthroughRenderer.cs`                        |
| `src/ui/modals/WalkthroughModal.tsx`               | `Scenes/Walkthrough.unity` + `Runtime/UI/*`                      |
| `src/ui/modals/WalkthroughPhaserStage.tsx`         | `Runtime/Renderer/WalkthroughRenderer.cs`                        |
| `src/ui/modals/ShowpieceOverlay.tsx`               | `Runtime/UI/ShowpieceOverlay.cs` (TODO)                          |
| `public/walkthrough/scenes/*.png` (12 baked HD)    | `Content/Scenes/*.png` (drop in as-is, configure as Sprite)      |
| `src/lib/i18n/{en,zh,ms,ta}.ts`                    | Unity Localization String Tables `Locales/{en,zh,ms,ta}`        |

## Project layout

```
UnityProject/
├── Assets/
│   ├── SGPathway/
│   │   ├── Runtime/                       ← code under SGPathway.Runtime asmdef
│   │   │   ├── Data/                      ← WalkthroughSO, ChapterSO, ActorSO, Beat, enums
│   │   │   ├── Engine/                    ← BeatsCalculator, WalkthroughPlayer
│   │   │   ├── Staging/                   ← Stage, StagedFigure, StageComputer
│   │   │   ├── Renderer/                  ← ActorView, WalkthroughRenderer, SceneryView
│   │   │   └── UI/                        ← TimelineScrubber, BranchOverlay
│   │   ├── Editor/                        ← WalkthroughImporter + asset menu helpers
│   │   └── Content/
│   │       ├── Walkthroughs/{Stemi,Stroke}/   ← *.asset (SOs) + _source.json (intermediate)
│   │       ├── Actors/                        ← shared ActorSO assets
│   │       └── Scenes/                        ← scenery PNGs from the web bake
│   └── Scenes/{Bootstrap,Walkthrough}.unity
├── Packages/manifest.json                ← Cinemachine, Localization, URP, Input System, 2D
├── ProjectSettings/                       ← Unity 6000.0 LTS
└── docs/
    ├── PORT_PLAN.md                       ← this file
    └── SETUP.md                           ← Unity Hub + MCP wiring + Editor open instructions
```

## Content port pipeline

The chapter data in `walkthrough-stemi.ts` / `walkthrough-stroke.ts` is
plain declarative TS — perfect for an offline export step. We avoid
hand-translating ~1300 lines of TypeScript by going through a JSON
intermediate:

1. `gamev2/tools/export-walkthroughs.mjs` — Node script (not written
   yet). Reads the TS files via `esbuild --bundle --format=cjs` (or
   `tsx`), evaluates the `stemiWalkthrough` / `strokeWalkthrough`
   exports, and writes:
   ```
   UnityProject/Assets/SGPathway/Content/Walkthroughs/Stemi/_source.json
   UnityProject/Assets/SGPathway/Content/Walkthroughs/Stroke/_source.json
   ```
2. `WalkthroughImporter` (in `Editor/`) bakes each JSON → one
   `WalkthroughSO.asset` + N `ChapterSO.asset` + M `ActorSO.asset`.
   Cross-references (Actor by id, defaultNextChapter, branch options)
   are resolved by id within the import pass.
3. LocalizedString fields are seeded with the English literal on first
   import (since the source is English-authored per SUMMARY.md
   v9.12 hold). Translated tables (`zh`, `ms`, `ta`) are linked later
   when native review unblocks them.

This pipeline is idempotent — re-running it overwrites the .asset files
in place without breaking scene references, because each baked SO keeps
the same GUID across imports.

## Rendering decisions

- **2D first, 3D optional.** The web stage is 480×270 normalised. The
  Unity renderer projects this to world space via
  `worldUnitsPerStageUnit` (default 0.01), keeping all existing beat
  positions valid. A 3D Cinemachine setup can be added behind the same
  `WalkthroughRenderer` event surface later.
- **Scene backdrops are sprites.** The 12 baked 1440×810 PNGs from
  `Game-source/public/walkthrough/scenes/` drop straight in via
  `SceneryView` + `SpriteRenderer`. v9.14.1's per-scene mood (warm /
  daylight / sterile / surgical / transit / industrial) ports to a URP
  Volume Profile per `SceneKind`.
- **Speaker focus + halo.** v9.8.1's pulsing ring is a single `ActorView`
  child object with a URP Sprite shader, driven by `IsLead`.
- **Speech band.** Top-broadcast band from v9.8.1 ports to a Canvas
  pinned to the top of the screen, fed by the lead actor's
  `beat.action` LocalizedString.
- **Showpieces.** v9.11 / v9.13 inline SVG showpieces (`stent-deployment`,
  `mri-bore-slide`, `aed-shock`, `thrombectomy-pass`) become a small set
  of Animator-driven prefabs in `Runtime/Renderer/Showpieces/`. External
  MP4 showpieces hit Unity's `VideoPlayer`.

## What this skeleton does NOT do yet

These are the open work items, in priority order:

1. **Content port (P0).** Write `tools/export-walkthroughs.mjs` + fill in
   `WalkthroughImporter.Import`. Until then there are no playable
   walkthroughs — engine code runs against empty SOs.
2. **Sprite rig (P0).** Bake the v9.7–v9.8 sprite system into a Unity
   sprite atlas: 7 poses × 7 expressions × 4 directions × N actors. The
   web `sprite-generator.tsx` is deterministic so we can render every
   permutation offline and commit a single atlas. `ActorView` already
   exposes the right Animator parameters; it just needs an Animator
   Controller wired to the atlas frames.
3. **Showpiece overlays (P1).** Port the four inline SVG showpieces to
   Animator-driven prefabs.
4. **Cinematic mood (P1).** URP Volume profile per `SceneKind` matching
   v9.14.1.
5. **Localization (P2).** Wire `com.unity.localization` to the four
   `Locales/{en,zh,ms,ta}` tables. Chinese case-content has full parity
   in the source; `ms`/`ta` walkthrough content is held pending native
   review (per SUMMARY.md), so don't ship those tables until cleared.
6. **Unit tests (P2).** Mirror `walkthrough.test.ts`,
   `walkthrough-staging.test.ts` under `Tests/EditMode/` so the engine
   port stays correct as it evolves.

## Validation gate before any content port

Before sinking time into the JSON exporter, open the project in Unity
once and verify:

- All `.asmdef` files resolve (no compile errors).
- `WalkthroughPlayer` shows in the Inspector with empty refs.
- `Create → SG Pathway → Walkthrough / Chapter / Actor` menu items appear.

If those three are green, the engine layer is correct and content port
is unblocked.
