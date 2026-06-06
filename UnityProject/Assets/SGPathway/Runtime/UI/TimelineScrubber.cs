using UnityEngine;
using UnityEngine.UI;
using SGPathway.Data;
using SGPathway.Engine;

namespace SGPathway.UI
{
    /// <summary>
    /// Minimal scrubber: a Slider mapped to the canonical timeline. Clicking
    /// jumps to the corresponding chapter + local-second.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimelineScrubber : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private Slider slider;

        private float _totalSec;
        private bool _suppressEvent;

        private void OnEnable()
        {
            if (player != null) player.OnTick.AddListener(OnTick);
            if (slider != null) slider.onValueChanged.AddListener(OnSliderChanged);
            if (player != null && player.Walkthrough != null)
                _totalSec = BeatsCalculator.CanonicalDurationSec(player.Walkthrough);
        }

        private void OnDisable()
        {
            if (player != null) player.OnTick.RemoveListener(OnTick);
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        private void OnTick(ChapterSO chapter, float localSec)
        {
            if (slider == null || _totalSec <= 0f) return;
            float globalSec = LocalToGlobal(chapter, localSec);
            _suppressEvent = true;
            slider.value = globalSec / _totalSec;
            _suppressEvent = false;
        }

        private void OnSliderChanged(float t)
        {
            if (_suppressEvent || player == null || player.Walkthrough == null) return;
            var location = BeatsCalculator.LocateInCanonical(player.Walkthrough, t * _totalSec);
            if (!location.HasValue) return;
            if (player.CurrentChapter != location.Value.Chapter) player.Play(location.Value.Chapter);
            player.Scrub(location.Value.LocalSec);
        }

        private float LocalToGlobal(ChapterSO chapter, float localSec)
        {
            float sum = 0f;
            foreach (var c in BeatsCalculator.CanonicalChapters(player.Walkthrough))
            {
                if (c == chapter) return sum + localSec;
                sum += c.DurationSec;
            }
            return sum;
        }
    }
}
