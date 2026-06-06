using System;
using UnityEngine;
using UnityEngine.Events;
using SGPathway.Data;

namespace SGPathway.Engine
{
    /// <summary>
    /// Drives a <see cref="WalkthroughSO"/>. Holds the play cursor, advances time,
    /// pauses at branch points, and raises events the renderer / UI subscribe to.
    ///
    /// Renderer integration is event-based so the same player can drive a 2D sprite
    /// renderer, a Cinemachine-based 3D camera rig, or a Timeline-bound scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WalkthroughPlayer : MonoBehaviour
    {
        [SerializeField] private WalkthroughSO walkthrough;
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Min(0.1f)] private float timeScale = 1f;

        public UnityEvent<ChapterSO> OnChapterEntered;
        public UnityEvent<ChapterSO> OnChapterExited;
        public UnityEvent<ChapterSO, float> OnTick;
        public UnityEvent<ChapterSO, BranchPoint> OnBranchPrompt;
        public UnityEvent OnFinished;

        public WalkthroughSO Walkthrough => walkthrough;
        public ChapterSO CurrentChapter { get; private set; }
        public float LocalTime { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsAwaitingBranchChoice { get; private set; }

        private void Start()
        {
            if (walkthrough == null) return;
            if (playOnStart) Play(walkthrough.StartChapter);
        }

        private void Update()
        {
            if (!IsPlaying || CurrentChapter == null || IsAwaitingBranchChoice) return;
            LocalTime += Time.deltaTime * timeScale;
            OnTick?.Invoke(CurrentChapter, LocalTime);
            if (LocalTime >= CurrentChapter.DurationSec)
            {
                if (CurrentChapter.BranchPoint != null)
                {
                    IsAwaitingBranchChoice = true;
                    OnBranchPrompt?.Invoke(CurrentChapter, CurrentChapter.BranchPoint);
                    return;
                }
                Advance(CurrentChapter.DefaultNextChapter);
            }
        }

        public void Play(ChapterSO chapter)
        {
            if (chapter == null) return;
            if (CurrentChapter != null) OnChapterExited?.Invoke(CurrentChapter);
            CurrentChapter = chapter;
            LocalTime = 0f;
            IsPlaying = true;
            IsAwaitingBranchChoice = false;
            OnChapterEntered?.Invoke(chapter);
        }

        public void Pause() => IsPlaying = false;
        public void Resume() => IsPlaying = true;

        /// <summary>Scrub to an absolute second within the current chapter.</summary>
        public void Scrub(float localSec)
        {
            if (CurrentChapter == null) return;
            LocalTime = Mathf.Clamp(localSec, 0f, CurrentChapter.DurationSec);
            OnTick?.Invoke(CurrentChapter, LocalTime);
        }

        /// <summary>Pick a branch option to resolve a pending decision.</summary>
        public void ChooseBranch(int branchOptionIndex)
        {
            if (!IsAwaitingBranchChoice || CurrentChapter == null) return;
            var next = BeatsCalculator.NextChapter(walkthrough, CurrentChapter, branchOptionIndex);
            Advance(next);
        }

        private void Advance(ChapterSO next)
        {
            if (next == null)
            {
                OnChapterExited?.Invoke(CurrentChapter);
                CurrentChapter = null;
                IsPlaying = false;
                IsAwaitingBranchChoice = false;
                OnFinished?.Invoke();
                return;
            }
            Play(next);
        }
    }
}
