using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorTools.Models.Messages
{
    internal sealed class ExtensionDebugPathChangeMessage : ValueChangedMessage<string>
    {
        public ExtensionDebugPathChangeMessage(string value) : base(value)
        {
        }
    }
}