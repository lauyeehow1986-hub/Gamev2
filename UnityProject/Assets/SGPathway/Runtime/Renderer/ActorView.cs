using UnityEngine;
using SGPathway.Data;

namespace SGPathway.Renderer
{
    /// <summary>
    /// Visual representation of one actor on the stage. Owned by the renderer;
    /// receives pose / expression / direction updates and animates accordingly.
    ///
    /// Sprite rig is intentionally not wired here yet — the v9.7-v9.8 web sprite
    /// system (deterministic per-actor SVGs + 7 poses + 7 expressions + 4-dir walk)
    /// needs to be re-baked as a Unity sprite atlas before this hooks up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer face;
        [SerializeField] private Animator animator;

        public ActorSO Actor { get; private set; }

        public void Bind(ActorSO actor)
        {
            Actor = actor;
            if (body != null) body.color = actor != null ? actor.Swatch : Color.white;
        }

        public void ApplyBeat(Beat beat, bool isLead)
        {
            if (animator == null || beat == null) return;
            animator.SetInteger("Pose", (int)beat.pose);
            animator.SetInteger("Expression", (int)beat.expression);
            animator.SetInteger("Direction", (int)beat.direction);
            animator.SetBool("Walking", beat.walking);
            animator.SetBool("IsLead", isLead);
        }
    }
}
