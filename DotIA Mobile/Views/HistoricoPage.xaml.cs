using DotIA_Mobile.Models;
using DotIA_Mobile.Services;

namespace DotIA_Mobile.Views
{
    public partial class HistoricoPage : ContentPage
    {
        private readonly IChatService _chatService;

        public HistoricoPage()
        {
            InitializeComponent();
            _chatService = new ChatService();
            CarregarHistorico();
        }

        private async void CarregarHistorico()
        {
            try
            {
                var historico = await _chatService.ObterHistoricoAsync(UserSession.UsuarioId ?? 0);

                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;

                if (!historico.Any())
                {
                    lblVazio.IsVisible = true;
                    return;
                }

                foreach (var chat in historico)
                {
                    AdicionarChatCard(chat);
                }
            }
            catch (Exception ex)
            {
                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;
                await DisplayAlert("Erro", $"Erro ao carregar histórico: {ex.Message}", "OK");
            }
        }

        private void AdicionarChatCard(ChatHistoricoDTO chat)
        {
            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 15,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#e5e7eb")
            };

            var mainLayout = new VerticalStackLayout { Spacing = 10 };

            // Header com título e status
            var headerLayout = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var lblTitulo = new Label
            {
                Text = chat.Titulo,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1e293b"),
                LineBreakMode = LineBreakMode.TailTruncation
            };
            Grid.SetColumn(lblTitulo, 0);

            var lblStatus = new Label
            {
                Text = ObterEmojiStatus(chat.Status),
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };
            Grid.SetColumn(lblStatus, 1);

            headerLayout.Children.Add(lblTitulo);
            headerLayout.Children.Add(lblStatus);

            // Pergunta
            var lblPergunta = new Label
            {
                Text = chat.Pergunta.Length > 100 ? chat.Pergunta.Substring(0, 100) + "..." : chat.Pergunta,
                FontSize = 14,
                TextColor = Color.FromArgb("#6b7280"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            // Data
            var lblData = new Label
            {
                Text = $"📅 {chat.DataHora:dd/MM/yyyy HH:mm}",
                FontSize = 12,
                TextColor = Color.FromArgb("#94a3b8")
            };

            // Status texto
            var lblStatusTexto = new Label
            {
                Text = $"Status: {chat.StatusTexto}",
                FontSize = 12,
                TextColor = ObterCorStatus(chat.Status),
                FontAttributes = FontAttributes.Bold
            };

            mainLayout.Children.Add(headerLayout);
            mainLayout.Children.Add(lblPergunta);
            mainLayout.Children.Add(lblData);
            mainLayout.Children.Add(lblStatusTexto);

            // Adicionar botão se tiver ticket pendente
            if (chat.Status == 3 && chat.IdTicket.HasValue)
            {
                var btnVerTicket = new Button
                {
                    Text = "📋 Ver Resposta do Técnico",
                    BackgroundColor = Color.FromArgb("#f59e0b"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    Padding = new Thickness(10, 5),
                    Margin = new Thickness(0, 10, 0, 0)
                };

                btnVerTicket.Clicked += async (s, e) =>
                {
                    await VerificarRespostaTecnico(chat.Id);
                };

                mainLayout.Children.Add(btnVerTicket);
            }

            // Botão de ver detalhes
            var btnDetalhes = new Button
            {
                Text = "Ver Detalhes",
                BackgroundColor = Color.FromArgb("#2563eb"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(10, 5),
                Margin = new Thickness(0, 5, 0, 0)
            };

            btnDetalhes.Clicked += async (s, e) =>
            {
                await Navigation.PushAsync(new DetalheChatPage(chat.Id));
            };

            mainLayout.Children.Add(btnDetalhes);

            frame.Content = mainLayout;
            historicoLayout.Children.Add(frame);
        }

        private string ObterEmojiStatus(int status)
        {
            return status switch
            {
                1 => "🔵", // Em andamento
                2 => "✅", // Concluído
                3 => "⏳", // Pendente
                4 => "🎯", // Resolvido
                _ => "❓"
            };
        }

        private Color ObterCorStatus(int status)
        {
            return status switch
            {
                1 => Color.FromArgb("#3b82f6"), // Azul - Em andamento
                2 => Color.FromArgb("#10b981"), // Verde - Concluído
                3 => Color.FromArgb("#f59e0b"), // Laranja - Pendente
                4 => Color.FromArgb("#8b5cf6"), // Roxo - Resolvido
                _ => Color.FromArgb("#6b7280")
            };
        }

        private async Task VerificarRespostaTecnico(int chatId)
        {
            try
            {
                var resposta = await _chatService.VerificarRespostaTecnicoAsync(chatId);

                if (resposta != null && resposta.TemResposta)
                {
                    await DisplayAlert("💬 Resposta do Técnico", resposta.Solucao, "OK");
                }
                else
                {
                    await DisplayAlert("ℹ️ Aguardando", "O técnico ainda não respondeu. Você será notificado assim que houver uma resposta.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao verificar resposta: {ex.Message}", "OK");
            }
        }
    }
}
