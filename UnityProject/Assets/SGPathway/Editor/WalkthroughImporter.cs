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

        private static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.Replace('\\', '/');
            if (string.IsNullOrEmpty(assetFolder) || assetFolder == "Assets" || AssetDatabase.IsValidFolder(assetFolder)) return;
            var parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
            var leaf = Path.GetFileName(assetFolder);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var so = ScriptableObject.CreateInstance<T>();
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

        private static ActorTeam ParseTeam(string s) => s switch
        {
            "patient" => ActorTeam.Patient, "bystander" => ActorTeam.Bystander,
            "first-responder" => ActorTeam.FirstResponder, "ambulance" => ActorTeam.Ambulance,
            "ed" => ActorTeam.ED, "cath" => ActorTeam.Cath, "ward" => ActorTeam.Ward,
            "rehab" => ActorTeam.Rehab, "outpatient" => ActorTeam.Outpatient, _ => ActorTeam.Support
        };
        private static SceneKind ParseScene(string s) => s switch
        {
            "kopitiam" => SceneKind.Kopitiam, "street" => SceneKind.Street, "mrt" => SceneKind.Mrt,
            "resus" => SceneKind.Resus, "cathlab" => SceneKind.Cathlab, "imaging" => SceneKind.Imaging,
            "counsel" => SceneKind.Counsel, "ward" => SceneKind.Ward, "pharmacy" => SceneKind.Pharmacy,
            "rehab" => SceneKind.Rehab, "clinic" => SceneKind.Clinic, "backhouse" => SceneKind.Backhouse,
            _ => SceneKind.Unspecified
        };
        private static BeatPose ParsePose(string s) => s switch
        {
            "walk" => BeatPose.Walk, "kneel" => BeatPose.Kneel, "sit" => BeatPose.Sit, "cpr" => BeatPose.Cpr,
            "collapsed" => BeatPose.Collapsed, "point" => BeatPose.Point, _ => BeatPose.Stand
        };
        private static BeatExpression ParseExpr(string s) => s switch
        {
            "alarmed" => BeatExpression.Alarmed, "distressed" => BeatExpression.Distressed,
            "pained" => BeatExpression.Pained, "focused" => BeatExpression.Focused,
            "relieved" => BeatExpression.Relieved, "unconscious" => BeatExpression.Unconscious, _ => BeatExpression.Neutral
        };
        private static BeatDirection ParseDir(string s) => s switch
        {
            "N" => BeatDirection.N, "E" => BeatDirection.E, "W" => BeatDirection.W, _ => BeatDirection.S
        };
        private static Showpiece ParseShowpiece(string kind, string svgId)
        {
            if (kind == "svg")
                return new Showpiece
                {
                    kind = svgId switch
                    {
                        "stent-deployment" => ShowpieceKind.StentDeployment,
                        "mri-bore-slide" => ShowpieceKind.MriBoreSlide,
                        "aed-shock" => ShowpieceKind.AedShock,
                        "thrombectomy-pass" => ShowpieceKind.ThrombectomyPass,
                        _ => ShowpieceKind.None
                    }
                };
            if (kind == "mp4") return new Showpiece { kind = ShowpieceKind.ExternalMp4 };
            return new Showpiece { kind = ShowpieceKind.None };
        }
    }
}
