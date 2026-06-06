# SG Pathway Unity — setup checklist

Three things have to be true before any AI-driven Unity work happens in
this project:

1. **Unity Editor 6000.0 LTS installed** (Unity Hub already present).
2. **The unity-mcp-plugin** package added to this project.
3. **The unity-mcp-server** registered in Claude Code's MCP config.

If you do these in order the next Claude Code session will see all 288
Unity tools and can drive the editor directly.

## 1 — Install Unity Editor 6000.0 LTS via Unity Hub

Unity Hub is already installed at `C:\Program Files\Unity Hub\`. Open it,
go **Installs → Install Editor → 6000.0 LTS**, and tick at minimum:

- **Windows Build Support (Mono)** — required to run the editor.
- **Microsoft Visual Studio Community 2022** — gives you `.csproj`
  generation and a C# compiler. (If you already have VS / Rider, skip.)
- **WebGL Build Support** — only if you want to ship the walkthrough as
  a web build later.

`ProjectSettings/ProjectVersion.txt` is pinned to `6000.0.32f1`. If the
Hub installs a newer 6000.0 patch, edit the file to match.

## 2 — Open this project, then add the unity-mcp-plugin

In Unity Hub: **Open → Add project from disk** → pick
`C:\Users\lauye\Documents\gamev2\UnityProject`. Let it import (first
import will take a few minutes — URP, Localization, Cinemachine, Input
System all get fetched).

Once the editor is open:

1. **Window → Package Manager**
2. Click **+ → Add package from git URL…**
3. Paste:
   ```
   https://github.com/AnkleBreaker-Studio/unity-mcp-plugin.git
   ```
4. Wait for it to compile. You should see `[MCP Bridge] Server started
   on port 7890` in the Unity Console.

## 3 — Register the unity-mcp-server with Claude Code

The Node server is already installed at:
```
C:\Users\lauye\Documents\gamev2\unity-mcp-server-main\unity-mcp-server-main
```
(`npm install` already ran in this session — confirmed exit 0.)

Open your Claude Code MCP config (`~/.claude.json`) and merge this
entry under `mcpServers`:

```json
"unity": {
  "command": "node",
  "args": [
    "C:/Users/lauye/Documents/gamev2/unity-mcp-server-main/unity-mcp-server-main/src/index.js"
  ],
  "env": {
    "UNITY_HUB_PATH": "C:\\Program Files\\Unity Hub\\Unity Hub.exe",
    "UNITY_BRIDGE_PORT": "7890"
  }
}
```

Quit Claude Code completely (so the MCP config reloads), then restart it
from this project root:

```
cd C:\Users\lauye\Documents\gamev2
claude
```

Test the wire by asking `claude` something like *"list installed Unity
editors"* — if the unity-mcp server is up you'll see them.

## 4 — Verify the engine compiles

With the editor open and Claude restarted, run these three checks:

1. **No compile errors** in the Unity Console after a clean reimport.
2. `Assets → Create → SG Pathway → Walkthrough` menu item appears
   (proves the data layer's CreateAssetMenu attributes are wired).
3. Drop a `WalkthroughPlayer` on an empty GameObject in a fresh scene —
   the Inspector should expose the four UnityEvents
   (`OnChapterEntered`, `OnChapterExited`, `OnTick`, `OnBranchPrompt`)
   and a `WalkthroughSO` slot.

If all three pass, the engine port is good and the next session can
start populating content via the JSON pipeline described in
`PORT_PLAN.md`.
