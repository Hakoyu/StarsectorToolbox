using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorTools.Models.Messages;

internal sealed class ExtensionDebugPathChangedMessage : ValueChangedMessage<string>
{
    public ExtensionDebugPathChangedMessage(string value) : base(value)
    {
    }
}