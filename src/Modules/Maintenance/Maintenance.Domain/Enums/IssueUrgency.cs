namespace Maintenance.Domain.Enums;

/// <summary>
/// Urgency levels for maintenance issues.
/// The Priority Queue Algorithm uses these values as sort keys:
///   Lower numeric value = higher priority (processed first).
///   Equal urgency → resolved by CreatedAt ascending (FIFO).
///
/// Critical (1): Safety hazard — e.g., broken elevator, gas leak, flooding.
/// High (2): Significant guest impact — e.g., broken shower, no hot water.
/// Normal (3): Moderate inconvenience — e.g., faulty TV, broken hairdryer.
/// Low (4): Cosmetic/minor — e.g., burned-out bulb in corridor.
/// </summary>
public enum IssueUrgency
{
    Critical = 1,
    High = 2,
    Normal = 3,
    Low = 4
}
