using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RideTogether.Entities.Enums;
using RideTogether.Interfaces;
using RideTogether.Interfaces.Services;
using RideTogether.Models;
using RideTogether.Models.DTO;
using RideTogether.Models.Enums;
using RideTogether.Services;

namespace RideTogether.Controllers;

[ApiController]
[Route("[controller]")]
public class RideController : ControllerBase
{
    private readonly IRideService _service;
    private readonly IRideBookingService _bookingService;

    public RideController(IRideService service, IRideBookingService bookingService)
    {
        _service = service;
        _bookingService = bookingService;
    }

    [HttpGet("all", Name = "all")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetRides());
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromBody] RideSearchRequest request,
        [FromServices] RideSearchParser parser,
        CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim();
        if (string.IsNullOrEmpty(query) || query.Length > 250)
            return BadRequest(new { message = "Enter a search of up to 250 characters." });

        RideSearchFilters filters;
        try
        {
            filters = await parser.ParseAsync(query, cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException ||
            ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return StatusCode(503, new { message = "Search is unavailable. Please try again." });
        }

        var culture = CultureInfo.InvariantCulture;
        DateOnly? date = null;
        TimeOnly? after = null;

        if (filters.Date is not null)
        {
            if (!DateOnly.TryParseExact(filters.Date, "yyyy-MM-dd", culture,
                    DateTimeStyles.None, out var parsedDate))
                return BadRequest(new { message = "Enter a clearer departure date." });

            date = parsedDate;
        }

        if (filters.After is not null)
        {
            if (!TimeOnly.TryParseExact(filters.After, "HH:mm", culture,
                    DateTimeStyles.None, out var parsedTime))
                return BadRequest(new { message = "Enter a clearer departure time." });

            after = parsedTime;
        }

        if (string.IsNullOrWhiteSpace(filters.Departure) &&
            string.IsNullOrWhiteSpace(filters.Destination) &&
            date is null && after is null)
            return BadRequest(new { message = "Enter a departure, destination, date, or time." });

        string[] formats =
        [
            "yyyy-MM-dd'T'HH:mm:ss", "dd-MM-yyyy'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm", "dd-MM-yyyy'T'HH:mm"
        ];

        var rides = await _service.GetRides();
        var matches = rides.Where(ride =>
        {
            if (ride.AvailableSeats <= 0 ||
                !MatchesCity(ride.Departure, filters.Departure) ||
                !MatchesCity(ride.Destination, filters.Destination))
                return false;

            if (date is null && after is null)
                return true;

            return DateTime.TryParseExact(
                       ride.StartAt, formats, culture, DateTimeStyles.None, out var startAt) &&
                   (date is null || DateOnly.FromDateTime(startAt) == date.Value) &&
                   (after is null || TimeOnly.FromDateTime(startAt) >= after.Value);
        }).ToList();

        return Ok(matches);
    }

    private static bool MatchesCity(string actual, string? requested)
    {
        return string.IsNullOrWhiteSpace(requested) ||
               CultureInfo.InvariantCulture.CompareInfo.Compare(
                   actual.Trim(), requested.Trim(),
                   CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
    }

    [HttpGet("by-id/{id}", Name = "GetRide")]
    public async Task<IActionResult> GetRide([FromRoute] int id)
    {
        return Ok(await _service.GetRideById(id));
    }

    [Authorize]
    [HttpPost("create", Name = "create")]
    public async Task<IActionResult> Create([FromBody] RideDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var ride = await _service.CreateRide(request, userId);

        return Ok(ride);
    }

    [Authorize]
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] RideDto request)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        if (request.Id <= 0)
            return BadRequest(new { message = "A valid ride ID is required." });

        var result = await _service.UpdateRide(request, userId);
        return ToWriteResponse(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _service.DeleteRideById(id, userId);
        return ToWriteResponse(result);
    }

    [Authorize]
    [HttpPost("{id:int}/book")]
    public async Task<IActionResult> Book(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _bookingService.BookRideAsync(
            id, userId, cancellationToken);

        return result switch
        {
            RideBookingResult.Success => NoContent(),
            RideBookingResult.NotFound => NotFound(),
            RideBookingResult.Forbidden => Forbid(),
            RideBookingResult.AlreadyBooked =>
                Conflict(new { message = "You already have a booking for this ride." }),
            RideBookingResult.Full =>
                Conflict(new { message = "No seats are available for this ride." }),
            _ => Problem(
                title: "Could not book this ride.",
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)
        && userId > 0;

    private IActionResult ToWriteResponse(RideManageResultDto result) => result.Result switch
    {
        RideManageResult.Created => Created(),
        RideManageResult.Deleted => NoContent(),
        RideManageResult.Forbidden => Forbid(),
        RideManageResult.NotFound => NotFound(),
        RideManageResult.Updated => Ok(result.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
    };
}