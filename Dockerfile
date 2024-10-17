# Stage 1: Base for building the .NET API
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base-api

COPY . /source
WORKDIR /source/ImmichFrame.WebApi

# Set default architecture argument for multi-arch builds
ARG TARGETARCH=amd64

# Restore dependencies with cache
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore

# Stage 2: Publish .NET API
FROM base-api AS publish-api

# Publish the app for the target architecture
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH} --use-current-runtime --self-contained false -o /app

# Stage 3: Build frontend with Node.js
FROM node:iron-alpine3.18@sha256:53108f67824964a573ea435fed258f6cee4d88343e9859a99d356883e71b490c AS build-node

USER node
WORKDIR /app
COPY --chown=node:node ./immichFrame.Web/package*.json ./

# Cache npm dependencies
RUN --mount=type=cache,target=/app/.npm npm ci
COPY --chown=node:node ./immichFrame.Web ./
RUN npm run build && npm prune --omit=dev

# Stage 4: Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

WORKDIR /app

# Copy .NET API and frontend assets
COPY --from=publish-api /app ./
COPY --from=build-node /app/build ./wwwroot

# Set non-privileged user
ARG APP_UID=1000
USER $APP_UID

ENTRYPOINT ["dotnet", "ImmichFrame.WebApi.dll"]
