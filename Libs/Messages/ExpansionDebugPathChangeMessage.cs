using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorTools.Libs.Messages
{
    internal sealed class ExpansionDebugPathChangeMessage : ValueChangedMessage<string>
    {
        public ExpansionDebugPathChangeMessage(string value) : base(value)
        {
        }
    }
}
