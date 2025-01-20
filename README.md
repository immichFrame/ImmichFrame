[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

> [!Tip]  
> Try out the ImmichFrame demo [here](https://demo.immichframe.online)!

> [!NOTE]  
> Instructions how to install ImmichFrame can be found [here](#-usage--installation)!

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/3rob3/ImmichFrame">
    <img src="design/AppIcon.png" alt="Logo" width="200" height="200">
  </a>

  <h3 align="center">ImmichFrame</h3>

  <p align="center">
    An awesome way to display your photos as an digital photo frame
    <br />
    <a href="https://immich.app/"><strong>Explore immich »</strong></a>
    <br />
    <br />
    <a href="https://github.com/3rob3/ImmichFrame/issues">Report Bug</a>
    ·
    <a href="https://github.com/3rob3/ImmichFrame/issues">Request Feature</a>
  </p>
</div>

## ⚠️ Disclaimer

**This project is not affiliated with [immich][immich-github-url]!**

<!-- ABOUT THE PROJECT -->

## 🛈 About The Project

This project is a digital photo frame application that interfaces with your [immich][immich-github-url] server. It is a cross-platform C# .NET 8 project that currently supports Android, Linux, macOS, and Windows.

## ✨ Demo
### Web Demo
<a href="https://demo.immichframe.online" target="_blank">
  <p>Visit the online demo</p>
<img src="/design/demo/web_demo.png" alt="Web Demo" height="300">
</a>

<!-- GETTING STARTED -->

## 🚀 Getting Started

ImmichFrame is easy to run on your desired plattform. Get the latest stable release from the [release page][releases-url] and unzip to desired folder (Linux, macOS, Windows), or install APK (Android).

### 📋 Prerequisites

- A set up and functioning immich server that is accessible by the network of the ImmichFrame device.

<!-- USAGE EXAMPLES -->

## 🔧 Usage / Installation

### 🌐 Browser
- [ImmichFrame Web](/Install_Web.md#-installation)

### 💻 Windows, Linux, MacOS, Android
- [ImmichFrame Client](/Install_Client.md#-installation)


## ⚙️ Configuration

| **Section**             | **Config-Key**             | **Value**                           | **Default**          | **Description**                                                                                                                       |
| ----------------------- | -------------------------- | ----------------------------------- | -------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| **Required**            | **ImmichServerUrl**        | **string**                          |                      | **The URL of your Immich server e.g. `http://photos.yourdomain.com` / `http://192.168.0.100:2283`.**                                  |
| **Required**            | **ApiKey**                 | **string**                          |                      | **Read more about how to obtain an [immich API key][immich-api-url].**                                                                |
| [Security](#security)   | AuthenticationSecret       | string                              |                      | When set, every client needs to authenticate via Bearer Token and this value.                                                         |
| [Filtering](#filtering) | Albums                     | string[]                            | []                   | UUID of album(s)                                                                                                                      |
| [Filtering](#filtering) | ExcludedAlbums             | string[]                            | []                   | UUID of excluded album(s)                                                                                                             |
| [Filtering](#filtering) | People                     | string[]                            | []                   | UUID of person(s)                                                                                                                     |
| [Filtering](#filtering) | ShowMemories               | boolean                             | false                | If this is set, memories are displayed.                                                                                               |
| [Filtering](#filtering) | ImagesFromDays             | int                                 |                      | Show images from the last X days. e.g 365 -> show images from the last year                                                           |
| [Filtering](#filtering) | ImagesFromDate             | Date                                |                      | Show images after date. Overwrites the `ImagesFromDays`-Setting                                                                       |
| [Filtering](#filtering) | ImagesUntilDate            | Date                                |                      | Show images before date.                                                                                                              |
| Caching                 | RenewImagesDuration        | int                                 | 30                   | Interval in days.                                                                                                                     |
| Caching                 | DownloadImages             | boolean                             | false                | \*Client only.                                                                                                                        |
| Caching                 | RefreshAlbumPeopleInterval | int                                 | 12                   | Interval in hours. Determines how often images are pulled from a person in immich.                                                    |
| Image                   | ImageZoom                  | boolean                             | true                 | Zooms into or out of an image and gives it a touch of life.                                                                           |
| Image                   | Interval                   | int                                 | 45                   | Image interval in seconds. How long a image is displayed in the frame.                                                                |
| Image                   | TransitionDuration         | float                               | 2                    | Duration in seconds.                                                                                                                  |
| [Weather](#weather)     | WeatherApiKey              | string                              |                      | Get api-key: [OpenWeatherMap][openweathermap-url].                                                                                    |
| [Weather](#weather)     | UnitSystem                 | imperial \| metric                  | imperial             | Imperial or metric system. (Fahrenheit or degrees)                                                                                    |
| [Weather](#weather)     | Language                   | string                              | en                   | 2 digit ISO code, sets the language of the weather description.                                                                       |
| [Weather](#weather)     | ShowWeatherDescription     | boolean                             | true                 | Displays the description of the current weather.                                                                                      |
| [Weather](#weather)     | WeatherLatLong             | boolean                             | 40.730610,-73.935242 | Set the weather location with lat/lon.                                                                                                |
| Clock                   | ShowClock                  | boolean                             | true                 | Displays the current time.                                                                                                            |
| Clock                   | ClockFormat                | string                              | hh:mm                | Time format.                                                                                                                          |
| [Calendar](#calendar)   | Webcalendars               | string[]                            | []                   | A list of webcalendar URIs in the .ics format. e.g. https://calendar.google.com/calendar/ical/XXXXXX/public/basic.ics                 |
| [Metadata](#metadata)   | ShowImageDesc              | boolean                             | true                 | Displays the description of the current image.                                                                                        |
| [Metadata](#metadata)   | ShowPeopleDesc             | boolean                             | true                 | Displays a comma separated list of names of all the people that are assigned in immich.                                               |
| [Metadata](#metadata)   | ShowImageLocation          | boolean                             | true                 | Displays the location of the current image.                                                                                           |
| [Metadata](#metadata)   | ImageLocationFormat        | string                              | City,State,Country   |                                                                                                                                       |
| [Metadata](#metadata)   | ShowPhotoDate              | boolean                             | true                 | Displays the date of the current image.                                                                                               |
| [Metadata](#metadata)   | PhotoDateFormat            | string                              | yyyy-MM-dd           | Date format. See [here](https://date-fns.org/v4.1.0/docs/format) for more information.                                                |
| UI                      | PrimaryColor               | string                              | #f5deb3              | Lets you choose a primary color for your UI. Use hex with alpha value to edit opacity.                                                |
| UI                      | SecondaryColor             | string                              | #000000              | Lets you choose a secondary color for your UI. (Only used with `style=solid or transition`) Use hex with alpha value to edit opacity. |
| UI                      | Style                      | none \| solid \| transition \| blur | none                 | Background-style of the clock and metadata.                                                                                           |
| UI                      | Layout                     | single \| splitview                 | splitview            | Allow two portrait images to be displayed next to each other                                                                          |
| UI                      | BaseFontSize               | string                              | 17px                 | Sets the base font size, uses [standard CSS formats](https://developer.mozilla.org/en-US/docs/Web/CSS/font-size).                     |
| [Misc](#misc)           | ImmichFrameAlbumName       | string                              |                      | \*Client only. Creates album and stores last 100 photos displayed.                                                                    |
| [Misc](#misc)           | Webhook                    | string                              |                      | Webhook URL to be notified e.g. http://example.com/notify                                                                             |

### Security
Basic authentication can be added via this setting. It is **NOT** recommended to expose immichFrame to the public web, if you still choose to do so, you can set this to a secure secret. Every client needs to authenticate itself with this secret. This can be done in the Webclient via input field or via URL-Parameter. The URL-Parameter will look like this: `?authsecret=[MYSECRET]`

If this is enabled, the web api required the `Authorization`-Header with `Bearer [MYSECRET]`.

### Filtering
You can get the UUIDs from the URL of the album/person. For this URL: `https://demo.immich.app/albums/85c85b29-c95d-4a8b-90f7-c87da1d518ba` this is the UUID: `85c85b29-c95d-4a8b-90f7-c87da1d518ba`

### Weather
Weather is enabled by entering an API key. Get yours free from [OpenWeatherMap][openweathermap-url]

### Calendar
If you are using Google Calendar, more information can be found [here](https://support.google.com/calendar/answer/37648?hl=en#zippy=%2Cget-your-calendar-view-only).

### Metadata
Needs documentation

### Misc
#### Webhook
A webhook to notify an external service is available. This is only enabled when the `Webhook`-Setting is set in your configuration. Your configured Webhook will be notified via `HTTP POST`-request.

A client can be identified by the `ClientIdentifier`. You can set/overwrite the `ClientIdentifier` by adding `?client=MyClient` to your ImmichFrame-URL. This only needs to be called once and is persisted. Delete the cache to reset the `ClientIdentifier`.

#### Events
Events will always contain a `Name`, `ClientIdentifier` and a `DateTime` to differentiate, but can contain more information.

| **Event**                  | **Description**                   | **Payload**                                                                                                                                             |
| -------------------------- | --------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ImageRequestedNotification | Notifies, when a Image requested. | `{"Name":"ImageRequestedNotification", "ClientIdentifier": "Frame_Kitchen", "DateTime":"2024-11-16T21:37:19.4933981+01:00", "RequestedImageId":"UUID"}` |

## 🛣️ Roadmap

- [x] Display random assets
- [x] Display Albums
- [x] Display Memories
- [x] Android build
- [x] Add License
- [x] Web app
- [ ] Add Additional Templates w/ Examples

See the [open issues](https://github.com/3rob3/ImmichFrame/issues) for a full list of proposed features (and known issues).

## ✍ Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📜 License

[GNU General Public License v3.0](LICENSE.txt)

## 🆘 Help

[Discord Channel][support-url]

## 🙏 Acknowledgments

- BIG thanks to the [immich team][immich-github-url] for creating an awesome tool

## 🌟 Star History

[![Star History Chart](https://api.star-history.com/svg?repos=3rob3/ImmichFrame&type=Date)](https://star-history.com/#3rob3/ImmichFrame&Date)

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->

[contributors-shield]: https://img.shields.io/github/contributors/3rob3/ImmichFrame.svg?style=for-the-badge
[contributors-url]: https://github.com/3rob3/ImmichFrame/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/3rob3/ImmichFrame.svg?style=for-the-badge
[forks-url]: https://github.com/3rob3/ImmichFrame/network/members
[stars-shield]: https://img.shields.io/github/stars/3rob3/ImmichFrame.svg?style=for-the-badge
[stars-url]: https://github.com/3rob3/ImmichFrame/stargazers
[issues-shield]: https://img.shields.io/github/issues/3rob3/ImmichFrame.svg?style=for-the-badge
[issues-url]: https://github.com/3rob3/ImmichFrame/issues
[license-shield]: https://img.shields.io/github/license/3rob3/ImmichFrame.svg?style=for-the-badge
[license-url]: https://github.com/3rob3/ImmichFrame/blob/master/LICENSE.txt
[releases-url]: https://github.com/3rob3/ImmichFrame/releases/latest
[support-url]: https://discord.com/channels/979116623879368755/1217843270244372480
[openweathermap-url]: https://openweathermap.org/
[immich-github-url]: https://github.com/immich-app/immich
[immich-api-url]: https://immich.app/docs/features/command-line-interface#obtain-the-api-key
