namespace Reception.Domain.Enums;

/// <summary>
/// Guest proximity preference used as the tiebreaker in the room assignment algorithm.
/// Applied only after type, cleanliness, longest-clean, and floor filters are exhausted.
/// </summary>
public enum ProximityPreference
{
    None = 0,
    NearElevator = 1,
    NearStaircase = 2
}
