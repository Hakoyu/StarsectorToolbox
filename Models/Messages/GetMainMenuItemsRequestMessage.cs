using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HKW.ViewModels.Controls;

namespace StarsectorTools.Models.Messages;

internal class GetMainMenuItemsRequestMessage : RequestMessage<List<ListBoxItemVM>>
{
}