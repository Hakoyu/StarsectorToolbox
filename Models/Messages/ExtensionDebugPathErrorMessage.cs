using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorTools.Models.Messages
{
    internal class ExtensionDebugPathErrorMessage : ValueChangedMessage<string>
    {
        public ExtensionDebugPathErrorMessage(string value) : base(value)
        {
        }
    }
}
