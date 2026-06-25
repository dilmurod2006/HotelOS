using HotelOS.SharedKernel.Abstractions;
using Reception.Domain.Enums;
using Reception.Domain.Events;
using Reception.Domain.ValueObjects;

namespace Reception.Domain.Entities;

/// <summary>
/// Aggregate Root: Booking.
///
/// A Booking ties a Guest to a Room for a specific DateRange.
/// The Billing Algorithm lives in ComputeFinalBill() — all financial logic
/// is centralised here so it cannot be accidentally bypassed by callers.
///
/// Billing Formula (per BTEC spec):
///   Total = (NightlyRate × NightCount) + RoomServiceCharges + ExtraFees
/// </summary>
public sealed class Booking : AggregateRoot<Guid>
{
    // ── Persistent Properties ─────────────────────────────────────────────────

    public Guid GuestId { get; private set; }
    public Guid RoomId { get; private set; }
    public DateRange StayPeriod { get; private set; } = null!;
    public BookingStatus Status { get; private set; }
    public RoomType RequestedRoomType { get; private set; }
    public int? PreferredFloor { get; private set; }
    public ProximityPreference ProximityPreference { get; private set; }

    /// <summary>Base charge: NightlyRate × NightCount.</summary>
    public Money RoomCharge { get; private set; } = null!;

    /// <summary>Sum of all Room Service orders billed to this booking.</summary>
    public Money RoomServiceCharges { get; private set; } = null!;

    /// <summary>
    /// Miscellaneous extra fees: minibar consumption, late check-out penalty,
    /// damage deposits, etc. Each fee is itemised for audit transparency.
    /// </summary>
    public Money ExtraFees { get; private set; } = null!;

    /// <summary>Computed final bill. Null until check-out is processed.</summary>
    public Money? TotalBill { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core navigation — not loaded by default (avoids N+1)
    public Guest Guest { get; private set; } = null!;
    public Room Room { get; private set; } = null!;

    private readonly List<ExtraFeeLineItem> _extraFeeLineItems = [];
    public IReadOnlyCollection<ExtraFeeLineItem> ExtraFeeLineItems => _extraFeeLineItems.AsReadOnly();

    private Booking() { }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Booking Create(
        Guid guestId,
        Guid roomId,
        DateRange stayPeriod,
        Money nightlyRate,
        RoomType requestedRoomType,
        int? preferredFloor = null,
        ProximityPreference proximityPreference = ProximityPreference.None)
    {
        var roomCharge = nightlyRate.Multiply(stayPeriod.NightCount);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            GuestId = guestId,
            RoomId = roomId,
            StayPeriod = stayPeriod,
            Status = BookingStatus.Pending,
            RequestedRoomType = requestedRoomType,
            PreferredFloor = preferredFloor,
            ProximityPreference = proximityPreference,
            RoomCharge = roomCharge,
            RoomServiceCharges = Money.Zero(nightlyRate.Currency),
            ExtraFees = Money.Zero(nightlyRate.Currency),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return booking;
    }

    // ── State Transitions ─────────────────────────────────────────────────────

    public void ConfirmPayment()
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only a Pending booking can be confirmed.");

        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Booking must be Confirmed before check-in.");

        Status = BookingStatus.CheckedIn;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Billing Algorithm — BTEC spec requirement.
    /// Computes the complete itemised bill at check-out time.
    ///
    /// Algorithm steps:
    /// 1. Base room charge already computed at booking creation (nightlyRate × nights).
    /// 2. Add all room service charges accumulated during the stay.
    /// 3. Add miscellaneous extra fees (minibar, late check-out penalty, etc.).
    /// 4. Apply any discount if applicable (future extension point).
    /// 5. Set TotalBill and transition status to CheckedOut.
    /// </summary>
    public Money CheckOut()
    {
        if (Status != BookingStatus.CheckedIn)
            throw new InvalidOperationException("Booking must be in CheckedIn status to check out.");

        // Step 1 + 2 + 3: Sum all components
        TotalBill = RoomCharge
            .Add(RoomServiceCharges)
            .Add(ExtraFees);

        Status = BookingStatus.CheckedOut;
        UpdatedAt = DateTime.UtcNow;

        return TotalBill;
    }

    public void Cancel()
    {
        if (Status is BookingStatus.CheckedOut or BookingStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a booking with status {Status}.");

        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BookingCancelledEvent(Id, RoomId));
    }

    // ── Financial Accumulation ─────────────────────────────────────────────

    /// <summary>
    /// Called by the Room Service module via MediatR event after each delivery.
    /// Accumulates charges on the correct booking for accurate billing.
    /// </summary>
    public void AddRoomServiceCharge(Money amount)
    {
        if (Status != BookingStatus.CheckedIn)
            throw new InvalidOperationException("Cannot add charges to a booking that is not active.");

        RoomServiceCharges = RoomServiceCharges.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddExtraFee(string description, Money amount)
    {
        if (Status != BookingStatus.CheckedIn)
            throw new InvalidOperationException("Cannot add fees to a booking that is not active.");

        _extraFeeLineItems.Add(new ExtraFeeLineItem(description, amount));
        ExtraFees = ExtraFees.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Owned entity — persisted in the same table as Booking via EF Core Table-Per-Hierarchy.
/// Provides an itemised audit trail of extra charges.
/// </summary>
public sealed class ExtraFeeLineItem
{
    public string Description { get; } = null!;
    public Money Amount { get; } = null!;
    public DateTime AddedAt { get; } = DateTime.UtcNow;

    private ExtraFeeLineItem() { }

    public ExtraFeeLineItem(string description, Money amount)
    {
        Description = description;
        Amount = amount;
    }
}
