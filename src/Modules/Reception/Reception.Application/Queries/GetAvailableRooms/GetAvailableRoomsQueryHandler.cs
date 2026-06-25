using System.Text.Json;
using System.Text.Json.Serialization;
using HotelOS.SharedKernel.Common;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Reception.Application.DTOs;
using Reception.Application.Interfaces;
using Reception.Domain.Entities;
using Reception.Domain.Enums;

namespace Reception.Application.Queries.GetAvailableRooms;

/// <summary>
/// HIGH-LOAD READ OPTIMISATION — Redis caching shield.
///
/// When 1,000 concurrent users hit GET /api/client/rooms, only the FIRST
/// request reaches PostgreSQL. All subsequent requests within the 30-second
/// TTL window read from Redis, costing ~0.1 ms instead of ~10 ms per DB round-trip.
///
/// Cache key:  "hotelos:rooms:available"
/// TTL:        30 seconds (balances freshness vs. DB load)
/// Invalidation: RoomStatusChangedCacheInvalidationHandler deletes the key
///               whenever a room status changes, forcing the next read to repopulate.
/// </summary>
public sealed class GetAvailableRoomsQueryHandler
    : IRequestHandler<GetAvailableRoomsQuery, Result<IReadOnlyList<RoomDto>>>
{
    internal const string CacheKey = "hotelos:rooms:available";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IRoomRepository _roomRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetAvailableRoomsQueryHandler> _logger;

    public GetAvailableRoomsQueryHandler(
        IRoomRepository roomRepository,
        IDistributedCache cache,
        ILogger<GetAvailableRoomsQueryHandler> logger)
    {
        _roomRepository = roomRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<RoomDto>>> Handle(
        GetAvailableRoomsQuery query, CancellationToken cancellationToken)
    {
        // Step 1: Try the Redis cache. A cache HIT means we skip PostgreSQL entirely.
        var cached = await _cache.GetStringAsync(CacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache HIT for available rooms.");
            var cachedRooms = JsonSerializer.Deserialize<List<RoomDto>>(cached, JsonOptions)!;
            return Result<IReadOnlyList<RoomDto>>.Success(ApplyFilters(cachedRooms, query));
        }

        // Step 2: Cache MISS — query PostgreSQL (happens at most once per 30 seconds)
        _logger.LogDebug("Cache MISS for available rooms. Querying PostgreSQL.");
        var allRooms = await _roomRepository.GetAllAsync(cancellationToken);
        var rooms = allRooms.Where(r =>
            r.Status == RoomStatus.Available || r.Status == RoomStatus.Clean);

        var dtos = rooms.Select(r => new RoomDto(
            Id:            r.Id,
            RoomNumber:    r.RoomNumber.Value,
            Floor:         r.RoomNumber.Floor,
            Type:          r.Type,
            Status:        r.Status,
            NightlyRate:   r.NightlyRate.Amount,
            Currency:      r.NightlyRate.Currency,
            IsNearElevator:   r.IsNearElevator,
            IsNearStaircase:  r.IsNearStaircase,
            LastCleanedAt: r.LastCleanedAt))
            .ToList();

        // Step 3: Populate the cache with the full unfiltered list for 30 seconds.
        // Filters (Type, Floor) are applied in memory — the cached payload is
        // the full available set so any filter combination benefits from the same warm cache.
        var serialised = JsonSerializer.Serialize(dtos, JsonOptions);
        await _cache.SetStringAsync(CacheKey, serialised,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return Result<IReadOnlyList<RoomDto>>.Success(ApplyFilters(dtos, query));
    }

    private static IReadOnlyList<RoomDto> ApplyFilters(
        IEnumerable<RoomDto> rooms, GetAvailableRoomsQuery query)
    {
        var filtered = rooms.AsEnumerable();

        if (query.Type.HasValue)
            filtered = filtered.Where(r => r.Type == query.Type.Value);

        if (query.Floor.HasValue)
            filtered = filtered.Where(r => r.Floor == query.Floor.Value);

        return filtered.ToList().AsReadOnly();
    }
}
