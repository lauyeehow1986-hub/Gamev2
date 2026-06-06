using System.Collections.Generic;
using SGPathway.Data;

namespace SGPathway.Staging
{
    /// <summary>
    /// Pure-function port of <c>src/lib/walkthrough-staging.ts</c>.
    /// </summary>
    public static class StageComputer
    {
        /// <summary>Stable per-chapter ordering: actors by first-appearance time.</summary>
        public static List<ActorSO> ChapterActorOrder(ChapterSO chapter)
        {
            var firstAt = new Dictionary<ActorSO, float>();
            if (chapter == null) return new List<ActorSO>();
            foreach (var b in chapter.Beats)
            {
                if (b == null || b.actor == null) continue;
                if (!firstAt.TryGetValue(b.actor, out var existing) || b.at < existing)
                    firstAt[b.actor] = b.at;
            }
            var ordered = new List<KeyValuePair<ActorSO, float>>(firstAt);
            ordered.Sort((a, b) =>
            {
                int byTime = a.Value.CompareTo(b.Value);
                if (byTime != 0) return byTime;
                return string.Compare(a.Key.Id, b.Key.Id, System.StringComparison.Ordinal);
            });
            var result = new List<ActorSO>(ordered.Count);
            foreach (var kv in ordered) result.Add(kv.Key);
            return result;
        }

        public sealed class Staging
        {
            public List<StagedFigure> Figures = new List<StagedFigure>();
            public ActorSO LeadActor;
        }

        public static Staging StageFigures(
            ChapterSO chapter,
            Dictionary<ActorSO, Beat> activeByActor,
            ActorSO selectedActor)
        {
            var result = new Staging();
            if (chapter == null) return result;

            // Lead = active beat with the highest `at`; deterministic tie-break by actor Id
            // (C# Dictionary iteration order is unspecified, so make ties reproducible).
            ActorSO leadActor = null;
            float leadAt = float.NegativeInfinity;
            foreach (var kv in activeByActor)
            {
                bool better = kv.Value.at > leadAt ||
                    (kv.Value.at == leadAt && leadActor == null) ||
                    (kv.Value.at == leadAt && leadActor != null &&
                     string.Compare(kv.Key.Id, leadActor.Id, System.StringComparison.Ordinal) < 0);
                if (better) { leadAt = kv.Value.at; leadActor = kv.Key; }
            }
            result.LeadActor = leadActor;

            var order = ChapterActorOrder(chapter);
            int total = order.Count;
            for (int i = 0; i < order.Count; i++)
            {
                var actor = order[i];
                activeByActor.TryGetValue(actor, out var beat);
                bool isActive = beat != null;
                bool isSelected = actor == selectedActor;
                if (!isActive && !isSelected) continue;

                var fallback = Stage.DefaultStagePos(i, total);
                float x = beat != null && beat.pos.hasValue ? beat.pos.value.x : fallback.x;
                float y = beat != null && beat.pos.hasValue ? beat.pos.value.y : fallback.y;
                result.Figures.Add(new StagedFigure
                {
                    Actor = actor,
                    Beat = beat,
                    X = x,
                    Y = y,
                    Scale = Stage.DepthScale(y),
                    IsActive = isActive,
                    IsSelected = isSelected,
                    IsLead = actor == leadActor,
                });
            }
            // Painter's algorithm: smaller y (further back) drawn first; stable tie-break by Id.
            result.Figures.Sort((a, b) =>
            {
                int byY = a.Y.CompareTo(b.Y);
                if (byY != 0) return byY;
                return string.Compare(a.Actor.Id, b.Actor.Id, System.StringComparison.Ordinal);
            });
            return result;
        }
    }
}
