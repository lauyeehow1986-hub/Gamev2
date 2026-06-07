using System.Collections.Generic;
using UnityEngine;
using SGPathway.Data;
using SGPathway.Engine;
using SGPathway.Staging;

namespace SGPathway.Renderer
{
    /// <summary>
    /// Projects the 480×270 stage into 3D and stages one <see cref="ActorView3D"/>
    /// per figure. Stage x→world X, stage y→world Z (depth); feet on the Y=0 floor.
    /// No depthScale — a real perspective camera supplies foreshortening.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WalkthroughView3D : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private ActorView3D actorPrefab;
        [SerializeField] private Transform stageRoot;
        [SerializeField] private float worldUnitsPerStageUnit = 0.05f;

        private readonly Dictionary<ActorSO, ActorView3D> _views = new Dictionary<ActorSO, ActorView3D>();
        private ActorSO _selected;

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

        public void SelectActor(ActorSO actor) => _selected = actor;

        private void OnChapterEntered(ChapterSO _)
        {
            foreach (var kv in _views) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _views.Clear();
        }

        private void OnTick(ChapterSO chapter, float t)
        {
            var active = BeatsCalculator.ActiveByActor(chapter, t);
            var staging = StageComputer.StageFigures(chapter, active, _selected);
            var live = new HashSet<ActorSO>();
            for (int i = 0; i < staging.Figures.Count; i++)
            {
                var f = staging.Figures[i];
                live.Add(f.Actor);
                if (!_views.TryGetValue(f.Actor, out var v) || v == null)
                {
                    v = Instantiate(actorPrefab, stageRoot != null ? stageRoot : transform);
                    v.Bind(f.Actor);
                    _views[f.Actor] = v;
                }
                v.transform.localPosition = StageToWorld(f.X, f.Y);
                v.Apply(f.Beat, f.IsLead);
            }
            foreach (var kv in _views)
                if (kv.Value != null) kv.Value.gameObject.SetActive(live.Contains(kv.Key));
        }

        private Vector3 StageToWorld(float x, float y)
            => new Vector3((x - Stage.Width * 0.5f) * worldUnitsPerStageUnit, 0f,
                           (Stage.HorizonY - y) * worldUnitsPerStageUnit);
    }
}
