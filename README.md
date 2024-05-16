[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]



<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/3rob3/ImmichFrame">
    <img src="https://raw.githubusercontent.com/immich-app/immich/main/design/immich-logo.svg" alt="Logo" width="200" height="200">
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
## About The Project

This project is a digital photo frame application that interfaces with your [immich][immich-github-url] server. It is a cross-platform C# .NET 7 project that currently supports Android, Linux, macOS, and Windows.

### Built With

* [![Avalonia][Avalonia]][Avalonia-url]

## ✨ Demo
![Screenshot 2024-03-28 at 10 10 04 AM](https://github.com/3rob3/ImmichFrame/assets/156599986/75720456-4ccc-4323-af77-b72f34952f40)

<!-- GETTING STARTED -->
## Getting Started

ImmichFrame is easy to run on your desired plattform. Get the latest stable release from the [release page][releases-url] and unzip to desired folder (Linux, macOS, Windows), or install APK (Android).

### Prerequisites
- A set up and functioning immich server that is accessible by the network of the ImmichFrame device.

<!-- USAGE EXAMPLES -->
## Usage
### Linux
- GUI - Double-click `Immich_Frame`.
- CLI - CD into folder, and launch with `./Immich_Frame`.
- SSH - CD into folder, and launch with `DISPLAY=:0.0 ./Immich_Frame`.
### Windows
- Double-click `Immich_Frame.exe`.
- Screensaver - Rename `Immich_Frame.exe` to `Immich_Frame.scr`. Right-click &rarr; Install. Configure screensaver settings and apply.
  - You will still have to click middle/bottom in the app to exit screensaver.
### MacOS
- Double-click `Immich_Frame`. Note: If nothing happens, right-click &rarr; open with &rarr; Utilities &rarr; Terminal. Check Always Open With.

## Settings
There are two options for configuring ImmichFrame; Settings.xml (Linux, macOS, Windows), or a GUI based settings screen (all platforms). If Settings.xml exists it will be used, and the GUI settings will be unavailable.

### Settings.xml
> [!IMPORTANT]  
> Make sure to copy the **Settings.example.xml** and name it **Settings.xml**. 
 
1. Rename the `Settings.example.xml` file to `Settings.xml`
2. Change `<ImmichServerUrl>` to your domain or local ip
   ```xml
    <ImmichServerUrl>http://yourdomain.com</ImmichServerUrl>
   ```
3. Change `<ApiKey>`. Read more about how to obtain an [immich API key][immich-api-url]
   ```xml
    <ApiKey>YourApiKey</ApiKey>
   ```
4. *Optional:* Choose albums you would like to display
   ```xml
    <Albums>
	    <Album>First Album UID</Album>
	    <Album>Second Album UID</Album>
    </Albums>
   ```
5. *Optional:* Choose people you would like to display
   ```xml
    <People>
	    <Album>First person UID</Album>
	    <Album>Second person UID</Album>
    </People>
   ```
6. *Optional:* Weather is enabled by entering an API key. Get yours free from [OpenWeatherMap][openweathermap-url]
```xml
    <WeatherApiKey>YourApiKey</WeatherApiKey>
   ```
7. Adjust other settings to your needs

### Settings GUI
Only available if Settings.xml does not exist. The same information as in `Settings.xml` will be enterred from this screen. Can be enterred at any time by clicking the upper middle quadrant of the screen (or Up arrow key), see `Interactions` section.

<!-- INTERACTIONS -->
## Interactions
### Touch/Mouse
The screen is configured in a 3x3 gird. You can touch or click:

|   -   | **Settings** |   -   |
| :---: | :---: | :---: |
| **Prev<br>image** | **Pause** | **Next<br>image** |
|   -   | **Quit** |   -   |

### Keyboard:
**Settings** - Up arrow <br/>
**Quit** - Down arrow <br/>
**Prev Image** - Left arrow <br/>
**Next Image** - Right arrow <br/>
**Pause** - Enter/Return <br/>
<!-- ROADMAP -->
## Roadmap

- [x] Display random assets
- [x] Display Albums
- [x] Display Memories
- [x] Android build
- [x] Add License
- [ ] Web app
- [ ] Add Additional Templates w/ Examples

See the [open issues](https://github.com/3rob3/ImmichFrame/issues) for a full list of proposed features (and known issues).

<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<!-- LICENSE -->
## License

[GNU General Public License v3.0](LICENSE.txt)

<!-- CONTACT -->
## Contact

TODO

<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* BIG thanks to the [immich team][immich-github-url] for creating an awesome tool
* [Img Shields](https://shields.io)
* [GitHub Pages](https://pages.github.com)

## Star History

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
[openweathermap-url]: https://openweathermap.org/
[immich-github-url]: https://github.com/immich-app/immich
[beach-screenshot]: https://github.com/3rob3/ImmichFrame/assets/156599986/a21a28d3-1111-4f35-8d4b-9d6ece84aac1
[Avalonia]: https://img.shields.io/badge/avalonia-purple?style=for-the-badge&logo=avalonia&logoColor=white
[Avalonia-url]: https://docs.avaloniaui.net/docs/welcome
[immich-api-url]: https://immich.app/docs/features/command-line-interface#obtain-the-api-key
