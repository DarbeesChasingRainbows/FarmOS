using FarmOS.Counter.Domain;
using FarmOS.Counter.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Counter.Application.Commands.Handlers;

public sealed class RegisterCommandHandlers(ICounterEventStore store) :
    ICommandHandler<OpenRegisterCommand, Guid>,
    ICommandHandler<CloseRegisterCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(OpenRegisterCommand cmd, CancellationToken ct)
    {
        var register = Register.Open(cmd.Location, cmd.OperatorName);
        await store.SaveRegisterAsync(register, "steward", ct);
        return register.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CloseRegisterCommand cmd, CancellationToken ct)
    {
        var register = await store.LoadRegisterAsync(cmd.RegisterId.ToString(), ct);
        var result = register.Close();
        if (result.IsFailure) return result.Error;
        await store.SaveRegisterAsync(register, "steward", ct);
        return register.Id.Value;
    }
}

public sealed class SaleCommandHandlers(ICounterEventStore store) :
    ICommandHandler<CompleteSaleCommand, Guid>,
    ICommandHandler<VoidSaleCommand, Guid>,
    ICommandHandler<RefundSaleCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(CompleteSaleCommand cmd, CancellationToken ct)
    {
        var result = Sale.Complete(new RegisterId(cmd.RegisterId), cmd.Items, cmd.Payments, cmd.CustomerName);
        if (result.IsFailure) return result.Error;
        var sale = result.Value;
        await store.SaveSaleAsync(sale, "steward", ct);
        return sale.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(VoidSaleCommand cmd, CancellationToken ct)
    {
        var sale = await store.LoadSaleAsync(cmd.SaleId.ToString(), ct);
        var result = sale.Void(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveSaleAsync(sale, "steward", ct);
        return sale.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(RefundSaleCommand cmd, CancellationToken ct)
    {
        var sale = await store.LoadSaleAsync(cmd.SaleId.ToString(), ct);
        var result = sale.Refund(cmd.RefundAmount, cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveSaleAsync(sale, "steward", ct);
        return sale.Id.Value;
    }
}

public sealed class CashDrawerCommandHandlers(ICounterEventStore store) :
    ICommandHandler<OpenCashDrawerCommand, Guid>,
    ICommandHandler<CountCashDrawerCommand, Guid>,
    ICommandHandler<ReconcileCashDrawerCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(OpenCashDrawerCommand cmd, CancellationToken ct)
    {
        var drawer = CashDrawer.Open(new RegisterId(cmd.RegisterId), cmd.StartingCash);
        await store.SaveCashDrawerAsync(drawer, "steward", ct);
        return drawer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(CountCashDrawerCommand cmd, CancellationToken ct)
    {
        var drawer = await store.LoadCashDrawerAsync(cmd.DrawerId.ToString(), ct);
        drawer.Count(cmd.Count);
        await store.SaveCashDrawerAsync(drawer, "steward", ct);
        return drawer.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(ReconcileCashDrawerCommand cmd, CancellationToken ct)
    {
        var drawer = await store.LoadCashDrawerAsync(cmd.DrawerId.ToString(), ct);
        var result = drawer.Reconcile();
        if (result.IsFailure) return result.Error;
        await store.SaveCashDrawerAsync(drawer, "steward", ct);
        return drawer.Id.Value;
    }
}
