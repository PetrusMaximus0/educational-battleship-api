namespace api.Enums;

public enum EClientState
{
    FleetSetup,
    FleetReady,
    WaitingForTurn,
    OnTurn,
    ApprovingShot,
    PendingShotApproval,
    Defeated,
    Victor,
}