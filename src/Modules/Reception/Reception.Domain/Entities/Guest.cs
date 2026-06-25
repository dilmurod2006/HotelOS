using HotelOS.SharedKernel.Abstractions;
using Reception.Domain.Enums;
using Reception.Domain.ValueObjects;

namespace Reception.Domain.Entities;

/// <summary>
/// Aggregate Root: Guest.
///
/// Stores the minimum personal data necessary for hotel operations.
/// ISO/IEC 27001 — data minimisation principle: we store only what is required
/// for billing and communication. Full payment card details are never stored here;
/// only the last four digits for receipt display.
/// </summary>
public sealed class Guest : AggregateRoot<Guid>
{
    public GuestName Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;

    /// <summary>
    /// Masked card reference — last 4 digits only.
    /// Full PAN is never persisted (PCI DSS compliance intent).
    /// </summary>
    public string? MaskedCardLast4 { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private readonly List<Booking> _bookings = [];
    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    private Guest() { }

    public static Guest Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber)
    {
        ValidateEmail(email);
        ValidatePhone(phoneNumber);

        return new Guest
        {
            Id = Guid.NewGuid(),
            Name = GuestName.Create(firstName, lastName),
            Email = email.Trim().ToLowerInvariant(),
            PhoneNumber = phoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetPaymentCard(string last4Digits)
    {
        if (last4Digits.Length != 4 || !last4Digits.All(char.IsDigit))
            throw new ArgumentException("Card reference must be exactly 4 digits.");

        MaskedCardLast4 = $"****-****-****-{last4Digits}";
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Length > 254)
            throw new ArgumentException("Invalid email address.", nameof(email));
    }

    private static void ValidatePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7 || phone.Length > 20)
            throw new ArgumentException("Phone number must be 7–20 characters.", nameof(phone));
    }
}
