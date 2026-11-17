namespace HotelApi.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "HotelApi";

    public string Audience { get; set; } = "HotelApiClient";

    /// <summary>
    /// Секрет подписи. Задаётся только через user-secrets или переменную Jwt__Key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public int ExpireMinutes { get; set; } = 120;
}
