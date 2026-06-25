namespace HotelOS.SharedKernel.Common;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    // Pre-defined cross-cutting errors used across all modules
    public static readonly Error NotFound = new("General.NotFound", "The requested resource was not found.");
    public static readonly Error Unauthorized = new("General.Unauthorized", "You are not authorized to perform this action.");
    public static readonly Error Conflict = new("General.Conflict", "A conflict occurred. The resource may have been modified by another process.");
    public static readonly Error ValidationFailed = new("General.Validation", "One or more validation errors occurred.");
    public static readonly Error ConcurrencyConflict = new("General.ConcurrencyConflict", "The resource was modified by another request. Please retry.");
}
