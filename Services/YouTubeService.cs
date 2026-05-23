using System.IO;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

public class YouTubeService : IYouTubeService
{
    private readonly YoutubeClient _client = new();

    public async Task<List<VideoModel>> GetPlaylistVideosAsync(string playlistUrl)
    {
        var result = new List<VideoModel>();

        await foreach (var video in _client.Playlists.GetVideosAsync(playlistUrl))
        {
            result.Add(new VideoModel
            {
                Title = video.Title,
                Author = video.Author.ChannelTitle,
                Duration = video.Duration ?? TimeSpan.Zero,
                VideoUrl = $"https://www.youtube.com/watch?v={video.Id}"
            });
        }

        return result;
    }

    public async Task<DownloadStreamInfo> GetStreamForPresetAsync(string videoUrl, DownloadPreset preset)
    {
        var manifest = await _client.Videos.Streams.GetManifestAsync(videoUrl);

        // VIDEO

        if (preset.DownloadType == DownloadType.Video)
        {
            var videoStreams = manifest.GetVideoOnlyStreams();

            var videoStream = videoStreams.FirstOrDefault(s => s.VideoQuality.MaxHeight == preset.QualityValue);

            // fallback

            videoStream ??= (VideoOnlyStreamInfo?)videoStreams.GetWithHighestVideoQuality();

            var audioStream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            return new DownloadStreamInfo
            {
                VideoStream = videoStream,
                AudioStream = (IAudioStreamInfo)audioStream,
                DownloadType = DownloadType.Video
            };
        }

        // AUDIO / MP3

        var audioStreams = manifest.GetAudioOnlyStreams();

        var audioStreamResult = audioStreams.FirstOrDefault(s => s.Bitrate.KiloBitsPerSecond >= preset.QualityValue);

        // fallback

        audioStreamResult ??= (AudioOnlyStreamInfo?)audioStreams.GetWithHighestBitrate();

        return new DownloadStreamInfo
        {
            AudioStream = audioStreamResult,
            DownloadType = DownloadType.AudioMp3
        };
    }

    public async Task<VideoModel> GetVideoAsync(string url)
    {
        var video = await _client.Videos.GetAsync(url);

        return new VideoModel
        {
            Title = video.Title,
            VideoUrl = video.Url,
            Author = video.Author.Title,
            Duration = (TimeSpan)video.Duration,
        };
    }
}