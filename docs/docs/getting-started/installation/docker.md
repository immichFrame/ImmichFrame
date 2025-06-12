---
sidebar_position: 2
---
# 🐋 Docker Setup [Docker Compose]

### Docker Compose

An example of the Settings.json can be found [here][example-json].

:::warning
Change `PATH/TO/CONFIG` to the correct path!
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
```

[github-root]: https://github.com/immichframe/ImmichFrame/blob/main
[example-json]: https://github.com/immichframe/ImmichFrame/blob/main/docker/Settings.example.json
[example-env]: https://github.com/immichframe/ImmichFrame/blob/main/docker/example.env