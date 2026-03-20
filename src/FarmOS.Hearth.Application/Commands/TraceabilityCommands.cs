using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands;

public record LogReceivingEventCommand(ProductCategory Category, string Description, string LotId, Quantity Amount, string SourceSupplier, DateTimeOffset Timestamp) : ICommand<Guid>;
public record LogTransformationEventCommand(ProductCategory Category, string Description, string NewLotId, Quantity Amount, string SourceLotId, DateTimeOffset Timestamp) : ICommand<Guid>;
public record LogShippingEventCommand(ProductCategory Category, string Description, string LotId, Quantity Amount, string Destination, DateTimeOffset Timestamp) : ICommand<Guid>;
