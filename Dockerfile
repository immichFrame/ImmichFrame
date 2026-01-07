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

# Copy .NET API and frontend assets
COPY --from=publish-api /app ./
COPY --from=build-node /app/build ./wwwroot

# Set non-privileged user
ARG APP_UID=1000

# Ensure the app user owns the files they need to modify
RUN chown -R $APP_UID:$APP_UID /app/wwwroot

# Create a startup script to handle BaseUrl replacement
RUN echo '#!/bin/sh\n\
if [ -n "$BaseUrl" ] && [ "$BaseUrl" != "/" ]; then\n\
  BASE_PATH=$(echo "$BaseUrl" | sed "s|/*$||")\n\
else\n\
  BASE_PATH=""\n\
fi\n\
echo "Applying BaseUrl: $BASE_PATH"\n\
find /app/wwwroot -type f \( -name "*.html" -o -name "*.js" -o -name "*.json" -o -name "*.webmanifest" -o -name "*.css" \) -exec sed -i "s|/__IMMICH_FRAME_BASE__|$BASE_PATH|g" {} +\n\
exec dotnet ImmichFrame.WebApi.dll' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

USER $APP_UID

ENTRYPOINT ["/app/entrypoint.sh"]
