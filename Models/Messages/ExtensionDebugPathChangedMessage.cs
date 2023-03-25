using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorToolbox.Models.Messages;

internal sealed class ExtensionDebugPathChangedMessage : ValueChangedMessage<string>
{
    public ExtensionDebugPathChangedMessage(string value) : base(value)
    {
    }
}