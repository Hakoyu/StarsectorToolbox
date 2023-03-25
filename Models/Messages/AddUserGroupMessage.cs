using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorToolbox.Models.Messages;

internal class AddUserGroupMessage : ValueChangedMessage<string>
{
    public AddUserGroupMessage(string value) : base(value)
    {
    }
}