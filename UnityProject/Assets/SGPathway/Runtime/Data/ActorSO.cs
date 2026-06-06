using UnityEngine;
using UnityEngine.Localization;

namespace SGPathway.Data
{
    [CreateAssetMenu(menuName = "SGPathway/Actor", fileName = "Actor")]
    public sealed class ActorSO : ScriptableObject
    {
        [SerializeField] private string key;      // actors-map KEY — the identity beats reference
        [SerializeField] private string id;       // inner id (provenance; may differ from key in Stroke)
        [SerializeField] private string roleText; // English literal (P2: move to LocalizedString)
        [SerializeField] private LocalizedString role;
        [SerializeField] private ActorTeam team;
        [SerializeField] private string bioText;
        [SerializeField] private LocalizedString bio;
        [SerializeField] private Color swatch = Color.white;

        public string Key => key;
        public string Id => id;
        public string RoleText => roleText;
        public LocalizedString Role => role;
        public ActorTeam Team => team;
        public string BioText => bioText;
        public LocalizedString Bio => bio;
        public Color Swatch => swatch;

        internal void Init(string key, string id, string roleText, ActorTeam team, string bioText, Color swatch)
        {
            this.key = key; this.id = id; this.roleText = roleText;
            this.team = team; this.bioText = bioText; this.swatch = swatch;
        }
    }
}
