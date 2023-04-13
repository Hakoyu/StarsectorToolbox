using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StarsectorToolbox.Models.Messages;

internal class ShowCrashReporterMessage : ValueChangedMessage<int>
{
    public ShowCrashReporterMessage(int value) : base(value)
    {
    }
}
