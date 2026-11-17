using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HotelApi.Tests;

public sealed class HotelApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hotel-lab9-{Guid.NewGuid():N}.db");

    public HotelApiFactory()
    {
        // Ключ читается в Program до того, как фабрика успевает дописать источники конфигурации.
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 48));
        Environment.SetEnvironmentVariable("Jwt__Issuer", "HotelApi");
        Environment.SetEnvironmentVariable("Jwt__Audience", "HotelApiClient");
        Environment.SetEnvironmentVariable("Jwt__ExpireMinutes", "120");
        Environment.SetEnvironmentVariable("ConnectionStrings__HotelDb", "Data Source=" + _dbPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(FindContentRoot());
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('k', 48),
                ["Jwt:Issuer"] = "HotelApi",
                ["Jwt:Audience"] = "HotelApiClient",
                ["Jwt:ExpireMinutes"] = "120",
                ["ConnectionStrings:HotelDb"] = "Data Source=" + _dbPath,
                ["Pagination:DefaultPageSize"] = "10",
                ["Pagination:MaxPageSize"] = "50"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            try
            {
                var path = suffix.Length == 0 ? _dbPath : _dbPath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Файл ещё держит SQLite. Временный каталог его подчистит.
            }
        }
    }

    private static string FindContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "HotelApi");
            if (File.Exists(Path.Combine(candidate, "HotelApi.csproj")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Не найден каталог HotelApi.");
    }
}
