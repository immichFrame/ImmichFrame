## Back
Go back to the [Full Readme](/README.md)

## ImmichFrame Web
- [Back](#back)
- [ImmichFrame Web](#immichframe-web)
- [✨ Demo](#-demo)
- [Installation](#installation)
- [Windows](#windows)
- [Linux](#linux)
- [MacOS](#macos)
  - [Autostart on Raspberry Pi OS](#autostart-on-raspberry-pi-os)
- [Android](#android)
  - [Android Screensaver](#android-screensaver)
  - [Android TV Screensaver](#android-tv-screensaver)
- [Configuration](#configuration)
- [Interactions](#interactions)
  - [Touch/Mouse](#touchmouse)
  - [Keyboard:](#keyboard)
  - [Settings GUI](#settings-gui)
- [Help](#help)

## ✨ Demo
![ImmichFrame Client](/design/demo/client_demo.png)

## Installation

## Windows
- Double-click `Immich_Frame.exe`.
- Screensaver - Rename `Immich_Frame.exe` to `Immich_Frame.scr`. Right-click &rarr; Install. Configure screensaver settings and apply.
  - You will still have to click middle/bottom in the app to exit screensaver.

## Linux

- GUI - Double-click `Immich_Frame`.
- CLI - CD into folder, and launch with `./Immich_Frame`.
- SSH - CD into folder, and launch with `DISPLAY=:0.0 ./Immich_Frame`.
- Ubuntu Desktop - Right-click Immich_Frame, properties, set 'Execute as program' to True, OK. Right-click Immich_Frame, Open with, choose 'Run Software', always use for this file type TRUE.
- If you get a permissions error run `chmod +x Immich_Frame`.

## MacOS

- GUI - Double-click `Immich_Frame`. Note: If nothing happens, right-click &rarr; open with &rarr; Utilities &rarr; Terminal. Check Always Open With.
- CLI - CD into folder, and launch with `./Immich_Frame`.
- If you get a permissions error run `chmod +x Immich_Frame`.

### Autostart on Raspberry Pi OS

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
## Android
The Android-Version of ImmichFrame is available on the [Google Play Store][play-store-link]. Download it via the store for automatic updates.

### Android Screensaver

- Run the app normally and configure settings.
- Go to Settings, Display, Advanced, Screen Saver, Current Screen Saver, choose ImmichFrame. Settings, Display, Advanced, Sleep, choose your sleep timeout. The menu options may differ slightly on different Android versions.

### Android TV Screensaver

- If you are unable to set ImmichFrame as a screen saver you may need to run this ADB command `adb shell settings put secure screensaver_components com.immichframe.immichframe/.ScreenSaverService`


## Configuration

> [!IMPORTANT]  
> Make sure to copy the **Settings.example.json** and name it **Settings.json**.

For more information, read [HERE](/README.md#configuration)

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

### Settings GUI

The same information as in `Settings.json` will be enterred from this screen. Can be enterred at any time by clicking the upper middle quadrant of the screen (or Up arrow key), see `Interactions` section. Settings can also be backup/restored from here.

## Help

[Discord Channel][support-url]


<!-- MARKDOWN LINKS & IMAGES -->
[support-url]: https://discord.com/channels/979116623879368755/1217843270244372480
[play-store-link]: https://play.google.com/store/apps/details?id=com.immichframe.immichframe
[releases-url]: https://github.com/3rob3/ImmichFrame/releases/latest