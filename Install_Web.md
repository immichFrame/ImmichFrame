## 🔙 Back
Go back to the [Full Readme](/README.md)

## 🌐 ImmichFrame Web
- [🔙 Back](#-back)
- [🌐 ImmichFrame Web](#-immichframe-web)
- [✨ Demo](#-demo)
- [🔧 Installation](#-installation)
- [🐋 Docker Compose](#-docker-compose)
  - [Docker Compose with environment variables](#docker-compose-with-environment-variables)
  - [Docker Compose with Settings.json](#docker-compose-with-settingsjson)
  - [Docker Compose with env file](#docker-compose-with-env-file)
- [⚙️ Configuration](#️-configuration)
- [🆘 Help](#-help)

## ✨ Demo
![ImmichFrame Web](/design/demo/web_demo.png)

## 🔧 Installation
ImmichFrame Web is installed via [Docker 🐋](#-docker-compose)

## 🐋 Docker Compose
### Docker Compose with environment variables

> [!NOTE]  
> Not every setting is needed. Only configure what you need!

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

An example of the Settings.json can be found [here](/docker/Settings.example.json).

> [!IMPORTANT]  
> Change `PATH/TO/CONFIG` to the correct path!

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

An example of the .env can be found [here](/docker/example.env).

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

## ⚙️ Configuration

For more information, read [here](/README.md#configuration).

## 🆘 Help

[Discord Channel][support-url]


<!-- MARKDOWN LINKS & IMAGES -->
[support-url]: https://discord.com/channels/979116623879368755/1217843270244372480
