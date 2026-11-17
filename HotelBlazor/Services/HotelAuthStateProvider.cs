using Microsoft.AspNetCore.Components.Authorization;

namespace HotelBlazor.Services;

public sealed class HotelAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthSession _session;

    public HotelAuthStateProvider(AuthSession session)
    {
        _session = session;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_session.Principal));

    public void Notify() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
