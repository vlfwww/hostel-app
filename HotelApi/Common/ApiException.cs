namespace HotelApi.Common;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public string Title { get; }

    public ApiException(int statusCode, string title, string detail)
        : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public static ApiException BadRequest(string detail) =>
        new(StatusCodes.Status400BadRequest, "Некорректный запрос", detail);

    public static ApiException Forbidden(string detail) =>
        new(StatusCodes.Status403Forbidden, "Доступ запрещён", detail);

    public static ApiException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "Не найдено", detail);

    public static ApiException Conflict(string detail) =>
        new(StatusCodes.Status409Conflict, "Конфликт", detail);
}
