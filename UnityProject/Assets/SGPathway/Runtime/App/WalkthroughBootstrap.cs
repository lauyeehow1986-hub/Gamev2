using UnityEngine;
using SGPathway.Engine;

namespace SGPathway.App
{
    /// <summary>
    /// Starts the player at a named chapter on Start (so a single scene can preview
    /// any chapter without re-authoring the WalkthroughSO's StartChapter).
    /// Leave <see cref="startChapterId"/> empty to use the walkthrough's StartChapter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WalkthroughBootstrap : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private string startChapterId;

        private void Start()
        {
            if (player == null || player.Walkthrough == null) return;
            var chapter = !string.IsNullOrEmpty(startChapterId)
                ? player.Walkthrough.FindChapterById(startChapterId)
                : player.Walkthrough.StartChapter;
            if (chapter != null) player.Play(chapter);
        }
    }
}
