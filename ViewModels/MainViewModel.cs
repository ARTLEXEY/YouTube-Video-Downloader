using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

public partial class MainViewModel : ObservableObject
{
    private readonly IYouTubeService _youTubeService;
    private readonly IFfmpegService _ffmpegService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string inputUrl;

    [ObservableProperty]
    private bool isBusy;

    public int TotalCount => Videos.Count;

    public int SelectedCount => Videos.Count(v => v.IsSelected);

    public ObservableCollection<DownloadPreset> Presets { get; } = new();

    [ObservableProperty]
    private DownloadPreset selectedPreset;

    public ObservableCollection<VideoItemViewModel> Videos { get; } = new();

    public MainViewModel(IYouTubeService youTubeService, IFfmpegService ffmpegService, IDialogService dialogService)
    {
        _youTubeService = youTubeService;
        _ffmpegService = ffmpegService;
        _dialogService = dialogService;

        Presets.Add(new DownloadPreset
        {
            DisplayName = "MP3 128 kbps",
            DownloadType = DownloadType.AudioMp3,
            QualityValue = 128
        });

        Presets.Add(new DownloadPreset
        {
            DisplayName = "MP3 192 kbps",
            DownloadType = DownloadType.AudioMp3,
            QualityValue = 192
        });

        Presets.Add(new DownloadPreset
        {
            DisplayName = "Video 360p",
            DownloadType = DownloadType.Video,
            QualityValue = 360
        });

        Presets.Add(new DownloadPreset
        {
            DisplayName = "Video 720p",
            DownloadType = DownloadType.Video,
            QualityValue = 720
        });

        Presets.Add(new DownloadPreset
        {
            DisplayName = "Video 1080p",
            DownloadType = DownloadType.Video,
            QualityValue = 1080
        });

        SelectedPreset = Presets.First();

        Videos.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(SelectedCount));
        };

        WeakReferenceMessenger.Default.Register<DownloadedFileChangedMessage>(this, (r, m) =>
        {
            OpenDownloadFolderCommand.NotifyCanExecuteChanged();
        });
    }

    [RelayCommand]
    private async Task AddFromUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(InputUrl))
            return;

        try
        {
            // если ссылка на плейлист

            try
            {
                var playlist = await _youTubeService.GetPlaylistVideosAsync(InputUrl);

                foreach (var videoModel in playlist)
                {
                    var viewModel = new VideoItemViewModel(videoModel);

                    AddVideo(viewModel);
                }

                InputUrl = "";

                return;
            }
            catch (ArgumentException)
            {
                // игнор
            }

            // если ссылка на видео

            var model = await _youTubeService.GetVideoAsync(InputUrl);

            var vm = new VideoItemViewModel(model);

            AddVideo(vm);

            InputUrl = "";
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync()
    {
        var selectedVideos = Videos.Where(v => v.IsSelected).ToList();

        Directory.CreateDirectory("Downloads");

        var preset = SelectedPreset;

        foreach (var video in selectedVideos)
        {
            try
            {
                video.Status = "Downloading";
                video.Progress = 0;

                var progress = new Progress<double>(p =>
                {
                    video.Progress = p * 100;
                });

                var stream = await _youTubeService.GetStreamForPresetAsync(video.VideoUrl, preset);

                if (stream == null)
                {
                    video.Status = "No stream found";

                    continue;
                }

                // VIDEO

                if (preset.DownloadType == DownloadType.Video)
                {
                    var extension = "mp4";

                    var path = Path.Combine("Downloads", $"{SanitizeFileName(video.Title)}.{extension}");

                    await _ffmpegService.MergeVideoAndAudioAsync(stream.VideoStream, stream.AudioStream, path, progress);

                    video.DownloadedFilePath = path;
                }

                // MP3

                else
                {
                    var path = Path.Combine("Downloads", $"{SanitizeFileName(video.Title)}.mp3");

                    await _ffmpegService.ConvertToMp3Async(stream.AudioStream, path, progress);

                    video.DownloadedFilePath = path;
                }

                video.Progress = 100;
                video.Status = "Completed";
            }
            catch (FileNotFoundException ex)
            {
                video.Status = "FFmpeg not found";

                _dialogService.ShowError(ex.Message, "FFmpeg Missing");

                return;
            }
            catch (Exception ex)
            {
                video.Status = ex.Message;
            }
        }
    }

    private string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    [RelayCommand]
    private void RemoveVideo(VideoItemViewModel video)
    {
        if (video == null)
            return;

        RemoveVideoInternal(video);
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        var selectedVideos = Videos.Where(v => v.IsSelected).ToList();

        if (selectedVideos.Count == 0)
            return;

        foreach (var video in selectedVideos)
        {
            RemoveVideoInternal(video);
        }
    }


    [RelayCommand]
    private void SelectAll()
    {
        foreach (var video in Videos)
        {
            video.IsSelected = true;
        }
    }

    [RelayCommand]
    private void UnselectAll()
    {
        foreach (var video in Videos)
        {
            video.IsSelected = false;
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        Videos.Clear();

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(SelectedCount));
    }

    private void AddVideo(VideoItemViewModel video)
    {
        if (Videos.Any(v => v.VideoUrl == video.VideoUrl))
            return;

        video.PropertyChanged += Video_PropertyChanged;

        Videos.Add(video);
    }

    private void RemoveVideoInternal(VideoItemViewModel video)
    {
        video.PropertyChanged -= Video_PropertyChanged;

        Videos.Remove(video);
    }

    private void Video_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoItemViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedCount));
        }
    }

    [RelayCommand]
    private void OpenUrl(VideoItemViewModel video)
    {
        if (video == null)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = video.VideoUrl,
            UseShellExecute = true
        });
    }

    [RelayCommand(CanExecute = nameof(CanOpenFolder))]
    private void OpenDownloadFolder(VideoItemViewModel video)
    {
        if (video == null)
            return;

        if (string.IsNullOrWhiteSpace(video.DownloadedFilePath))
        {
            MessageBox.Show("File has not been downloaded yet.");

            return;
        }

        if (!File.Exists(video.DownloadedFilePath))
        {
            MessageBox.Show("Downloaded file not found.");

            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",

            Arguments = $"/select,\"{video.DownloadedFilePath}\"",

            UseShellExecute = true
        });
    }

    private bool CanOpenFolder(VideoItemViewModel video)
    {
        return video != null && !string.IsNullOrWhiteSpace(video.DownloadedFilePath);
    }
}