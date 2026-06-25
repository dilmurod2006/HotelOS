// ISO/IEC/IEEE 29148 - System/software requirements: Every domain object must be
// identifiable and traceable. IEntity enforces a typed primary key contract.
namespace HotelOS.SharedKernel.Abstractions;

public interface IEntity<TId>
{
    TId Id { get; }
}
