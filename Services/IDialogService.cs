public interface IDialogService
{
    void ShowInfo(
        string message,
        string title = "Info");

    void ShowError(
        string message,
        string title = "Error");
}