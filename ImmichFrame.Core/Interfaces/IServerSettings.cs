﻿using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ImmichFrame.Core.Interfaces
{
    public interface IServerSettings
    {
        public IEnumerable<IAccountSettings> Accounts { get; }
        public IGeneralSettings GeneralSettings { get; }
    }

    public interface IAccountSettings
    {
        public string ImmichServerUrl { get; }
        public string ApiKey { get; }
        public bool ShowMemories { get; }
        public bool ShowFavorites { get; }
        public bool ShowArchived { get; }
        public int? ImagesFromDays { get; }
        public DateTime? ImagesFromDate { get; }
        public DateTime? ImagesUntilDate { get; }
        public List<Guid> Albums { get; }
        public List<Guid> ExcludedAlbums { get; }
        public List<Guid> People { get; }
        public int? Rating { get; }
        public string ImmichFrameAlbumName { get; }
    }

    public interface IGeneralSettings
    {
        public List<string> Webcalendars { get; }
        public int RefreshAlbumPeopleInterval { get; }
        public string? WeatherApiKey { get; }
        public string? WeatherLatLong { get; }
        public string? UnitSystem { get; }
        public string? Webhook { get; }
        public string? AuthenticationSecret { get; }
        public string Margin { get; }
        public int Interval { get; }
        public double TransitionDuration { get; }
        public bool DownloadImages { get; }
        public int RenewImagesDuration { get; }
        public bool ShowClock { get; }
        public string? ClockFormat { get; }
        public bool ShowProgressBar { get; }
        public bool ShowPhotoDate { get; }
        public string? PhotoDateFormat { get; }
        public bool ShowImageDesc { get; }
        public bool ShowPeopleDesc { get; }
        public bool ShowAlbumName { get; }
        public bool ShowImageLocation { get; }
        public string? ImageLocationFormat { get; }
        public string? PrimaryColor { get; }
        public string? SecondaryColor { get; }
        public string Style { get; }
        public string? BaseFontSize { get; }
        public bool ShowWeatherDescription { get; }
        public bool UnattendedMode { get; }
        public bool ImageZoom { get; }
        public bool ImagePan { get; }
        public bool ImageFill { get; }
        public string Layout { get; }
        public string Language { get; }

        /// <summary>
        /// This TypeResolver is here to prevent serialization of AuthenticationSecret.
        ///
        /// It must be registered with the serializer.
        /// </summary>
        [JsonIgnore]
        public static IJsonTypeInfoResolver TypeInfoResolver
        {
            get
            {
                var defaultResolver = new DefaultJsonTypeInfoResolver();

                defaultResolver.Modifiers.Add(typeInfo =>
                {
                    if (!typeof(IGeneralSettings).IsAssignableFrom(typeInfo.Type)) return;

                    typeInfo.Properties.ToList().ForEach(prop =>
                    {
                        if (prop.AttributeProvider is PropertyInfo property)
                        {
                            if (property.Name == nameof(AuthenticationSecret))
                            {
                                prop.ShouldSerialize = (_, _) => false;
                            }
                        }
                    });
                });

                return defaultResolver;
            }
        }
    }
}