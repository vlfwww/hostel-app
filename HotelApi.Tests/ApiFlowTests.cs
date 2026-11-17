using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HotelApi.Dtos;

namespace HotelApi.Tests;

public class ApiFlowTests : IClassFixture<HotelApiFactory>
{
    private readonly HotelApiFactory _factory;

    public ApiFlowTests(HotelApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Catalog_is_public_admin_creates_room_guest_is_forbidden()
    {
        using var client = _factory.CreateClient();

        var catalogResponse = await client.GetAsync("/api/rooms");
        catalogResponse.EnsureSuccessStatusCode();
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<PagedResult<RoomResponse>>();
        Assert.NotNull(catalog);
        Assert.True(catalog!.TotalCount >= 4);

        var admin = await LoginAsync(client, "admin", "Admin123!");
        Assert.Equal("Admin", admin.Role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.Token);

        var created = await client.PostAsJsonAsync("/api/rooms", new UpsertRoomRequest
        {
            Number = "777",
            Type = "Standard",
            PricePerNight = 99,
            Capacity = 2,
            IsAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var room = await created.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.Equal("777", room!.Number);

        var guest = await LoginAsync(client, "ivanov", "Guest123!");
        Assert.Equal("Guest", guest.Role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guest.Token);

        var forbidden = await client.PostAsJsonAsync("/api/rooms", new UpsertRoomRequest
        {
            Number = "778",
            Type = "Standard",
            PricePerNight = 90,
            Capacity = 2,
            IsAvailable = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var ownBookings = await client.GetAsync("/api/bookings");
        ownBookings.EnsureSuccessStatusCode();
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            UserName = userName,
            Password = password
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonSerializer.Deserialize<LoginResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Пустой ответ входа.");
    }
}
