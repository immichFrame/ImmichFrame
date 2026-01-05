using ImmichFrame.WebApi.Data;
using ImmichFrame.WebApi.Data.Entities;
using ImmichFrame.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmichFrame.WebApi.Services;

public sealed class SqliteAccountOverrideStore(ConfigDbContext db) : IAccountOverrideStore
{
    public async Task<AccountOverrideDto?> GetAsync(CancellationToken ct = default)
    {
        var entity = await db.AccountOverrides.AsNoTracking().SingleOrDefaultAsync(x => x.Id == AccountOverrideEntity.SingletonId, ct);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<long> GetVersionAsync(CancellationToken ct = default)
    {
        var updatedAt = await db.AccountOverrides.AsNoTracking()
            .Where(x => x.Id == AccountOverrideEntity.SingletonId)
            .Select(x => (DateTime?)x.UpdatedAtUtc)
            .SingleOrDefaultAsync(ct);

        return updatedAt?.Ticks ?? 0;
    }

    public async Task UpsertAsync(AccountOverrideDto dto, CancellationToken ct = default)
    {
        var entity = await db.AccountOverrides.SingleOrDefaultAsync(x => x.Id == AccountOverrideEntity.SingletonId, ct);
        if (entity == null)
        {
            entity = new AccountOverrideEntity
            {
                Id = AccountOverrideEntity.SingletonId
            };
            db.AccountOverrides.Add(entity);
        }

        Apply(entity, dto);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    private static void Apply(AccountOverrideEntity entity, AccountOverrideDto dto)
    {
        entity.ShowMemories = dto.ShowMemories;
        entity.ShowFavorites = dto.ShowFavorites;
        entity.ShowArchived = dto.ShowArchived;

        entity.ImagesFromDays = dto.ImagesFromDays;
        entity.ImagesFromDate = dto.ImagesFromDate;
        entity.ImagesUntilDate = dto.ImagesUntilDate;

        entity.Albums = dto.Albums;
        entity.ExcludedAlbums = dto.ExcludedAlbums;
        entity.People = dto.People;

        entity.Rating = dto.Rating;
    }

    private static AccountOverrideDto ToDto(AccountOverrideEntity entity) => new()
    {
        ShowMemories = entity.ShowMemories,
        ShowFavorites = entity.ShowFavorites,
        ShowArchived = entity.ShowArchived,
        ImagesFromDays = entity.ImagesFromDays,
        ImagesFromDate = entity.ImagesFromDate,
        ImagesUntilDate = entity.ImagesUntilDate,
        Albums = entity.Albums,
        ExcludedAlbums = entity.ExcludedAlbums,
        People = entity.People,
        Rating = entity.Rating
    };
}


