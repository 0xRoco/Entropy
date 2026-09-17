namespace Entropy.Game.Components;

public sealed class ArrivalState
{
    public string LegalIdentityStatus { get; set; } = "unregistered";
    public bool HasTemporaryPermit { get; set; }
    public int PermitExpiryMinute { get; set; }
    public bool EntryRestricted { get; set; } = true;
    public bool HasEnteredSpire { get; set; }

    public bool HasValidPermit(int elapsedMinutes) =>
        HasTemporaryPermit && elapsedMinutes < PermitExpiryMinute;
}
