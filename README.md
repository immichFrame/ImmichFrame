[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

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
<img src="/design/demo/web_demo.png" alt="Web Demo" height="300">

### Client Demo
<img src="/design/demo/client_demo.png" alt="Client Demo" height="300">

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

| **Section**             | **Config-Key**             | **Value**          | **Default**          | **Description**                                                                                                       |
| ----------------------- | -------------------------- | ------------------ | -------------------- | --------------------------------------------------------------------------------------------------------------------- |
| **Required**            | **ImmichServerUrl**        | **string**         |                      | **The URL of your Immich server e.g. `http://photos.yourdomain.com` / `http://192.168.0.100:2283`.**                  |
| **Required**            | **ApiKey**                 | **string**         |                      | **Read more about how to obtain an [immich API key][immich-api-url].**                                                |
| [Filtering](#filtering) | Albums                     | string[]           | []                   | UUID of album(s)                                                                                                      |
| [Filtering](#filtering) | ExcludedAlbums             | string[]           | []                   | UUID of excluded album(s)                                                                                             |
| [Filtering](#filtering) | People                     | string[]           | []                   | UUID of person(s)                                                                                                     |
| [Filtering](#filtering) | ShowMemories               | boolean            | false                | If this is set, memories are displayed.                                                                               |
| [Caching](#caching)     | RenewImagesDuration        | int                | 30                   | \*Client only. Interval in days.                                                                                      |
| [Caching](#caching)     | DownloadImages             | boolean            | false                | \*Client only.                                                                                                        |
| [Caching](#caching)     | RefreshAlbumPeopleInterval | int                | 12                   | Interval in hours. Determines how often images are pulled from a person in immich.                                    |
| [Image](#image)         | ImageZoom                  | boolean            | true                 | Zooms into or out of an image and gives it a touch of life.                                                           |
| [Image](#image)         | Interval                   | int                | 45                   | Image interval in seconds. How long a image is displayed in the frame.                                                |
| [Image](#image)         | TransitionDuration         | float              | 2                    | Duration in seconds.                                                                                                  |
| [Image](#image)         | ImageStretch               | int                | Uniform              | \*Client only.                                                                                                        |
| [Weather](#weather)     | WeatherApiKey              | string             |                      | Get api-key: [OpenWeatherMap][openweathermap-url].                                                                    |
| [Weather](#weather)     | UnitSystem                 | imperial \| metric | imperial             | Imperial or metric system. (Fahrenheit or degrees)                                                                    |
| [Weather](#weather)     | Language                   | string             | en                   | 2 digit ISO code, sets the language of the weather description.                                                       |
| [Weather](#weather)     | ShowWeatherDescription     | boolean            | true                 | Displays the description of the current weather.                                                                      |
| [Weather](#weather)     | WeatherFontSize            | int                | 36                   | \*Client only.                                                                                                        |
| [Weather](#weather)     | WeatherLatLong             | boolean            | 40.730610,-73.935242 | Set the weather location with lat/lon.                                                                                |
| [Clock](#clock)         | ShowClock                  | boolean            | true                 | Displays the current time.                                                                                            |
| [Clock](#clock)         | ClockFontSize              | int                | 48                   | \*Client only.                                                                                                        |
| [Clock](#clock)         | ClockFormat                | string             | hh:mm                | Time format.                                                                                                          |
| [Calendar](#calendar)   | Webcalendar                | string[]           | []                   | A list of webcalendar URIs in the .ics format. e.g. https://calendar.google.com/calendar/ical/XXXXXX/public/basic.ics |
| [Metadata](#metadata)   | ShowImageDesc              | boolean            | true                 | Displays the description of the current image.                                                                        |
| [Metadata](#metadata)   | ImageDescFontSize          | int                | 3                    | \*Client only.                                                                                                        |
| [Metadata](#metadata)   | ShowImageLocation          | boolean            | true                 | Displays the location of the current image.                                                                           |
| [Metadata](#metadata)   | ImageLocationFormat        | string             | City,State,Country   | \*Client only.                                                                                                        |
| [Metadata](#metadata)   | ImageLocationFontSize      | int                | 36                   | \*Client only.                                                                                                        |
| [Metadata](#metadata)   | ShowPhotoDate              | boolean            | true                 | Displays the date of the current image.                                                                               |
| [Metadata](#metadata)   | PhotoDateFontSize          | int                | 36                   | \*Client only.                                                                                                        |
| [Metadata](#metadata)   | PhotoDateFormat            | string             | yyyy-MM-dd           | Date format.                                                                                                          |
| [UI](#ui)               | FontColor                  | string             | #FFFFFF              | \*Client only.                                                                                                        |
| [Misc](#misc)           | ImmichFrameAlbumName       | string             |                      | Creates album and stores last 100 photos displayed.                                                                   |
| [Misc](#misc)           | Margin                     | string             | 0,0,0,0              | \*Client only. Optionally fine tune margins to adjust for under/over scan.                                            |
| [Misc](#misc)           | UnattendedMode             | boolean            | false                | \*Client only. Don't show error messages, silently keep trying.                                                       |

### Filtering
You can get the UUIDs from the URL of the album/person. For this URL: `https://demo.immich.app/albums/85c85b29-c95d-4a8b-90f7-c87da1d518ba` this is the UUID: `85c85b29-c95d-4a8b-90f7-c87da1d518ba`

### Caching
Needs documentation

### Image
Needs documentation

### Weather
Weather is enabled by entering an API key. Get yours free from [OpenWeatherMap][openweathermap-url]

### Clock
Needs documentation

### Calendar
If you are using Google Calendar, more information can be found [here](https://support.google.com/calendar/answer/37648?hl=en#zippy=%2Cget-your-calendar-view-only).

### Metadata
Needs documentation

### UI
Needs documentation

### Misc
Needs documentation

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
