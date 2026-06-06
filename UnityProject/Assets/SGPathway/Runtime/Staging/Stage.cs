using UnityEngine;

namespace SGPathway.Staging
{
    /// <summary>
    /// Stage geometry constants — ports <c>src/lib/scenery.tsx</c>:
    /// STAGE_W=480, STAGE_H=270, HORIZON_Y=150. The Unity renderer keeps these
    /// stage units internal so existing chapter content authored against the
    /// SVG/Phaser 480x270 stage drops straight in without remapping.
    /// </summary>
    public static class Stage
    {
        public const float Width = 480f;
        public const float Height = 270f;
        public const float HorizonY = 150f;

        /// <summary>
        /// Deterministic fallback stage position when a beat does not pin one explicitly.
        /// Direct port of <c>defaultStagePos</c>.
        /// </summary>
        public static Vector2 DefaultStagePos(int index, int count)
        {
            int n = count < 1 ? 1 : count;
            const float cx = 250f;
            float spread = Mathf.Min(300f, 60f * n);
            float startX = cx - spread / 2f;
            float step = n > 1 ? spread / (n - 1) : 0f;
            float x = n == 1 ? cx : startX + index * step;
            float y = 228f + ((index % 2 == 0) ? 0f : 16f);
            return new Vector2(x, y);
        }

        /// <summary>Depth scale at a given y — port of <c>depthScale</c>.</summary>
        public static float DepthScale(float y)
        {
            float t = (y - HorizonY) / (Height - HorizonY);
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            return 0.7f + t * 0.6f;
        }
    }
}
