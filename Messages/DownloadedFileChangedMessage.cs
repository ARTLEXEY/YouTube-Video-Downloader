using CommunityToolkit.Mvvm.Messaging.Messages;

public class DownloadedFileChangedMessage : ValueChangedMessage<VideoItemViewModel>
{
    public DownloadedFileChangedMessage(VideoItemViewModel value) : base(value)
    {
    }
}