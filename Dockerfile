# Stage 1: Base for building the .NET API
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS base-api

COPY . /source
WORKDIR /source/ImmichFrame.WebApi

# Set default architecture argument for multi-arch builds
ARG TARGETARCH
ARG VERSION

ENV APP_VERSION=$VERSION

# Restore dependencies with cache
RUN dotnet restore

# Stage 2: Publish .NET API
FROM base-api AS publish-api

ARG TARGETARCH
ARG VERSION
ENV APP_VERSION=$VERSION

# Publish the app for the target architecture
RUN dotnet publish --runtime linux-${TARGETARCH} --self-contained false -p:AssemblyVersion=$VERSION -o /app

# Stage 3: Build frontend with Node.js
FROM node:22-alpine AS build-node

USER node
WORKDIR /app
COPY --chown=node:node ./immichFrame.Web/package*.json ./

# Cache npm dependencies
RUN npm i
COPY --chown=node:node ./immichFrame.Web ./
RUN npm run build && npm prune --omit=dev

# Stage 4: Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS final

WORKDIR /app

# Optional: für Diagnostik bei Laufzeit
ARG VERSION
ENV APP_VERSION=$VERSION

LABEL org.opencontainers.image.title="ImmichFrame" \
      org.opencontainers.image.source="https://github.com/immichFrame/ImmichFrame" \
      org.opencontainers.image.url="https://github.com/immichFrame/ImmichFrame" \
      org.opencontainers.image.licenses="GPL-3.0" \
      org.opencontainers.image.version="v${VERSION}" \
      org.opencontainers.image.revision="d8bcf4f"

EXPOSE 8080

# Legacy V1 environment configuration. These are intentionally declared so
# container managers can discover the supported ImmichFrame settings.
ENV ImmichServerUrl="" \
    ApiKey="" \
    Albums="" \
    ExcludedAlbums="" \
    People="" \
    ShowMemories="false" \
    ImagesFromDays="" \
    ImagesFromDate="" \
    ImagesUntilDate="" \
    ImageZoom="true" \
    Interval="45" \
    TransitionDuration="2" \
    WeatherApiKey="" \
    UnitSystem="imperial" \
    Language="en" \
    ShowWeatherDescription="true" \
    WeatherLatLong="" \
    ShowClock="true" \
    ClockFormat="hh:mm" \
    Webcalendars="" \
    ShowImageDesc="true" \
    ShowPeopleDesc="true" \
    ShowImageLocation="true" \
    ImageLocationFormat="City,State,Country" \
    ShowPhotoDate="true" \
    PhotoDateFormat="yyyy-MM-dd" \
    PrimaryColor="#f5deb3" \
    SecondaryColor="#000000" \
    Style="none" \
    Layout="splitview" \
    BaseFontSize="17px"
# Copy .NET API and frontend assets
COPY --from=publish-api /app ./
COPY --from=build-node /app/build ./wwwroot

# Set non-privileged user
ARG APP_UID=1000
USER $APP_UID

ENTRYPOINT ["dotnet", "ImmichFrame.WebApi.dll"]
