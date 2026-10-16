---
title: 🔭 Overview
sidebar_position: 1
---

# What is ImmichFrame?

**ImmichFrame** is a companion project to [Immich][immich-github-url], built to turn your photo and video collection into a beautiful digital photo frame experience.

[Immich][immich-github-url] is an excellent self-hosted media management solution – like your private Google Photos. But what if you want to display your favorite photos in a living room, hallway, or even on an e-ink display?

That's where **ImmichFrame** comes in.

### How It Works

ImmichFrame operates as a **two-tier architecture**:

```
┌──────────────────┐            ┌────────────────────────┐            ┌───────────────────────────┐
│                  │  API Key   │                        │  HTTP/LAN  │      Display Clients      │
│  Immich Server   │ ◄────────► │   ImmichFrame Server   │ ◄────────► │  • Web Browser / PWA      │
│ (Photo Library)  │            │   (Docker Container)   │            │  • Android App / TV       │
│                  │            │                        │            │  • Apple TV / iPad / iOS  │
└──────────────────┘            └────────────────────────┘            └───────────────────────────┘
```

:::info Important for New Users
**Display devices and apps do not connect directly to your Immich server.**

Instead, you run the **ImmichFrame server** (typically via Docker), which communicates with your Immich instance. Your display devices (tablets, photo frames, smart TVs, or web browsers) then connect to your **ImmichFrame server**.
:::

1. **Immich Server**: Stores and manages your media library.
2. **ImmichFrame Server (this repository)**: Connects to Immich using an API key. It manages photo selection, caching, album filters, weather and clock overlays, and serves both the built-in web display and the [Admin UI](./getting-started/admin-ui.md).
3. **Display Clients**: Any device that displays your frame:
   - **Built-in Web Client**: Open `http://<immichframe-ip>:8080` in any modern web browser or add to home screen as a fullscreen PWA.
   - **[Android App & Screensaver](./getting-started/apps.md#android)**: Run on tablets, Android TV, Chromecast, or dedicated digital photo frames (available on [Google Play][play-store-link] or sideload via APK from the [Android repository][github-android-repo]).
   - **[Apple TV & iOS App](./getting-started/apps.md#apple)**: Available on the [Apple App Store][app-store-link] for Apple TV, iPad, and iPhone.

---

### Quick Start

1. **Deploy ImmichFrame Server**: Follow the [Docker Setup](./getting-started/installation/docker.md) guide to start the container.
2. **Configure**: Open the [Admin UI](./getting-started/admin-ui.md) at `http://<immichframe-ip>:8080/admin` to set your Immich server URL, API key, and preferences (or provide a `Settings.yml`/`Settings.json`).
3. **Display**: Open `http://<immichframe-ip>:8080` on your display device or connect via the [Client Apps](./getting-started/apps.md).

---

### Use Cases

- A wall-mounted tablet as a dynamic photo frame
- Raspberry Pi-powered digital frames
- Smart TVs (Android TV, Apple TV, Google TV) as screensavers
- E-Ink photo displays
- Office displays or dashboards with rotating team/event photos

---

### Wrapped up

- **Immich** = Your photo library
- **ImmichFrame Server** = The companion server that connects to Immich and orchestrates your frame
- **ImmichFrame Clients** = The displays showing off your library

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->

[contributors-shield]: https://img.shields.io/github/contributors/immichFrame/ImmichFrame.svg?style=for-the-badge
[contributors-url]: https://github.com/immichFrame/ImmichFrame/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/immichFrame/ImmichFrame.svg?style=for-the-badge
[forks-url]: https://github.com/immichFrame/ImmichFrame/network/members
[stars-shield]: https://img.shields.io/github/stars/immichFrame/ImmichFrame.svg?style=for-the-badge
[stars-url]: https://github.com/immichFrame/ImmichFrame/stargazers
[issues-shield]: https://img.shields.io/github/issues/immichFrame/ImmichFrame.svg?style=for-the-badge
[issues-url]: https://github.com/immichFrame/ImmichFrame/issues
[license-shield]: https://img.shields.io/github/license/immichFrame/ImmichFrame.svg?style=for-the-badge
[license-url]: https://github.com/immichFrame/ImmichFrame/blob/master/LICENSE.txt
[releases-url]: https://github.com/immichFrame/ImmichFrame/releases/latest
[support-url]: https://discord.com/channels/979116623879368755/1217843270244372480
[openweathermap-url]: https://openweathermap.org/
[immich-github-url]: https://github.com/immich-app/immich
[immich-api-url]: https://immich.app/docs/features/command-line-interface#obtain-the-api-key
[play-store-link]: https://play.google.com/store/apps/details?id=com.immichframe.immichframe
[github-android-repo]: https://github.com/immichFrame/ImmichFrame_Android
[app-store-link]: https://apps.apple.com/us/app/immichframe/id6742748077