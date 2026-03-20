using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;
using FarmOS.IoT.Domain;
using MediatR;

namespace FarmOS.IoT.Application.Commands;

public record CreateZoneCommand(
    string Name,
    ZoneType ZoneType,
    string? Description = null,
    GridPosition? GridPos = null,
    GeoPosition? GeoPos = null,
    Guid? ParentZoneId = null) : ICommand<string>;

public record UpdateZoneCommand(
    Guid ZoneId,
    string Name,
    string? Description = null) : ICommand<Unit>;

public record ArchiveZoneCommand(
    Guid ZoneId,
    string Reason) : ICommand<Unit>;
