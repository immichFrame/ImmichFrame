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
# Don't use USER $APP_UID yet, as we need to write entrypoint.sh and then use it to perform runtime operations

# Create entrypoint script
RUN echo '#!/bin/bash\n\
set -e\n\
\n\
# Use BASE_URL env var if set, otherwise try to extract from Settings.json/yml if they exist, else default to /\n\
BASE_PATH="${BASE_URL}"\n\
\n\
if [ -z "$BASE_PATH" ]; then\n\
    if [ -f "/app/Config/Settings.json" ]; then\n\
        BASE_PATH=$(grep -oE "\"BaseUrl\":\s*\"[^\"]+\"" /app/Config/Settings.json | cut -d\" -f4 || echo "/")\n\
    elif [ -f "/app/Config/Settings.yml" ]; then\n\
        BASE_PATH=$(grep -E "^BaseUrl:" /app/Config/Settings.yml | awk \"{print \$2}\" || echo "/")\n\
    fi\n\
fi\n\
\n\
BASE_PATH=${BASE_PATH:-/}\n\
\n\
# Ensure BASE_PATH starts with / and does not end with / (unless it is just /)\n\
[[ "${BASE_PATH}" != /* ]] && BASE_PATH="/${BASE_PATH}"\n\
[[ "${BASE_PATH}" != "/" ]] && BASE_PATH="${BASE_PATH%/}"\n\
\n\
echo "Setting base path to ${BASE_PATH}"\n\
\n\
# Copy wwwroot to runtime directory to keep original clean and allow re-runs\n\
cp -rf /app/wwwroot /app/wwwroot-runtime\n\
\n\
# Replace placeholder with actual base path in the runtime copy\n\
find /app/wwwroot-runtime -type f -exec sed -i "s|/__IMMICH_FRAME_BASE__|${BASE_PATH}|g" {} +\n\
\n\
# We need to make sure the app serves from wwwroot-runtime or we symlink it\n\
rm -rf /app/wwwroot && ln -s /app/wwwroot-runtime /app/wwwroot\n\
\n\
exec dotnet ImmichFrame.WebApi.dll "$@"' > /app/entrypoint.sh \
    && chmod +x /app/entrypoint.sh

# Now we can set the user, but we need to make sure the user has permissions to the runtime directory
# Actually, it might be better to run as root for the setup part of the entrypoint and then gosu/su-exec
# but the original Dockerfile uses USER $APP_UID. 
# If we want to stay with USER $APP_UID, we must ensure /app is writable by it.

RUN chown -R $APP_UID /app

USER $APP_UID

ENTRYPOINT ["/app/entrypoint.sh"]
