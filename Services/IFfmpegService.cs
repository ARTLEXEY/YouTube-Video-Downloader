using YoutubeExplode.Videos.Streams;

public interface IFfmpegService
{
    Task ConvertToMp3Async(IAudioStreamInfo audioStream, string outputPath, IProgress<double> progress);

    Task MergeVideoAndAudioAsync(IVideoStreamInfo videoStream, IAudioStreamInfo audioStream, string outputPath, IProgress<double> progress);
}