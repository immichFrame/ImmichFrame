---
sidebar_position: 2
---

# 🔧 Configuration

### Settings

| **Section**             | **Config-Key**             | **Value**                           | **Default**          | **Description**                                                                                                                                                 |
| ----------------------- | -------------------------- | ----------------------------------- | -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Required**            | **ImmichServerUrl**        | **string**                          |                      | **The URL of your Immich server e.g. `http://photos.yourdomain.com` / `http://192.168.0.100:2283`.**                                                            |
| **Required**            | **ApiKey**                 | **string**                          |                      | **Read more about how to obtain an [Immich API key][immich-api-url].**                                                                                          |
| [Security](#security)   | AuthenticationSecret       | string                              |                      | When set, every client needs to authenticate via Bearer Token and this value.                                                                                   |
| [Filtering](#filtering) | Albums                     | string[]                            | []                   | UUID of album(s)                                                                                                                                                |
| [Filtering](#filtering) | ExcludedAlbums             | string[]                            | []                   | UUID of excluded album(s)                                                                                                                                       |
| [Filtering](#filtering) | People                     | string[]                            | []                   | UUID of person(s)                                                                                                                                               |
| [Filtering](#filtering) | Rating                     | int                                 |                      | Rating of an image in stars, allowed values from -1 to 5. This will only show images with the exact rating you are filtering for.                               |
| [Filtering](#filtering) | ShowMemories               | boolean                             | false                | If this is set, memories are displayed.                                                                                                                         |
| [Filtering](#filtering) | ShowFavorites              | boolean                             | false                | If this is set, favorites are displayed.                                                                                                                        |
| [Filtering](#filtering) | ShowArchived               | boolean                             | false                | If this is set, assets marked archived are displayed.                                                                                                           |
| [Filtering](#filtering) | ImagesFromDays             | int                                 |                      | Show images from the last X days. e.g 365 -> show images from the last year                                                                                     |
| [Filtering](#filtering) | ImagesFromDate             | Date                                |                      | Show images after date. Overwrites the `ImagesFromDays`-Setting                                                                                                 |
| [Filtering](#filtering) | ImagesUntilDate            | Date                                |                      | Show images before date.                                                                                                                                        |
| Caching                 | RenewImagesDuration        | int                                 | 30                   | Interval in days.                                                                                                                                               |
| Caching                 | DownloadImages             | boolean                             | false                | \*Client only.                                                                                                                                                  |
| Caching                 | RefreshAlbumPeopleInterval | int                                 | 12                   | Interval in hours. Determines how often images are pulled from a person in immich.                                                                              |
| Image                   | ImageZoom                  | boolean                             | true                 | Zooms into or out of an image and gives it a touch of life.                                                                                                     |
| Image                   | ImageFill                  | boolean                             | false                | Whether image should fill available space. Aspect ratio maintained but may be cropped.                                                                          |
| Image                   | Interval                   | int                                 | 45                   | Image interval in seconds. How long a image is displayed in the frame.                                                                                          |
| Image                   | TransitionDuration         | float                               | 2                    | Duration in seconds.                                                                                                                                            |
| [Weather](#weather)     | WeatherApiKey              | string                              |                      | Get an API key from [OpenWeatherMap][openweathermap-url].                                                                                                       |
| [Weather](#weather)     | UnitSystem                 | imperial \| metric                  | imperial             | Imperial or metric system. (Fahrenheit or degrees)                                                                                                              |
| [Weather](#weather)     | Language                   | string                              | en                   | 2 digit ISO code, sets the language of the weather description.                                                                                                 |
| [Weather](#weather)     | ShowWeatherDescription     | boolean                             | true                 | Displays the description of the current weather.                                                                                                                |
| [Weather](#weather)     | WeatherLatLong             | boolean                             | 40.730610,-73.935242 | Set the weather location with lat/lon.                                                                                                                          |
| Clock                   | ShowClock                  | boolean                             | true                 | Displays the current time.                                                                                                                                      |
| Clock                   | ClockFormat                | string                              | hh:mm                | Time format.                                                                                                                                                    |
| [Calendar](#calendar)   | Webcalendars               | string[]                            | []                   | A list of webcalendar URIs in the .ics format, can include basic auth. e.g. username:password;https://calendar.google.com/calendar/ical/XXXXXX/public/basic.ics |
| [Metadata](#metadata)   | ShowImageDesc              | boolean                             | true                 | Displays the description of the current image.                                                                                                                  |
| [Metadata](#metadata)   | ShowPeopleDesc             | boolean                             | true                 | Displays a comma separated list of names of all the people that are assigned in immich.                                                                         |
| [Metadata](#metadata)   | ShowAlbumName              | boolean                             | true                 | Displays a comma separated list of names of all the albums for an image.                                                                                        |
| [Metadata](#metadata)   | ShowImageLocation          | boolean                             | true                 | Displays the location of the current image.                                                                                                                     |
| [Metadata](#metadata)   | ImageLocationFormat        | string                              | City,State,Country   |                                                                                                                                                                 |
| [Metadata](#metadata)   | ShowPhotoDate              | boolean                             | true                 | Displays the date of the current image.                                                                                                                         |
| [Metadata](#metadata)   | PhotoDateFormat            | string                              | yyyy-MM-dd           | Date format. See [here](https://date-fns.org/v4.1.0/docs/format) for more information.                                                                          |
| UI                      | PrimaryColor               | string                              | #f5deb3              | Lets you choose a primary color for your UI. Use hex with alpha value to edit opacity.                                                                          |
| UI                      | SecondaryColor             | string                              | #000000              | Lets you choose a secondary color for your UI. (Only used with `style=solid or transition`) Use hex with alpha value to edit opacity.                           |
| UI                      | Style                      | none \| solid \| transition \| blur | none                 | Background-style of the clock and metadata.                                                                                                                     |
| UI                      | Layout                     | single \| splitview                 | splitview            | Allow two portrait images to be displayed next to each other                                                                                                    |
| UI                      | BaseFontSize               | string                              | 17px                 | Sets the base font size, uses [standard CSS formats](https://developer.mozilla.org/en-US/docs/Web/CSS/font-size).                                               |
| [Misc](#misc)           | ImmichFrameAlbumName       | string                              |                      | \*Client only. Creates album and stores last 100 photos displayed.                                                                                              |
| [Misc](#misc)           | Webhook                    | string                              |                      | Webhook URL to be notified e.g. http://example.com/notify                                                                                                       |

### Security
Basic authentication can be added via this setting. It is **NOT** recommended to expose immichFrame to the public web, if you still choose to do so, you can set this to a secure secret. Every client needs to authenticate itself with this secret. This can be done in the Webclient via input field or via URL-Parameter. The URL-Parameter will look like this: `?authsecret=[MYSECRET]`

If this is enabled, the web api required the `Authorization`-Header with `Bearer [MYSECRET]`.

### Filtering
You can get the UUIDs from the URL of the album/person. For this URL: `https://demo.immich.app/albums/85c85b29-c95d-4a8b-90f7-c87da1d518ba` this is the UUID: `85c85b29-c95d-4a8b-90f7-c87da1d518ba`

### Weather
Weather is enabled by entering an API key. Get yours free from [OpenWeatherMap][openweathermap-url]

### Calendar
If you are using Google Calendar, more information can be found [here](https://support.google.com/calendar/answer/37648?hl=en#zippy=%2Cget-your-calendar-view-only).

Calendar supports basic authentication:
Example:
No Auth: `https://calendar.google.com/calendar/ical/XXXXXX/public/basic.ics`
With Auth: `username:password;https://calendar.google.com/calendar/ical/XXXXXX/public/basic.ics`

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
      - /PATH/TO/YOUR/custom.css:/app/wwwroot/static/custom.css"
```

[openweathermap-url]: https://openweathermap.org/appid
[immich-api-url]: https://immich.app/docs/features/command-line-interface#obtain-the-api-key