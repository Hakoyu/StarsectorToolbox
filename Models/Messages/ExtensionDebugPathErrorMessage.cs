using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorTools.Models.Messages;

internal class ExtensionDebugPathErrorMessage : ValueChangedMessage<string>
{
    public ExtensionDebugPathErrorMessage(string value) : base(value)
    {
    }
}