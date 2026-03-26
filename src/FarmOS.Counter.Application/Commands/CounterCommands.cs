using FarmOS.Counter.Domain;
using FarmOS.SharedKernel;
using FarmOS.SharedKernel.CQRS;

namespace FarmOS.Counter.Application.Commands;

// --- Register ----------------------------------------------------------------

public record OpenRegisterCommand(RegisterLocation Location, string OperatorName) : ICommand<Guid>;
public record CloseRegisterCommand(Guid RegisterId) : ICommand<Guid>;

// --- Sale --------------------------------------------------------------------

public record CompleteSaleCommand(Guid RegisterId, IReadOnlyList<SaleLineItem> Items, IReadOnlyList<PaymentRecord> Payments, string? CustomerName) : ICommand<Guid>;
public record VoidSaleCommand(Guid SaleId, string Reason) : ICommand<Guid>;
public record RefundSaleCommand(Guid SaleId, decimal RefundAmount, string Reason) : ICommand<Guid>;

// --- CashDrawer --------------------------------------------------------------

public record OpenCashDrawerCommand(Guid RegisterId, decimal StartingCash) : ICommand<Guid>;
public record CountCashDrawerCommand(Guid DrawerId, DrawerCount Count) : ICommand<Guid>;
public record ReconcileCashDrawerCommand(Guid DrawerId) : ICommand<Guid>;
