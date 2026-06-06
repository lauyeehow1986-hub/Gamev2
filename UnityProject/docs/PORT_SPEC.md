# SG Pathway → Unity — Port Spec (foundation findings)

Derived from a parallel read-only analysis of the web source
(`Game-source/src/lib/*`) vs the existing Unity C# (`Assets/SGPathway/*`),
2026-06-06. These findings are **renderer-agnostic** — they hold whether the
final render is 2D or 3D, because the data / engine / staging layers are shared.
The 3D rendering layer is the only part still pending design decisions (§4).

## 1. Status: the foundation is faithful

- **Stage geometry** (`Stage.cs`): EXACT port of the TS source.
  `STAGE_W=480, STAGE_H=270, HORIZON_Y=150`; `defaultStagePos` and `depthScale`
  are line-for-line identical. No fixes needed.
- **Engine + staging** (`BeatsCalculator`, `StageComputer`, `WalkthroughPlayer`):
  algorithmically faithful. `beatsAt`, lead selection, `chapterActorOrder`,
  painter's sort, branch resolution, canonical traversal, and `locateInCanonical`
  all reproduce the TS logic including edge cases.

## 2. Phase 0 — must-fix before content import

### Test infrastructure (P1 — blocks all tests)
1. No EditMode test assembly exists. Create
   `Assets/SGPathway/Tests/Editor/SGPathway.Tests.asmdef` (refs `SGPathway.Runtime`,
   `Unity.Localization`, test framework; Editor-only; nunit precompiled ref).
2. SO fields are all `private [SerializeField]` with read-only getters → tests
   can't build fixtures. Add test-only construction:
   `[assembly: InternalsVisibleTo("SGPathway.Tests")]` + internal factory/setters,
   OR a `TestWalkthroughBuilder` using SerializedObject/reflection.
   **This is the #1 Phase-0 enabler.**

### Data-model gaps (P1 — content won't resolve without these)
3. **Actor-identity bug (highest risk).** `beat.actorId` references the actors-MAP
   KEY, not `actor.id`. In the STROKE pathway these diverge (key `patient` → id
   `stroke-patient`, etc.). `ActorSO` has no key field. FIX: add `ActorSO.Key`;
   resolve `Beat.actor` by key; add `WalkthroughSO.FindActorByKey`. Importer must
   de-dup actors by key (one shared `ActorSO` per key) so the
   `Dictionary<ActorSO,Beat>` grouping matches the TS by-id grouping.
4. **Enum string→value maps** the importer must use (raw `Enum.Parse` fails):
   ActorTeam `first-responder`→FirstResponder, `ed`→ED, `cath`→Cath;
   BeatPose `cpr`→Cpr; SceneKind `mrt`→Mrt; Showpiece svg-id fold
   (`stent-deployment`→StentDeployment, `mri-bore-slide`→MriBoreSlide,
   `aed-shock`→AedShock, `thrombectomy-pass`→ThrombectomyPass).
5. **swatch**: TS optional hex string → C# `Color`. Parse with
   `ColorUtility.TryParseHtmlString`.
6. **"Absent" sentinels**: wire `hasBranchPoint` (true only when branchPoint
   present), `SceneKind.Unspecified` (scene omitted), `ShowpieceKind.None`
   (no showpiece). Renderer treats None/Unspecified as "nothing".
7. `Beat.focus` is dead data (declared, never used) — keep for forward-compat.

### Correctness hardening (P2/P3)
8. Determinism: make painter's sort stable (tie-break by actor id) and lead
   selection tie-break explicit (equal `at` → lower id), so C#
   Dictionary / `List.Sort` nondeterminism can't diverge from the TS engine.
9. Validation: importer asserts every `nextChapter` / `defaultNextChapter` is a
   member of `walkthrough.chapters`, and that chapter ids + actor keys are unique.

## 3. Content pipeline (Phase 1)

- `tools/export-walkthroughs.mjs`: load `walkthrough-{stemi,stroke}.ts` (type-only
  imports → headless eval via tsx/esbuild), emit one `_source.json` per pathway to
  `Assets/SGPathway/Content/Walkthroughs/<id>/_source.json`.
- JSON design: actors + chapters as ARRAYS (JsonUtility-friendly); promote the map
  key to an explicit `key` field (distinct from inner `id`); id cross-refs as
  strings with a `…Ref` suffix (`startChapterRef`, `defaultNextChapterRef`,
  `nextChapterRef`, `actorRef`); omit optional fields when absent (importer applies
  TS defaults: pose=Stand, expression=Neutral, direction=S). Header carries
  `stageUnits {w:480,h:270}`.
- `WalkthroughImporter` (currently a stub): two-pass bake — (1) create all
  ChapterSO/ActorSO, index by key; (2) wire references + beats. Idempotent (stable
  GUIDs). Seed LocalizedStrings with the English literal.
- Content-integrity tests (port after import): STEMI/Stroke start at `collapse`;
  every beat → known actor; every branch/default target resolves; beats within
  [0,duration]; thrombectomy ships `thrombectomy-pass`; every clinical chapter
  stages a patient.

## 4. 3D rendering — pending decisions

Data/engine/staging carry over untouched; only the renderer / scenery / actor-view
layer is new for 3D. Stage→3D mapping (verified): `x→worldX`, `y→worldZ`
(depth; y=150 far → y=270 near), feet on a `Y=0` floor; `depthScale` is retired
once a real perspective camera is in place.

OPEN (await user): render pipeline (HDRP vs URP), character-asset source,
environment approach, slice-first sequencing. The 12 scenes already have per-scene
3D-rebuild descriptions + mood grading cues captured from `scenery.tsx`
(kopitiam, street, mrt, resus, cathlab, imaging, counsel, ward, pharmacy, rehab,
clinic, backhouse).
