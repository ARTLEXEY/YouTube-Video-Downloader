using System.Diagnostics;
using System.IO;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

public class FfmpegService : IFfmpegService
{
    private readonly YoutubeClient _client = new();

    public async Task ConvertToMp3Async(IAudioStreamInfo audioStream, string outputPath, IProgress<double> progress)
    {
        var tempAudio = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}." + audioStream.Container.Name);

        await _client.Videos.Streams.DownloadAsync(audioStream, tempAudio, progress);

        var ffmpegPath = EnsureFfmpegExists();

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-i \"{tempAudio}\" " + $"-vn -ab 192k " + $"\"{outputPath}\" -y",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        await process.WaitForExitAsync();

        File.Delete(tempAudio);
    }

    public async Task MergeVideoAndAudioAsync(IVideoStreamInfo videoStream, IAudioStreamInfo audioStream, string outputPath, IProgress<double> progress)
    {
        var tempVideo = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}." + videoStream.Container.Name);

        var tempAudio = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}." + audioStream.Container.Name);

        await _client.Videos.Streams.DownloadAsync(videoStream, tempVideo, progress);

        await _client.Videos.Streams.DownloadAsync(audioStream, tempAudio);

        var ffmpegPath = EnsureFfmpegExists();

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-i \"{tempVideo}\" " + $"-i \"{tempAudio}\" " + $"-c:v copy -c:a aac " + $"\"{outputPath}\" -y",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        await process.WaitForExitAsync();

        File.Delete(tempVideo);
        File.Delete(tempAudio);
    }

    private string EnsureFfmpegExists()
    {
        var toolsPath = Path.Combine(AppContext.BaseDirectory, "Tools");

        Directory.CreateDirectory(toolsPath);

        var ffmpegPath = Path.Combine(toolsPath, "ffmpeg.exe");

        if (!File.Exists(ffmpegPath))
        {
            throw new FileNotFoundException($"ffmpeg.exe not found in:\n{toolsPath}");
        }

        return ffmpegPath;
    }
}