using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace SGPathway.Data
{
    [Serializable]
    public sealed class BranchOption
    {
        public LocalizedString label;
        public LocalizedString hint;
        public string labelText;
        public string hintText;
        public ChapterSO nextChapter;
    }

    [Serializable]
    public sealed class BranchPoint
    {
        public LocalizedString prompt;
        public string promptText;
        public List<BranchOption> options = new List<BranchOption>();
    }

    [CreateAssetMenu(menuName = "SGPathway/Chapter", fileName = "Chapter")]
    public sealed class ChapterSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private LocalizedString title;
        [SerializeField] private string titleText;
        [SerializeField] private SceneKind scene;

        [Min(0)]
        [SerializeField] private float durationSec = 30f;

        [SerializeField] private string timeOfDay;
        [SerializeField] private string location;

        [SerializeField] private List<Beat> beats = new List<Beat>();

        [SerializeField] private BranchPoint branchPoint;
        [SerializeField] private bool hasBranchPoint;

        [SerializeField] private ChapterSO defaultNextChapter;

        public string Id => id;
        public LocalizedString Title => title;
        public string TitleText => titleText;
        public SceneKind Scene => scene;
        public float DurationSec => durationSec;
        public string TimeOfDay => timeOfDay;
        public string Location => location;
        public IReadOnlyList<Beat> Beats => beats;
        public BranchPoint BranchPoint => hasBranchPoint ? branchPoint : null;
        public ChapterSO DefaultNextChapter => defaultNextChapter;

        internal void InitMeta(string id, string titleText, SceneKind scene,
            float durationSec, string timeOfDay, string location)
        {
            this.id = id; this.titleText = titleText; this.scene = scene;
            this.durationSec = durationSec; this.timeOfDay = timeOfDay; this.location = location;
        }
        internal void SetBeats(List<Beat> b) => beats = b;
        internal void SetBranchPoint(BranchPoint bp) { branchPoint = bp; hasBranchPoint = bp != null; }
        internal void SetDefaultNext(ChapterSO c) => defaultNextChapter = c;
    }
}
