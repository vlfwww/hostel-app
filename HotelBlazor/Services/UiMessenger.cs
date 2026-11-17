namespace HotelBlazor.Services;

public sealed class UiMessenger
{
    public string? Message { get; private set; }

    public bool IsError { get; private set; }

    public event Action? Changed;

    public void Success(string message) => Show(message, false);

    public void Error(string message) => Show(message, true);

    public void Clear()
    {
        Message = null;
        Changed?.Invoke();
    }

    private void Show(string message, bool error)
    {
        Message = message;
        IsError = error;
        Changed?.Invoke();
    }
}
