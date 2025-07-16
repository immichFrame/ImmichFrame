---
sidebar_position: 3
---

# 📱 Apps

## Apple TV
ImmichFrame is available on the [Apple TV App Store][app-store-link].

See also: [ImmichFrame Apple TV repository][github-appletv-repo]

## iOS and iPadOS
You can "install" ImmichFrame as a PWA by opening in a browser and going to Share Menu-Add to Homescreen.

## Android
The Android-Version of ImmichFrame is available on the [Google Play Store][play-store-link]. Download it via the store for automatic updates. You can also sideload via APK available in Releases.

See also: [ImmichFrame Android repository][github-android-repo]

#### Android Screensaver

[How to Set Immich Frame as Android TV Screensaver Video Tutorial][setting-androidtv-screensaver]
- Run the app normally and configure settings.
- Go to `Settings > Display > Advanced > Screen Saver > Current Screen Saver` and choose ImmichFrame. `Settings > Display > Advanced > Sleep`, choose your sleep timeout. The menu options may differ slightly on different Android versions.
  - Enable Developer Mode on your Android TV Settings
  - Enable usb debugging
  - Add adb to your system variables so we can execute it in powershell/terminal/cmd 
  - Connect to the android tv device you want the screensaver on by running terminal/cmd/powershell 
    ```bash
    adb connect 192.168.xx.xxx
    ```
    (`192.168.xx.xxx` will be your android tv internal ip ie. `192.168.1.227`) you should see a prompt on your TV screen saying enable adb debugging. Click the checkmark and select ok. (Sometimes you may need to run the command a second time to get it to connect) 
  - If you are unable to set ImmichFrame as a screen saver you may need to run this ADB command 
    ```bash
    adb shell settings put secure screensaver_components com.immichframe.immichframe/.ScreenSaverService
    ```
  - To view screensaver timeout use this ADB command
    ```bash
    adb shell settings get system screen_off_timeout
    ```
  - To set screensaver timeout use this ADB command
    ```bash
    adb shell settings put system screen_off_timeout 60000
    ```
    (timeout is is ms, so this would be 60 seconds).
  - Once done disconnect using command
    ```bash
    adb disconnect 192.168.xx.xxx
    ```
    then re-do the above steps for any other device you want to change the screensaver to immichframe on another android tv device

### Remote Control
The Android app runs a small local HTTP server on port 53287 that accepts commands `dim`, `undim`, `next`, `previous`, `pause`, `settings`, `brightness?value=x`.  
Brightness value must be a float between 0.00 and 1.00, or -1.00 to use the system default.   
Examples:  
```shell
curl http://192.168.0.136:53287/next
```  
```shell
curl "http://192.168.0.136:53287/brightness?value=0.5"
```

### Frameo
ImmichFrame can be run on inexpensive Frameo digital photo frames with some additional effort. You can typically find these for ~$40 USD. These devices are low powered and run a very old Android version, so they cannot run the full WebView version of the app (however most of the main features are still supported except SplitView). We have found a method to update the WebView, but it is not without risks, see Frameo Webview Update below.  
 If you have not already, you will need to install ADB on your PC ([ADB instructions][ADB-link]).
ADB is often enabled on these devices by default, if it is not go to Frameo Settings-About-Enable Beta Program. Toggle ADB Access On-Off-On. Use the ADB commands below to sideload ImmichFrame APK, configure it to your liking, then disable the Frameo app to to set ImmichFrame as default Home app:
  - Sideload ImmichFrame:
    ```bash
    adb install /path/to/ImmichFrame_vXX.apk
    ```
  - Update existing ImmichFrame:
    ```bash
    adb install -r /path/to/ImmichFrame_vXX.apk
    ```
  - Start ImmichFrame:
    ```bash
    adb shell am start com.immichframe.immichframe/.MainActivity
    ```
    - Swipe down to enter ImmichFrame Settings
    - Configure URL and Authorization Secret (optional)
    - Disable WebView
  - Set as default HOME app
    - On first reboot after install you will be asked to select default Launcher, select ImmichFrame
    - Alternatively you can disable Frameo
      ```bash
      adb shell su
      pm disable net.frameo.frame
      exit
      ```
      If this doesn't stick on reboot, repeat the commands but power cycle after exit command
  - Some other useful ADB commands:
    - Reboot:
      ```bash
      adb reboot
      ``` 
        - You can also reboot or shutdown by holding down power button
    - Access Android Settings:
      ```bash 
      adb shell am start -a android.settings.SETTINGS
      ```
    - Re-enable Frameo: repeat disable commands above but replace "disable" with "enable"
    - Start Frameo app:
      ```bash 
      adb shell am start net.frameo.frame
      ```
    - Uninstall ImmichFrame:
      ```
      adb uninstall com.immichframe.immichframe
      ```
  
### Frameo WebView Update

```
⚠️ Disclaimer:

This might not work for you or require extra steps that are not covered here. Since there are a myriad of devices and Android versions, consider this procedure as not tested. Especially when talking about repurposed digital frames, vendor lockdowns vastly differ.

If this worked for you, consider opening a 'Show and tell'-Discussion.
```

```
❗️ Warning:
This might break your warranty
```

The basic procedure of updating the WebView installation consists of a few steps. Read through and understand them first, before you attempt any changes.

There are tested and working examples for Android 6.01 10.1" Frameo devices provided at the end. If you have one of those, you might as well skip directly to them.

- Download WebView to PC from [APKMirror (WebView by Lineage)][webview-lineage]
  - You most likely must use an APK with com.android.webview as the package name, that's why LineageOS version is recommended instead of the Google version (which is com.google.android.webview)
  - You must use a version that is compatible with your Android version
  - You must use a version that is compatible with your device's architecture
- Find the current WebView APK on your device
  ```shell
  adb shell pm path com.android.webview
  ```
  might return something like `/system/app/webview/webview.apk` or `/product/app/webview/webview.apk`
- Rename downloaded APK to match the original one (compare with output of the command above)
  ```
  mv downloaded-webview.apk webview.apk
  ```
- Push new apk to sdcard
  ```shell
  adb push /path/to/your/new/webview.apk /sdcard/
  ```
- Enter root shell
  ```shell
  adb shell su
  ```
- Mount the correct partition as read-write. Replace `/partition` with the first path of the original WebView APK on your device
  ```shell
  adb shell mount -o rw,remount /partition
  ```
- Backup original WebView APK
  ```shell
  mount -o rw,remount /partition && cp /path/to/original/webview.apk /path/to/original/webview.apk.bak
  ```
- Overwrite original WebView APK with the downloaded one
```shell
  mount -o rw,remount /partition && cp /sdcard/webview.apk /path/to/original/webview.apk
  ```
- Delete the oat folder recursively 
  - needed for Android 6.1 and most likely for all devices, where WebView is installed as a system app below `/system`
  - might not be needed/possible if original WebView is installed somewhere else, for example below `/product`
  ```shell
  mount -o rw,remount /system && rm -rf /system/app/webview/oat
  ```
- exit root
  ```shell
  exit
  ```
- Reboot device
  ```shell
  adb reboot
  ```
  This should reset the mounted /partition to read-only automatically
- You may or may not need to also follow up with an adb install of the APK
- Verify WebView version (optional)
  
  In Android developer settings, you can view the "WebView implementation". Check that this reflects the new version you pushed.

#### Complete step-by-step instructions for Android 6.01
Follow instructions below to update WebView to 106. This has been tested and working on Android 6.01 10.1" Frameo devices:
- Download WebView 106 to PC:
  [Lineage OS WebView 106-0-5249-126-12][webview-lineage-106]
- Push new apk to sdcard
  ```shell
  adb push /path/to/your/new/webview.apk /sdcard/
  ```
- Enter root shell
  ```shell
  adb shell su
  ```
- Backup original WebView APK
  ```shell
  mount -o rw,remount /system && cp /system/app/webview/webview.apk /system/app/webview/webview.apk.bak
  ```
- Delete the oat folder recursively
  ```shell
  mount -o rw,remount /system && rm -rf /system/app/webview/oat
  ```
- Copy new WebView to system
  ```shell
  mount -o rw,remount /system && cp /sdcard/webview.apk /system/app/webview/webview.apk
  ```
- exit root
  ```shell
  exit
  ```
- Reboot device
  ```shell
  adb reboot
  ```
### Desktop (Windows, MacOS, Linux)

Get the latest Desktop-App from the [GitHub Releases][releases-url]-Page.

The screen is configured in a 3x3 gird. You can touch or click:

|         -          | **Settings** |         -          |
| :----------------: | :----------: | :----------------: |
| **Prev<br/>image** |  **Pause**   | **Next<br/>image** |
|         -          |   **Quit**   |         -          |

### MacOS

- Open DMG, drag immichframe.app to applications folder.
- If you get an error that it is "damaged and can't be opened, you should move it to the trash" run following command in terminal.
  ```bash 
  xattr -c /Applications/immichframe.app
  ```

## Keyboard
**Settings** - Up arrow  
**Quit** - Down arrow  
**Prev Image** - Left arrow  
**Next Image** - Right arrow  
**Pause** - Enter/Return  


<!-- MARKDOWN LINKS & IMAGES -->
[play-store-link]: https://play.google.com/store/apps/details?id=com.immichframe.immichframe
[github-android-repo]: https://github.com/immichFrame/ImmichFrame_Android
[github-appletv-repo]: https://github.com/immichFrame/ImmichFrame_Apple
[app-store-link]: https://apps.apple.com/us/app/immichframe/id6742748077
[releases-url]: https://github.com/3rob3/ImmichFrame/releases/latest
[ADB-link]: https://www.xda-developers.com/install-adb-windows-macos-linux/
[webview-lineage]: https://www.apkmirror.com/apk/lineageos/android-system-webview-2/
[webview-lineage-106]: https://www.apkmirror.com/apk/lineageos/android-system-webview-2/android-system-webview-2-106-0-5249-126-release/android-system-webview-106-0-5249-126-12-android-apk-download/
[setting-androidtv-screensaver]: https://youtu.be/m3Arh-hrWks
