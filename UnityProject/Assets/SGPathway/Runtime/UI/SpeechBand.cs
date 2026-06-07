using UnityEngine;
using SGPathway.Data;
using SGPathway.Engine;
using SGPathway.Staging;

namespace SGPathway.UI
{
    /// <summary>
    /// Top broadcast caption: shows the current lead actor's beat <c>actionText</c>
    /// and their role. Driven by <see cref="WalkthroughPlayer.OnTick"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpeechBand : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private TMPro.TMP_Text caption;
        [SerializeField] private TMPro.TMP_Text speaker;

        private void OnEnable() { if (player != null) player.OnTick.AddListener(OnTick); }
        private void OnDisable() { if (player != null) player.OnTick.RemoveListener(OnTick); }

        private void OnTick(ChapterSO chapter, float t)
        {
            var active = BeatsCalculator.ActiveByActor(chapter, t);
            var staging = StageComputer.StageFigures(chapter, active, null);
            if (staging.LeadActor != null && active.TryGetValue(staging.LeadActor, out var beat))
            {
                if (caption != null) caption.text = beat.actionText;
                if (speaker != null) speaker.text = staging.LeadActor.RoleText;
            }
        }
    }
}
