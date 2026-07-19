# ImmichFrame transcoder sidecar

Run this alongside the normal Compose file:

```sh
docker compose -f docker-compose.yml -f docker-compose.transcoder.yml up -d --build
```

Set `IMMICH_URL` and `IMMICH_API_KEY` in `docker/.env` to the same single
Immich account configured by ImmichFrame. `TRANSCODER_PROFILES` is a comma-
separated list of retained heights; `TRANSCODER_PLAYBACK_PROFILE` is the one
served to the frame. The service stores its SQLite database in `transcoder-state`
and its immutable completed MP4 files in `transcoded-media` as
`<height>/<asset-id>.mp4`.
