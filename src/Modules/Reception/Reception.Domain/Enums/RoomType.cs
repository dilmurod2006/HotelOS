namespace Reception.Domain.Enums;

/// <summary>
/// Categorises rooms by capacity and amenity level.
/// Room assignment algorithm uses this as its PRIMARY filter:
/// only rooms whose type matches the booking request are candidates.
/// </summary>
public enum RoomType
{
    Single = 1,
    Double = 2,
    Suite = 3,
    AccessibleSingle = 4,   // ADA/disability-accessible single
    AccessibleDouble = 5    // ADA/disability-accessible double
}
