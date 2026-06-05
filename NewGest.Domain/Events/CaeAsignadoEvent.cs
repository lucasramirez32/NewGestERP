using NewGest.Domain.Common;

namespace NewGest.Domain.Events;

public record CaeAsignadoEvent(int IdComprobante, int IdEmpresa, string CodigoCae) : IDomainEvent;
