using FarmOS.Counter.Domain.Aggregates;

namespace FarmOS.Counter.Application;

public interface ICounterEventStore
{
    Task<Register> LoadRegisterAsync(string registerId, CancellationToken ct);
    Task SaveRegisterAsync(Register register, string userId, CancellationToken ct);

    Task<Sale> LoadSaleAsync(string saleId, CancellationToken ct);
    Task SaveSaleAsync(Sale sale, string userId, CancellationToken ct);

    Task<CashDrawer> LoadCashDrawerAsync(string drawerId, CancellationToken ct);
    Task SaveCashDrawerAsync(CashDrawer drawer, string userId, CancellationToken ct);
}
