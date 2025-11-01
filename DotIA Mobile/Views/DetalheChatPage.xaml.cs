using DotIA_Mobile.Models;
using DotIA_Mobile.Services;

namespace DotIA_Mobile.Views
{
    public partial class DetalheChatPage : ContentPage
    {
        private readonly IChatService _chatService;
        private readonly int _chatId;

        public DetalheChatPage(int chatId)
        {
            InitializeComponent();
            _chatService = new ChatService();
            _chatId = chatId;
            CarregarDetalhes();
        }

        private async void CarregarDetalhes()
        {
            try
            {
                var detalhes = await _chatService.ObterDetalhesChatAsync(_chatId);

                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;

                if (detalhes == null)
                {
                    await DisplayAlert("Erro", "Não foi possível carregar os detalhes", "OK");
                    await Navigation.PopAsync();
                    return;
                }

                ExibirDetalhes(detalhes);
            }
            catch (Exception ex)
            {
                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;
                await DisplayAlert("Erro", $"Erro ao carregar detalhes: {ex.Message}", "OK");
                await Navigation.PopAsync();
            }
        }

        private void ExibirDetalhes(DetalhesChat detalhes)
        {
            var chat = detalhes.Chat;

            // Título
            var frameTitulo = new Frame
            {
                BackgroundColor = Color.FromArgb("#2563eb"),
                Padding = 20,
                CornerRadius = 15,
                HasShadow = true
            };

            var lblTitulo = new Label
            {
                Text = chat.Titulo,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center
            };

            frameTitulo.Content = lblTitulo;
            mainLayout.Children.Add(frameTitulo);

            // Informações
            var frameInfo = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 20,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#e5e7eb")
            };

            var infoLayout = new VerticalStackLayout { Spacing = 15 };

            // Data
            infoLayout.Children.Add(new Label
            {
                Text = $"📅 Data: {chat.DataHora:dd/MM/yyyy HH:mm}",
                FontSize = 14,
                TextColor = Color.FromArgb("#6b7280")
            });

            // Status
            infoLayout.Children.Add(new Label
            {
                Text = $"📊 Status: {chat.StatusTexto}",
                FontSize = 14,
                TextColor = ObterCorStatus(chat.Status),
                FontAttributes = FontAttributes.Bold
            });

            frameInfo.Content = infoLayout;
            mainLayout.Children.Add(frameInfo);

            // Pergunta
            var framePergunta = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 20,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#e5e7eb")
            };

            var perguntaLayout = new VerticalStackLayout { Spacing = 10 };

            perguntaLayout.Children.Add(new Label
            {
                Text = "❓ Sua Pergunta",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1e293b")
            });

            perguntaLayout.Children.Add(new Label
            {
                Text = chat.Pergunta,
                FontSize = 14,
                TextColor = Color.FromArgb("#6b7280")
            });

            framePergunta.Content = perguntaLayout;
            mainLayout.Children.Add(framePergunta);

            // Resposta
            var frameResposta = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 20,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#e5e7eb")
            };

            var respostaLayout = new VerticalStackLayout { Spacing = 10 };

            respostaLayout.Children.Add(new Label
            {
                Text = "🤖 Resposta da IA",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2563eb")
            });

            respostaLayout.Children.Add(new Label
            {
                Text = chat.Resposta,
                FontSize = 14,
                TextColor = Color.FromArgb("#6b7280")
            });

            frameResposta.Content = respostaLayout;
            mainLayout.Children.Add(frameResposta);

            // Se tiver ticket
            if (chat.IdTicket.HasValue && detalhes.Ticket != null)
            {
                var frameTicket = new Frame
                {
                    BackgroundColor = Color.FromArgb("#fef3c7"),
                    Padding = 20,
                    CornerRadius = 15,
                    HasShadow = true,
                    BorderColor = Color.FromArgb("#fbbf24")
                };

                var ticketLayout = new VerticalStackLayout { Spacing = 10 };

                ticketLayout.Children.Add(new Label
                {
                    Text = "🎫 Ticket com Técnico",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#92400e")
                });

                ticketLayout.Children.Add(new Label
                {
                    Text = $"ID do Ticket: #{detalhes.Ticket.Id}",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#92400e")
                });

                if (!string.IsNullOrEmpty(detalhes.Ticket.Solucao))
                {
                    ticketLayout.Children.Add(new Label
                    {
                        Text = "💬 Conversa:",
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#92400e"),
                        Margin = new Thickness(0, 10, 0, 0)
                    });

                    ticketLayout.Children.Add(new Label
                    {
                        Text = detalhes.Ticket.Solucao,
                        FontSize = 14,
                        TextColor = Color.FromArgb("#92400e")
                    });

                    // Se ainda estiver pendente, permitir enviar mensagem
                    if (chat.Status == 3)
                    {
                        var entryMensagem = new Entry
                        {
                            Placeholder = "Digite sua mensagem para o técnico...",
                            BackgroundColor = Colors.White,
                            Margin = new Thickness(0, 10, 0, 0)
                        };

                        var btnEnviarMensagem = new Button
                        {
                            Text = "Enviar Mensagem ao Técnico",
                            BackgroundColor = Color.FromArgb("#f59e0b"),
                            TextColor = Colors.White,
                            CornerRadius = 10,
                            Margin = new Thickness(0, 5, 0, 0)
                        };

                        btnEnviarMensagem.Clicked += async (s, e) =>
                        {
                            await EnviarMensagemParaTecnico(entryMensagem.Text);
                        };

                        ticketLayout.Children.Add(entryMensagem);
                        ticketLayout.Children.Add(btnEnviarMensagem);
                    }
                }

                frameTicket.Content = ticketLayout;
                mainLayout.Children.Add(frameTicket);
            }

            // Botões de ação
            var frameAcoes = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 15,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#e5e7eb")
            };

            var acoesLayout = new VerticalStackLayout { Spacing = 10 };

            var btnExcluir = new Button
            {
                Text = "🗑️ Excluir Conversa",
                BackgroundColor = Color.FromArgb("#ef4444"),
                TextColor = Colors.White,
                CornerRadius = 10
            };

            btnExcluir.Clicked += async (s, e) =>
            {
                var confirmacao = await DisplayAlert("Confirmação", "Deseja realmente excluir esta conversa?", "Sim", "Não");
                if (confirmacao)
                {
                    await ExcluirChat();
                }
            };

            acoesLayout.Children.Add(btnExcluir);
            frameAcoes.Content = acoesLayout;
            mainLayout.Children.Add(frameAcoes);
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

        private async Task EnviarMensagemParaTecnico(string mensagem)
        {
            if (string.IsNullOrWhiteSpace(mensagem))
            {
                await DisplayAlert("Atenção", "Digite uma mensagem!", "OK");
                return;
            }

            try
            {
                var request = new MensagemUsuarioRequest
                {
                    ChatId = _chatId,
                    Mensagem = mensagem
                };

                var sucesso = await _chatService.EnviarMensagemParaTecnicoAsync(request);

                if (sucesso)
                {
                    await DisplayAlert("✅ Sucesso", "Mensagem enviada ao técnico!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Erro", "Não foi possível enviar a mensagem", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao enviar mensagem: {ex.Message}", "OK");
            }
        }

        private async Task ExcluirChat()
        {
            try
            {
                var sucesso = await _chatService.ExcluirChatAsync(_chatId);

                if (sucesso)
                {
                    await DisplayAlert("✅ Sucesso", "Conversa excluída com sucesso!", "OK");
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Erro", "Não foi possível excluir a conversa", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao excluir conversa: {ex.Message}", "OK");
            }
        }
    }
}
