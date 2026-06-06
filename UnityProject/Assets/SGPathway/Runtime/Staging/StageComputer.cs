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

            ActorSO leadActor = null;
            float leadAt = float.NegativeInfinity;
            foreach (var kv in activeByActor)
            {
                if (kv.Value.at > leadAt)
                {
                    leadAt = kv.Value.at;
                    leadActor = kv.Key;
                }
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
            // Painter's algorithm: figures with smaller y (further back) drawn first.
            result.Figures.Sort((a, b) => a.Y.CompareTo(b.Y));
            return result;
        }
    }
}
