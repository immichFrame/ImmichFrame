---
sidebar_position: 3
---

# 🛠️ Admin UI

ImmichFrame ships with a built-in admin interface at **`/admin`** where every setting —
display options, weather, calendars, and your Immich accounts — can be edited from the
browser. Changes are applied **live**, without restarting the container.

## Enabling the admin UI

The admin UI is protected by a dedicated password and is **disabled until a password is set**.
Set the `IMMICHFRAME_ADMIN_PASSWORD` environment variable:

```yaml title="docker-compose.yml"
services:
  immichframe:
    image: ghcr.io/immichframe/immichframe:latest
    environment:
      IMMICHFRAME_ADMIN_PASSWORD: your-secret-password
    volumes:
      - ./config:/app/Config
```

Alternatively you can set the `AdminPassword` setting (in the `General` section) via the
admin UI itself once you are logged in. The environment variable **always wins** — if you
ever lock yourself out by saving a wrong password, set `IMMICHFRAME_ADMIN_PASSWORD` and
restart the container to regain access.

:::caution
The admin UI exposes your Immich API keys to authenticated admins and transmits them in
plain text. Run ImmichFrame behind HTTPS (e.g. a reverse proxy) if it is reachable from
outside your trusted network.
:::

## Where settings are stored

Settings edited in the admin UI are stored in a SQLite database (`immichframe.db`) inside
the config directory (`/app/Config` in Docker). This makes the config directory the one
place to persist:

- Mount `/app/Config` as a **writable** volume, otherwise saving settings fails.
- **The database is the source of truth.** On the very first start, an existing
  `Settings.json`/`Settings.yml` or environment-variable config is imported into the
  database once. After that, changes to those files are ignored (a log line reminds you
  of this at startup).
- Back up the config directory to keep your settings.

## Zero-configuration start

ImmichFrame now also starts **without any configuration**: the slideshow shows a hint that
no accounts are configured, and you can do the entire initial setup — including adding
your first Immich account — through the admin UI.

Related change: the container no longer exits when an Immich server is unreachable or
incompatible at startup. It logs a critical warning instead, so you can fix the account
settings via the admin UI.

## Applying changes

- **Display settings** (interval, layout, clock, …) are served live; slideshow devices pick
  them up the next time the page loads.
- **Account changes** rebuild the asset pipeline in the background — no restart needed.
- **Weather and calendar** changes take effect within ~15 minutes (internal caches).
- Every account has a **Test connection** button to verify the server URL and API key
  before saving.
