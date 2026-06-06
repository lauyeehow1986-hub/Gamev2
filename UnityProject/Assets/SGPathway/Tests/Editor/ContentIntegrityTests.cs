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
        public void Every_branch_target_resolves(string id)
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
