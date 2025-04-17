## 🔙 Back
Go back to the [Full Readme](/README.md).

## 💻 ImmichFrame Client
- [🔙 Back](#-back)
- [💻 ImmichFrame Client](#-immichframe-client)
- [✨ Demo](#-demo)
- [🔧 Installation](#-installation)
  - [Windows](#windows)
  - [Linux](#linux)
    - [Autostart on Raspberry Pi OS](#autostart-on-raspberry-pi-os)
  - [MacOS](#macos)
  - [iOS/iPadOS](#ios-and-ipados)
  - [Apple TV](#apple-tv)
  - [Android](#android)
    - [Android Screensaver](#android-screensaver)
- [⚙️ Configuration](#️-configuration)
- [💬 Interactions](#-interactions)
  - [Touch/Mouse](#touchmouse)
  - [Keyboard:](#keyboard)
  - [Settings GUI](#settings-gui)
- [🆘 Help](#-help)


## 🔧 Installation

### Windows
- Double-click `Immich_Frame.exe`.
- Screensaver - Rename `Immich_Frame.exe` to `Immich_Frame.scr`. Right-click &rarr; Install. Configure screensaver settings and apply.
  - You will still have to click middle/bottom in the app to exit screensaver.

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

### MacOS

- Open DMG, drag immichframe.app to applications folder.
- If you get an error that it is "damaged and can't be opened, you should move it to the trash" run `xattr -c /Applications/immichframe.app` in terminal.

### iOS and iPadOS
You can "install" ImmichFrame as a PWA by opening in a browser and going to Share Menu-Add to Homescreen.

### Apple TV
ImmichFrame is available on the [Apple TV App Store][app-store-link].

### Android
The Android-Version of ImmichFrame is available on the [Google Play Store][play-store-link]. Download it via the store for automatic updates. You can also sideload via APK available in Releases.


#### Android Screensaver

- Run the app normally and configure settings.
- Go to Settings, Display, Advanced, Screen Saver, Current Screen Saver, choose ImmichFrame. Settings, Display, Advanced, Sleep, choose your sleep timeout. The menu options may differ slightly on different Android versions.
  - If you are unable to set ImmichFrame as a screen saver you may need to run this ADB command `adb shell settings put secure screensaver_components com.immichframe.immichframe/.ScreenSaverService`
  - To view screensaver timeout use this ADB command `adb shell settings get system screen_off_timeout`
  - To set screensaver timeout use this ADB command `adb shell settings put system screen_off_timeout 60000` (timeout is is ms, so this would be 60 seconds).

#### Frameo
ImmichFrame can be run on inexpensive Frameo digital photo frames with some additional effort. You can typically find these for ~$40 USD. These devices are low powered and run a very old Android version, so they cannot run the full WebView version of the app (however most of the main features are still supported except SplitView). If you have not already, you will need to install ADB on your PC ([ADB instructions][ADB-link]).
ADB is often enabled on these devices by default, if it is not go to Frameo Settings-About-Enable Beta Program. Toggle ADB Access On-Off-On. Use the ADB commands below to sideload ImmichFrame APK, configure it to your liking, then disable the Frameo app to to set ImmichFrame as default Home app:
  - Sideload ImmichFrame: adb install /path/to/ImmichFrame_vXX.apk
  - Update existing ImmichFrame: adb install -r /path/to/ImmichFrame_vXX.apk
  - Start ImmichFrame: adb shell am start com.immichframe.immichframe/.MainActivity
      - Swipe down to enter ImmichFrame Settings
      - Configure URL and Authorization Secret (optional)
      - Disable WebView
  - Disable Frameo: 
      - adb shell su
      - pm disable net.frameo.frame
      - exit
        - If this doesn't stick on reboot, repeat the commands but power cycle after exit command
  - Some other useful ADB commands:
    - Reboot: adb reboot
        - You can also reboot or shutdown by holding down power button
    - Access Android Settings: adb shell am start -a android.settings.SETTINGS
    - Re-enable Frameo: repeat disable commands above but replace "disable" with "enable"
    - Start Frameo app: adb shell am start net.frameo.frame
    - Uninstall ImmichFrame: adb uninstall com.immichframe.immichframe

## ⚙️ Configuration

Simply enter the URL of ImmichFrame web Docker. All other configuration is handled in the Docker container. 

## 💬 Interactions

### Android
Settings - swipe down.  
Previous Image/Pause/Next Image - Touch the left side/center/or right side of screen
### Android TV and Apple TV
Settings - D-pad UP.  
Previous Image/Pause/Next Image - D-pad left/D-pad center/D-pad right

### Desktop (Windows, MacOS, Linux)

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

## 🆘 Help

[Discord Channel][support-url]


<!-- MARKDOWN LINKS & IMAGES -->
[support-url]: https://discord.com/channels/979116623879368755/1217843270244372480
[play-store-link]: https://play.google.com/store/apps/details?id=com.immichframe.immichframe
[app-store-link]: https://apps.apple.com/us/app/immichframe/id6742748077
[releases-url]: https://github.com/3rob3/ImmichFrame/releases/latest
[ADB-link]: https://www.xda-developers.com/install-adb-windows-macos-linux/