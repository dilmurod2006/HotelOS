namespace Reception.Domain.ValueObjects;

/// <summary>
/// Value Object: GuestName.
/// Centralises name validation and formatting rules.
/// ISO/IEC 27001 — personal data must be validated at the boundary to prevent
/// injection attacks and data integrity issues.
/// </summary>
public sealed record GuestName
{
    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";

    private GuestName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static GuestName Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 50)
            throw new ArgumentException("First name must be 1–50 characters.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 50)
            throw new ArgumentException("Last name must be 1–50 characters.", nameof(lastName));

        return new GuestName(
            firstName.Trim(),
            lastName.Trim());
    }

    public override string ToString() => FullName;
}
