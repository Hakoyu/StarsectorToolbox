using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
