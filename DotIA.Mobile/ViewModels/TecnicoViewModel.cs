using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotIA.Mobile.Models;
using DotIA.Mobile.Services;
using DotIA.Mobile.Views;
using System.Collections.ObjectModel;
using System.Timers;
using System.Text.RegularExpressions;

namespace DotIA.Mobile.ViewModels
{
    // Classe para representar uma mensagem individual
    public class MensagemChat
    {
        public string Texto { get; set; } = string.Empty;
        public bool IsUsuario { get; set; } // true = cliente, false = IA/Técnico
        public DateTime DataHora { get; set; }
        public string NomeRemetente { get; set; } = string.Empty;
    }

    public partial class TecnicoViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly UserSessionService _userSession;
        private System.Timers.Timer? _refreshTimer;

        [ObservableProperty]
        private ObservableCollection<TicketDTO> tickets = new();

        [ObservableProperty]
        private TicketDTO? ticketSelecionado;

        [ObservableProperty]
        private ObservableCollection<MensagemChat> mensagens = new();

        [ObservableProperty]
        private string solucao = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private int totalPendentes;

        [ObservableProperty]
        private string nomeUsuario = string.Empty;

        [ObservableProperty]
        private bool mostrarLista = true;

        [ObservableProperty]
        private bool mostrarChat = false;

        public TecnicoViewModel(ApiService apiService, UserSessionService userSession)
        {
            _apiService = apiService;
            _userSession = userSession;
            NomeUsuario = _userSession.Nome ?? "Técnico";
        }

        public async Task InitializeAsync()
        {
            await CarregarTicketsAsync();
            StartAutoRefresh();
        }

        public void StopAutoRefresh()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        private void StartAutoRefresh()
        {
            StopAutoRefresh();

            _refreshTimer = new System.Timers.Timer(5000); // 5 segundos
            _refreshTimer.Elapsed += async (s, e) => await CarregarTicketsAsync();
            _refreshTimer.Start();
        }

        [RelayCommand]
        private async Task CarregarTicketsAsync()
        {
            try
            {
                var ticketsList = await _apiService.ObterTicketsPendentesAsync();

                // Atualiza apenas se houver mudanças
                if (!TicketsIguais(Tickets, ticketsList))
                {
                    Tickets = new ObservableCollection<TicketDTO>(ticketsList);
                    TotalPendentes = ticketsList.Count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar tickets: {ex.Message}");
            }
        }

        private bool TicketsIguais(ObservableCollection<TicketDTO> lista1, List<TicketDTO> lista2)
        {
            if (lista1.Count != lista2.Count) return false;

            for (int i = 0; i < lista1.Count; i++)
            {
                if (lista1[i].Id != lista2[i].Id ||
                    lista1[i].Status != lista2[i].Status ||
                    lista1[i].Solucao != lista2[i].Solucao)
                {
                    return false;
                }
            }

            return true;
        }

        // ✅ Comandos separados para evitar ArgumentException com CommandParameter
        [RelayCommand]
        private async Task EnviarRespostaAsync()
        {
            await ResponderTicketInternoAsync(false);
        }

        [RelayCommand]
        private async Task ResolverTicketCompletoAsync()
        {
            // Verifica se tem ticket selecionado
            if (TicketSelecionado == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Atenção", "Nenhum ticket selecionado.", "OK");
                return;
            }

            // Pede confirmação antes de resolver
            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Confirmar",
                $"Deseja marcar o ticket #{TicketSelecionado.Id} como resolvido?",
                "Sim, Resolver",
                "Cancelar"
            );

            if (!confirmar)
                return;

            // Se tem mensagem no campo, envia junto
            if (!string.IsNullOrWhiteSpace(Solucao))
            {
                await ResponderTicketInternoAsync(true);
            }
            else
            {
                // Marca como resolvido sem enviar mensagem
                await MarcarComoResolvidoAsync();
            }
        }

        private async Task MarcarComoResolvidoAsync()
        {
            if (TicketSelecionado == null)
                return;

            IsLoading = true;

            try
            {
                var request = new ResolverTicketRequest
                {
                    TicketId = TicketSelecionado.Id,
                    Solucao = string.Empty, // Sem mensagem
                    MarcarComoResolvido = true
                };

                var sucesso = await _apiService.ResolverTicketAsync(request);

                if (sucesso)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Sucesso", "Ticket resolvido com sucesso!", "OK");

                    // Fecha o chat e volta para lista
                    TicketSelecionado = null;
                    Mensagens.Clear();
                    Solucao = string.Empty;
                    MostrarLista = true;
                    MostrarChat = false;

                    await CarregarTicketsAsync();
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Erro", "Erro ao resolver ticket.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ResponderTicketInternoAsync(bool marcarResolvido)
        {
            if (TicketSelecionado == null || string.IsNullOrWhiteSpace(Solucao))
            {
                await Application.Current!.MainPage!.DisplayAlert("Atenção", "Digite uma resposta para o ticket.", "OK");
                return;
            }

            IsLoading = true;
            var solucaoTexto = Solucao; // Salva antes de limpar

            try
            {
                var request = new ResolverTicketRequest
                {
                    TicketId = TicketSelecionado.Id,
                    Solucao = solucaoTexto,
                    MarcarComoResolvido = marcarResolvido
                };

                var sucesso = await _apiService.ResolverTicketAsync(request);

                if (sucesso)
                {
                    // Adiciona a mensagem do técnico na lista de mensagens
                    Mensagens.Add(new MensagemChat
                    {
                        Texto = solucaoTexto,
                        IsUsuario = false,
                        DataHora = DateTime.Now,
                        NomeRemetente = "Técnico 🔧"
                    });

                    // Limpa apenas o campo de solução
                    Solucao = string.Empty;

                    // Se marcar como resolvido, mostra alerta e fecha o chat
                    if (marcarResolvido)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Sucesso", "Ticket resolvido com sucesso!", "OK");
                        TicketSelecionado = null;
                        Mensagens.Clear();
                        MostrarLista = true;
                        MostrarChat = false;
                    }
                    // Senão, apenas mantém o chat aberto (sem alerta)

                    await CarregarTicketsAsync();
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Erro", "Erro ao responder ticket.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SelecionarTicketAsync(TicketDTO ticket)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== SELECIONANDO TICKET #{ticket?.Id} ===");
                TicketSelecionado = ticket;
                Solucao = string.Empty;

                // Parsear mensagens do histórico
                ParsearMensagens(ticket);

                // Alterna para view do chat
                MostrarLista = false;
                MostrarChat = true;

                System.Diagnostics.Debug.WriteLine($"TicketSelecionado definido: {TicketSelecionado != null}");
                System.Diagnostics.Debug.WriteLine($"Total de mensagens: {Mensagens.Count}");
                System.Diagnostics.Debug.WriteLine($"MostrarLista: {MostrarLista}, MostrarChat: {MostrarChat}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERRO ao selecionar ticket: {ex.Message}");
            }
        }

        private void ParsearMensagens(TicketDTO ticket)
        {
            Mensagens.Clear();

            // Regex para detectar mensagens com timestamp: [dd/MM/yyyy HH:mm]
            var regexTimestamp = new Regex(@"\[(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+?)(?=\n\n\[|$)", RegexOptions.Singleline);

            // Processar perguntas do cliente
            if (!string.IsNullOrWhiteSpace(ticket.PerguntaOriginal))
            {
                var matchesPerguntas = regexTimestamp.Matches(ticket.PerguntaOriginal);

                if (matchesPerguntas.Count > 0)
                {
                    // Tem timestamps - mensagens concatenadas
                    foreach (Match match in matchesPerguntas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.None, out DateTime dataHora))
                        {
                            Mensagens.Add(new MensagemChat
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = true,
                                DataHora = dataHora,
                                NomeRemetente = ticket.NomeSolicitante
                            });
                        }
                    }
                }
                else
                {
                    // Mensagem única original
                    Mensagens.Add(new MensagemChat
                    {
                        Texto = ticket.PerguntaOriginal,
                        IsUsuario = true,
                        DataHora = ticket.DataAbertura,
                        NomeRemetente = ticket.NomeSolicitante
                    });
                }
            }

            // Processar respostas da IA
            if (!string.IsNullOrWhiteSpace(ticket.RespostaIA))
            {
                var matchesRespostas = regexTimestamp.Matches(ticket.RespostaIA);

                if (matchesRespostas.Count > 0)
                {
                    // Tem timestamps - respostas concatenadas
                    foreach (Match match in matchesRespostas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.None, out DateTime dataHora))
                        {
                            Mensagens.Add(new MensagemChat
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = false,
                                DataHora = dataHora,
                                NomeRemetente = "DotIA 🤖"
                            });
                        }
                    }
                }
                else
                {
                    // Resposta única original
                    Mensagens.Add(new MensagemChat
                    {
                        Texto = ticket.RespostaIA,
                        IsUsuario = false,
                        DataHora = ticket.DataAbertura,
                        NomeRemetente = "DotIA 🤖"
                    });
                }
            }

            // Ordenar mensagens por data
            var mensagensOrdenadas = Mensagens.OrderBy(m => m.DataHora).ToList();
            Mensagens.Clear();
            foreach (var msg in mensagensOrdenadas)
            {
                Mensagens.Add(msg);
            }

            System.Diagnostics.Debug.WriteLine($"Total de mensagens parseadas: {Mensagens.Count}");
        }

        [RelayCommand]
        private void FecharChat()
        {
            System.Diagnostics.Debug.WriteLine("=== FECHANDO CHAT ===");
            TicketSelecionado = null;
            Solucao = string.Empty;
            Mensagens.Clear();

            // Volta para view da lista
            MostrarLista = true;
            MostrarChat = false;
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== INICIANDO LOGOUT ===");
                StopAutoRefresh();
                _userSession.ClearSession();

                // Usa Application.Current ao invés de Shell.Current
                if (Application.Current?.MainPage != null)
                {
                    var loginPage = App.Current?.Handler?.MauiContext?.Services.GetService<LoginPage>();
                    if (loginPage != null)
                    {
                        Application.Current.MainPage = loginPage;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ERRO: LoginPage não encontrada");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERRO no logout: {ex.Message}");
            }
        }
    }
}
