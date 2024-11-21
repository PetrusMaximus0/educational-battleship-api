namespace api.Enums;

public enum EClientState
{
    Disconnected,
    NotInSession,
    JoinedSession,
    FleetSetup,
    FleetReady,
    WaitingForTurn,
    OnTurn,
}