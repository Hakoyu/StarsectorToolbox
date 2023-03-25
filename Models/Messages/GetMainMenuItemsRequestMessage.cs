using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HKW.ViewModels.Controls;

namespace StarsectorToolbox.Models.Messages;

internal class GetMainMenuItemsRequestMessage : RequestMessage<List<ListBoxItemVM>>
{
}