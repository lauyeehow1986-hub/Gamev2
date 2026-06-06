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
