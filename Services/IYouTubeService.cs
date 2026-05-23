using YoutubeExplode.Videos.Streams;

public interface IYouTubeService
{
    Task<List<VideoModel>> GetPlaylistVideosAsync(string playlistUrl);

    Task<VideoModel> GetVideoAsync(string url);

    Task<DownloadStreamInfo> GetStreamForPresetAsync(string videoUrl, DownloadPreset preset);
}