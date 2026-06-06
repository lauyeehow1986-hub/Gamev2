using System;
using UnityEngine;
using SGPathway.Data;
using SGPathway.Engine;

namespace SGPathway.Renderer
{
    /// <summary>
    /// Swaps the stage backdrop sprite when the player enters a new chapter.
    /// Uses the v9.14 pre-baked 1440x810 HD PNGs (one per <see cref="SceneKind"/>)
    /// as a first pass; v2 of this view should add the v9.14.1 per-scene mood
    /// (warm / daylight / sterile / surgical / transit / industrial) via URP volume.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneryView : MonoBehaviour
    {
        [Serializable]
        public struct SceneEntry
        {
            public SceneKind kind;
            public Sprite sprite;
        }

        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private SpriteRenderer backdrop;
        [SerializeField] private SceneEntry[] scenes;

        private void OnEnable()
        {
            if (player != null) player.OnChapterEntered.AddListener(OnChapterEntered);
        }

        private void OnDisable()
        {
            if (player != null) player.OnChapterEntered.RemoveListener(OnChapterEntered);
        }

        private void OnChapterEntered(ChapterSO chapter)
        {
            if (chapter == null || backdrop == null) return;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].kind == chapter.Scene)
                {
                    backdrop.sprite = scenes[i].sprite;
                    return;
                }
            }
        }
    }
}
