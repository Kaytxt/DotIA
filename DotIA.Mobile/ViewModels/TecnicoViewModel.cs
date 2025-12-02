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
    public class MensagemChat
    {
        public string Texto { get; set; } = string.Empty;
        public bool IsUsuario { get; set; }
        public DateTime DataHora { get; set; }
        public string NomeRemetente { get; set; } = string.Empty;
    }

    public partial class TecnicoViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly UserSessionService _userSession;
        private System.Timers.Timer? _refreshTimer;

        private int? _ticketAtualId = null;

        // HashSet pra evitar duplicação de mensagens (tipo um filtro)
        private readonly HashSet<string> _mensagensProcessadas = new HashSet<string>();

        // janela de tempo de 2 minutos pra considerar mensagens duplicadas
        private bool MensagemJaExiste(string texto, string remetente, DateTime dataHora)
        {
            return Mensagens.Any(m =>
                m.Texto == texto &&
                m.NomeRemetente == remetente &&
                Math.Abs((m.DataHora - dataHora).TotalMinutes) < 2);
        }

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

            _refreshTimer = new System.Timers.Timer(10000); // atualiza a cada 10 segundos
            _refreshTimer.Elapsed += async (s, e) => await CarregarTicketsAsync();
            _refreshTimer.Start();
        }

        [RelayCommand]
        private async Task CarregarTicketsAsync()
        {
            try
            {
                var ticketsList = await _apiService.ObterTicketsPendentesAsync();

                if (!TicketsIguais(Tickets, ticketsList))
                {
                    Tickets = new ObservableCollection<TicketDTO>(ticketsList);
                    TotalPendentes = ticketsList.Count;
                }

                // se tem um ticket aberto, verifica se chegou mensagem nova
                if (_ticketAtualId.HasValue && TicketSelecionado != null)
                {
                    var ticketAtualizado = ticketsList.FirstOrDefault(t => t.Id == _ticketAtualId.Value);

                    if (ticketAtualizado != null)
                    {
                        bool solucaoMudou = ticketAtualizado.Solucao != TicketSelecionado.Solucao;

                        if (solucaoMudou)
                        {
                            TicketSelecionado = ticketAtualizado;
                            ParsearMensagensNovas(ticketAtualizado);
                        }
                    }
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

        [RelayCommand]
        private async Task EnviarRespostaAsync()
        {
            await ResponderTicketInternoAsync(false);
        }

        [RelayCommand]
        private async Task ResolverTicketCompletoAsync()
        {
            if (TicketSelecionado == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Atenção", "Nenhum ticket selecionado.", "OK");
                return;
            }

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Confirmar",
                $"Deseja marcar o ticket #{TicketSelecionado.Id} como resolvido?",
                "Sim, Resolver",
                "Cancelar"
            );

            if (!confirmar)
                return;

            if (!string.IsNullOrWhiteSpace(Solucao))
            {
                await ResponderTicketInternoAsync(true);
            }
            else
            {
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

                    TicketSelecionado = null;
                    _ticketAtualId = null;
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
                    // usa timestamp da última mensagem + 1 segundo pra manter ordem cronológica
                    var ultimaMensagem = Mensagens.LastOrDefault();
                    var dataHoraEnvio = ultimaMensagem != null && ultimaMensagem.DataHora >= DateTime.Now
                        ? ultimaMensagem.DataHora.AddSeconds(1)
                        : DateTime.Now;

                    var mensagemTecnico = new MensagemChat
                    {
                        Texto = solucaoTexto.Trim(),
                        IsUsuario = false,
                        DataHora = dataHoraEnvio,
                        NomeRemetente = "Técnico"
                    };
                    Mensagens.Add(mensagemTecnico);

                    // adiciona no HashSet pra não duplicar no polling
                    var chaveTecnico = $"Técnico:{dataHoraEnvio:dd/MM/yyyy HH:mm}:{solucaoTexto.Trim()}";
                    _mensagensProcessadas.Add(chaveTecnico);

                    Solucao = string.Empty;

                    if (marcarResolvido)
                    {
                        await Application.Current!.MainPage!.DisplayAlert("Sucesso", "Ticket resolvido com sucesso!", "OK");
                        TicketSelecionado = null;
                        _ticketAtualId = null;
                        Mensagens.Clear();
                        MostrarLista = true;
                        MostrarChat = false;
                    }
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

                _ticketAtualId = ticket?.Id;

                ParsearMensagens(ticket);

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
            _mensagensProcessadas.Clear();

            // regex pra detectar mensagens com timestamp tipo [dd/MM/yyyy HH:mm]
            var regexTimestamp = new Regex(@"\[(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+?)(?=\n\n\[|$)", RegexOptions.Singleline);
            if (!string.IsNullOrWhiteSpace(ticket.PerguntaOriginal))
            {
                var matchesPerguntas = regexTimestamp.Matches(ticket.PerguntaOriginal);

                if (matchesPerguntas.Count > 0)
                {
                    foreach (Match match in matchesPerguntas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                        {
                            Mensagens.Add(new MensagemChat
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = true,
                                DataHora = dataHora.ToLocalTime(),
                                NomeRemetente = ticket.NomeSolicitante
                            });
                        }
                    }
                }
                else
                {
                    Mensagens.Add(new MensagemChat
                    {
                        Texto = ticket.PerguntaOriginal,
                        IsUsuario = true,
                        DataHora = ticket.DataAbertura,
                        NomeRemetente = ticket.NomeSolicitante
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(ticket.RespostaIA))
            {
                var matchesRespostas = regexTimestamp.Matches(ticket.RespostaIA);

                if (matchesRespostas.Count > 0)
                {
                    foreach (Match match in matchesRespostas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                        {
                            Mensagens.Add(new MensagemChat
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = false,
                                DataHora = dataHora.ToLocalTime(),
                                NomeRemetente = "DotIA"
                            });
                        }
                    }
                }
                else
                {
                    Mensagens.Add(new MensagemChat
                    {
                        Texto = ticket.RespostaIA,
                        IsUsuario = false,
                        DataHora = ticket.DataAbertura,
                        NomeRemetente = "DotIA"
                    });
                }
            }

            // processar mensagens do chat entre técnico e usuário (vem no campo Solucao)
            if (!string.IsNullOrWhiteSpace(ticket.Solucao))
            {
                var mensagens = ticket.Solucao.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var mensagem in mensagens)
                {
                    if (!string.IsNullOrWhiteSpace(mensagem))
                    {
                        var m = mensagem.Trim();

                        var usuarioRegex = new Regex(@"^\[USUÁRIO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", RegexOptions.Singleline);
                        var tecnicoRegex = new Regex(@"^\[TÉCNICO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", RegexOptions.Singleline);

                        var matchUsuario = usuarioRegex.Match(m);
                        var matchTecnico = tecnicoRegex.Match(m);

                        if (matchUsuario.Success && DateTime.TryParseExact(matchUsuario.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHoraUsuario))
                        {
                            Mensagens.Add(new MensagemChat
                            {
                                Texto = matchUsuario.Groups[2].Value.Trim(),
                                IsUsuario = true,
                                DataHora = dataHoraUsuario.ToLocalTime(),
                                NomeRemetente = ticket.NomeSolicitante
                            });
                        }
                        else if (matchTecnico.Success && DateTime.TryParseExact(matchTecnico.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHoraTecnico))
                        {
                            Mensagens.Add(new MensagemChat
                            {
                                Texto = matchTecnico.Groups[2].Value.Trim(),
                                IsUsuario = false,
                                DataHora = dataHoraTecnico.ToLocalTime(),
                                NomeRemetente = "Técnico"
                            });
                        }
                    }
                }
            }

            var mensagensOrdenadas = Mensagens.OrderBy(m => m.DataHora).ToList();
            Mensagens.Clear();
            foreach (var msg in mensagensOrdenadas)
            {
                Mensagens.Add(msg);
                var chave = $"{msg.NomeRemetente}:{msg.DataHora:dd/MM/yyyy HH:mm}:{msg.Texto}";
                _mensagensProcessadas.Add(chave);
            }

            System.Diagnostics.Debug.WriteLine($"Total de mensagens parseadas: {Mensagens.Count}");
        }

        // adiciona só as mensagens novas sem dar Clear (evita reload visual)
        private void ParsearMensagensNovas(TicketDTO ticket)
        {
            var novasMensagens = new List<MensagemChat>();
            var regexTimestamp = new Regex(@"\[(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+?)(?=\n\n\[|$)", RegexOptions.Singleline);
            if (!string.IsNullOrWhiteSpace(ticket.PerguntaOriginal))
            {
                var matchesPerguntas = regexTimestamp.Matches(ticket.PerguntaOriginal);
                foreach (Match match in matchesPerguntas)
                {
                    if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                        null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                    {
                        var dataHoraLocal = dataHora.ToLocalTime();
                        var texto = match.Groups[2].Value.Trim();
                        var chave = $"{ticket.NomeSolicitante}:{dataHoraLocal:dd/MM/yyyy HH:mm}:{texto}";
                        if (!_mensagensProcessadas.Contains(chave))
                        {
                            novasMensagens.Add(new MensagemChat
                            {
                                Texto = texto,
                                IsUsuario = true,
                                DataHora = dataHoraLocal,
                                NomeRemetente = ticket.NomeSolicitante
                            });
                            _mensagensProcessadas.Add(chave);
                        }
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(ticket.Solucao))
            {
                var mensagens = ticket.Solucao.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var mensagem in mensagens)
                {
                    if (!string.IsNullOrWhiteSpace(mensagem))
                    {
                        var m = mensagem.Trim();

                        var usuarioRegex = new Regex(@"^\[USUÁRIO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", RegexOptions.Singleline);
                        var tecnicoRegex = new Regex(@"^\[TÉCNICO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", RegexOptions.Singleline);

                        var matchUsuario = usuarioRegex.Match(m);
                        var matchTecnico = tecnicoRegex.Match(m);

                        if (matchUsuario.Success)
                        {
                            if (DateTime.TryParseExact(matchUsuario.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHoraUsuario))
                            {
                                var dataHoraLocal = dataHoraUsuario.ToLocalTime();
                                var texto = matchUsuario.Groups[2].Value.Trim();
                                var chave = $"{ticket.NomeSolicitante}:{dataHoraLocal:dd/MM/yyyy HH:mm}:{texto}";

                                var hashSetContains = _mensagensProcessadas.Contains(chave);
                                var jaExiste = MensagemJaExiste(texto, ticket.NomeSolicitante, dataHoraLocal);

                                if (!hashSetContains && !jaExiste)
                                {
                                    novasMensagens.Add(new MensagemChat
                                    {
                                        Texto = texto,
                                        IsUsuario = true,
                                        DataHora = dataHoraLocal,
                                        NomeRemetente = ticket.NomeSolicitante
                                    });
                                    _mensagensProcessadas.Add(chave);
                                }
                            }
                        }
                        else if (matchTecnico.Success)
                        {
                            if (DateTime.TryParseExact(matchTecnico.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHoraTecnico))
                            {
                                var dataHoraLocal = dataHoraTecnico.ToLocalTime();
                                var texto = matchTecnico.Groups[2].Value.Trim();
                                var chave = $"Técnico:{dataHoraLocal:dd/MM/yyyy HH:mm}:{texto}";

                                var hashSetContains = _mensagensProcessadas.Contains(chave);
                                var jaExiste = MensagemJaExiste(texto, "Técnico", dataHoraLocal);

                                if (!hashSetContains && !jaExiste)
                                {
                                    novasMensagens.Add(new MensagemChat
                                    {
                                        Texto = texto,
                                        IsUsuario = false,
                                        DataHora = dataHoraLocal,
                                        NomeRemetente = "Técnico"
                                    });
                                    _mensagensProcessadas.Add(chave);
                                }
                            }
                        }
                    }
                }
            }

            // insere mensagens novas na posição correta mantendo ordem cronológica
            if (novasMensagens.Count > 0)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var novaMensagem in novasMensagens.OrderBy(m => m.DataHora))
                    {
                        int index = Mensagens.Count;
                        for (int i = Mensagens.Count - 1; i >= 0; i--)
                        {
                            if (Mensagens[i].DataHora <= novaMensagem.DataHora)
                            {
                                index = i + 1;
                                break;
                            }
                            if (i == 0)
                            {
                                index = 0;
                            }
                        }
                        Mensagens.Insert(index, novaMensagem);
                    }
                });
            }
        }

        [RelayCommand]
        private void FecharChat()
        {
            TicketSelecionado = null;
            _ticketAtualId = null;
            _mensagensProcessadas.Clear();
            Solucao = string.Empty;
            Mensagens.Clear();

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
