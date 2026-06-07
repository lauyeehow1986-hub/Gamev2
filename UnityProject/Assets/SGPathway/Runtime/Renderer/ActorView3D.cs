using UnityEngine;
using SGPathway.Data;

namespace SGPathway.Renderer
{
    /// <summary>
    /// One staged figure in 3D. The <see cref="visual"/> child is a placeholder
    /// now; a Character-Creator-4-rigged model replaces it later under the same
    /// transform contract. Pose/expression/direction will drive an Animator once
    /// the rig is in (no-op for the placeholder).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorView3D : MonoBehaviour
    {
        [SerializeField] private Transform visual;        // body root (placeholder capsule / CC4 model)
        [SerializeField] private UnityEngine.Renderer bodyRenderer;   // tinted by team swatch
        [SerializeField] private GameObject leadRing;     // speaker highlight
        [SerializeField] private TMPro.TMP_Text label;    // role nameplate

        public ActorSO Actor { get; private set; }
        private MaterialPropertyBlock _mpb;

        public void Bind(ActorSO actor)
        {
            Actor = actor;
            if (label != null) label.text = actor != null ? actor.RoleText : "";
            if (bodyRenderer != null && actor != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                bodyRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", actor.Swatch);
                bodyRenderer.SetPropertyBlock(_mpb);
            }
        }

        public void Apply(Beat beat, bool isLead)
        {
            if (leadRing != null) leadRing.SetActive(isLead);
        }
    }
}
