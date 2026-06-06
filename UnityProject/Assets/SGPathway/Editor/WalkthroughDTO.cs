using System;
using System.Collections.Generic;

namespace SGPathway.EditorTools
{
    [Serializable]
    public class WalkthroughDTO
    {
        public string id;
        public string title;
        public string startChapterRef;
        public List<ActorDTO> actors = new List<ActorDTO>();
        public List<ChapterDTO> chapters = new List<ChapterDTO>();
    }

    [Serializable]
    public class ActorDTO
    {
        public string key, id, role, team, bio, swatch;
    }

    [Serializable]
    public class ChapterDTO
    {
        public string key, id, title, scene, timeOfDay, location, defaultNextChapterRef;
        public float durationSec;
        public bool hasDefaultNext, hasBranchPoint;
        public BranchPointDTO branchPoint;
        public List<BeatDTO> beats = new List<BeatDTO>();
    }

    [Serializable]
    public class BranchPointDTO
    {
        public string prompt;
        public List<BranchOptionDTO> options = new List<BranchOptionDTO>();
    }

    [Serializable]
    public class BranchOptionDTO
    {
        public string label, hint, nextChapterRef;
    }

    [Serializable]
    public class BeatDTO
    {
        public float at;
        public string actorRef, action;
        public bool hasPos;
        public float posX, posY;
        public string direction, pose, expression, showpieceKind, showpieceSvgId;
        public bool walking;
    }
}
