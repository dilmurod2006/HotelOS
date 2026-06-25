using Reception.Application.Interfaces;
using Reception.Domain.Entities;
using Reception.Domain.Enums;

namespace Reception.Application.Services;

/// <summary>
/// Application Service: Room Assignment Algorithm.
///
/// This is the core algorithm required by the BTEC assignment (Task 1.1).
/// It is placed in the Application layer (not Domain) because it depends on
/// IRoomRepository — a repository call is an infrastructure concern. Pure domain
/// logic has no such dependency.
///
/// ALGORITHM OVERVIEW (five prioritised filters applied in strict order):
/// ─────────────────────────────────────────────────────────────────────
/// Filter 1 — Type Match    : only rooms matching the requested RoomType qualify
/// Filter 2 — Status Match  : only "bookable" rooms (Clean or Available status)
/// Filter 3 — Longest Clean : sort by LastCleanedAt ASC so rooms that have been
///                            sitting clean the longest get priority (even wear)
/// Filter 4 — Floor Prefer  : prefer the guest's requested floor; graceful fallback
///                            to any floor if that floor has no matches
/// Filter 5 — Proximity     : final tiebreaker — NearElevator or NearStaircase
///
/// Complexity: O(n) repository read + O(n log n) sort where n = rooms of correct type.
/// For GrandStay's 120-room scale this is negligible; no caching needed at this stage.
/// </summary>
public sealed class RoomAssignmentService
{
    private readonly IRoomRepository _rooms;

    public RoomAssignmentService(IRoomRepository rooms)
        => _rooms = rooms;

    /// <summary>
    /// Selects the optimal room for a guest check-in request.
    /// Returns <c>null</c> when no suitable room exists (triggers TS-07 response).
    /// </summary>
    /// <param name="requestedType">The room category the guest booked.</param>
    /// <param name="preferredFloor">Optional floor number the guest prefers.</param>
    /// <param name="proximityPreference">Optional proximity tiebreaker.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Room?> FindBestRoomAsync(
        RoomType requestedType,
        int? preferredFloor,
        ProximityPreference proximityPreference,
        CancellationToken ct = default)
    {
        // ── FILTER 1: Fetch all rooms matching the requested type ─────────────
        // The repository executes: WHERE RoomType = @type
        // We pull all into memory for the subsequent in-memory filters because
        // the full dataset per type is small (GrandStay has ~30 rooms per type max).
        var typeMatchedRooms = await _rooms.GetAvailableRoomsByTypeAsync(requestedType, ct);

        if (typeMatchedRooms.Count == 0)
            return null; // No rooms of this type exist at all → TS-07

        // ── FILTER 2: Exclude non-bookable statuses ───────────────────────────
        // "Bookable" means the room is physically ready for a guest right now.
        // Available: newly created or returned from maintenance
        // Clean:     freshly cleaned by Housekeeping and ready for assignment
        // All other statuses (Occupied, Dirty, Cleaning, PendingPayment, UnderMaintenance)
        // mean the room is unavailable for new bookings.
        var bookableRooms = typeMatchedRooms
            .Where(r => r.Status is RoomStatus.Available or RoomStatus.Clean)
            .ToList();

        if (bookableRooms.Count == 0)
            return null; // All rooms of this type are currently unavailable → TS-07

        // ── FILTER 3: Sort by "Longest Clean" (rotation fairness) ────────────
        // Rooms that have been clean the longest are assigned first.
        // This distributes physical wear evenly across all rooms over time.
        //
        // LastCleanedAt == null means the room has never been cleaned (brand new).
        // We sort these LAST with DateTime.MaxValue so new rooms are used only
        // when all cleaned rooms are exhausted.
        //
        // Example ordering for 3 candidate rooms:
        //   Room 101 → LastCleanedAt = 10:00 AM  (was cleaned earliest → FIRST)
        //   Room 102 → LastCleanedAt = 11:30 AM
        //   Room 103 → LastCleanedAt = null       (never cleaned → LAST)
        var sortedByLongestClean = bookableRooms
            .OrderBy(r => r.LastCleanedAt ?? DateTime.MaxValue)
            .ToList();

        // ── FILTER 4: Apply floor preference (with graceful fallback) ─────────
        // If the guest specified a floor, attempt to satisfy it.
        // If no rooms are available on that floor, fall back to any floor.
        // We never leave a guest unassigned purely due to floor preference.
        var floorFilteredRooms = ApplyFloorPreference(sortedByLongestClean, preferredFloor);

        // ── FILTER 5: Apply proximity tiebreaker ─────────────────────────────
        // Among rooms that survived all previous filters, prefer rooms matching
        // the guest's proximity preference. If none match, return the best
        // available room regardless (preference is advisory, not mandatory).
        return ApplyProximityPreference(floorFilteredRooms, proximityPreference);
    }

    // ── Private filter helpers ────────────────────────────────────────────────

    /// <summary>
    /// Attempts floor preference, degrades to all floors if needed.
    /// Preserves the Longest-Clean sort order within each floor subset.
    /// </summary>
    private static List<Room> ApplyFloorPreference(
        List<Room> sortedRooms,
        int? preferredFloor)
    {
        if (preferredFloor is null)
            return sortedRooms; // Guest has no floor preference → keep all

        // Attempt to find rooms on the preferred floor
        var onPreferredFloor = sortedRooms
            .Where(r => r.Floor == preferredFloor.Value)
            .ToList();

        // Fallback: if no rooms exist on that floor, use any floor (TS-01 fallback)
        return onPreferredFloor.Count > 0
            ? onPreferredFloor
            : sortedRooms;
    }

    /// <summary>
    /// Applies proximity preference as the final tiebreaker.
    ///
    /// Strategy: move matching rooms to the front of the list; others follow.
    /// If no rooms match the preference, the first room (longest-clean) is
    /// returned anyway — proximity is best-effort, not a hard constraint.
    /// </summary>
    private static Room? ApplyProximityPreference(
        List<Room> floorFilteredRooms,
        ProximityPreference preference)
    {
        if (floorFilteredRooms.Count == 0)
            return null;

        // No preference specified → return the room that has been clean the longest
        if (preference == ProximityPreference.None)
            return floorFilteredRooms[0];

        // Find matching rooms (proximity preference satisfied)
        var matching = preference switch
        {
            ProximityPreference.NearElevator  => floorFilteredRooms.Where(r => r.IsNearElevator).ToList(),
            ProximityPreference.NearStaircase => floorFilteredRooms.Where(r => r.IsNearStaircase).ToList(),
            _ => []
        };

        // Return best matching room; if no matches, fall back to best overall
        // This ensures a room is always returned when rooms exist — satisfying TS-07
        // only when the inventory is genuinely exhausted, not due to preferences.
        return matching.Count > 0
            ? matching[0]          // Best room satisfying proximity preference
            : floorFilteredRooms[0]; // Graceful fallback: ignore proximity preference
    }
}
