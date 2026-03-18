#!/bin/sh

CONFIG_DIR="${IMMICHFRAME_CONFIG_PATH:-/app/Config}"

if [ -n "$BaseUrl" ] && [ "$BaseUrl" != "/" ]; then
  BASE_PATH=$(echo "$BaseUrl" | sed 's|/*$||')
else
  FILE_BASE_URL=""

  if [ -f "$CONFIG_DIR/Settings.json" ]; then
    FILE_BASE_URL=$(grep -o '"BaseUrl"[[:space:]]*:[[:space:]]*"[^"]*"' "$CONFIG_DIR/Settings.json" | head -1 | sed 's/.*"\([^"]*\)"$/\1/')
  fi

  if [ -z "$FILE_BASE_URL" ] && [ -f "$CONFIG_DIR/Settings.yml" ]; then
    FILE_BASE_URL=$(grep -E '^[[:space:]]+BaseUrl:' "$CONFIG_DIR/Settings.yml" | head -1 | sed 's/.*BaseUrl:[[:space:]]*//' | tr -d "' \"")
  fi

  if [ -z "$FILE_BASE_URL" ] && [ -f "$CONFIG_DIR/Settings.yaml" ]; then
    FILE_BASE_URL=$(grep -E '^[[:space:]]+BaseUrl:' "$CONFIG_DIR/Settings.yaml" | head -1 | sed 's/.*BaseUrl:[[:space:]]*//' | tr -d "' \"")
  fi

  if [ -n "$FILE_BASE_URL" ] && [ "$FILE_BASE_URL" != "/" ]; then
    BASE_PATH=$(echo "$FILE_BASE_URL" | sed 's|/*$||')
  else
    BASE_PATH=""
  fi
fi

echo "Applying BaseUrl: $BASE_PATH"
find /app/wwwroot -type f \( -name "*.html" -o -name "*.js" -o -name "*.json" -o -name "*.webmanifest" -o -name "*.css" \) -exec sed -i "s|/__IMMICH_FRAME_BASE__|$BASE_PATH|g" {} +
exec dotnet ImmichFrame.WebApi.dll
