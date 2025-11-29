using DotIA.Mobile.Models;
using DotIA.Mobile.ViewModels;

namespace DotIA.Mobile.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;
    private bool _isMenuOpen = false;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        if (_isMenuOpen)
            await CloseMenuAsync();
        else
            await OpenMenuAsync();
    }

    private async Task OpenMenuAsync()
    {
        _isMenuOpen = true;

        // Mostrar overlay
        Overlay.IsVisible = true;
        Overlay.InputTransparent = false;

        // Animar menu e overlay
        var tasks = new[]
        {
            Sidebar.TranslateTo(0, 0, 250, Easing.CubicOut),
            Overlay.FadeTo(0.5, 250)
        };

        await Task.WhenAll(tasks);
    }

    private async Task CloseMenuAsync()
    {
        _isMenuOpen = false;

        // Animar menu e overlay
        var tasks = new[]
        {
            Sidebar.TranslateTo(-280, 0, 250, Easing.CubicIn),
            Overlay.FadeTo(0, 250)
        };

        await Task.WhenAll(tasks);

        // Esconder overlay
        Overlay.IsVisible = false;
        Overlay.InputTransparent = true;
    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseMenuAsync();
    }

    private async void OnMenuItemClicked(object sender, EventArgs e)
    {
        // Fechar menu após clicar em um item
        await CloseMenuAsync();
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

    private DateTime _lastPointerPressedTime;
    private ChatHistoricoDTO? _lastPressedChat;

    private async void OnChatItemPointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not ChatHistoricoDTO chat)
            return;

        _lastPressedChat = chat;
        _lastPointerPressedTime = DateTime.Now;

        // Aguarda 500ms para detectar long press
        await Task.Delay(500);

        // Verifica se ainda está pressionado (long press)
        if (_lastPressedChat == chat && (DateTime.Now - _lastPointerPressedTime).TotalMilliseconds >= 500)
        {
            await MostrarMenuContextoChat(chat);
        }
    }

    private async Task MostrarMenuContextoChat(ChatHistoricoDTO chat)
    {
        var action = await DisplayActionSheet(
            chat.TituloExibicao,
            "Cancelar",
            "Excluir Chat",
            "Editar Título"
        );

        if (action == "Editar Título")
        {
            if (BindingContext is ChatViewModel viewModel)
            {
                await viewModel.EditarTituloChatCommand.ExecuteAsync(chat);
            }
        }
        else if (action == "Excluir Chat")
        {
            if (BindingContext is ChatViewModel viewModel)
            {
                await viewModel.ExcluirChatCommand.ExecuteAsync(chat.Id);
            }
        }
    }
}
