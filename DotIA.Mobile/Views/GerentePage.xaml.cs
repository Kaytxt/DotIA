using DotIA.Mobile.ViewModels;

namespace DotIA.Mobile.Views;

public partial class GerentePage : ContentPage
{
    private readonly GerenteViewModel _viewModel;

    public GerentePage(GerenteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("=== GerentePage.OnAppearing - Iniciando ===");
        await _viewModel.InitializeAsync();
        System.Diagnostics.Debug.WriteLine("=== GerentePage.OnAppearing - Concluído ===");
    }
}
