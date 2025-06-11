---
sidebar_position: 2
---
# 🐋 Docker Setup [Docker Compose]

### Docker Compose
```yaml
name: immichframe
services:
  immichframe:
    container_name: immichframe
    image: ghcr.io/immichframe/immichframe:latest
    restart: on-failure
    ports:
      - "8080:8080"
    environment:
      TZ: "Europe/Berlin"
      ImmichServerUrl: "URL"
      ApiKey: "KEY"
      # AuthenticationSecret: ""
      # Interval: "10"
      # TransitionDuration: "2"   
      # ImageZoom: "true"
      # ImagePan: "false"
      # ImageFill: "false"
      # Layout: "splitview"         
      # DownloadImages: "false"
      # ShowMemories: "false"
      # ShowFavorites: "false"
      # ShowArchived: "false"
      # ImagesFromDays: ""
      # ImagesFromDate: ""
      # ImagesUntilDate: ""
      # RenewImagesDuration: "30"
      # Rating: "5"
      # Albums: "ALBUM1,ALBUM2"
      # ExcludedAlbums: "ALBUM3,ALBUM4"
      # People: "PERSON1,PERSON2"
      # Webcalendars: "https://calendar.mycalendar.com/basic.ics,webcal://calendar.mycalendar.com/basic.ics"
      # RefreshAlbumPeopleInterval: "12"
      # ShowClock: "true"
      # ClockFormat: "hh:mm"
      # ShowPhotoDate: "true"
      # PhotoDateFormat: "yyyy-MM-dd"
      # ShowImageDesc: "true"
      # ShowPeopleDesc: "true"
      # ShowAlbumName: "true"
      # ShowImageLocation: "true"
      # ImageLocationFormat: "City,State,Country"
      # PrimaryColor: "#F5DEB3"
      # SecondaryColor: "#000000"
      # Style: "none"
      # BaseFontSize: "17px"
      # WeatherApiKey: ""
      # ShowWeatherDescription: "true"
      # UnitSystem: "imperial"
      # WeatherLatLong: ""
      # Language: "en"      
      # Webhook: ""
```

### Docker Compose with Settings.json

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

### Docker Compose with env file

An example of the .env can be found [here][example-env].

```yaml
name: immichframe
services:
  immichframe:
    container_name: immichframe
    image: ghcr.io/immichframe/immichframe:latest
    restart: on-failure
    ports:
      - "8080:8080"
    env_file:
      - .env
    environment:
      TZ: "Europe/Berlin"
```

[github-root]: https://github.com/immichframe/ImmichFrame/blob/main
[example-json]: https://github.com/immichframe/ImmichFrame/blob/main/docker/Settings.example.json
[example-env]: https://github.com/immichframe/ImmichFrame/blob/main/docker/example.env