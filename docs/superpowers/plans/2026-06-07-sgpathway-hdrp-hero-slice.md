# SG Pathway — HDRP 3D Hero Slice (Resus Bay) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (or subagent-driven-development). Steps use `- [ ]` checkboxes. All `unity_*` calls pass `port: 7891`.

**Goal:** Stand up HDRP and produce a playable, scrubbable **3D resus-bay chapter** of the STEMI walkthrough — real 3D environment, a perspective Cinemachine camera, beat-driven figure staging, speech band, timeline scrub, and branch overlay — with **placeholder actors** that Character Creator 4 characters drop into next.

**Architecture:** Reuse the verified data/engine/staging layer. Switch the project to **HDRP** (Linear). Add 3D rendering components (`WalkthroughView3D`, `ActorView3D`) that consume `WalkthroughPlayer` events and map the 480×270 stage to world space (`x→X`, `y→Z`, feet on `Y=0`). Build the resus environment from primitives + HDRP materials. Camera language via Cinemachine; UI via uGUI + TMP (already referenced).

**Tech Stack:** Unity 6000.4.10f1, HDRP, Cinemachine 3, Input System, TMP, Unity MCP (port 7891). Characters: Character Creator 4 → FBX (rigged) → HDRP (gated; separate follow-up).

**Scope:** ONE scene (resus bay) playing the STEMI chapter whose `SceneKind == Resus`, with placeholder capsule-actors. **Out of scope here:** CC4 character import/rig (next step once assets exist), the other 11 scenes, Stroke, showpieces, per-scene mood grading, localization.

**Quality note:** "Award-winning" comes from HDRP lighting + materials + camera + (later) CC4 characters. This plan builds the lit, staged, playable shell; the character pass + lighting polish elevate it.

---

## File Structure

| File | Responsibility | Action |
| --- | --- | --- |
| `UnityProject/Assets/Settings/HDRP/*.asset` | HDRP pipeline + global settings + volume profile | Create (via MCP/HDRP) |
| `UnityProject/Assets/SGPathway/Runtime/Renderer/ActorView3D.cs` | One 3D figure: placeholder body + label + speaker ring; applies team colour, lead state | Create |
| `UnityProject/Assets/SGPathway/Runtime/Renderer/WalkthroughView3D.cs` | Subscribes to player; stages 3D figures from `StageComputer`; maps stage→world | Create |
| `UnityProject/Assets/SGPathway/Runtime/UI/SpeechBand.cs` | Top broadcast caption from lead actor's `actionText` | Create |
| `UnityProject/Assets/SGPathway/Scenes/Walkthrough.unity` | The playable scene | Create |
| `UnityProject/Assets/SGPathway/Content/Prefabs/ActorPlaceholder.prefab` | Placeholder actor prefab (CC4 swaps the visual child) | Create (via MCP) |

---

## Task A: HDRP foundation

**Files:** `Packages/manifest.json` (HDRP), `Assets/Settings/HDRP/*`.

- [ ] **A1 — Add the HDRP package.** Call `unity_packages_add({ name: "com.unity.render-pipelines.high-definition", port: 7891 })`. Wait for import (minutes; the bridge reclaims port 7891 after the domain reload). Then `unity_get_compilation_errors` → 0.
- [ ] **A2 — Configure HDRP + Linear.** Run the **HDRP Wizard**: `unity_execute_menu_item({ menuPath: "Window/Rendering/HDRP Wizard", port: 7891 })`, then via `unity_execute_code` invoke the wizard's "Fix All" (HDRP `HDWizard`/`HDProjectSettings`) OR script the setup:
  - Ensure `HDRenderPipelineGlobalSettings` exists; create an `HDRenderPipelineAsset` at `Assets/Settings/HDRP/HDRPAsset.asset`.
  - `GraphicsSettings.defaultRenderPipeline` + `QualitySettings.renderPipeline` = the HDRP asset.
  - `PlayerSettings.colorSpace = ColorSpace.Linear`.
  - **Fallback:** if scripting the HDRP asset is flaky, ask the user to click **Window ▸ Rendering ▸ HDRP Wizard ▸ Fix All** once.
- [ ] **A3 — Verify.** `unity_project_info` → `renderPipeline` is High-Definition, `colorSpace` Linear. Create a scratch scene with a Directional Light + a cube, `unity_screenshot_game` → cube renders lit (not magenta/black). Delete scratch scene.
- [ ] **A4 — Commit + push** (`Assets/Settings/**`, `Packages/manifest.json`, `Packages/packages-lock.json`).

## Task B: 3D rendering components (C#)

**Files:** Create `Runtime/Renderer/ActorView3D.cs`, `Runtime/Renderer/WalkthroughView3D.cs`, `Runtime/UI/SpeechBand.cs`.

- [ ] **B1 — ActorView3D.** Create `UnityProject/Assets/SGPathway/Runtime/Renderer/ActorView3D.cs`:
```csharp
using UnityEngine;
using SGPathway.Data;

namespace SGPathway.Renderer
{
    /// <summary>One staged figure in 3D. The `visual` child is a placeholder now;
    /// a CC4-rigged character replaces it later (same transform contract).</summary>
    [DisallowMultipleComponent]
    public sealed class ActorView3D : MonoBehaviour
    {
        [SerializeField] private Transform visual;        // body root (placeholder capsule / CC4 model)
        [SerializeField] private Renderer bodyRenderer;   // tinted by team swatch
        [SerializeField] private GameObject leadRing;     // speaker highlight
        [SerializeField] private TMPro.TMP_Text label;    // role nameplate

        public ActorSO Actor { get; private set; }
        private MaterialPropertyBlock _mpb;

        public void Bind(ActorSO actor)
        {
            Actor = actor;
            if (label != null) label.text = actor != null ? actor.RoleText : "";
            if (bodyRenderer != null && actor != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                bodyRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", actor.Swatch);
                bodyRenderer.SetPropertyBlock(_mpb);
            }
        }

        public void Apply(Beat beat, bool isLead)
        {
            if (leadRing != null) leadRing.SetActive(isLead);
            // pose/expression/direction drive an Animator once CC4 rigs are in (no-op for placeholder).
        }
    }
}
```
- [ ] **B2 — WalkthroughView3D.** Create `UnityProject/Assets/SGPathway/Runtime/Renderer/WalkthroughView3D.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using SGPathway.Data;
using SGPathway.Engine;
using SGPathway.Staging;

namespace SGPathway.Renderer
{
    /// <summary>Projects the 480×270 stage into 3D and stages one ActorView3D per figure.
    /// Stage x→world X, stage y→world Z (depth); feet on the Y=0 floor. No depthScale
    /// (a real perspective camera supplies foreshortening).</summary>
    [DisallowMultipleComponent]
    public sealed class WalkthroughView3D : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private ActorView3D actorPrefab;
        [SerializeField] private Transform stageRoot;
        [SerializeField] private float worldUnitsPerStageUnit = 0.05f;

        private readonly Dictionary<ActorSO, ActorView3D> _views = new Dictionary<ActorSO, ActorView3D>();
        private ActorSO _selected;

        private void OnEnable()
        {
            if (player == null) return;
            player.OnChapterEntered.AddListener(OnChapterEntered);
            player.OnTick.AddListener(OnTick);
        }
        private void OnDisable()
        {
            if (player == null) return;
            player.OnChapterEntered.RemoveListener(OnChapterEntered);
            player.OnTick.RemoveListener(OnTick);
        }

        private void OnChapterEntered(ChapterSO _)
        {
            foreach (var kv in _views) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _views.Clear();
        }

        private void OnTick(ChapterSO chapter, float t)
        {
            var active = BeatsCalculator.ActiveByActor(chapter, t);
            var staging = StageComputer.StageFigures(chapter, active, _selected);
            var live = new HashSet<ActorSO>();
            foreach (var f in staging.Figures)
            {
                live.Add(f.Actor);
                if (!_views.TryGetValue(f.Actor, out var v) || v == null)
                {
                    v = Instantiate(actorPrefab, stageRoot != null ? stageRoot : transform);
                    v.Bind(f.Actor);
                    _views[f.Actor] = v;
                }
                v.transform.localPosition = StageToWorld(f.X, f.Y);
                v.Apply(f.Beat, f.IsLead);
            }
            foreach (var kv in _views) if (kv.Value != null) kv.Value.gameObject.SetActive(live.Contains(kv.Key));
        }

        private Vector3 StageToWorld(float x, float y)
            => new Vector3((x - Stage.Width * 0.5f) * worldUnitsPerStageUnit, 0f,
                           (Stage.HorizonY - y) * worldUnitsPerStageUnit);
    }
}
```
- [ ] **B3 — SpeechBand.** Create `UnityProject/Assets/SGPathway/Runtime/UI/SpeechBand.cs`:
```csharp
using UnityEngine;
using SGPathway.Data;
using SGPathway.Engine;
using SGPathway.Staging;

namespace SGPathway.UI
{
    /// <summary>Top broadcast caption: shows the lead actor's current beat actionText.</summary>
    [DisallowMultipleComponent]
    public sealed class SpeechBand : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private TMPro.TMP_Text caption;
        [SerializeField] private TMPro.TMP_Text speaker;

        private void OnEnable() { if (player != null) player.OnTick.AddListener(OnTick); }
        private void OnDisable() { if (player != null) player.OnTick.RemoveListener(OnTick); }

        private void OnTick(ChapterSO chapter, float t)
        {
            var active = BeatsCalculator.ActiveByActor(chapter, t);
            var staging = StageComputer.StageFigures(chapter, active, null);
            if (staging.LeadActor != null && active.TryGetValue(staging.LeadActor, out var beat))
            {
                if (caption != null) caption.text = beat.actionText;
                if (speaker != null) speaker.text = staging.LeadActor.RoleText;
            }
        }
    }
}
```
- [ ] **B4 — Compile-check** (`Assets/Refresh` + `unity_get_compilation_errors` → 0). Commit + push.

## Task C: Walkthrough scene + camera + player wiring

- [ ] **C1 — Identify the resus chapter.** `unity_execute_code` to find the STEMI ChapterSO with `Scene == SceneKind.Resus` (load `stemi-pathway-v1`, scan `Chapters`); record its id. Use it as the slice's starting chapter.
- [ ] **C2 — New scene** `Assets/SGPathway/Scenes/Walkthrough.unity` (`unity_scene_new` then save). Add: Directional Light (key), an HDRP Global Volume, a Cinemachine Brain on the Main Camera, a `CinemachineCamera` framing the action zone (downstage centre), an EventSystem.
- [ ] **C3 — Player + views.** Create a `Walkthrough` GameObject with `WalkthroughPlayer` (assign the STEMI `WalkthroughSO`; set play-on-start, but start at the resus chapter via a small bootstrap or `playOnStart=false` + `Play(resusChapter)`), `WalkthroughView3D` (+ a `StageRoot` child), and a screen-space Canvas with `SpeechBand` + `TimelineScrubber` + `BranchOverlay`. Wire references via `unity_component_set_reference` / `unity_component_batch_wire`.
- [ ] **C4 — Verify** scene hierarchy via `unity_scene_hierarchy`; save scene. Commit + push.

## Task D: Placeholder actor prefab

- [ ] **D1 — Build prefab** `Assets/SGPathway/Content/Prefabs/ActorPlaceholder.prefab`: root with `ActorView3D`; child `visual` = a capsule (~1.7 world-units tall scaled by `worldUnitsPerStageUnit`) with an HDRP Lit material; a world-space `leadRing` (thin torus/quad) at feet; a billboarded TMP label above head. Assign the `ActorView3D` serialized fields. (Use `unity_gameobject_create`, `unity_component_add`, `unity_material_create`, `unity_asset_create_prefab`.)
- [ ] **D2 — Wire** the prefab into `WalkthroughView3D.actorPrefab` in the scene. Commit + push.

## Task E: Resus-bay environment (blockout, HDRP materials)

- [ ] **E1 — Build the room** under an `Environment` root, sized to the stage footprint (480×270 stage units × `worldUnitsPerStageUnit`): teal-walled bay — floor plane, back wall + two side walls, ceiling with light strips; props per `PORT_SPEC`/scene inventory: chrome **resus trolley** (centre, downstage), **vitals monitor** (right, emissive green ECG quad), **IV pole + drip** (left), a **curtain rail** with bay curtains. Primitives + a small set of HDRP Lit/Emissive materials (teal walls, chrome, white linen). Keep the downstage-centre action band clear.
- [ ] **E2 — Light it** for a clinical-sterile mood (cool key + soft fill, mild bloom + exposure in the volume).
- [ ] **E3 — Verify** `unity_screenshot_scene` + `unity_screenshot_game` — recognisable resus bay, properly lit. Iterate on materials/lights until it reads well. Commit + push.

## Task F: Play + scrub the chapter

- [ ] **F1 — Enter play mode** (`unity_play_mode`), let the resus chapter run; `unity_screenshot_game` mid-chapter → placeholder figures staged in the bay, speech band showing the lead actor's line.
- [ ] **F2 — Scrub** via the timeline (set slider / call `WalkthroughPlayer.Scrub` through `unity_execute_code`); screenshot at 2–3 offsets → figures restage correctly. If the chapter has a branch, verify the overlay appears and a pick advances.
- [ ] **F3 — Exit play mode.** Capture a hero screenshot. Commit + push.

---

## Done-when
- Project renders in **HDRP** (Linear); scratch + resus scenes render lit, no magenta.
- `Walkthrough.unity` plays the resus STEMI chapter: placeholder figures stage by beat (x→X, y→Z), speech band shows the lead's `actionText`, timeline scrubs, branch overlay works.
- 0 compile errors; foundation EditMode tests still green.
- Pushed to GitHub.

## Next (gated on assets)
- **CC4 characters:** import 3 rigged FBX (patient, doctor, nurse) → HDRP materials → swap the `visual` child in `ActorPlaceholder` (or per-actor prefab variants); wire pose/expression/direction to an Animator in `ActorView3D.Apply`. Then lighting/camera polish → scale to other scenes/chapters and Stroke.
