using DotIA.Mobile.ViewModels;

namespace DotIA.Mobile.Views;

public partial class TecnicoPage : ContentPage
{
    private readonly TecnicoViewModel _viewModel;

    public TecnicoPage(TecnicoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopAutoRefresh();
    }
}
