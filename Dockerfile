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
FROM node:22-bookworm-slim AS build-node

USER node
WORKDIR /app
COPY --chown=node:node ./immichFrame.Web/package*.json ./

# Cache npm dependencies
RUN npm ci
COPY --chown=node:node ./immichFrame.Web ./
RUN npm run build && npm prune --omit=dev

# Stage 4: Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS final

WORKDIR /app

# Optional: für Diagnostik bei Laufzeit
ARG VERSION
ENV APP_VERSION=$VERSION

# Copy .NET API and frontend assets
COPY --from=publish-api /app ./
COPY --from=build-node /app/build ./wwwroot

# The settings database lives here, so it has to be writable by the runtime user.
# A fresh named volume inherits this ownership; a bind mount keeps the host's, which
# is why the host directory has to be chown'ed to the same uid.
ARG APP_UID=1000
RUN mkdir -p /app/Config && chown -R $APP_UID:0 /app/Config && chmod -R g+w /app/Config

# Set non-privileged user
USER $APP_UID

ENTRYPOINT ["dotnet", "ImmichFrame.WebApi.dll"]
