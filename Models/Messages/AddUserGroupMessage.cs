using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorTools.Models.Messages
{
    internal class AddUserGroupMessage : ValueChangedMessage<string>
    {
        public AddUserGroupMessage(string value) : base(value)
        {
        }
    }
}