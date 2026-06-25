namespace Reception.Domain.ValueObjects;

/// <summary>
/// Value Object: DateRange.
/// Encapsulates a booking's check-in and check-out window. The billing algorithm
/// uses NightCount to calculate the base charge without any caller needing to
/// know the date arithmetic — a clean example of abstraction.
/// </summary>
public sealed record DateRange
{
    public DateOnly CheckIn { get; }
    public DateOnly CheckOut { get; }

    /// <summary>Number of billable nights. Minimum is 1 (same-day bookings not allowed).</summary>
    public int NightCount => CheckOut.DayNumber - CheckIn.DayNumber;

    private DateRange(DateOnly checkIn, DateOnly checkOut)
    {
        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    public static DateRange Create(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
            throw new ArgumentException("Check-out must be after check-in.");
        if (checkIn < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new ArgumentException("Check-in cannot be in the past.");

        return new DateRange(checkIn, checkOut);
    }

    public bool OverlapsWith(DateRange other)
        => CheckIn < other.CheckOut && CheckOut > other.CheckIn;

    public override string ToString()
        => $"{CheckIn:yyyy-MM-dd} → {CheckOut:yyyy-MM-dd} ({NightCount} nights)";
}
