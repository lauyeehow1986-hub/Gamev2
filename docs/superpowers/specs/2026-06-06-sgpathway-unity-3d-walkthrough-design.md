# SG Pathway — Unity 6 HDRP 3D Walkthrough (Design Spec)

Date: 2026-06-06 · Status: for review · Supersedes the 2D port framing in
`UnityProject/docs/PORT_PLAN.md`. Foundation findings: `UnityProject/docs/PORT_SPEC.md`.

## Summary

Rebuild the SG Pathway **walkthrough cinematic** (STEMI + Stroke patient journeys)
as a real-time **3D reimagining** in Unity 6 (6000.4.10f1) on **HDRP**, driven via
the Unity MCP. Quality target: a polished, cinematic, showreel-grade result — *not*
a faithful 2D port. Built **slice-first**: one hero scene (the resus bay) with the
patient + 2–3 hero clinicians taken to the full quality bar, validated in-editor,
then scaled to all scenes/actors and both pathways.

The verified renderer-agnostic foundation (data / engine / staging) is reused
unchanged; only the rendering layer is new.

## Scope

**In:** the v9.4→v9.14.1 walkthrough — chapters, beats, branch decisions,
scrubbable timeline, two pathways (STEMI, Stroke), 12 environments, the full actor
cast, the 4 showpieces, per-scene mood — reimagined in 3D.

**Out:** the rest of SG Pathway (campaigns, exams/OSCE, branching cases, vitals,
analytics, peer review, case authoring). English-first; localization structure kept
but zh/ms/ta walkthrough content deferred (native-review hold per SUMMARY v9.12).

## Reused (verified faithful — see PORT_SPEC.md)

- **Data:** `WalkthroughSO / ChapterSO / ActorSO / Beat / enums` (with Phase-0 fixes).
- **Engine:** `WalkthroughPlayer`, `BeatsCalculator` (traversal, time, branch pauses,
  canonical timeline, scrub).
- **Staging:** `Stage / StageComputer / StagedFigure` (figure placement; exact port
  of the source geometry).

Event-driven: the 3D renderer subscribes to the same `WalkthroughPlayer` events as
any other renderer would.

## New (3D / HDRP)

- **Render pipeline:** switch Built-in/URP → **HDRP**; **Linear** color space; an
  HDRP Global Volume (exposure, ACES tonemapping, bloom, depth-of-field, contact
  shadows, GI). Per-`SceneKind` grading to echo the source moods
  (warm / daylight / sterile / surgical / transit / industrial).
- **Camera & edit:** **Cinemachine 3** rig + **Timeline** for shot language
  (push-ins, over-the-shoulder on the lead actor, reveals), triggered by
  beat/chapter events. The 2D `depthScale` is retired — a real perspective camera
  provides foreshortening.
- **Characters:** realistic humanoids from a premium source (Character Creator 4
  and/or Mixamo), rigged (AccuRIG / Mixamo / mesh2motion), with HDRP skin/cloth
  materials. Identity via the new `ActorSO.Key`. Outfits: scrubs, white coats,
  SCDF/paramedic, patient gown, public commuters. Animation: a curated library +
  hand-keyed hero beats (collapse, CPR, intubation, defib, PCI, thrombectomy,
  walking transitions). Beat `pose / expression / direction / walking` drive the
  Animator/playable layer.
- **Environments:** authored/procedural **3D rooms** for hero scenes, to a high bar,
  using the per-scene rebuild descriptions captured from `scenery.tsx`. Stage
  `(x,y) → world (x, z)`, feet on a `Y=0` floor; the downstage-centre action zone
  kept clear of props.
- **UI:** hybrid world + screen-space — top speech band (lead actor's `beat.action`),
  chapter chyron (`timeOfDay / location / title`), branch-decision overlay, and a
  scrubbable timeline. Diegetic where possible (e.g. monitors show a live ECG).
- **Showpieces:** the four procedural b-roll moments (`stent-deployment`,
  `mri-bore-slide`, `aed-shock`, `thrombectomy-pass`) as 3D set-pieces / Timeline
  sequences.

## Quality bar & dependencies (honest)

"Award-winning" is driven mostly by HDRP lighting + materials, character/animation
asset quality, and cinematography — not by tool choice. The gating dependency is
**asset sourcing on a locked-down machine** (CC4 install / Mixamo access / Asset
Store reachability). Slice-first exists precisely to validate the *achievable* bar
on one scene before committing to scale. Fallback if premium realistic humans prove
unobtainable: high-quality **stylized** 3D (still 3D, not 2D), decided after the slice.

## Phase 0 — foundation fixes (renderer-agnostic; full list in PORT_SPEC.md)

- ✅ Bridge connected (7891); clean compile.
- Add `ActorSO.Key`; resolve `Beat.actor` by key; add `WalkthroughSO.FindActorByKey`;
  importer de-dups actors by key (Stroke has key≠id).
- Enum string→value maps (`first-responder/ed/cath/cpr/mrt`, showpiece svg-id fold);
  `swatch` hex→`Color`; absent sentinels (`hasBranchPoint`, `SceneKind.Unspecified`,
  `ShowpieceKind.None`).
- Determinism guards (stable painter sort + explicit lead tie-break);
  branch/default-target + uniqueness validation.
- Test assembly + fixture hooks; port 11 engine + 1 staging EditMode tests.

## Phase 1 — content pipeline

- `tools/export-walkthroughs.mjs`: TS → JSON per pathway (type-only imports →
  headless eval via tsx/esbuild) → `Content/Walkthroughs/<id>/_source.json`.
- `WalkthroughImporter`: two-pass bake JSON → SOs (create + index by key, then wire
  references/beats), idempotent (stable GUIDs), English literals seeded.
- Content-integrity EditMode tests after import.

## Phase 2+ — the 3D build (slice-first)

1. HDRP switch + Linear + base Volume; Cinemachine/Timeline scaffold; `Walkthrough`
   scene.
2. **Hero slice:** resus-bay environment + patient + 2–3 clinicians (realistic,
   rigged, lit) + speech band + timeline scrub + one branch + camera language.
   Verify to the bar (screenshots + play-mode).
3. **Scale:** remaining STEMI scenes/actors → full STEMI; then Stroke; then
   showpieces + per-scene mood polish; optional standalone/WebGL build.

## Verification

EditMode tests (engine, staging, content integrity); MCP scene/game **screenshots**
each milestone; play-mode walkthrough runs. Nothing is "done" until verified in-editor.

## Risks

- **Asset sourcing on a locked-down box** (primary).
- HDRP setup/perf; Gamma→Linear shift (no existing visuals to disturb yet).
- Production scale (≈30–48 actors, 12 scenes, 2 pathways) — mitigated by slice-first
  + reuse of the verified foundation.
- Auto-rig (mesh2motion/Mixamo) quality variance — assessed during the slice.
