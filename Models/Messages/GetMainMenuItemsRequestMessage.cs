using CommunityToolkit.Mvvm.Messaging.Messages;
using HKW.HKWViewModels.Controls;

namespace StarsectorToolbox.Models.Messages;

internal class GetMainMenuItemsRequestMessage : RequestMessage<List<ListBoxItemVM>> { }
