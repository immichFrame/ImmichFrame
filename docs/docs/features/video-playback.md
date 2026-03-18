---
title: Video Playback
sidebar_position: 1
---

# Video Playback

:::warning[Experimental]
Video playback is an experimental feature and may not work as expected in all environments. Functionality and configuration options are subject to change.
:::

ImmichFrame supports playing video assets from your Immich library alongside photos in your slideshow.

## Enabling Video Playback

Video playback is disabled by default. To enable it, set `ShowVideos: true` in your account configuration:

```yaml
Accounts:
  - ImmichServerUrl: 'https://your-immich-server.com'
    ApiKey: 'your-api-key'
    ShowVideos: true  # Enable video playback
```

## Audio

By default, videos play muted. To enable audio playback, set `PlayAudio: true` in the General settings:

```yaml
General:
  PlayAudio: true  # Enable audio for videos
```

:::note
When `PlayAudio` is enabled, autoplay may be blocked by some browsers due to their autoplay policies. ImmichFrame handles this gracefully by attempting to play with audio, and falling back to muted playback if blocked.
:::

## How It Works

- **Duration**: Videos play for their full duration. The slideshow timer adjusts automatically to match the video length.
- **Progress Bar**: The progress bar reflects the video duration when a video is playing.
- **Streaming**: Videos are streamed directly from Immich rather than being pre-downloaded, reducing memory usage.
- **Transitions**: Videos transition smoothly like photos, with the same fade effects.

## Troubleshooting

### Videos not playing

1. Ensure `ShowVideos: true` is set in your account configuration
2. Check that your Immich server has video assets
3. Verify your API key has the `asset.view` permission

### No audio

1. Ensure `PlayAudio: true` is set in General settings
2. Check your device volume
3. Some browsers block autoplay with audio - try interacting with the page first

### Videos buffering or stuttering

- Check your network connection between ImmichFrame and Immich
- Large video files may take longer to buffer initially
- Consider transcoding very large videos in Immich

### Memory issues on mobile devices

If you experience crashes on devices with limited memory:
- Videos are streamed to minimize memory usage
- The app automatically cleans up video resources when transitioning between assets
- Consider using shorter video clips in your library