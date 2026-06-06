using UnityEngine;
using UnityEditor;

namespace SGPathway.EditorTools
{
    /// <summary>
    /// Imports the Game source's TS walkthrough definitions into Unity
    /// ScriptableObjects. Pipeline:
    ///
    /// 1. <c>tools/export-walkthroughs.mjs</c> (in gamev2/, not shipped here yet)
    ///    reads <c>Game-source/src/lib/walkthrough-stemi.ts</c> and
    ///    <c>walkthrough-stroke.ts</c>, evaluates them, and emits a JSON
    ///    intermediate (one file per walkthrough) under
    ///    <c>Assets/SGPathway/Content/Walkthroughs/&lt;id&gt;/_source.json</c>.
    /// 2. This importer bakes those JSON intermediates into <c>WalkthroughSO</c>,
    ///    <c>ChapterSO</c>, and <c>ActorSO</c> assets in the same directory.
    /// 3. LocalizedString fields fall back to literal English on first import;
    ///    translated tables are linked later via the Localization package.
    ///
    /// NOTE: full implementation is deferred — once the Unity MCP is wired we
    /// can iterate this in the editor with the AI driving asset creation
    /// directly. The stub keeps the menu item visible so the workflow is
    /// discoverable from day one.
    /// </summary>
    public static class WalkthroughImporter
    {
        [MenuItem("SG Pathway/Import Walkthroughs from JSON…")]
        public static void Import()
        {
            EditorUtility.DisplayDialog(
                "SG Pathway — Importer",
                "Stub. Run `node tools/export-walkthroughs.mjs` in gamev2/ first " +
                "to emit the JSON intermediates, then implement bake here.\n\n" +
                "See UnityProject/docs/PORT_PLAN.md § Content port pipeline.",
                "OK");
        }
    }
}
