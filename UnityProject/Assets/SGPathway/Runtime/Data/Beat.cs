using System;
using UnityEngine;
using UnityEngine.Localization;

namespace SGPathway.Data
{
    [Serializable]
    public struct OptionalVector2
    {
        public bool hasValue;
        public Vector2 value;
        public static OptionalVector2 None => new OptionalVector2 { hasValue = false, value = default };
        public static OptionalVector2 Of(Vector2 v) => new OptionalVector2 { hasValue = true, value = v };
    }

    [Serializable]
    public struct Showpiece
    {
        public ShowpieceKind kind;
        public string externalSrc;
        public string posterSrc;
        public LocalizedString title;
        public LocalizedString caption;
        public bool loop;
    }

    [Serializable]
    public sealed class Beat
    {
        [Tooltip("Seconds from the start of the containing Chapter.")]
        public float at;

        [Tooltip("Actor performing this beat. Resolved by WalkthroughSO actors (by key).")]
        public ActorSO actor;

        [Tooltip("Short prose of what they do at this beat (localized).")]
        public LocalizedString action;

        [Tooltip("English literal of the action (P2: fold into LocalizedString tables).")]
        public string actionText;

        [Tooltip("Scene-relative camera focus (0..1 normalised), optional.")]
        public OptionalVector2 focus;

        [Tooltip("Facing direction.")]
        public BeatDirection direction;

        [Tooltip("Play the walk cycle in addition to the interaction loop.")]
        public bool walking;

        [Tooltip("Explicit stage position (480x270 stage units). When unset, fall back to deterministic staging arc.")]
        public OptionalVector2 pos;

        public BeatPose pose;
        public BeatExpression expression;

        public Showpiece showpiece;
    }
}
