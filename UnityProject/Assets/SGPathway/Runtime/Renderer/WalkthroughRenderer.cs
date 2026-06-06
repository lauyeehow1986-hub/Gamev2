using System.Collections.Generic;
using UnityEngine;
using SGPathway.Data;
using SGPathway.Engine;
using SGPathway.Staging;

namespace SGPathway.Renderer
{
    /// <summary>
    /// Consumes <see cref="WalkthroughPlayer"/> ticks and projects the stage
    /// (480x270 logical units) into world space, instantiating one
    /// <see cref="ActorView"/> per staged figure.
    ///
    /// The renderer is intentionally event-driven so it can be swapped for a
    /// 3D Cinemachine setup later without touching the data/engine layers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WalkthroughRenderer : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private ActorView actorPrefab;
        [SerializeField] private Transform stageRoot;
        [SerializeField] private float worldUnitsPerStageUnit = 0.01f;

        private readonly Dictionary<ActorSO, ActorView> _views = new Dictionary<ActorSO, ActorView>();
        private ActorSO _selectedActor;

        private void OnEnable()
        {
            if (player == null) return;
            player.OnChapterEntered.AddListener(OnChapterEntered);
            player.OnTick.AddListener(OnTick);
        }

        private void OnDisable()
        {
            if (player == null) return;
            player.OnChapterEntered.RemoveListener(OnChapterEntered);
            player.OnTick.RemoveListener(OnTick);
        }

        public void SelectActor(ActorSO actor) => _selectedActor = actor;

        private void OnChapterEntered(ChapterSO _)
        {
            foreach (var kv in _views)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _views.Clear();
        }

        private void OnTick(ChapterSO chapter, float localSec)
        {
            var active = BeatsCalculator.ActiveByActor(chapter, localSec);
            var staging = StageComputer.StageFigures(chapter, active, _selectedActor);

            var liveSet = new HashSet<ActorSO>();
            for (int i = 0; i < staging.Figures.Count; i++)
            {
                var f = staging.Figures[i];
                liveSet.Add(f.Actor);
                if (!_views.TryGetValue(f.Actor, out var view) || view == null)
                {
                    view = Instantiate(actorPrefab, stageRoot != null ? stageRoot : transform);
                    view.Bind(f.Actor);
                    _views[f.Actor] = view;
                }
                view.transform.localPosition = StageToWorld(f.X, f.Y);
                view.transform.localScale = Vector3.one * f.Scale;
                view.ApplyBeat(f.Beat, f.IsLead);
            }

            foreach (var kv in _views)
            {
                if (kv.Value == null) continue;
                kv.Value.gameObject.SetActive(liveSet.Contains(kv.Key));
            }
        }

        private Vector3 StageToWorld(float x, float y)
        {
            // Centre the 480x270 stage on origin; invert Y so larger stage-Y is "lower" / closer.
            float wx = (x - Stage.Width * 0.5f) * worldUnitsPerStageUnit;
            float wy = -(y - Stage.Height * 0.5f) * worldUnitsPerStageUnit;
            // Use stage-Y as a sort key so painter ordering survives perspective.
            return new Vector3(wx, wy, y * worldUnitsPerStageUnit);
        }
    }
}
