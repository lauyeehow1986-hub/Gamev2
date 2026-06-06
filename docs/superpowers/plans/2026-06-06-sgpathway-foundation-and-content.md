# SG Pathway — Foundation Fixes & Content Pipeline (Implementation Plan)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the STEMI + Stroke walkthrough content load correctly into Unity ScriptableObjects, with the data/engine/staging layer fixed and covered by EditMode tests — a verified, renderer-agnostic foundation the 3D build can sit on.

**Architecture:** Reuse the verified engine/staging C#. Fix the data model (actor key≠id, English-text companion fields, determinism). Stand up an EditMode test assembly. Add a dependency-free Node exporter that turns the source `walkthrough-{stemi,stroke}.ts` into JsonUtility-friendly JSON, and a two-pass `WalkthroughImporter` that bakes that JSON into `*.asset` ScriptableObjects (idempotent, stable GUIDs). Verify with ported engine tests + content-integrity tests.

**Tech Stack:** Unity 6000.4.10f1, C# (asmdef-based), Unity Test Framework (NUnit, EditMode), Node ≥18 (ESM), Unity MCP bridge (port **7891**) for compile/test/verify.

**Scope note:** This plan is the renderer-agnostic foundation only. The HDRP/3D rendering, characters, environments, camera, and UI are a **separate plan** written after this lands (see `docs/superpowers/specs/2026-06-06-sgpathway-unity-3d-walkthrough-design.md`). Localization tables are deferred (P2): English text is stored in plain companion fields now; the existing `LocalizedString` fields stay for the later localization pass.

**Conventions used in every Unity step:** pass `port: 7891` to every `unity_*` MCP call. "Compile-check" means `unity_get_compilation_errors({ severity: "error", port: 7891 })` and expecting `count: 0`. Commits use **scoped** `git add <specific paths>` (never `git add -A` — the repo has large untracked binaries and an extracted `Game-source/`).

---

## File Structure

| File | Responsibility | Action |
| --- | --- | --- |
| `UnityProject/Assets/SGPathway/Runtime/Properties/AssemblyInfo.cs` | Expose runtime internals to Editor + Tests asmdefs | Create |
| `UnityProject/Assets/SGPathway/Runtime/Data/ActorSO.cs` | Add `Key`, English `RoleText`/`BioText`, internal `Init` | Modify |
| `UnityProject/Assets/SGPathway/Runtime/Data/ChapterSO.cs` | Add `TitleText`, internal setters | Modify |
| `UnityProject/Assets/SGPathway/Runtime/Data/WalkthroughSO.cs` | Add `TitleText`, `FindActorByKey`, internal setters | Modify |
| `UnityProject/Assets/SGPathway/Runtime/Data/Beat.cs` | Add `actionText`; `BranchPoint.promptText`; `BranchOption.labelText/hintText` | Modify |
| `UnityProject/Assets/SGPathway/Runtime/Staging/StageComputer.cs` | Deterministic lead tie-break + stable painter sort | Modify |
| `UnityProject/Assets/SGPathway/Tests/Editor/SGPathway.Tests.asmdef` | EditMode test assembly | Create |
| `UnityProject/Assets/SGPathway/Tests/Editor/TestWalkthroughBuilder.cs` | In-memory fixtures for tests | Create |
| `UnityProject/Assets/SGPathway/Tests/Editor/EngineTests.cs` | Port of `walkthrough.test.ts` | Create |
| `UnityProject/Assets/SGPathway/Tests/Editor/StagingTests.cs` | Port of `walkthrough-staging.test.ts` (synthetic) + determinism | Create |
| `tools/export-walkthroughs.mjs` | TS → JSON exporter (dependency-free) | Create |
| `UnityProject/Assets/SGPathway/Editor/WalkthroughImporter.cs` | Two-pass JSON → SO bake | Replace (stub today) |
| `UnityProject/Assets/SGPathway/Editor/WalkthroughDTO.cs` | JsonUtility DTOs | Create |
| `UnityProject/Assets/SGPathway/Content/Walkthroughs/{Stemi,Stroke}/_source.json` | Baked JSON intermediates | Generated |
| `UnityProject/Assets/SGPathway/Tests/Editor/ContentIntegrityTests.cs` | Validate baked content | Create |

---

## Task 0: Pre-flight — make commits safe

**Files:** `UnityProject/.gitignore` (verify), repo root check.

- [ ] **Step 1: Confirm the git repo root and that `Game-source/` + big binaries are ignored**

Run:
```bash
git -C "C:/Users/lauye/Documents/gamev2" rev-parse --show-toplevel
git -C "C:/Users/lauye/Documents/gamev2" check-ignore -v Game-source Godot_v4.6.3-stable_win64.exe.zip "Game-claude-healthcare-pathway-game-X7oRQ.zip" 2>/dev/null || echo "NOT IGNORED"
```
Expected: a toplevel path prints. If the big files/`Game-source` are "NOT IGNORED", do Step 2; else skip to Step 3.

- [ ] **Step 2: Add ignore rules at the repo root (only if needed)**

Append to the repo-root `.gitignore` (path from Step 1):
```gitignore
# SG Pathway working files — never commit these
Game-source/
*.zip
Godot_v4.6.3-stable_win64.exe/
UnityProject/Library/
UnityProject/Temp/
UnityProject/Logs/
UnityProject/obj/
```

- [ ] **Step 3: Commit the spec + planning docs already written**

```bash
cd "C:/Users/lauye/Documents/gamev2"
git add docs/superpowers/specs/2026-06-06-sgpathway-unity-3d-walkthrough-design.md docs/superpowers/plans/2026-06-06-sgpathway-foundation-and-content.md UnityProject/docs/PORT_SPEC.md
git commit -m "docs: SG Pathway Unity 3D design spec, foundation plan, port spec"
```

---

## Task 1: Runtime data-model fixes (key, English text, internal hooks)

**Files:**
- Create: `UnityProject/Assets/SGPathway/Runtime/Properties/AssemblyInfo.cs`
- Modify: `Runtime/Data/ActorSO.cs`, `Runtime/Data/ChapterSO.cs`, `Runtime/Data/WalkthroughSO.cs`, `Runtime/Data/Beat.cs`

- [ ] **Step 1: Expose internals to Editor + Tests**

Create `UnityProject/Assets/SGPathway/Runtime/Properties/AssemblyInfo.cs`:
```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SGPathway.Editor")]
[assembly: InternalsVisibleTo("SGPathway.Tests")]
```

- [ ] **Step 2: ActorSO — add `key`, English text, internal `Init`**

Replace the body of `Runtime/Data/ActorSO.cs` with:
```csharp
using UnityEngine;
using UnityEngine.Localization;

namespace SGPathway.Data
{
    [CreateAssetMenu(menuName = "SGPathway/Actor", fileName = "Actor")]
    public sealed class ActorSO : ScriptableObject
    {
        [SerializeField] private string key;     // actors-map KEY — the identity beats reference
        [SerializeField] private string id;      // inner id (provenance; may differ from key in Stroke)
        [SerializeField] private string roleText; // English literal (P2: move to LocalizedString)
        [SerializeField] private LocalizedString role;
        [SerializeField] private ActorTeam team;
        [SerializeField] private string bioText;
        [SerializeField] private LocalizedString bio;
        [SerializeField] private Color swatch = Color.white;

        public string Key => key;
        public string Id => id;
        public string RoleText => roleText;
        public LocalizedString Role => role;
        public ActorTeam Team => team;
        public string BioText => bioText;
        public LocalizedString Bio => bio;
        public Color Swatch => swatch;

        internal void Init(string key, string id, string roleText, ActorTeam team, string bioText, Color swatch)
        {
            this.key = key; this.id = id; this.roleText = roleText;
            this.team = team; this.bioText = bioText; this.swatch = swatch;
        }
    }
}
```

- [ ] **Step 3: ChapterSO — add `titleText` + internal setters**

In `Runtime/Data/ChapterSO.cs`, add the field after `private LocalizedString title;`:
```csharp
        [SerializeField] private string titleText;
```
Add the getter after `public LocalizedString Title => title;`:
```csharp
        public string TitleText => titleText;
```
Add these internal methods inside the `ChapterSO` class (before the closing brace):
```csharp
        internal void InitMeta(string id, string titleText, SceneKind scene,
            float durationSec, string timeOfDay, string location)
        {
            this.id = id; this.titleText = titleText; this.scene = scene;
            this.durationSec = durationSec; this.timeOfDay = timeOfDay; this.location = location;
        }
        internal void SetBeats(System.Collections.Generic.List<Beat> b) => beats = b;
        internal void SetBranchPoint(BranchPoint bp) { branchPoint = bp; hasBranchPoint = bp != null; }
        internal void SetDefaultNext(ChapterSO c) => defaultNextChapter = c;
```

- [ ] **Step 4: WalkthroughSO — add `titleText`, `FindActorByKey`, internal setters**

In `Runtime/Data/WalkthroughSO.cs`, add after `private LocalizedString title;`:
```csharp
        [SerializeField] private string titleText;
```
Add after `public LocalizedString Title => title;`:
```csharp
        public string TitleText => titleText;
```
Add inside the class (before the closing brace):
```csharp
        public ActorSO FindActorByKey(string actorKey)
        {
            if (string.IsNullOrEmpty(actorKey)) return null;
            for (int i = 0; i < actors.Count; i++)
                if (actors[i] != null && actors[i].Key == actorKey) return actors[i];
            return null;
        }

        internal void InitMeta(string id, string titleText) { this.id = id; this.titleText = titleText; }
        internal void SetStartChapter(ChapterSO c) => startChapter = c;
        internal void SetChapters(System.Collections.Generic.List<ChapterSO> c) => chapters = c;
        internal void SetActors(System.Collections.Generic.List<ActorSO> a) => actors = a;
```

- [ ] **Step 5: Beat / BranchPoint / BranchOption — add English text fields**

In `Runtime/Data/Beat.cs`: add to `BranchOption`:
```csharp
        public string labelText;
        public string hintText;
```
add to `BranchPoint`:
```csharp
        public string promptText;
```
add to `Beat` (after the `action` field):
```csharp
        public string actionText;
```

- [ ] **Step 6: Compile-check via MCP**

Call `unity_get_compilation_errors({ severity: "error", port: 7891 })`.
Expected: `count: 0`. (Focus the Unity window first if it hasn't recompiled.)

- [ ] **Step 7: Commit**

```bash
cd "C:/Users/lauye/Documents/gamev2"
git add UnityProject/Assets/SGPathway/Runtime/Properties/AssemblyInfo.cs UnityProject/Assets/SGPathway/Runtime/Data/ActorSO.cs UnityProject/Assets/SGPathway/Runtime/Data/ChapterSO.cs UnityProject/Assets/SGPathway/Runtime/Data/WalkthroughSO.cs UnityProject/Assets/SGPathway/Runtime/Data/Beat.cs
git commit -m "feat(data): actor key, English text fields, internal bake/test hooks"
```

---

## Task 2: Determinism guards in StageComputer

**Files:** Modify `Runtime/Staging/StageComputer.cs`.

- [ ] **Step 1: Deterministic lead selection (tie-break on equal `at` by actor Id)**

Replace the lead loop (the `foreach (var kv in activeByActor)` block that sets `leadActor`) with:
```csharp
            foreach (var kv in activeByActor)
            {
                bool better = kv.Value.at > leadAt ||
                    (kv.Value.at == leadAt && leadActor != null &&
                     string.Compare(kv.Key.Id, leadActor.Id, System.StringComparison.Ordinal) < 0) ||
                    (kv.Value.at == leadAt && leadActor == null);
                if (better) { leadAt = kv.Value.at; leadActor = kv.Key; }
            }
```

- [ ] **Step 2: Stable painter sort (tie-break on equal Y by actor Id)**

Replace `result.Figures.Sort((a, b) => a.Y.CompareTo(b.Y));` with:
```csharp
            result.Figures.Sort((a, b) =>
            {
                int byY = a.Y.CompareTo(b.Y);
                if (byY != 0) return byY;
                return string.Compare(a.Actor.Id, b.Actor.Id, System.StringComparison.Ordinal);
            });
```

- [ ] **Step 3: Compile-check** — `unity_get_compilation_errors` → `count: 0`.

- [ ] **Step 4: Commit**
```bash
cd "C:/Users/lauye/Documents/gamev2"
git add UnityProject/Assets/SGPathway/Runtime/Staging/StageComputer.cs
git commit -m "fix(staging): deterministic lead + stable painter order"
```

---

## Task 3: EditMode test assembly + fixture builder

**Files:** Create `Tests/Editor/SGPathway.Tests.asmdef`, `Tests/Editor/TestWalkthroughBuilder.cs`.

- [ ] **Step 1: Create the test assembly definition**

Create `UnityProject/Assets/SGPathway/Tests/Editor/SGPathway.Tests.asmdef`:
```json
{
    "name": "SGPathway.Tests",
    "rootNamespace": "SGPathway.Tests",
    "references": [ "SGPathway.Runtime", "UnityEngine.TestRunner", "UnityEditor.TestRunner" ],
    "includePlatforms": [ "Editor" ],
    "excludePlatforms": [],
    "overrideReferences": true,
    "precompiledReferences": [ "nunit.framework.dll" ],
    "autoReferenced": false,
    "defineConstraints": [ "UNITY_INCLUDE_TESTS" ],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create the fixture builder**

Create `UnityProject/Assets/SGPathway/Tests/Editor/TestWalkthroughBuilder.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using SGPathway.Data;

namespace SGPathway.Tests
{
    /// <summary>Builds in-memory SO fixtures (no asset files) for EditMode tests.</summary>
    internal static class TestWalkthroughBuilder
    {
        public static ActorSO Actor(string key, ActorTeam team = ActorTeam.Patient)
        {
            var a = ScriptableObject.CreateInstance<ActorSO>();
            a.Init(key, key, key, team, key, Color.white);
            return a;
        }

        public static ChapterSO Chapter(string id, float durationSec)
        {
            var c = ScriptableObject.CreateInstance<ChapterSO>();
            c.InitMeta(id, id, SceneKind.Unspecified, durationSec, "", "");
            return c;
        }

        public static Beat Beat(float at, ActorSO actor, string action = "")
            => new Beat { at = at, actor = actor, actionText = action };

        public static WalkthroughSO Walkthrough(string id, ChapterSO start,
            List<ChapterSO> chapters, List<ActorSO> actors)
        {
            var w = ScriptableObject.CreateInstance<WalkthroughSO>();
            w.InitMeta(id, id);
            w.SetStartChapter(start);
            w.SetChapters(chapters);
            w.SetActors(actors);
            return w;
        }

        /// <summary>The `tinyWalkthrough` fixture mirroring walkthrough.test.ts.</summary>
        public static WalkthroughSO Tiny(out ChapterSO a, out ChapterSO b, out ChapterSO c, out ChapterSO d)
        {
            var x = Actor("x", ActorTeam.Patient);
            var y = Actor("y", ActorTeam.ED);

            a = Chapter("a", 10f);
            a.SetBeats(new List<Beat> { Beat(0, x, "x0"), Beat(4, x, "x4"), Beat(2, y, "y2") });

            b = Chapter("b", 5f);
            b.SetBeats(new List<Beat> { Beat(0, x, "b0") });

            c = Chapter("c", 3f);
            d = Chapter("d", 4f);

            a.SetDefaultNext(b);
            // branch: option0 -> c, option1 -> d, option2 -> (unresolvable: null nextChapter)
            var branch = new BranchPoint
            {
                promptText = "where to?",
                options = new List<BranchOption>
                {
                    new BranchOption { labelText = "to c", nextChapter = c },
                    new BranchOption { labelText = "to d", nextChapter = d },
                    new BranchOption { labelText = "missing", nextChapter = null },
                }
            };
            b.SetBranchPoint(branch);

            return Walkthrough("tiny", a,
                new List<ChapterSO> { a, b, c, d },
                new List<ActorSO> { x, y });
        }
    }
}
```

- [ ] **Step 3: Compile-check** — `unity_get_compilation_errors` → `count: 0`.

- [ ] **Step 4: Commit**
```bash
cd "C:/Users/lauye/Documents/gamev2"
git add UnityProject/Assets/SGPathway/Tests/Editor/SGPathway.Tests.asmdef UnityProject/Assets/SGPathway/Tests/Editor/TestWalkthroughBuilder.cs
git commit -m "test: EditMode assembly + walkthrough fixture builder"
```

---

## Task 4: Engine EditMode tests (port of walkthrough.test.ts)

**Files:** Create `Tests/Editor/EngineTests.cs`.

- [ ] **Step 1: Write the tests**

Create `UnityProject/Assets/SGPathway/Tests/Editor/EngineTests.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SGPathway.Data;
using SGPathway.Engine;

namespace SGPathway.Tests
{
    public class EngineTests
    {
        private WalkthroughSO _w;
        private ChapterSO _a, _b, _c, _d;

        [SetUp] public void SetUp() => _w = TestWalkthroughBuilder.Tiny(out _a, out _b, out _c, out _d);

        [Test] public void FindChapterById_hit_and_miss()
        {
            Assert.AreEqual(_a, _w.FindChapterById("a"));
            Assert.IsNull(_w.FindChapterById("nope"));
        }

        [Test] public void NextChapter_follows_default_when_no_branch_pick()
            => Assert.AreEqual(_b, BeatsCalculator.NextChapter(_w, _a, null));

        [Test] public void NextChapter_resolves_branch_pick()
        {
            Assert.AreEqual(_c, BeatsCalculator.NextChapter(_w, _b, 0));
            Assert.AreEqual(_d, BeatsCalculator.NextChapter(_w, _b, 1));
        }

        [Test] public void NextChapter_null_for_unresolvable_option()
            => Assert.IsNull(BeatsCalculator.NextChapter(_w, _b, 2));

        [Test] public void NextChapter_null_for_unknown_or_out_of_range()
        {
            Assert.IsNull(BeatsCalculator.NextChapter(_w, null, null));
            Assert.IsNull(BeatsCalculator.NextChapter(_w, _b, 99));
        }

        [Test] public void CanonicalChapters_follows_defaults_stops_at_branch()
            => CollectionAssert.AreEqual(new[] { _a, _b }, BeatsCalculator.CanonicalChapters(_w));

        [Test] public void CanonicalDurationSec_sums_canonical()
            => Assert.AreEqual(15f, BeatsCalculator.CanonicalDurationSec(_w));

        [Test] public void LocateInCanonical_maps_global_to_chapter_local()
        {
            void Check(float g, ChapterSO ch, float local)
            {
                var loc = BeatsCalculator.LocateInCanonical(_w, g);
                Assert.IsTrue(loc.HasValue);
                Assert.AreEqual(ch, loc.Value.Chapter);
                Assert.AreEqual(local, loc.Value.LocalSec, 1e-4f);
            }
            Check(0f, _a, 0f); Check(7f, _a, 7f); Check(10f, _a, 10f);
            Check(12f, _b, 2f); Check(999f, _b, 5f);
        }

        private static List<string> ActiveActorIds(IEnumerable<Beat> beats)
            => beats.Select(x => x.actor.Key).OrderBy(s => s).ToList();

        [Test] public void BeatsAt_latest_beat_per_actor()
        {
            CollectionAssert.AreEqual(new[] { "x" }, ActiveActorIds(BeatsCalculator.BeatsAt(_a, 1f)));
            CollectionAssert.AreEqual(new[] { "x", "y" }, ActiveActorIds(BeatsCalculator.BeatsAt(_a, 3f)));
            // at t=5: x's latest is x@4, y's latest is y@2
            var at5 = BeatsCalculator.BeatsAt(_a, 5f).ToDictionary(z => z.actor.Key, z => z.actionText);
            Assert.AreEqual("x4", at5["x"]);
            Assert.AreEqual("y2", at5["y"]);
        }

        [Test] public void BeatsAt_clamps_upper()
        {
            var ten = ActiveActorIds(BeatsCalculator.BeatsAt(_a, 10f));
            var big = ActiveActorIds(BeatsCalculator.BeatsAt(_a, 999f));
            CollectionAssert.AreEqual(ten, big);
        }

        [Test] public void BeatsAt_negative_is_zero()
            => CollectionAssert.AreEqual(new[] { "x" }, ActiveActorIds(BeatsCalculator.BeatsAt(_a, -5f)));
    }
}
```

- [ ] **Step 2: Run the EngineTests EditMode tests**

Run the Unity Test Runner (EditMode) filtered to `SGPathway.Tests.EngineTests` — via the MCP test tool (search `unity_advanced_tool` for "run tests" / Test Runner), or in-editor: **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run Selected**.
Expected: 11 tests, all PASS.

- [ ] **Step 3: Commit**
```bash
cd "C:/Users/lauye/Documents/gamev2"
git add UnityProject/Assets/SGPathway/Tests/Editor/EngineTests.cs
git commit -m "test(engine): port walkthrough.test.ts (11 cases)"
```

---

## Task 5: Staging EditMode tests (ordering + determinism)

**Files:** Create `Tests/Editor/StagingTests.cs`.

- [ ] **Step 1: Write the tests**

Create `UnityProject/Assets/SGPathway/Tests/Editor/StagingTests.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SGPathway.Data;
using SGPathway.Staging;

namespace SGPathway.Tests
{
    public class StagingTests
    {
        [Test] public void ChapterActorOrder_by_first_appearance_then_id()
        {
            var alpha = TestWalkthroughBuilder.Actor("alpha");
            var zeta = TestWalkthroughBuilder.Actor("zeta");
            var late = TestWalkthroughBuilder.Actor("late");
            var ch = TestWalkthroughBuilder.Chapter("ch", 30f);
            ch.SetBeats(new List<Beat>
            {
                TestWalkthroughBuilder.Beat(5, late),
                TestWalkthroughBuilder.Beat(0, zeta),
                TestWalkthroughBuilder.Beat(0, alpha),
            });
            var order = StageComputer.ChapterActorOrder(ch).Select(a => a.Key).ToList();
            CollectionAssert.AreEqual(new[] { "alpha", "zeta", "late" }, order);
        }

        [Test] public void StageFigures_painter_order_is_back_to_front_and_deterministic()
        {
            // Two actors with equal y -> deterministic tie-break by Id.
            var p = TestWalkthroughBuilder.Actor("aaa");
            var q = TestWalkthroughBuilder.Actor("bbb");
            var ch = TestWalkthroughBuilder.Chapter("ch", 30f);
            var bp = new Beat { at = 0, actor = p, pos = OptionalVector2.Of(new UnityEngine.Vector2(100, 200)) };
            var bq = new Beat { at = 0, actor = q, pos = OptionalVector2.Of(new UnityEngine.Vector2(300, 200)) };
            ch.SetBeats(new List<Beat> { bp, bq });
            var active = new Dictionary<ActorSO, Beat> { { p, bp }, { q, bq } };
            var staging = StageComputer.StageFigures(ch, active, null);
            // equal y -> ordered by Id ("aaa" before "bbb")
            CollectionAssert.AreEqual(new[] { "aaa", "bbb" }, staging.Figures.Select(f => f.Actor.Key).ToList());
        }

        [Test] public void StageFigures_lead_is_latest_fired_active_beat()
        {
            var p = TestWalkthroughBuilder.Actor("p");
            var q = TestWalkthroughBuilder.Actor("q");
            var ch = TestWalkthroughBuilder.Chapter("ch", 30f);
            var bp = new Beat { at = 2, actor = p };
            var bq = new Beat { at = 6, actor = q };
            ch.SetBeats(new List<Beat> { bp, bq });
            var active = new Dictionary<ActorSO, Beat> { { p, bp }, { q, bq } };
            var staging = StageComputer.StageFigures(ch, active, null);
            Assert.AreEqual(q, staging.LeadActor);
        }

        [Test] public void StageFigures_keeps_selected_inactive_actor_when_it_has_beats()
        {
            var p = TestWalkthroughBuilder.Actor("p");
            var ch = TestWalkthroughBuilder.Chapter("ch", 30f);
            ch.SetBeats(new List<Beat> { new Beat { at = 14, actor = p } });
            var staging = StageComputer.StageFigures(ch, new Dictionary<ActorSO, Beat>(), p);
            Assert.AreEqual(1, staging.Figures.Count);
            Assert.IsFalse(staging.Figures[0].IsActive);
            Assert.IsTrue(staging.Figures[0].IsSelected);
        }
    }
}
```

- [ ] **Step 2: Run StagingTests (EditMode)** — Expected: 4 tests PASS.

- [ ] **Step 3: Commit**
```bash
cd "C:/Users/lauye/Documents/gamev2"
git add UnityProject/Assets/SGPathway/Tests/Editor/StagingTests.cs
git commit -m "test(staging): ordering, painter determinism, lead, selection"
```

---

## Task 6: Node exporter — walkthrough TS → JSON

**Files:** Create `tools/export-walkthroughs.mjs`.

Approach: the source files are `import type {...}` + a plain `export const … = { … }` object literal. Strip the type-only import and the `: Walkthrough` annotation, import the result as ESM via a temp `.mjs`, then re-shape to a JsonUtility-friendly DTO (arrays; map keys promoted to `key`; presence flags for optionals; enum/scene/showpiece as strings).

- [ ] **Step 1: Write the exporter**

Create `tools/export-walkthroughs.mjs`:
```js
import { readFile, writeFile, mkdir, rm } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';
import path from 'node:path';

const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname.replace(/^\/(\w:)/, '$1')), '..');
const SRC = path.join(ROOT, 'Game-source', 'src', 'lib');
const OUT = path.join(ROOT, 'UnityProject', 'Assets', 'SGPathway', 'Content', 'Walkthroughs');

async function loadConst(tsFile, exportName) {
  const raw = await readFile(tsFile, 'utf8');
  const js = raw
    .replace(/^\s*import\s+type[\s\S]*?;\s*$/m, '')        // drop type-only import
    .replace(new RegExp(`export const ${exportName}\\s*:\\s*Walkthrough`), `export const ${exportName}`);
  const tmp = tsFile + '.tmp.mjs';
  await writeFile(tmp, js, 'utf8');
  try { return (await import(pathToFileURL(tmp).href))[exportName]; }
  finally { await rm(tmp, { force: true }); }
}

const str = (v) => (v == null ? '' : String(v));

function beatDTO(b) {
  const sp = b.showpiece;
  return {
    at: b.at,
    actorRef: b.actorId,
    action: str(b.action),
    hasPos: !!b.pos,
    posX: b.pos ? b.pos.x : 0,
    posY: b.pos ? b.pos.y : 0,
    direction: str(b.direction || 'S'),
    walking: !!b.walking,
    pose: str(b.pose || 'stand'),
    expression: str(b.expression || 'neutral'),
    showpieceKind: sp ? sp.kind : '',
    showpieceSvgId: sp && sp.kind === 'svg' ? sp.id : '',
  };
}

function chapterDTO(key, c) {
  const bp = c.branchPoint;
  return {
    key,
    id: str(c.id),
    title: str(c.title),
    scene: str(c.scene || ''),
    durationSec: c.durationSec,
    timeOfDay: str(c.timeOfDay),
    location: str(c.location),
    hasDefaultNext: !!c.defaultNextChapterId,
    defaultNextChapterRef: str(c.defaultNextChapterId),
    hasBranchPoint: !!bp,
    branchPoint: bp ? {
      prompt: str(bp.prompt),
      options: bp.options.map(o => ({ label: str(o.label), hint: str(o.hint), nextChapterRef: str(o.nextChapterId) })),
    } : { prompt: '', options: [] },
    beats: c.beats.map(beatDTO),
  };
}

function toDTO(w) {
  return {
    id: w.id,
    title: str(w.title),
    startChapterRef: str(w.startChapterId),
    actors: Object.entries(w.actors).map(([key, a]) => ({
      key, id: str(a.id), role: str(a.role), team: str(a.team), bio: str(a.bio), swatch: str(a.swatch || '#ffffff'),
    })),
    chapters: Object.entries(w.chapters).map(([key, c]) => chapterDTO(key, c)),
  };
}

async function run() {
  const jobs = [
    ['walkthrough-stemi.ts', 'stemiWalkthrough', 'Stemi'],
    ['walkthrough-stroke.ts', 'strokeWalkthrough', 'Stroke'],
  ];
  for (const [file, name, folder] of jobs) {
    const w = await loadConst(path.join(SRC, file), name);
    const dto = toDTO(w);
    const dir = path.join(OUT, folder);
    await mkdir(dir, { recursive: true });
    await writeFile(path.join(dir, '_source.json'), JSON.stringify(dto, null, 2), 'utf8');
    console.log(`${folder}: ${dto.actors.length} actors, ${dto.chapters.length} chapters -> ${path.join(dir, '_source.json')}`);
  }
}
run().catch(e => { console.error(e); process.exit(1); });
```

- [ ] **Step 2: Run it**

Run:
```bash
cd "C:/Users/lauye/Documents/gamev2" && node tools/export-walkthroughs.mjs
```
Expected: two lines like `Stemi: 30 actors, 21 chapters -> ...` and `Stroke: 18 actors, 12 chapters -> ...` (counts approximate). If the path-on-Windows munging fails, set `const ROOT = "C:/Users/lauye/Documents/gamev2"` literally and re-run.

- [ ] **Step 3: Sanity-check the JSON**

Run:
```bash
node -e "const d=require('./UnityProject/Assets/SGPathway/Content/Walkthroughs/Stroke/_source.json'); const a=d.actors.find(x=>x.key!==x.id); console.log('key!=id sample:', a && (a.key+' / '+a.id)); console.log('start:', d.startChapterRef);"
```
Expected: prints a `key / id` divergence (e.g. `patient / stroke-patient`) — confirms the key≠id case is captured.

- [ ] **Step 4: Commit**
```bash
cd "C:/Users/lauye/Documents/gamev2"
git add tools/export-walkthroughs.mjs UnityProject/Assets/SGPathway/Content/Walkthroughs/Stemi/_source.json UnityProject/Assets/SGPathway/Content/Walkthroughs/Stroke/_source.json
git commit -m "feat(content): TS->JSON walkthrough exporter + baked sources"
```

---

## Task 7: WalkthroughImporter — JSON → ScriptableObjects (two-pass)

**Files:** Create `Editor/WalkthroughDTO.cs`; replace `Editor/WalkthroughImporter.cs`. Verify `Editor/SGPathway.Editor.asmdef` references `SGPathway.Runtime` + `Unity.Localization` (add if missing).

- [ ] **Step 1: DTOs (JsonUtility-friendly)**

Create `UnityProject/Assets/SGPathway/Editor/WalkthroughDTO.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace SGPathway.EditorTools
{
    [Serializable] public class WalkthroughDTO
    {
        public string id; public string title; public string startChapterRef;
        public List<ActorDTO> actors = new(); public List<ChapterDTO> chapters = new();
    }
    [Serializable] public class ActorDTO
    { public string key, id, role, team, bio, swatch; }
    [Serializable] public class ChapterDTO
    {
        public string key, id, title, scene, timeOfDay, location, defaultNextChapterRef;
        public float durationSec; public bool hasDefaultNext, hasBranchPoint;
        public BranchPointDTO branchPoint; public List<BeatDTO> beats = new();
    }
    [Serializable] public class BranchPointDTO
    { public string prompt; public List<BranchOptionDTO> options = new(); }
    [Serializable] public class BranchOptionDTO
    { public string label, hint, nextChapterRef; }
    [Serializable] public class BeatDTO
    {
        public float at; public string actorRef, action; public bool hasPos; public float posX, posY;
        public string direction, pose, expression, showpieceKind, showpieceSvgId; public bool walking;
    }
}
```

- [ ] **Step 2: The importer**

Replace `UnityProject/Assets/SGPathway/Editor/WalkthroughImporter.cs` with:
```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using SGPathway.Data;

namespace SGPathway.EditorTools
{
    /// <summary>Bakes Content/Walkthroughs/&lt;Folder&gt;/_source.json into SO assets (idempotent).</summary>
    public static class WalkthroughImporter
    {
        private const string Root = "Assets/SGPathway/Content/Walkthroughs";

        [MenuItem("SG Pathway/Import Walkthroughs from JSON")]
        public static void ImportAll()
        {
            ImportOne("Stemi");
            ImportOne("Stroke");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SGPathway] Walkthrough import complete.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var so = ScriptableObject.CreateInstance<T>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        public static void ImportOne(string folder)
        {
            string jsonPath = $"{Root}/{folder}/_source.json";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (asset == null) { Debug.LogError($"[SGPathway] Missing {jsonPath}. Run tools/export-walkthroughs.mjs."); return; }
            var dto = JsonUtility.FromJson<WalkthroughDTO>(asset.text);

            // ---- Pass 1: create/find all actors + chapters, index by key ----
            var actorsByKey = new Dictionary<string, ActorSO>();
            var actorList = new List<ActorSO>();
            foreach (var a in dto.actors)
            {
                var so = LoadOrCreate<ActorSO>($"{Root}/{folder}/Actors/{a.key}.asset");
                Color c = ColorUtility.TryParseHtmlString(string.IsNullOrEmpty(a.swatch) ? "#ffffff" : a.swatch, out var col) ? col : Color.white;
                so.Init(a.key, a.id, a.role, ParseTeam(a.team), a.bio, c);
                EditorUtility.SetDirty(so);
                actorsByKey[a.key] = so; actorList.Add(so);
            }

            var chaptersByKey = new Dictionary<string, ChapterSO>();
            var chapterList = new List<ChapterSO>();
            foreach (var ch in dto.chapters)
            {
                var so = LoadOrCreate<ChapterSO>($"{Root}/{folder}/Chapters/{ch.key}.asset");
                so.InitMeta(ch.id, ch.title, ParseScene(ch.scene), ch.durationSec, ch.timeOfDay, ch.location);
                chaptersByKey[ch.key] = so; chapterList.Add(so);
            }

            // ---- Pass 2: wire beats + branches + defaults ----
            foreach (var ch in dto.chapters)
            {
                var so = chaptersByKey[ch.key];
                var beats = new List<Beat>();
                foreach (var b in ch.beats)
                {
                    actorsByKey.TryGetValue(b.actorRef, out var actor);
                    if (actor == null) Debug.LogWarning($"[SGPathway] {folder}/{ch.key}: beat actorRef '{b.actorRef}' not found.");
                    beats.Add(new Beat
                    {
                        at = b.at, actor = actor, actionText = b.action,
                        pos = b.hasPos ? OptionalVector2.Of(new Vector2(b.posX, b.posY)) : OptionalVector2.None,
                        direction = ParseDir(b.direction), walking = b.walking,
                        pose = ParsePose(b.pose), expression = ParseExpr(b.expression),
                        showpiece = ParseShowpiece(b.showpieceKind, b.showpieceSvgId),
                    });
                }
                so.SetBeats(beats);

                if (ch.hasBranchPoint && ch.branchPoint != null)
                {
                    var bp = new BranchPoint { promptText = ch.branchPoint.prompt, options = new List<BranchOption>() };
                    foreach (var o in ch.branchPoint.options)
                    {
                        chaptersByKey.TryGetValue(o.nextChapterRef, out var next);
                        bp.options.Add(new BranchOption { labelText = o.label, hintText = o.hint, nextChapter = next });
                    }
                    so.SetBranchPoint(bp);
                }
                else so.SetBranchPoint(null);

                if (ch.hasDefaultNext && chaptersByKey.TryGetValue(ch.defaultNextChapterRef, out var dn))
                    so.SetDefaultNext(dn);
                else so.SetDefaultNext(null);

                EditorUtility.SetDirty(so);
            }

            // ---- Walkthrough root ----
            var w = LoadOrCreate<WalkthroughSO>($"{Root}/{folder}/{dto.id}.asset");
            w.InitMeta(dto.id, dto.title);
            chaptersByKey.TryGetValue(dto.startChapterRef, out var start);
            w.SetStartChapter(start);
            w.SetChapters(chapterList);
            w.SetActors(actorList);
            EditorUtility.SetDirty(w);
            Debug.Log($"[SGPathway] {folder}: {actorList.Count} actors, {chapterList.Count} chapters baked.");
        }

        private static ActorTeam ParseTeam(string s) => s switch {
            "patient" => ActorTeam.Patient, "bystander" => ActorTeam.Bystander,
            "first-responder" => ActorTeam.FirstResponder, "ambulance" => ActorTeam.Ambulance,
            "ed" => ActorTeam.ED, "cath" => ActorTeam.Cath, "ward" => ActorTeam.Ward,
            "rehab" => ActorTeam.Rehab, "outpatient" => ActorTeam.Outpatient, _ => ActorTeam.Support };
        private static SceneKind ParseScene(string s) => s switch {
            "kopitiam" => SceneKind.Kopitiam, "street" => SceneKind.Street, "mrt" => SceneKind.Mrt,
            "resus" => SceneKind.Resus, "cathlab" => SceneKind.Cathlab, "imaging" => SceneKind.Imaging,
            "counsel" => SceneKind.Counsel, "ward" => SceneKind.Ward, "pharmacy" => SceneKind.Pharmacy,
            "rehab" => SceneKind.Rehab, "clinic" => SceneKind.Clinic, "backhouse" => SceneKind.Backhouse,
            _ => SceneKind.Unspecified };
        private static BeatPose ParsePose(string s) => s switch {
            "walk" => BeatPose.Walk, "kneel" => BeatPose.Kneel, "sit" => BeatPose.Sit, "cpr" => BeatPose.Cpr,
            "collapsed" => BeatPose.Collapsed, "point" => BeatPose.Point, _ => BeatPose.Stand };
        private static BeatExpression ParseExpr(string s) => s switch {
            "alarmed" => BeatExpression.Alarmed, "distressed" => BeatExpression.Distressed,
            "pained" => BeatExpression.Pained, "focused" => BeatExpression.Focused,
            "relieved" => BeatExpression.Relieved, "unconscious" => BeatExpression.Unconscious, _ => BeatExpression.Neutral };
        private static BeatDirection ParseDir(string s) => s switch {
            "N" => BeatDirection.N, "E" => BeatDirection.E, "W" => BeatDirection.W, _ => BeatDirection.S };
        private static Showpiece ParseShowpiece(string kind, string svgId)
        {
            if (kind == "svg") return new Showpiece { kind = svgId switch {
                "stent-deployment" => ShowpieceKind.StentDeployment, "mri-bore-slide" => ShowpieceKind.MriBoreSlide,
                "aed-shock" => ShowpieceKind.AedShock, "thrombectomy-pass" => ShowpieceKind.ThrombectomyPass,
                _ => ShowpieceKind.None } };
            if (kind == "mp4") return new Showpiece { kind = ShowpieceKind.ExternalMp4 };
            return new Showpiece { kind = ShowpieceKind.None };
        }
    }
}
```

- [ ] **Step 3: Compile-check** — `unity_get_compilation_errors({ severity:"error", port:7891 })` → `count: 0`. (If `_source.json` isn't seen as a `TextAsset` yet, call `unity_asset_import`/refresh or focus the editor.)

- [ ] **Step 4: Commit**
```bash
cd "C:/Users/lauye/Documents/gamev2"
git add UnityProject/Assets/SGPathway/Editor/WalkthroughDTO.cs UnityProject/Assets/SGPathway/Editor/WalkthroughImporter.cs
git commit -m "feat(import): two-pass JSON->ScriptableObject walkthrough baker"
```

---

## Task 8: Run the import + content-integrity tests

**Files:** Create `Tests/Editor/ContentIntegrityTests.cs`.

- [ ] **Step 1: Run the importer via MCP**

Call `unity_execute_menu_item({ menuPath: "SG Pathway/Import Walkthroughs from JSON", port: 7891 })`.
Then `unity_console_log({ type: "all", count: 30, port: 7891 })`.
Expected: `Stemi: … baked.`, `Stroke: … baked.`, and no errors. Assets appear under `Assets/SGPathway/Content/Walkthroughs/{Stemi,Stroke}/`.

- [ ] **Step 2: Verify assets exist**

Call `unity_asset_list({ folder: "Assets/SGPathway/Content/Walkthroughs/Stemi", port: 7891 })` (and `/Stroke`).
Expected: a `stemi-pathway-v1.asset`, a `Chapters/` folder of ChapterSOs, an `Actors/` folder of ActorSOs.

- [ ] **Step 3: Write content-integrity tests**

Create `UnityProject/Assets/SGPathway/Tests/Editor/ContentIntegrityTests.cs`:
```csharp
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SGPathway.Data;
using SGPathway.Engine;

namespace SGPathway.Tests
{
    public class ContentIntegrityTests
    {
        private static WalkthroughSO Load(string id)
        {
            var guid = AssetDatabase.FindAssets($"{id} t:WalkthroughSO").FirstOrDefault();
            Assert.IsNotNull(guid, $"WalkthroughSO '{id}' not found — run the importer first.");
            return AssetDatabase.LoadAssetAtPath<WalkthroughSO>(AssetDatabase.GUIDToAssetPath(guid));
        }

        [TestCase("stemi-pathway-v1")]
        [TestCase("stroke-pathway-v1")]
        public void Walkthrough_starts_at_collapse(string id)
        {
            var w = Load(id);
            Assert.IsNotNull(w.StartChapter, "start chapter unresolved");
            Assert.AreEqual("collapse", w.StartChapter.Id);
        }

        [TestCase("stemi-pathway-v1")]
        [TestCase("stroke-pathway-v1")]
        public void Every_beat_references_a_known_actor(string id)
        {
            var w = Load(id);
            foreach (var ch in w.Chapters)
                foreach (var b in ch.Beats)
                    Assert.IsNotNull(b.actor, $"{ch.Id}: a beat has an unresolved actor");
        }

        [TestCase("stemi-pathway-v1")]
        [TestCase("stroke-pathway-v1")]
        public void Every_default_and_branch_target_resolves(string id)
        {
            var w = Load(id);
            foreach (var ch in w.Chapters)
            {
                var bp = ch.BranchPoint;
                if (bp != null)
                    foreach (var o in bp.options)
                        Assert.IsNotNull(o.nextChapter, $"{ch.Id}: branch option '{o.labelText}' has no target");
            }
        }

        [TestCase("stemi-pathway-v1")]
        [TestCase("stroke-pathway-v1")]
        public void Beats_within_chapter_duration(string id)
        {
            var w = Load(id);
            foreach (var ch in w.Chapters)
                foreach (var b in ch.Beats)
                    Assert.IsTrue(b.at >= 0f && b.at <= ch.DurationSec, $"{ch.Id}: beat at {b.at} outside [0,{ch.DurationSec}]");
        }

        [Test] public void Stroke_thrombectomy_has_showpiece()
        {
            var w = Load("stroke-pathway-v1");
            bool found = w.Chapters.Any(c => c.Beats.Any(b => b.showpiece.kind == ShowpieceKind.ThrombectomyPass));
            Assert.IsTrue(found, "stroke pathway missing thrombectomy-pass showpiece");
        }

        [TestCase("stemi-pathway-v1")]
        [TestCase("stroke-pathway-v1")]
        public void Canonical_path_is_nontrivial(string id)
        {
            var w = Load(id);
            Assert.Greater(BeatsCalculator.CanonicalChapters(w).Count, 1);
            Assert.Greater(BeatsCalculator.CanonicalDurationSec(w), 0f);
        }
    }
}
```

- [ ] **Step 4: Run ContentIntegrityTests (EditMode)** — Expected: all PASS (10 cases via TestCase expansion). If a branch-target test fails, inspect the offending chapter's `_source.json` `nextChapterRef` vs chapter `key`s.

- [ ] **Step 5: Commit**
```bash
cd "C:/Users/lauye/Documents/gamev2"
git add UnityProject/Assets/SGPathway/Content/Walkthroughs UnityProject/Assets/SGPathway/Tests/Editor/ContentIntegrityTests.cs
git commit -m "test(content): integrity checks on baked STEMI/Stroke walkthroughs"
```

---

## Done-when

- `unity_get_compilation_errors` → 0 errors.
- All EditMode tests pass (Engine 11, Staging 4, ContentIntegrity 10).
- `Assets/SGPathway/Content/Walkthroughs/{Stemi,Stroke}/` contain a `WalkthroughSO` + ChapterSOs + ActorSOs, with `Beat.actor`, branch/default targets, and `startChapter` all resolved (incl. the Stroke key≠id actors).
- Re-running the importer overwrites in place without new assets/GUID churn (idempotent).

This is the verified, renderer-agnostic foundation. **Next plan:** HDRP setup + the resus-bay 3D hero slice.
