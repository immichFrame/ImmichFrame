[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/3rob3/ImmichFrame">
    <img src="ImmichFrame/Assets/AppIcon.png" alt="Logo" width="200" height="200">
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

This project is a digital photo frame application that interfaces with your [immich][immich-github-url] server. It is a cross-platform C# .NET 8 project that currently supports Android, Linux, macOS, and Windows.

### Built With

- [![Avalonia][Avalonia]][Avalonia-url]

## ✨ Demo

![Screenshot 2024-03-28 at 10 10 04 AM](https://github.com/3rob3/ImmichFrame/assets/156599986/75720456-4ccc-4323-af77-b72f34952f40)

<!-- GETTING STARTED -->

## Getting Started

ImmichFrame is easy to run on your desired plattform. Get the latest stable release from the [release page][releases-url] and unzip to desired folder (Linux, macOS, Windows), or install APK (Android).

> [!TIP]
> The Android-Version of ImmichFrame is available on the [Google Play Store][play-store-link]. Download it via the store for automatic updates.

### Prerequisites

- A set up and functioning immich server that is accessible by the network of the ImmichFrame device.

<!-- USAGE EXAMPLES -->

## Usage

### Docker (WebApp)

#### Compose

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

#### Docker Run

```bash
docker run -td \
  --name immichFrame \
  --restart on-failure \
  -v PATH/TO/CONFIG:/app/Config \
  -p 8080:8080 \
  -e TZ="Europe/Berlin" \
  ghcr.io/immichframe/immichframe:latest
```

### Linux

- GUI - Double-click `Immich_Frame`.
- CLI - CD into folder, and launch with `./Immich_Frame`.
- SSH - CD into folder, and launch with `DISPLAY=:0.0 ./Immich_Frame`.
- Ubuntu Desktop - Right-click Immich_Frame, properties, set 'Execute as program' to True, OK. Right-click Immich_Frame, Open with, choose 'Run Software', always use for this file type TRUE.
- If you get a permissions error run `chmod +x Immich_Frame`.

#### Autostart on Raspberry Pi OS

The latest Raspberry Pi OS, Bookworm uses Wayland as default, but also includes X11. The following assumes standard user is `pi` and you are working on a fresh install of "Raspberry Pi OS with desktop".
To autostart ImmichFrame in Wayland, run `nano /home/pi/.config/wayfire.ini` in terminal, to add the following at the end.

```
[autostart]
immichframe=/home/pi/{dir with Immich_Frame}/Immich_Frame
```

Wayland does not have an easy way to hide the cursor. If you want that, then change to X11 as the default desktop session through running `sudo raspi-config` in terminal, then Advanced Options &rarr; Wayland &rarr; X11.
Then install unclutter:

```
sudo apt install -y unclutter-xfixes
```

After this, copy the default autostart file to your home folder and add a line to enable autostart, by running the following in terminal:

```
cp /etc/xdg/lxsession/LXDE-pi/autostart /home/pi/.config/lxsession/LXDE-pi/autostart
echo "@/home/pi/{dir with Immich_Frame}/Immich_Frame" > sudo tee /home/pi/.config/lxsession/LXDE-pi/autostart
```

### Windows

- Double-click `Immich_Frame.exe`.
- Screensaver - Rename `Immich_Frame.exe` to `Immich_Frame.scr`. Right-click &rarr; Install. Configure screensaver settings and apply.
  - You will still have to click middle/bottom in the app to exit screensaver.

### MacOS

- GUI - Double-click `Immich_Frame`. Note: If nothing happens, right-click &rarr; open with &rarr; Utilities &rarr; Terminal. Check Always Open With.
- CLI - CD into folder, and launch with `./Immich_Frame`.
- If you get a permissions error run `chmod +x Immich_Frame`.

### Android Screensaver

- Run the app normally and configure settings.
- Go to Settings, Display, Advanced, Screen Saver, Current Screen Saver, choose ImmichFrame. Settings, Display, Advanced, Sleep, choose your sleep timeout. The menu options may differ slightly on different Android versions.

### Android TV Screensaver

- If you are unable to set ImmichFrame as a screen saver you may need to run this ADB command `adb shell settings put secure screensaver_components com.immichframe.immichframe/.ScreenSaverService`

## Settings

There are two options for configuring ImmichFrame; Settings.json (Linux, macOS, Windows), or a GUI based settings screen (all platforms).

### Settings.json

> [!IMPORTANT]  
> Make sure to copy the **Settings.example.json** and name it **Settings.json**.

1. Rename the `Settings.example.json` file to `Settings.json`
2. Change `ImmichServerUrl` to your domain or local ip
   ```json
   "ImmichServerUrl": "http://yourdomain.com",
   or
   "ImmichServerUrl": "192.168.0.100:2283",
   ```
3. Change `ApiKey`. Read more about how to obtain an [immich API key][immich-api-url]
   ```json
   "ApiKey": "YourApiKey",
   ```
4. _Optional:_ Choose albums you would like to display

   ```json
   "Albums": ["First Album UID","Second Album UID"],
   ```

   > [!TIP]  
   > You can get the Album UID from the URL of the album. For this URL: `https://demo.immich.app/albums/85c85b29-c95d-4a8b-90f7-c87da1d518ba` this is the UID: `85c85b29-c95d-4a8b-90f7-c87da1d518ba`

5. _Optional:_ Choose people you would like to display

   ```json
    "People": ["First Person UID","Second Person UID"],
   ```

   > [!TIP]  
   > You can get the Person UID from the URL of the person. For this URL: `https://demo.immich.app/people/faff4d55-e859-4f6c-ae34-80f14da486c7` this is the UID: `faff4d55-e859-4f6c-ae34-80f14da486c7`

6. _Optional:_ Weather is enabled by entering an API key. Get yours free from [OpenWeatherMap][openweathermap-url]

```json
    "WeatherApiKey": "YourApiKey",
    "WeatherLatLong": "YourLatitude,YourLongitude",
```

7. Adjust other settings to your needs

### Settings GUI

The same information as in `Settings.json` will be enterred from this screen. Can be enterred at any time by clicking the upper middle quadrant of the screen (or Up arrow key), see `Interactions` section. Settings can also be backup/restored from here.

<!-- INTERACTIONS -->

## Interactions

### Touch/Mouse

The screen is configured in a 3x3 gird. You can touch or click:

|         -         | **Settings** |         -         |
| :---------------: | :----------: | :---------------: |
| **Prev<br>image** |  **Pause**   | **Next<br>image** |
|         -         |   **Quit**   |         -         |

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

<!-- Help -->

## Help

[Discord Channel](https://discord.com/channels/979116623879368755/1217843270244372480)

<!-- ACKNOWLEDGMENTS -->

## Acknowledgments

- BIG thanks to the [immich team][immich-github-url] for creating an awesome tool
- [Img Shields](https://shields.io)
- [GitHub Pages](https://pages.github.com)

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
[play-store-link]: https://play.google.com/store/apps/details?id=com.immichframe.immichframe
