using FarmOS.Pasture.Domain;
using FarmOS.Pasture.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Pasture.Application.Commands.Handlers;

public sealed class AnimalCommandHandlers(IPastureEventStore eventStore) :
    ICommandHandler<RegisterAnimalCommand, Guid>,
    ICommandHandler<IsolateAnimalCommand, Guid>,
    ICommandHandler<ReturnAnimalToHerdCommand, Guid>,
    ICommandHandler<RecordTreatmentCommand, Guid>,
    ICommandHandler<RecordPregnancyCommand, Guid>,
    ICommandHandler<RecordBirthCommand, Guid>,
    ICommandHandler<ButcherAnimalCommand, Guid>,
    ICommandHandler<SellAnimalCommand, Guid>,
    ICommandHandler<RecordWeightCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(
        RegisterAnimalCommand cmd, CancellationToken ct)
    {
        var animal = Animal.Register(cmd.Tags, cmd.Species, cmd.Breed, cmd.Sex, cmd.DateAcquired, cmd.Nickname);
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        IsolateAnimalCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.AnimalId.ToString(), ct);
        var result = animal.Isolate(cmd.Reason, cmd.Date);
        if (result.IsFailure) return result.Error;
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        ReturnAnimalToHerdCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.AnimalId.ToString(), ct);
        animal.ReturnToHerd(new HerdId(cmd.HerdId), cmd.Date);
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        RecordTreatmentCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.AnimalId.ToString(), ct);
        var treatment = new Treatment(cmd.Name, cmd.Dosage, cmd.Route, cmd.Date, cmd.Notes, cmd.WithdrawalPeriodDays);
        animal.RecordTreatment(treatment);
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        RecordPregnancyCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.AnimalId.ToString(), ct);
        var sireId = cmd.SireId.HasValue ? new AnimalId(cmd.SireId.Value) : null;
        var result = animal.RecordPregnancy(cmd.ExpectedDue, sireId);
        if (result.IsFailure) return result.Error;
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        RecordBirthCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.DamId.ToString(), ct);
        animal.RecordBirth(new AnimalId(cmd.OffspringId), cmd.Date);
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        ButcherAnimalCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.AnimalId.ToString(), ct);
        var record = new ButcherRecord(cmd.Date, cmd.Processor, cmd.HangingWeight, cmd.CutSheet);
        var result = animal.Butcher(record);
        if (result.IsFailure) return result.Error;
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        SellAnimalCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.AnimalId.ToString(), ct);
        var record = new SaleRecord(cmd.Date, cmd.Price, cmd.Buyer, cmd.Notes);
        var result = animal.Sell(record);
        if (result.IsFailure) return result.Error;
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        RecordWeightCommand cmd, CancellationToken ct)
    {
        var animal = await eventStore.LoadAnimalAsync(cmd.AnimalId.ToString(), ct);
        animal.RecordWeight(cmd.Weight, cmd.Date);
        await eventStore.SaveAnimalAsync(animal, "system", ct);
        return animal.Id.Value;
    }
}
