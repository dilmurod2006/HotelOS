namespace Housekeeping.Domain.Enums;

/// <summary>
/// Determines the order in which cleaning tasks are assigned.
/// A room expected for early check-in is Rush; a standard vacated room is Normal.
/// </summary>
public enum CleaningPriority
{
    Rush = 1,     // Guest arriving within 2 hours; room needed urgently
    Normal = 2    // Standard post-checkout cleaning
}
