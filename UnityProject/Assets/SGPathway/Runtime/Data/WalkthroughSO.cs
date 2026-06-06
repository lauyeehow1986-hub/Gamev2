using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace SGPathway.Data
{
    [CreateAssetMenu(menuName = "SGPathway/Walkthrough", fileName = "Walkthrough")]
    public sealed class WalkthroughSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private LocalizedString title;
        [SerializeField] private string titleText;
        [SerializeField] private ChapterSO startChapter;
        [SerializeField] private List<ChapterSO> chapters = new List<ChapterSO>();
        [SerializeField] private List<ActorSO> actors = new List<ActorSO>();

        public string Id => id;
        public LocalizedString Title => title;
        public string TitleText => titleText;
        public ChapterSO StartChapter => startChapter;
        public IReadOnlyList<ChapterSO> Chapters => chapters;
        public IReadOnlyList<ActorSO> Actors => actors;

        public ChapterSO FindChapterById(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId)) return null;
            for (int i = 0; i < chapters.Count; i++)
            {
                var c = chapters[i];
                if (c != null && c.Id == chapterId) return c;
            }
            return null;
        }

        public ActorSO FindActorByKey(string actorKey)
        {
            if (string.IsNullOrEmpty(actorKey)) return null;
            for (int i = 0; i < actors.Count; i++)
                if (actors[i] != null && actors[i].Key == actorKey) return actors[i];
            return null;
        }

        internal void InitMeta(string id, string titleText) { this.id = id; this.titleText = titleText; }
        internal void SetStartChapter(ChapterSO c) => startChapter = c;
        internal void SetChapters(List<ChapterSO> c) => chapters = c;
        internal void SetActors(List<ActorSO> a) => actors = a;
    }
}
