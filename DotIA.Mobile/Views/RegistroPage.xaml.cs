using DotIA.Mobile.ViewModels;

namespace DotIA.Mobile.Views;

public partial class RegistroPage : ContentPage
{
    private readonly RegistroViewModel _viewModel;

    public RegistroPage(RegistroViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDepartamentosAsync();
    }
}
