using FarmOS.Commerce.Domain.Aggregates;

namespace FarmOS.Commerce.Application;

public interface ICommerceEventStore
{
    Task<CSASeason> LoadSeasonAsync(string seasonId, CancellationToken ct);
    Task SaveSeasonAsync(CSASeason season, string userId, CancellationToken ct);

    Task<CSAMember> LoadMemberAsync(string memberId, CancellationToken ct);
    Task SaveMemberAsync(CSAMember member, string userId, CancellationToken ct);

    Task<Order> LoadOrderAsync(string orderId, CancellationToken ct);
    Task SaveOrderAsync(Order order, string userId, CancellationToken ct);

    Task<Customer> LoadCustomerAsync(string customerId, CancellationToken ct);
    Task SaveCustomerAsync(Customer customer, string userId, CancellationToken ct);

    Task<BuyingClub> LoadBuyingClubAsync(string clubId, CancellationToken ct);
    Task SaveBuyingClubAsync(BuyingClub club, string userId, CancellationToken ct);

    Task<WholesaleAccount> LoadWholesaleAccountAsync(string accountId, CancellationToken ct);
    Task SaveWholesaleAccountAsync(WholesaleAccount account, string userId, CancellationToken ct);
}
