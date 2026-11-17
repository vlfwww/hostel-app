using HotelApi.Dtos;
using HotelApi.Services;

namespace HotelApi.Endpoints;

public static class BookingEndpoints
{
    public static RouteGroupBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings")
            .WithTags("Бронирования")
            .RequireAuthorization("GuestOrAdmin")
            .AddEndpointFilter<Common.ValidationFilter>();

        group.MapGet("/", GetBookings)
            .WithName("GetBookings")
            .WithSummary("Список бронирований. Гость видит только свои.")
            .WithDescription("Фильтры: status, roomId, guestId (guestId учитывается только у администратора). Сортировка: checkIn, -checkIn, price, -price.")
            .Produces<PagedResult<BookingResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:int}", GetBooking)
            .WithName("GetBooking")
            .WithSummary("Бронирование по идентификатору")
            .Produces<BookingResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateBooking)
            .WithName("CreateBooking")
            .WithSummary("Создать бронирование")
            .Produces<BookingResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}/status", UpdateStatus)
            .WithName("UpdateBookingStatus")
            .WithSummary("Сменить статус. Гость может только отменить своё активное бронирование.")
            .Produces<BookingResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapDelete("/{id:int}", DeleteBooking)
            .WithName("DeleteBooking")
            .WithSummary("Удалить бронирование")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<IResult> GetBookings(
        [AsParameters] BookingQuery query,
        IBookingService bookings,
        CancellationToken cancellationToken)
    {
        var result = await bookings.SearchAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetBooking(int id, IBookingService bookings, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetAsync(id, cancellationToken);
        return TypedResults.Ok(booking);
    }

    private static async Task<IResult> CreateBooking(
        CreateBookingRequest request,
        IBookingService bookings,
        CancellationToken cancellationToken)
    {
        var created = await bookings.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/bookings/{created.Id}", created);
    }

    private static async Task<IResult> UpdateStatus(
        int id,
        UpdateBookingStatusRequest request,
        IBookingService bookings,
        CancellationToken cancellationToken)
    {
        var updated = await bookings.UpdateStatusAsync(id, request, cancellationToken);
        return TypedResults.Ok(updated);
    }

    private static async Task<IResult> DeleteBooking(int id, IBookingService bookings, CancellationToken cancellationToken)
    {
        await bookings.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
