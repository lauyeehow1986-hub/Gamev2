namespace SGPathway.Data
{
    public enum ActorTeam
    {
        Patient,
        Bystander,
        FirstResponder,
        Ambulance,
        ED,
        Cath,
        Ward,
        Rehab,
        Outpatient,
        Support,
    }

    public enum BeatPose
    {
        Stand,
        Walk,
        Kneel,
        Sit,
        Cpr,
        Collapsed,
        Point,
    }

    public enum BeatExpression
    {
        Neutral,
        Alarmed,
        Distressed,
        Pained,
        Focused,
        Relieved,
        Unconscious,
    }

    public enum BeatDirection
    {
        S,
        N,
        E,
        W,
    }

    public enum SceneKind
    {
        Unspecified,
        Kopitiam,
        Street,
        Mrt,
        Resus,
        Cathlab,
        Imaging,
        Counsel,
        Ward,
        Pharmacy,
        Rehab,
        Clinic,
        Backhouse,
    }

    public enum ShowpieceKind
    {
        None,
        StentDeployment,
        MriBoreSlide,
        AedShock,
        ThrombectomyPass,
        ExternalMp4,
    }
}
