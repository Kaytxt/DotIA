using DotIA.Mobile.ViewModels;

namespace DotIA.Mobile.Views;

public partial class AbrirTicketPage : ContentPage
{
    public AbrirTicketPage(AbrirTicketViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
