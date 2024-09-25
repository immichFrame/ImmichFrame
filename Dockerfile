# Create a stage for building the application.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base-api

COPY . /source

WORKDIR /source/ImmichFrame.WebApi

ARG TARGETARCH

# Restore dependencies
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore

# FROM base-api AS build-api
# # Build the application.
# # Leverage a cache mount to /root/.nuget/packages so that subsequent builds don't have to re-download packages.
# # If TARGETARCH is "amd64", replace it with "x64" - "x64" is .NET's canonical name for this and "amd64" doesn't
# #   work in .NET 6.0.
# RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
#     dotnet build

FROM base-api AS publish-api
# Build the application.
# Leverage a cache mount to /root/.nuget/packages so that subsequent builds don't have to re-download packages.
# If TARGETARCH is "amd64", replace it with "x64" - "x64" is .NET's canonical name for this and "amd64" doesn't
#   work in .NET 6.0.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app

FROM node:iron-alpine3.18@sha256:53108f67824964a573ea435fed258f6cee4d88343e9859a99d356883e71b490c AS build-node
USER node
WORKDIR /app
COPY --chown=node:node ./immichFrame.Web/package*.json ./
RUN npm ci
COPY --chown=node:node ./immichFrame.Web .
RUN npm run build
RUN npm prune --omit=dev

# FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
# WORKDIR /source
# RUN apt-get update -yq && apt-get upgrade -yq && apt-get install -yq curl git nano
# RUN curl -sL https://deb.nodesource.com/setup_20.x | bash - && apt-get install -yq nodejs build-essential
# COPY --from=build-api /source .
# WORKDIR /source/immichFrame.Web
# RUN npm ci
# WORKDIR /source/ImmichFrame.WebApi
# # ENTRYPOINT ["dotnet", "run"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copy everything needed to run the app from the "publish" and "build-node" stage.
COPY --from=publish-api /app .
COPY --from=build-node /app/build ./wwwroot

# Switch to a non-privileged user (defined in the base image) that the app will run under.
USER $APP_UID

ENTRYPOINT ["dotnet", "ImmichFrame.WebApi.dll"]
