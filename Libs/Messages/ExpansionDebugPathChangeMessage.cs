using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorTools.Libs.Messages
{
    internal sealed class ExtensionDebugPathChangeMessage : ValueChangedMessage<string>
    {
        public ExtensionDebugPathChangeMessage(string value) : base(value)
        {
        }
    }
}
