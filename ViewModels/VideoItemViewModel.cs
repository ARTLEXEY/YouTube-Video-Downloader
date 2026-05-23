using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

public partial class VideoItemViewModel : ObservableObject
{
    public VideoModel Model { get; }

    public string Title => Model.Title;

    public string Author => Model.Author;

    public TimeSpan Duration => Model.Duration;

    public string VideoUrl => Model.VideoUrl;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string status = DownloadStatus.Waiting.ToString();

    [ObservableProperty]
    private string? downloadedFilePath;

    public VideoItemViewModel(VideoModel model)
    {
        Model = model;
    }

    partial void OnDownloadedFilePathChanged(string? value)
    {
        WeakReferenceMessenger.Default.Send(new DownloadedFileChangedMessage(this));
    }
}