---
sidebar_position: 2
---

# 🔧 Configuration

ImmichFrame looks for a configuration directory in the following places:

1. It reads the `IMMICHFRAME_CONFIG_PATH` environment variable (if present).
2. A `Config` directory relative to the ImmichFrame executable.
3. A `Config` directory relative to the current working directory.

Inside the configuration directory, ImmichFrame loads the first of these files:

1. `Settings.json`
2. `Settings.yml`
3. `Settings.yaml`

If none of these files are found, ImmichFrame loads its config from environment
variables.

### Full configuration reference:

:::warning
It is not recommended to copy this full configuration file.
Only override settings you intend to change.
Defaults might change between versions, so keeping your config minimal helps future upgrades.
:::

Defaults are below, only one account with `ImmichServerUrl` and `ApiKey`|`ApiKeyFile` are required.

```yaml
# settings applicable to the web client - when viewing with a browser or webview
General:
  # When set, every client needs to authenticate via Bearer Token and this value.
  AuthenticationSecret: null  # string, no default
  # whether to download images to the server
  DownloadImages: false  # boolean
  # if images are downloaded, re-download if age (in days) is more than this
  RenewImagesDuration: 30  # int
  # A list of webcalendar URIs in the .ics format. e.g. https://calendar.google.com/calendar/ical/XXXXXX/public/basic.ics
  Webcalendars:  # string[]
    - UUID
  # Interval in hours. Determines how often images are pulled from a person in immich.
  RefreshAlbumPeopleInterval: 12  # int
  # Date format. See https://date-fns.org/v4.1.0/docs/format for more information.
  PhotoDateFormat: 'MM/dd/yyyy'  # string
  ImageLocationFormat: 'City,State,Country'
  # Get an API key from OpenWeatherMap: https://openweathermap.org/appid
  WeatherApiKey: ''  # string
  # Imperial or metric system (Fahrenheit or Celsius)
  UnitSystem: 'imperial'  # 'imperial' | 'metric'
  # Set the weather location with lat/lon.
  WeatherLatLong: '40.730610,-73.935242'  # string
  # 2 digit ISO code, sets the language of the weather description.
  Language: 'en'  # string
  # Webhook URL to be notified e.g. http://example.com/notify
  Webhook: null  # string
  # Image interval in seconds. How long an image is displayed in the frame.
  Interval: 45
  # Duration in seconds.
  TransitionDuration: 2  # float
  # Displays the current time.
  ShowClock: true  # boolean
  # Time format
  ClockFormat: 'hh:mm'  # string
  # Date format for the clock
  ClockDateFormat: 'eee, MMM d' # string
  # Displays the progress bar.
  ShowProgressBar: true  # boolean
  # Displays the date of the current image.
  ShowPhotoDate: true  # boolean
  # Displays the description of the current image.
  ShowImageDesc: true  # boolean
  # Displays a comma separated list of names of all the people that are assigned in immich.
  ShowPeopleDesc: true  # boolean
  # Displays a comma separated list of names of all the albums for an image.
  ShowAlbumName: true  # boolean
  # Displays the location of the current image.
  ShowImageLocation: true  # boolean
  # Lets you choose a primary color for your UI. Use hex with alpha value to edit opacity.
  PrimaryColor: '#f5deb3'  # string
  # Lets you choose a secondary color for your UI. (Only used with `style=solid or transition`) Use hex with alpha value to edit opacity.
  SecondaryColor: '#000000'  # string
  # Background-style of the clock and metadata.
  Style: 'none'  # none | solid | transition | blur
  # Sets the base font size, uses standard CSS formats (https://developer.mozilla.org/en-US/docs/Web/CSS/font-size)
  BaseFontSize: '17px'  # string
  # Displays the description of the current weather.
  ShowWeatherDescription: true  # boolean
  # URL for the icon to load for the current weather condition
  WeatherIconUrl: 'https://openweathermap.org/img/wn/{IconId}.png'
  # Zooms into or out of an image and gives it a touch of life.
  ImageZoom: true  # boolean
  # Pans an image in a random direction and gives it a touch of life.
  ImagePan: false  # boolean
  # Whether image should fill available space. Aspect ratio maintained but may be cropped.
  ImageFill: false  # boolean
  # Allow two portrait images to be displayed next to each other
  Layout: 'splitview'  # single | splitview

# multiple accounts permitted
Accounts:
  - # The URL of your Immich server e.g. `http://photos.yourdomain.com` / `http://192.168.0.100:2283`.
    ImmichServerUrl: 'REQUIRED'  # string, required, no default
    # Read more about how to obtain an Immich API key: https://immich.app/docs/features/command-line-interface#obtain-the-api-key
    # Exactly one of ApiKey or ApiKeyFile must be set.
    ApiKey: "super-secret-api-key"
    # ApiKeyFile: "/path/to/api.key"
    # Show images after date. Overwrites the `ImagesFromDays`-Setting
    ImagesFromDate: null  # Date
    # If this is set, memories are displayed.
    ShowMemories: false  # boolean
    # If this is set, favorites are displayed.
    ShowFavorites: false  # boolean
    # If this is set, assets marked archived are displayed.
    ShowArchived: false  # boolean
    # Show images from the last X days, e.g., 365 -> show images from the last year
    ImagesFromDays: null  # int
    # Show images before date.
    ImagesUntilDate: '2020-01-02'  # Date
    # Rating of an image in stars, allowed values from -1 to 5. This will only show images with the exact rating you are filtering for.
    Rating: null  # int
    # UUID of album(s) - e.g. ['00000000-0000-0000-0000-000000000001']
    Albums:  # string[]
      - UUID
    # UUID of excluded album(s)
    ExcludedAlbums:  # string[]
      - UUID
    # UUID of People
    People:  # string[]
      - UUID

  ```
### Security
Basic authentication can be added via `AuthenticationSecret`. It is **NOT** recommended to expose immichFrame to the public web, if you still choose to do so, you can set this to a secure secret. Every client needs to authenticate itself with this secret. This can be done in the Webclient via input field or via URL-Parameter. The URL-Parameter will look like this: `?authsecret=[MYSECRET]`

If this is enabled, the web api required the `Authorization`-Header with `Bearer [MYSECRET]`.

### Filtering on Albums or People
You can get the UUIDs from the URL of the album/person. For this URL: `https://demo.immich.app/albums/85c85b29-c95d-4a8b-90f7-c87da1d518ba` this is the UUID: `85c85b29-c95d-4a8b-90f7-c87da1d518ba`

### Weather
Weather is enabled by entering an API key. Get yours free from [OpenWeatherMap][openweathermap-url]

### Calendar
If you are using Google Calendar, more information can be found [here](https://support.google.com/calendar/answer/37648?hl=en#zippy=%2Cget-your-calendar-view-only).

### Misc
#### Webhook
A webhook to notify an external service is available. This is only enabled when the `Webhook`-Setting is set in your configuration. Your configured Webhook will be notified via `HTTP POST`-request.

A client can be identified by the `ClientIdentifier`. You can set/overwrite the `ClientIdentifier` by adding `?client=MyClient` to your ImmichFrame-URL. This only needs to be called once and is persisted. Delete the cache to reset the `ClientIdentifier`.

#### Events
Events will always contain a `Name`, `ClientIdentifier` and a `DateTime` to differentiate, but can contain more information.

| **Event**                  | **Description**                      | **Payload**                                                                                                                                             |
| -------------------------- | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ImageRequestedNotification | Notifies when an image is requested. | `{"Name":"ImageRequestedNotification", "ClientIdentifier": "Frame_Kitchen", "DateTime":"2024-11-16T21:37:19.4933981+01:00", "RequestedImageId":"UUID"}` |

### Multiple Immich Accounts
ImmichFrame can be configured to access multiple Immich accounts, on the same or different servers.

Images will be drawn from each account proportionally based on the total number of images present in each account (not included filtering, this is not yet implemented).

### API Key Permissions
For full ImmichFrame functionality, the API key being used needs the following permissions:

- `album.read`
- `album.statistics`
- `asset.view`
- `asset.read`
- `asset.statistics`
- `face.read`
- `memory.read`
- `person.read`
- `person.statistics`


### Custom CSS
ImmichFrame can be customized even further using CSS. This will apply to browsers, and apps using WebView (i.e. everything but Frameo and AppleTV):
- Create a custom.css file somewhere on your host server with your desired content, for example:  
```css
#progressbar {  
  visibility: hidden;  
}
```
- Add an entry in your immichframe compose pointing to it:  
```
volumes:  
      - /PATH/TO/YOUR/custom.css:/app/wwwroot/static/custom.css
```


[openweathermap-url]: https://openweathermap.org/appid