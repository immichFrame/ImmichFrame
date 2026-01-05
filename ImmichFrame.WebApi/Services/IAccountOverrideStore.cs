using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Services;

public interface IAccountOverrideStore
{
    Task<AccountOverrideDto?> GetAsync(CancellationToken ct = default);
    Task<long> GetVersionAsync(CancellationToken ct = default);
    Task UpsertAsync(AccountOverrideDto dto, CancellationToken ct = default);
}


