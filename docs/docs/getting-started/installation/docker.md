---
sidebar_position: 2
---

# 🐋 Docker Setup [Docker Compose]

This guide shows how to start **ImmichFrame** using Docker Compose.

---

## Configuration Files

:::tip Recommended
For most users, the `Settings.yml` setup is easier to read and modify.
:::

Example configuration files:

- [`Settings.yml` example][example-yaml] — imported once on first start
- [`Settings.json` example][example-json] — imported once on first start
- [`.env` example][example-env] — admin password, config path and log level

Starting without a settings file is fine: set `IMMICHFRAME_ADMIN_PASSWORD` and
configure everything in the [admin UI](../admin-ui.md).

---

## Docker Compose Example

:::warning Important
If using yaml or json settings, replace `PATH/TO/CONFIG` with the actual path to your config folder containing the settings file!
:::

```yaml
name: immichframe
services:
  immichframe:
    container_name: immichframe
    image: ghcr.io/immichframe/immichframe:latest
    restart: on-failure
    volumes:
      - PATH/TO/CONFIG:/app/Config
    ports:
      - "8080:8080"
    environment:
      TZ: "Europe/Berlin"
      IMMICHFRAME_ADMIN_PASSWORD: "CHANGE_ME"
```

[github-root]: https://github.com/immichframe/ImmichFrame/blob/main
[example-json]: https://github.com/immichframe/ImmichFrame/blob/main/docker/Settings.example.json
[example-yaml]: https://github.com/immichframe/ImmichFrame/blob/main/docker/Settings.example.yml
[example-env]: https://github.com/immichframe/ImmichFrame/blob/main/docker/example.env