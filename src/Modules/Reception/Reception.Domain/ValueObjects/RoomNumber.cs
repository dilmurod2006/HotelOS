namespace Reception.Domain.ValueObjects;

/// <summary>
/// Value Object: RoomNumber.
///
/// C# records provide structural equality by default — two RoomNumbers with the same
/// value are equal without manually implementing Equals/GetHashCode.
/// This is the OOP principle of Encapsulation: the format rule (floor * 100 + unit)
/// is enforced here, not scattered across the codebase.
/// </summary>
public sealed record RoomNumber
{
    public int Floor { get; }
    public int Unit { get; }
    public string Value { get; }

    private RoomNumber(int floor, int unit)
    {
        Floor = floor;
        Unit = unit;
        // Convention: floor 1, unit 5 → "105"; floor 3, unit 12 → "312"
        Value = $"{floor}{unit:D2}";
    }

    /// <summary>
    /// Factory method with invariant enforcement. Rooms must be on floors 1–6,
    /// with up to 20 units per floor, matching GrandStay's physical layout.
    /// </summary>
    public static RoomNumber Create(int floor, int unit)
    {
        if (floor < 1 || floor > 6)
            throw new ArgumentOutOfRangeException(nameof(floor), "Floor must be between 1 and 6.");
        if (unit < 1 || unit > 20)
            throw new ArgumentOutOfRangeException(nameof(unit), "Unit must be between 1 and 20.");

        return new RoomNumber(floor, unit);
    }

    public override string ToString() => Value;
}
