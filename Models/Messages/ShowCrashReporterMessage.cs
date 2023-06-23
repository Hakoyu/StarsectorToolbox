using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorToolbox.Models.Messages;

internal class ShowCrashReporterMessage : ValueChangedMessage<int>
{
    public ShowCrashReporterMessage(int value)
        : base(value) { }
}
