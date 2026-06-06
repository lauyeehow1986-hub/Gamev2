using System.Collections.Generic;
using SGPathway.Data;

namespace SGPathway.Engine
{
    /// <summary>
    /// Pure-function port of <c>src/lib/walkthrough.ts</c> (beatsAt, canonicalChapterIds,
    /// canonicalDurationSec, locateInCanonical, nextChapterId).
    /// Kept side-effect-free so it is unit-testable without a Unity scene.
    /// </summary>
    public static class BeatsCalculator
    {
        /// <summary>
        /// Beats that should be "active" at time <paramref name="t"/> seconds into the chapter.
        /// A beat is active from its declared <c>at</c> until the next beat for the same actor.
        /// </summary>
        public static List<Beat> BeatsAt(ChapterSO chapter, float t)
        {
            var result = new List<Beat>();
            if (chapter == null) return result;
            float clamped = t < 0f ? 0f : (t > chapter.DurationSec ? chapter.DurationSec : t);

            var byActor = new Dictionary<ActorSO, List<Beat>>();
            foreach (var b in chapter.Beats)
            {
                if (b == null || b.actor == null) continue;
                if (!byActor.TryGetValue(b.actor, out var list))
                {
                    list = new List<Beat>();
                    byActor[b.actor] = list;
                }
                list.Add(b);
            }

            foreach (var kv in byActor)
            {
                var sorted = kv.Value;
                sorted.Sort((a, b) => a.at.CompareTo(b.at));
                Beat chosen = null;
                foreach (var b in sorted)
                {
                    if (b.at <= clamped) chosen = b;
                    else break;
                }
                if (chosen != null) result.Add(chosen);
            }
            return result;
        }

        /// <summary>
        /// Group <see cref="BeatsAt"/> output by actor for downstream consumers (staging).
        /// </summary>
        public static Dictionary<ActorSO, Beat> ActiveByActor(ChapterSO chapter, float t)
        {
            var map = new Dictionary<ActorSO, Beat>();
            foreach (var b in BeatsAt(chapter, t)) map[b.actor] = b;
            return map;
        }

        /// <summary>
        /// Resolve the next chapter given the current one and an optional learner-picked
        /// branch index. Returns null when the walkthrough ends.
        /// </summary>
        public static ChapterSO NextChapter(WalkthroughSO walkthrough, ChapterSO current, int? pickedBranchIndex)
        {
            if (walkthrough == null || current == null) return null;
            var branch = current.BranchPoint;
            if (branch != null && pickedBranchIndex.HasValue)
            {
                int idx = pickedBranchIndex.Value;
                if (idx < 0 || idx >= branch.options.Count) return null;
                var picked = branch.options[idx];
                return picked != null ? picked.nextChapter : null;
            }
            return current.DefaultNextChapter;
        }

        /// <summary>
        /// Linear chapter ids reachable from start following only <c>defaultNextChapter</c>.
        /// Branch points are ignored (learner-driven). Used by the scrubber to render the
        /// canonical timeline.
        /// </summary>
        public static List<ChapterSO> CanonicalChapters(WalkthroughSO walkthrough)
        {
            var result = new List<ChapterSO>();
            if (walkthrough == null) return result;
            var seen = new HashSet<ChapterSO>();
            var cursor = walkthrough.StartChapter;
            while (cursor != null && seen.Add(cursor))
            {
                result.Add(cursor);
                cursor = cursor.DefaultNextChapter;
            }
            return result;
        }

        /// <summary>Sum of canonical-chapter durations.</summary>
        public static float CanonicalDurationSec(WalkthroughSO walkthrough)
        {
            float total = 0f;
            foreach (var c in CanonicalChapters(walkthrough)) total += c.DurationSec;
            return total;
        }

        public readonly struct CanonicalLocation
        {
            public CanonicalLocation(ChapterSO chapter, float localSec)
            {
                Chapter = chapter;
                LocalSec = localSec;
            }
            public ChapterSO Chapter { get; }
            public float LocalSec { get; }
        }

        /// <summary>Convert a global second-offset to (chapter, local-second).</summary>
        public static CanonicalLocation? LocateInCanonical(WalkthroughSO walkthrough, float globalSec)
        {
            var ids = CanonicalChapters(walkthrough);
            if (ids.Count == 0) return null;
            float remaining = globalSec < 0f ? 0f : globalSec;
            for (int i = 0; i < ids.Count; i++)
            {
                var c = ids[i];
                if (remaining <= c.DurationSec) return new CanonicalLocation(c, remaining);
                remaining -= c.DurationSec;
            }
            var last = ids[ids.Count - 1];
            return new CanonicalLocation(last, last.DurationSec);
        }
    }
}
