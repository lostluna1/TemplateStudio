using CommunityToolkit.Mvvm.ComponentModel;

namespace Param_RootNamespace.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string title = "Main";
}
