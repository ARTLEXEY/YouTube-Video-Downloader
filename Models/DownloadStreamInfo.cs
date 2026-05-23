using YoutubeExplode.Videos.Streams;

public class DownloadStreamInfo
{
    public IVideoStreamInfo VideoStream { get; set; }

    public IAudioStreamInfo AudioStream { get; set; }

    public DownloadType DownloadType { get; set; }
}