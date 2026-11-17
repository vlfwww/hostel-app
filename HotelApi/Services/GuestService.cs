using HotelApi.Common;
using HotelApi.Data;
using HotelApi.Dtos;
using HotelApi.Mapping;
using HotelApi.Models;
using HotelApi.Repositories;

namespace HotelApi.Services;

public interface IGuestService
{
    Task<IReadOnlyList<GuestResponse>> ListAsync(CancellationToken cancellationToken);

    Task<GuestResponse> GetAsync(int id, CancellationToken cancellationToken);

    Task<GuestResponse> CreateAsync(CreateGuestRequest request, CancellationToken cancellationToken);
}

public sealed class GuestService : IGuestService
{
    private readonly IGuestRepository _guests;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GuestService> _logger;

    public GuestService(IGuestRepository guests, IUnitOfWork unitOfWork, ILogger<GuestService> logger)
    {
        _guests = guests;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GuestResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var guests = await _guests.ListAsync(cancellationToken);
        return guests.Select(GuestMapper.ToResponse).ToList();
    }

    public async Task<GuestResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var guest = await _guests.GetByIdAsync(id, cancellationToken)
            ?? throw ApiException.NotFound($"Гость {id} не найден.");
        return GuestMapper.ToResponse(guest);
    }

    public async Task<GuestResponse> CreateAsync(CreateGuestRequest request, CancellationToken cancellationToken)
    {
        var passport = request.PassportNumber.Trim();
        if (await _guests.PassportExistsAsync(passport, cancellationToken))
        {
            throw ApiException.Conflict($"Гость с паспортом {passport} уже есть.");
        }

        var guest = new Guest
        {
            FullName = request.FullName.Trim(),
            Phone = Clean(request.Phone),
            Email = Clean(request.Email),
            PassportNumber = passport
        };

        await _guests.AddAsync(guest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Администратор добавил гостя {GuestId} ({FullName})", guest.Id, guest.FullName);
        return GuestMapper.ToResponse(guest);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
