using FarmOS.Ledger.Domain.Aggregates;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Ledger.Application.Commands.Handlers;

public sealed class ExpenseCommandHandlers(ILedgerEventStore store) :
    ICommandHandler<RecordExpenseCommand, Guid>,
    ICommandHandler<VoidExpenseCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RecordExpenseCommand cmd, CancellationToken ct)
    {
        var expense = Expense.Record(cmd.Description, cmd.Category, cmd.Context,
            cmd.Items, cmd.Total, cmd.Vendor, cmd.Date, cmd.ReceiptPath);
        await store.SaveExpenseAsync(expense, "steward", ct);
        return expense.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(VoidExpenseCommand cmd, CancellationToken ct)
    {
        var expense = await store.LoadExpenseAsync(cmd.ExpenseId.ToString(), ct);
        var result = expense.Void(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveExpenseAsync(expense, "steward", ct);
        return expense.Id.Value;
    }
}

public sealed class RevenueCommandHandlers(ILedgerEventStore store) :
    ICommandHandler<RecordRevenueCommand, Guid>,
    ICommandHandler<VoidRevenueCommand, Guid>
{
    public async Task<Result<Guid, DomainError>> Handle(RecordRevenueCommand cmd, CancellationToken ct)
    {
        var revenue = Revenue.Record(cmd.Description, cmd.Category, cmd.Context,
            cmd.Items, cmd.Total, cmd.CustomerName, cmd.Date);
        await store.SaveRevenueAsync(revenue, "steward", ct);
        return revenue.Id.Value;
    }

    public async Task<Result<Guid, DomainError>> Handle(VoidRevenueCommand cmd, CancellationToken ct)
    {
        var revenue = await store.LoadRevenueAsync(cmd.RevenueId.ToString(), ct);
        var result = revenue.Void(cmd.Reason);
        if (result.IsFailure) return result.Error;
        await store.SaveRevenueAsync(revenue, "steward", ct);
        return revenue.Id.Value;
    }
}
