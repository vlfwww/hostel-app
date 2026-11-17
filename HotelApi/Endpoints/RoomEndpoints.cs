using HotelApi.Dtos;
using HotelApi.Services;

namespace HotelApi.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms")
            .WithTags("Номера")
            .AddEndpointFilter<Common.ValidationFilter>();

        group.MapGet("/", GetRooms)
            .WithName("GetRooms")
            .WithSummary("Список номеров с фильтрацией, сортировкой и пагинацией")
            .WithDescription("Фильтры: type, minPrice, maxPrice, available, freeFrom, freeTo. Сортировка: number, -number, price, -price.")
            .AllowAnonymous()
            .Produces<PagedResult<RoomResponse>>();

        group.MapGet("/{id:int}", GetRoom)
            .WithName("GetRoom")
            .WithSummary("Номер по идентификатору")
            .AllowAnonymous()
            .Produces<RoomResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateRoom)
            .WithName("CreateRoom")
            .WithSummary("Создать номер")
            .RequireAuthorization("AdminOnly")
            .Produces<RoomResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", UpdateRoom)
            .WithName("UpdateRoom")
            .WithSummary("Изменить номер")
            .RequireAuthorization("AdminOnly")
            .Produces<RoomResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteRoom)
            .WithName("DeleteRoom")
            .WithSummary("Удалить номер")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> GetRooms(
        [AsParameters] RoomQuery query,
        IRoomService rooms,
        CancellationToken cancellationToken)
    {
        var result = await rooms.SearchAsync(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetRoom(int id, IRoomService rooms, CancellationToken cancellationToken)
    {
        var room = await rooms.GetAsync(id, cancellationToken);
        return TypedResults.Ok(room);
    }

    private static async Task<IResult> CreateRoom(UpsertRoomRequest request, IRoomService rooms, CancellationToken cancellationToken)
    {
        var created = await rooms.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/rooms/{created.Id}", created);
    }

    private static async Task<IResult> UpdateRoom(int id, UpsertRoomRequest request, IRoomService rooms, CancellationToken cancellationToken)
    {
        var updated = await rooms.UpdateAsync(id, request, cancellationToken);
        return TypedResults.Ok(updated);
    }

    private static async Task<IResult> DeleteRoom(int id, IRoomService rooms, CancellationToken cancellationToken)
    {
        await rooms.DeleteAsync(id, cancellationToken);
        return TypedResults.NoContent();
    }
}
