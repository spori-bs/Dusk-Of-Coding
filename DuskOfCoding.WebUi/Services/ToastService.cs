namespace DuskOfCoding.WebUi.Services;

/// <summary>
/// Lightweight scoped service for displaying toast notifications.
/// Components subscribe to OnToast and the ToastContainer renders the UI.
/// </summary>
public sealed class ToastService
{
    public event Func<string, string, Task>? OnToast;

    public Task ShowSuccess(string message) => Invoke(message, "success");
    public Task ShowError(string message)   => Invoke(message, "danger");
    public Task ShowInfo(string message)    => Invoke(message, "info");

    private Task Invoke(string message, string type)
        => OnToast?.Invoke(message, type) ?? Task.CompletedTask;
}
