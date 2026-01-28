using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Data;
using ImmichFrame.WebApi.Data.Entities;
using ImmichFrame.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ImmichFrame.WebApi.Services;

public interface IGeneralSettingsStore
{
    Task<GeneralSettingsDto?> GetAsync(CancellationToken ct = default);
    Task<long> GetVersionAsync(CancellationToken ct = default);
    Task<GeneralSettingsDto> GetOrCreateFromBaseAsync(IGeneralSettings baseSettings, CancellationToken ct = default);
    Task UpsertAsync(GeneralSettingsDto dto, CancellationToken ct = default);
}

public sealed class SqliteGeneralSettingsStore(ConfigDbContext db) : IGeneralSettingsStore
{
    public async Task<GeneralSettingsDto?> GetAsync(CancellationToken ct = default)
    {
        var entity = await db.GeneralSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == GeneralSettingsEntity.SingletonId, ct);
        return entity == null ? null : ToDto(entity);
    }

    public async Task<long> GetVersionAsync(CancellationToken ct = default)
    {
        var updatedAt = await db.GeneralSettings.AsNoTracking()
            .Where(x => x.Id == GeneralSettingsEntity.SingletonId)
            .Select(x => (DateTime?)x.UpdatedAtUtc)
            .SingleOrDefaultAsync(ct);

        return updatedAt?.Ticks ?? 0;
    }

    public async Task<GeneralSettingsDto> GetOrCreateFromBaseAsync(IGeneralSettings baseSettings, CancellationToken ct = default)
    {
        var existing = await db.GeneralSettings.SingleOrDefaultAsync(x => x.Id == GeneralSettingsEntity.SingletonId, ct);
        if (existing != null)
        {
            return ToDto(existing);
        }

        var entity = new GeneralSettingsEntity
        {
            Id = GeneralSettingsEntity.SingletonId
        };
        Apply(entity, baseSettings);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.GeneralSettings.Add(entity);
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task UpsertAsync(GeneralSettingsDto dto, CancellationToken ct = default)
    {
        var entity = await db.GeneralSettings.SingleOrDefaultAsync(x => x.Id == GeneralSettingsEntity.SingletonId, ct);
        if (entity == null)
        {
            entity = new GeneralSettingsEntity
            {
                Id = GeneralSettingsEntity.SingletonId
            };
            db.GeneralSettings.Add(entity);
        }

        Apply(entity, dto);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static void Apply(GeneralSettingsEntity entity, IGeneralSettings s)
    {
        entity.DownloadImages = s.DownloadImages;
        entity.Language = s.Language;
        entity.ImageLocationFormat = s.ImageLocationFormat;
        entity.PhotoDateFormat = s.PhotoDateFormat;
        entity.Interval = s.Interval;
        entity.TransitionDuration = s.TransitionDuration;
        entity.ShowClock = s.ShowClock;
        entity.ClockFormat = s.ClockFormat;
        entity.ClockDateFormat = s.ClockDateFormat;
        entity.ShowProgressBar = s.ShowProgressBar;
        entity.ShowPhotoDate = s.ShowPhotoDate;
        entity.ShowImageDesc = s.ShowImageDesc;
        entity.ShowPeopleDesc = s.ShowPeopleDesc;
        entity.ShowAlbumName = s.ShowAlbumName;
        entity.ShowImageLocation = s.ShowImageLocation;
        entity.PrimaryColor = s.PrimaryColor;
        entity.SecondaryColor = s.SecondaryColor;
        entity.Style = s.Style;
        entity.BaseFontSize = s.BaseFontSize;
        entity.ShowWeatherDescription = s.ShowWeatherDescription;
        entity.WeatherIconUrl = s.WeatherIconUrl;
        entity.ImageZoom = s.ImageZoom;
        entity.ImagePan = s.ImagePan;
        entity.ImageFill = s.ImageFill;
        entity.Layout = s.Layout;
        entity.RenewImagesDuration = s.RenewImagesDuration;
        entity.Webcalendars = s.Webcalendars.ToList();
        entity.RefreshAlbumPeopleInterval = s.RefreshAlbumPeopleInterval;
        entity.WeatherApiKey = s.WeatherApiKey;
        entity.UnitSystem = s.UnitSystem;
        entity.WeatherLatLong = s.WeatherLatLong;
        entity.Webhook = s.Webhook;
    }

    private static void Apply(GeneralSettingsEntity entity, GeneralSettingsDto dto)
    {
        entity.DownloadImages = dto.DownloadImages;
        entity.Language = dto.Language;
        entity.ImageLocationFormat = dto.ImageLocationFormat;
        entity.PhotoDateFormat = dto.PhotoDateFormat;
        entity.Interval = dto.Interval;
        entity.TransitionDuration = dto.TransitionDuration;
        entity.ShowClock = dto.ShowClock;
        entity.ClockFormat = dto.ClockFormat;
        entity.ClockDateFormat = dto.ClockDateFormat;
        entity.ShowProgressBar = dto.ShowProgressBar;
        entity.ShowPhotoDate = dto.ShowPhotoDate;
        entity.ShowImageDesc = dto.ShowImageDesc;
        entity.ShowPeopleDesc = dto.ShowPeopleDesc;
        entity.ShowAlbumName = dto.ShowAlbumName;
        entity.ShowImageLocation = dto.ShowImageLocation;
        entity.PrimaryColor = dto.PrimaryColor;
        entity.SecondaryColor = dto.SecondaryColor;
        entity.Style = dto.Style;
        entity.BaseFontSize = dto.BaseFontSize;
        entity.ShowWeatherDescription = dto.ShowWeatherDescription;
        entity.WeatherIconUrl = dto.WeatherIconUrl;
        entity.ImageZoom = dto.ImageZoom;
        entity.ImagePan = dto.ImagePan;
        entity.ImageFill = dto.ImageFill;
        entity.Layout = dto.Layout;
        entity.RenewImagesDuration = dto.RenewImagesDuration;
        entity.Webcalendars = dto.Webcalendars ?? new();
        entity.RefreshAlbumPeopleInterval = dto.RefreshAlbumPeopleInterval;
        entity.WeatherApiKey = dto.WeatherApiKey;
        entity.UnitSystem = dto.UnitSystem;
        entity.WeatherLatLong = dto.WeatherLatLong;
        entity.Webhook = dto.Webhook;
    }

    private static GeneralSettingsDto ToDto(GeneralSettingsEntity e) => new()
    {
        DownloadImages = e.DownloadImages,
        Language = e.Language,
        ImageLocationFormat = e.ImageLocationFormat,
        PhotoDateFormat = e.PhotoDateFormat,
        Interval = e.Interval,
        TransitionDuration = e.TransitionDuration,
        ShowClock = e.ShowClock,
        ClockFormat = e.ClockFormat,
        ClockDateFormat = e.ClockDateFormat,
        ShowProgressBar = e.ShowProgressBar,
        ShowPhotoDate = e.ShowPhotoDate,
        ShowImageDesc = e.ShowImageDesc,
        ShowPeopleDesc = e.ShowPeopleDesc,
        ShowAlbumName = e.ShowAlbumName,
        ShowImageLocation = e.ShowImageLocation,
        PrimaryColor = e.PrimaryColor,
        SecondaryColor = e.SecondaryColor,
        Style = e.Style,
        BaseFontSize = e.BaseFontSize,
        ShowWeatherDescription = e.ShowWeatherDescription,
        WeatherIconUrl = e.WeatherIconUrl,
        ImageZoom = e.ImageZoom,
        ImagePan = e.ImagePan,
        ImageFill = e.ImageFill,
        Layout = e.Layout,
        RenewImagesDuration = e.RenewImagesDuration,
        Webcalendars = e.Webcalendars.ToList(),
        RefreshAlbumPeopleInterval = e.RefreshAlbumPeopleInterval,
        WeatherApiKey = e.WeatherApiKey,
        UnitSystem = e.UnitSystem,
        WeatherLatLong = e.WeatherLatLong,
        Webhook = e.Webhook
    };
}


