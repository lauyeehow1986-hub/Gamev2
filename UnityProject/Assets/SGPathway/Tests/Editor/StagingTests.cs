using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
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

        [Test] public void StageFigures_painter_order_back_to_front_and_deterministic()
        {
            var p = TestWalkthroughBuilder.Actor("aaa");
            var q = TestWalkthroughBuilder.Actor("bbb");
            var ch = TestWalkthroughBuilder.Chapter("ch", 30f);
            var bp = new Beat { at = 0, actor = p, pos = OptionalVector2.Of(new Vector2(100, 200)) };
            var bq = new Beat { at = 0, actor = q, pos = OptionalVector2.Of(new Vector2(300, 200)) };
            ch.SetBeats(new List<Beat> { bp, bq });
            var active = new Dictionary<ActorSO, Beat> { { p, bp }, { q, bq } };
            var staging = StageComputer.StageFigures(ch, active, null);
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
