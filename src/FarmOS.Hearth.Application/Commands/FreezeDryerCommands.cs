using FarmOS.Hearth.Domain;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Hearth.Application.Commands;

public record StartFreezeDryerBatchCommand(string BatchCode, Guid DryerId, string ProductDescription, decimal PreDryWeight) : ICommand<Guid>;
public record RecordFreezeDryerReadingCommand(Guid BatchId, FreezeDryerReading Reading) : ICommand<Guid>;
public record AdvanceFreezeDryerPhaseCommand(Guid BatchId, FreezeDryerPhase NextPhase) : ICommand<Guid>;
public record CompleteFreezeDryerBatchCommand(Guid BatchId, decimal PostDryWeight) : ICommand<Guid>;
public record AbortFreezeDryerBatchCommand(Guid BatchId, string Reason) : ICommand<Guid>;
