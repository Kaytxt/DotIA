using DotIA_Mobile.Models;
using DotIA_Mobile.Services;

namespace DotIA_Mobile.Views
{
    public partial class ChatPage : ContentPage
    {
        private readonly IChatService _chatService;
        private int _chatAtualId = 0;

        public ChatPage()
        {
            InitializeComponent();
            _chatService = new ChatService();
            
            // Exibir nome do usuário
            if (!string.IsNullOrEmpty(UserSession.Nome))
            {
                lblBemVindo.Text = $"Olá, {UserSession.Nome}!";
            }
        }

        private async void OnEnviarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPergunta.Text))
            {
                await DisplayAlert("Atenção", "Digite uma pergunta!", "OK");
                return;
            }

            var pergunta = txtPergunta.Text.Trim();
            txtPergunta.Text = string.Empty;
            btnEnviar.IsEnabled = false;

            // Adicionar mensagem do usuário
            AdicionarMensagemUsuario(pergunta);

            // Adicionar indicador de loading
            var loadingFrame = AdicionarMensagemLoading();

            try
            {
                var request = new ChatRequest
                {
                    UsuarioId = UserSession.UsuarioId ?? 0,
                    Pergunta = pergunta
                };

                var response = await _chatService.EnviarPerguntaAsync(request);

                // Remover loading
                conversaLayout.Children.Remove(loadingFrame);

                if (response.Sucesso)
                {
                    _chatAtualId = response.ChatId;
                    
                    // Adicionar resposta da IA
                    AdicionarMensagemIA(response.Resposta, response.ChatId);

                    // Mostrar botões de avaliação
                    AdicionarBotoesAvaliacao(pergunta, response.Resposta, response.ChatId);
                }
                else
                {
                    AdicionarMensagemErro(response.Resposta);
                }
            }
            catch (Exception ex)
            {
                conversaLayout.Children.Remove(loadingFrame);
                AdicionarMensagemErro($"Erro ao enviar mensagem: {ex.Message}");
            }
            finally
            {
                btnEnviar.IsEnabled = true;
                await scrollView.ScrollToAsync(0, scrollView.ContentSize.Height, true);
            }
        }

        private void AdicionarMensagemUsuario(string mensagem)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#2563eb"),
                Padding = 15,
                CornerRadius = 15,
                HasShadow = true,
                HorizontalOptions = LayoutOptions.End,
                MaximumWidthRequest = 300
            };

            var label = new Label
            {
                Text = mensagem,
                TextColor = Colors.White,
                FontSize = 14
            };

            frame.Content = label;
            conversaLayout.Children.Add(frame);
        }

        private void AdicionarMensagemIA(string mensagem, int chatId)
        {
            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 15,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#e5e7eb"),
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 300
            };

            var layout = new VerticalStackLayout { Spacing = 10 };

            var labelTitulo = new Label
            {
                Text = "🤖 DotIA",
                TextColor = Color.FromArgb("#2563eb"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            };

            var labelMensagem = new Label
            {
                Text = mensagem,
                TextColor = Color.FromArgb("#1e293b"),
                FontSize = 14
            };

            layout.Children.Add(labelTitulo);
            layout.Children.Add(labelMensagem);
            frame.Content = layout;
            conversaLayout.Children.Add(frame);
        }

        private Frame AdicionarMensagemLoading()
        {
            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                Padding = 15,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#e5e7eb"),
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 150
            };

            var layout = new HorizontalStackLayout { Spacing = 10 };

            var activityIndicator = new ActivityIndicator
            {
                IsRunning = true,
                Color = Color.FromArgb("#2563eb")
            };

            var label = new Label
            {
                Text = "Pensando...",
                TextColor = Color.FromArgb("#6b7280"),
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            };

            layout.Children.Add(activityIndicator);
            layout.Children.Add(label);
            frame.Content = layout;
            conversaLayout.Children.Add(frame);

            return frame;
        }

        private void AdicionarMensagemErro(string erro)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#fef2f2"),
                Padding = 15,
                CornerRadius = 15,
                HasShadow = true,
                BorderColor = Color.FromArgb("#fecaca"),
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 300
            };

            var label = new Label
            {
                Text = $"❌ {erro}",
                TextColor = Color.FromArgb("#dc2626"),
                FontSize = 14
            };

            frame.Content = label;
            conversaLayout.Children.Add(frame);
        }

        private void AdicionarBotoesAvaliacao(string pergunta, string resposta, int chatId)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#f8fafc"),
                Padding = 15,
                CornerRadius = 15,
                HasShadow = false,
                BorderColor = Color.FromArgb("#e5e7eb"),
                HorizontalOptions = LayoutOptions.Center
            };

            var layout = new VerticalStackLayout { Spacing = 10 };

            var labelPergunta = new Label
            {
                Text = "Esta resposta foi útil?",
                TextColor = Color.FromArgb("#6b7280"),
                FontSize = 13,
                HorizontalOptions = LayoutOptions.Center
            };

            var buttonsLayout = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center
            };

            var btnUtil = new Button
            {
                Text = "👍 Sim",
                BackgroundColor = Color.FromArgb("#10b981"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(20, 10)
            };

            var btnNaoUtil = new Button
            {
                Text = "👎 Não",
                BackgroundColor = Color.FromArgb("#ef4444"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = new Thickness(20, 10)
            };

            btnUtil.Clicked += async (s, e) =>
            {
                await AvaliarResposta(pergunta, resposta, chatId, true);
                conversaLayout.Children.Remove(frame);
            };

            btnNaoUtil.Clicked += async (s, e) =>
            {
                await AvaliarResposta(pergunta, resposta, chatId, false);
                conversaLayout.Children.Remove(frame);
            };

            buttonsLayout.Children.Add(btnUtil);
            buttonsLayout.Children.Add(btnNaoUtil);

            layout.Children.Add(labelPergunta);
            layout.Children.Add(buttonsLayout);

            frame.Content = layout;
            conversaLayout.Children.Add(frame);
        }

        private async Task AvaliarResposta(string pergunta, string resposta, int chatId, bool foiUtil)
        {
            try
            {
                var request = new AvaliacaoRequest
                {
                    UsuarioId = UserSession.UsuarioId ?? 0,
                    Pergunta = pergunta,
                    Resposta = resposta,
                    FoiUtil = foiUtil,
                    ChatId = chatId
                };

                var sucesso = await _chatService.AvaliarRespostaAsync(request);

                if (sucesso)
                {
                    if (foiUtil)
                    {
                        await DisplayAlert("✅ Obrigado!", "Ficamos felizes em ajudar!", "OK");
                    }
                    else
                    {
                        await DisplayAlert("📋 Ticket Criado", "Um técnico irá analisar seu problema e responder em breve.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", $"Erro ao avaliar resposta: {ex.Message}", "OK");
            }
        }

        private async void OnVerHistoricoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HistoricoPage());
        }
    }
}
