using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ImmichFrame.Core.Models;
using ImmichFrame.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImmichFrame.WebApi.Controllers;

public class WeatherCitySearchRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    // Accept the draft key so city search also works before the initial settings save.
    [StringLength(256)]
    public string? ApiKey { get; set; }
}

[ApiController]
[Route("api/Admin/Weather")]
[Authorize(AuthenticationSchemes = ImmichFrameAdminAuthenticationHandler.SchemeName)]
public class WeatherAdminController(IWeatherService weatherService) : ControllerBase
{
    [HttpPost("SearchCities", Name = "SearchWeatherCities")]
    [ProducesResponseType(typeof(List<WeatherCity>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<WeatherCity>>> SearchCities(
        [FromBody] WeatherCitySearchRequest search, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            return Ok(await weatherService.SearchCities(search.Query, search.ApiKey, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
            or InvalidOperationException or OperationCanceledException)
        {
            // Provider exception messages can contain the key-bearing URL.
            return Problem(detail: "OpenWeatherMap city search failed. Check your weather API key and try again.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
