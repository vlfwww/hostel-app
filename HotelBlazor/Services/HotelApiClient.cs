using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBlazor.Models;

namespace HotelBlazor.Services;

public sealed class ApiProblemException : Exception
{
    public ApiProblemException(int status, string message, Exception? inner = null)
        : base(message, inner)
    {
        Status = status;
    }

    public int Status { get; }

    public static async Task<ApiProblemException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = $"Запрос отклонён ({(int)response.StatusCode}).";
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ApiProblemException((int)response.StatusCode, message);
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var detail = ReadString(root, "detail");
            var title = ReadString(root, "title");
            var fields = new List<string>();
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in property.Value.EnumerateArray())
                    {
                        var line = item.GetString();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            fields.Add(line);
                        }
                    }
                }
            }

            message = fields.Count > 0 ? string.Join(" ", fields) : detail ?? title ?? message;
        }
        catch (JsonException)
        {
            if (text.Length < 300)
            {
                message = text;
            }
        }

        return new ApiProblemException((int)response.StatusCode, message);
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public sealed class HotelApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly AuthSession _session;

    public HotelApiClient(IHttpClientFactory factory, AuthSession session)
    {
        _http = factory.CreateClient("hotel");
        _session = session;
    }

    public Task<LoginResult> LoginAsync(LoginDraft draft, CancellationToken cancellationToken = default) =>
        ReadAsync<LoginResult>(HttpMethod.Post, "api/auth/login", new
        {
            userName = draft.UserName,
            password = draft.Password
        }, cancellationToken);

    public Task<LoginResult> RegisterAsync(RegisterDraft draft, CancellationToken cancellationToken = default) =>
        ReadAsync<LoginResult>(HttpMethod.Post, "api/auth/register", new
        {
            userName = draft.UserName,
            password = draft.Password,
            fullName = draft.FullName,
            phone = BlankToNull(draft.Phone),
            email = draft.Email,
            passportNumber = draft.PassportNumber
        }, cancellationToken);

    public Task<UserProfile> MeAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<UserProfile>(HttpMethod.Get, "api/auth/me", null, cancellationToken);

    public Task<Paged<RoomModel>> GetRoomsAsync(RoomFilter filter, CancellationToken cancellationToken = default)
    {
        var parts = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };
        Add(parts, "type", filter.Type);
        Add(parts, "sort", filter.Sort);
        if (filter.MinPrice is decimal min)
        {
            parts.Add("minPrice=" + min.ToString(CultureInfo.InvariantCulture));
        }

        if (filter.MaxPrice is decimal max)
        {
            parts.Add("maxPrice=" + max.ToString(CultureInfo.InvariantCulture));
        }

        if (filter.Available is bool available)
        {
            parts.Add("available=" + (available ? "true" : "false"));
        }

        return ReadAsync<Paged<RoomModel>>(HttpMethod.Get, "api/rooms?" + string.Join('&', parts), null, cancellationToken);
    }

    public Task<RoomModel> GetRoomAsync(int id, CancellationToken cancellationToken = default) =>
        ReadAsync<RoomModel>(HttpMethod.Get, $"api/rooms/{id}", null, cancellationToken);

    public Task<RoomModel> CreateRoomAsync(RoomDraft draft, CancellationToken cancellationToken = default) =>
        ReadAsync<RoomModel>(HttpMethod.Post, "api/rooms", RoomBody(draft), cancellationToken);

    public Task<RoomModel> UpdateRoomAsync(int id, RoomDraft draft, CancellationToken cancellationToken = default) =>
        ReadAsync<RoomModel>(HttpMethod.Put, $"api/rooms/{id}", RoomBody(draft), cancellationToken);

    public Task DeleteRoomAsync(int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/rooms/{id}", null, cancellationToken);

    public Task<Paged<BookingModel>> GetBookingsAsync(BookingFilter filter, CancellationToken cancellationToken = default)
    {
        var parts = new List<string>
        {
            $"page={filter.Page}",
            $"pageSize={filter.PageSize}"
        };
        Add(parts, "status", filter.Status);
        Add(parts, "sort", filter.Sort);
        return ReadAsync<Paged<BookingModel>>(HttpMethod.Get, "api/bookings?" + string.Join('&', parts), null, cancellationToken);
    }

    public Task<BookingModel> CreateBookingAsync(int roomId, BookingDraft draft, CancellationToken cancellationToken = default) =>
        ReadAsync<BookingModel>(HttpMethod.Post, "api/bookings", new
        {
            roomId,
            checkIn = draft.CheckIn,
            checkOut = draft.CheckOut,
            guestId = draft.GuestId
        }, cancellationToken);

    public Task<BookingModel> UpdateBookingStatusAsync(int id, string status, CancellationToken cancellationToken = default) =>
        ReadAsync<BookingModel>(HttpMethod.Put, $"api/bookings/{id}/status", new { status }, cancellationToken);

    public Task DeleteBookingAsync(int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/bookings/{id}", null, cancellationToken);

    public Task<List<GuestModel>> GetGuestsAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<List<GuestModel>>(HttpMethod.Get, "api/guests", null, cancellationToken);

    public Task<GuestModel> CreateGuestAsync(GuestDraft draft, CancellationToken cancellationToken = default) =>
        ReadAsync<GuestModel>(HttpMethod.Post, "api/guests", GuestBody(draft), cancellationToken);

    public Task<GuestModel> UpdateGuestAsync(int id, GuestDraft draft, CancellationToken cancellationToken = default) =>
        ReadAsync<GuestModel>(HttpMethod.Put, $"api/guests/{id}", GuestBody(draft), cancellationToken);

    public Task DeleteGuestAsync(int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/guests/{id}", null, cancellationToken);

    private static object RoomBody(RoomDraft draft) => new
    {
        number = draft.Number,
        type = draft.Type,
        pricePerNight = draft.PricePerNight,
        capacity = draft.Capacity,
        isAvailable = draft.IsAvailable
    };

    private static object GuestBody(GuestDraft draft) => new
    {
        fullName = draft.FullName,
        phone = BlankToNull(draft.Phone),
        email = draft.Email,
        passportNumber = draft.PassportNumber
    };

    private async Task<T> ReadAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var value = await SendAsync<T>(method, path, body, cancellationToken);
        return value ?? throw new ApiProblemException((int)HttpStatusCode.OK, "Гостиница вернула пустой ответ.");
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken) =>
        await SendAsync<object>(method, path, body, cancellationToken);

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(_session.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new ApiProblemException(0, "Сейчас не удаётся связаться с гостиницей. Обновите страницу.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiProblemException(0, "Гостиница отвечает слишком долго. Попробуйте ещё раз.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await ApiProblemException.FromResponseAsync(response, cancellationToken);
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return default;
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength == 0)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
        }
    }

    private static void Add(List<string> parts, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(name + "=" + Uri.EscapeDataString(value.Trim()));
        }
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
