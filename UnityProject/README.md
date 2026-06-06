# SG Pathway — Unity 6 walkthrough port

Unity rebuild of the v9.4 → v9.14.1 scrubbable cinematic from the web
[`Game`](https://github.com/lauyeehow1986-hub/Game) repo. STEMI +
Stroke pathways only; the rest of the SG Pathway product
(campaigns, exams, branching cases, vitals, analytics, peer review)
is out of scope for this milestone.

## State

- **Engine + data layer:** ported and compiles. `BeatsCalculator`,
  `StageComputer`, and the data ScriptableObjects mirror the TS sources
  1:1.
- **Renderer + UI:** stubs only. `WalkthroughRenderer`, `SceneryView`,
  `ActorView`, `TimelineScrubber`, `BranchOverlay` exist with the right
  shape but need their prefabs / atlases wired in the editor.
- **Content:** not yet imported. The Bandersnatch chapters still live in
  `Game-source/src/lib/walkthrough-{stemi,stroke}.ts`. The JSON export
  pipeline + editor importer are described in `docs/PORT_PLAN.md`
  §"Content port pipeline".

## Start here

1. Read `docs/SETUP.md` — installs Unity 6000.0 LTS, adds the
   unity-mcp-plugin, wires the unity-mcp-server into Claude Code.
2. Read `docs/PORT_PLAN.md` — explains what's mapped where, what's still
   open, and the validation gate before the content port.
3. Open the project in Unity Hub and confirm a clean compile (no errors
   in the Console).

Once those three are green, the next AI session has working MCP tools
and can drive the editor to bake content, sprites, and showpieces.
