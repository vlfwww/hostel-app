using HotelApi.Dtos;
using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/guests")]
[Authorize(Roles = Roles.Admin)]
public class GuestsController : ControllerBase
{
    private readonly IGuestService _guests;

    public GuestsController(IGuestService guests)
    {
        _guests = guests;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuestResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var guests = await _guests.ListAsync(cancellationToken);
        return Ok(guests);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GuestResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var guest = await _guests.GetAsync(id, cancellationToken);
        return Ok(guest);
    }

    [HttpPost]
    public async Task<ActionResult<GuestResponse>> Create(
        [FromBody] CreateGuestRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var details = new ValidationProblemDetails(ModelState)
            {
                Title = "Ошибка валидации",
                Instance = HttpContext.Request.Path
            };
            details.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return ValidationProblem(details);
        }

        var created = await _guests.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
