using System.Security.Cryptography;
using System.Text;
using ArangoDBNetStandard;
using ArangoDBNetStandard.CursorApi.Models;
using FarmOS.SharedKernel.Auth;

namespace FarmOS.SharedKernel.Infrastructure;

/// <summary>
/// PIN-based authentication backed by ArangoDB.
/// Uses SHA-256 hashing for PIN storage — adequate for a 4-6 digit PIN in a LAN-only sovereign system.
/// </summary>
public sealed class ArangoAuthService : IAuthService
{
    private readonly IArangoDBClient _client;
    private const string UsersCollection = "farm_users";

    public ArangoAuthService(IArangoDBClient client)
    {
        _client = client;
    }

    public async Task<FarmUser?> AuthenticateByPinAsync(string pin, CancellationToken ct)
    {
        var hash = HashPin(pin);

        var aql = @"
            FOR u IN @@collection
                FILTER u.PinHash == @hash
                LIMIT 1
                RETURN u
        ";

        var cursor = await _client.Cursor.PostCursorAsync<UserDoc>(
            new PostCursorBody
            {
                Query = aql,
                BindVars = new Dictionary<string, object>
                {
                    ["@collection"] = UsersCollection,
                    ["hash"] = hash
                }
            });

        var doc = cursor.Result.FirstOrDefault();
        if (doc is null) return null;

        return new FarmUser(doc._key, doc.Name, doc.Role, doc.PinHash);
    }

    public string HashPin(string pin)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
        return Convert.ToHexStringLower(bytes);
    }

    public bool VerifyPin(string pin, string hash)
        => HashPin(pin) == hash;

    private record UserDoc
    {
        public string _key { get; init; } = "";
        public string Name { get; init; } = "";
        public string Role { get; init; } = "";
        public string PinHash { get; init; } = "";
    }
}
