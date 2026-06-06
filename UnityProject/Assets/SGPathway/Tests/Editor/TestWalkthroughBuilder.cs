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
