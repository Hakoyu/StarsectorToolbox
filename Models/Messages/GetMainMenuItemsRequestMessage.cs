using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HKW.ViewModels.Controls;

namespace StarsectorTools.Models.Messages
{
    internal class GetMainMenuItemsRequestMessage : RequestMessage<List<ListBoxItemVM>>
    {
    }
}
