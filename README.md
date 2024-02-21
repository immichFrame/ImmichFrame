This project was created in VS Code using the Avalonia extension. It is a C# .NET 7 cross platform project and currently supports Linux, MacOS, and Windows. I am running it on a Raspberry Pi 5.
Configure your server URL, API key, and more in the Settings.xml file. The screen is configured in quadrants, click or touch middle/right to skip forward, middle/left to go back to last image, and middle/bottom to quit.

> [!IMPORTANT]  
> Make sure to copy the **Settings.example.xml** and name it **Settings.xml**

Settings field descriptions:
- ImmichServerUrl: The IP address or URL of your Immich server (ex. http://192.168.0.100:2283)
- ApiKey: Your Immich ApiKey created on ImmichServerUrl/user-settings
- *Albums\**: Choose one or multiple Albums to have in the rotation
  - *AlbumId\**: Immich Album UID, (ex. ImmichServerUrl/albums/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
- Interval: Interval for photo cycling in seconds (ex. 8)
- ShowClock: Show clock in lower left bottom. Boolean (ex. true)
- ClockFontSize: The font size for the clock (ex. 48)
- ClockFormat: The format of the clock (ex. h:mm tt). See here for options https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
- ShowPhotoDate: Show the date photo was taken. Boolean (ex. true)
- PhotoDateFontSize: The font size for the photo date (ex. 36)
- PhotoDateFormat: The format of the date (ex. MM/dd/yyyy). See here for options https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
- ShowWeather: Show the current weather. Boolean (ex. true)
- WeatherFontSize: The font size for the weather data (ex. 12)
- WeatherUnits: The temperature units. either celcius or fahrenheit
- WeatherLatLong: Your latitude/longitude coordinates (ex. 40.7128,74.0060)

\* *Optional parameters*
