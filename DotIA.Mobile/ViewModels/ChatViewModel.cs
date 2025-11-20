using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotIA.Mobile.Models;
using DotIA.Mobile.Services;
using DotIA.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Timers;

namespace DotIA.Mobile.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly UserSessionService _userSession;
        private System.Timers.Timer? _refreshTimer;

        // Track current active chat (like Web's chatAtualId and chatAtualStatus)
        private int? _chatAtualId = null;
        private int? _chatAtualStatus = null;

        // ✅ HashSet para rastrear mensagens já exibidas (evita reload visual)
        private readonly HashSet<string> _mensagensProcessadas = new HashSet<string>();

        // ✅ Método auxiliar para verificar se mensagem já existe (com janela de tempo de 2 minutos)
        private bool MensagemJaExiste(string texto, string remetente, DateTime dataHora)
        {
            // Verifica se já existe uma mensagem com mesmo texto e remetente dentro de 2 minutos
            return Mensagens.Any(m =>
                m.Texto == texto &&
                m.NomeRemetente == remetente &&
                Math.Abs((m.DataHora - dataHora).TotalMinutes) < 2);
        }

        [ObservableProperty]
        private string pergunta = string.Empty;

        [ObservableProperty]
        private string resposta = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ChatHistoricoDTO> chats = new();

        [ObservableProperty]
        private ChatHistoricoDTO? chatSelecionado;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isSendingMessage;

        [ObservableProperty]
        private string mensagemParaTecnico = string.Empty;

        [ObservableProperty]
        private string nomeUsuario = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ChatMensagem> mensagens = new();

        [ObservableProperty]
        private bool mostrarBotoesAvaliacao = false; // Botões fixos de avaliação (chat todo)

        [ObservableProperty]
        private string statusAtual = string.Empty; // Status do chat atual

        [ObservableProperty]
        private bool mostrarStatusBadge = false; // Se deve mostrar badge de status

        [ObservableProperty]
        private bool chatBloqueado = false; // Se o chat está bloqueado (resolvido/concluído)

        public ChatViewModel(ApiService apiService, UserSessionService userSession)
        {
            _apiService = apiService;
            _userSession = userSession;
            NomeUsuario = _userSession.Nome ?? "Usuário";

            // Escuta quando um ticket é criado para recarregar o histórico
            MessagingCenter.Subscribe<AbrirTicketViewModel>(this, "TicketCriado", async (sender) =>
            {
                System.Diagnostics.Debug.WriteLine("📩 Ticket criado, recarregando histórico...");
                await CarregarHistoricoAsync();
            });
        }

        public async Task InitializeAsync()
        {
            await CarregarHistoricoAsync();
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

            _refreshTimer = new System.Timers.Timer(10000); // ✅ 10 segundos (reduzido carga de polling)
            _refreshTimer.Elapsed += async (s, e) => await CarregarHistoricoAsync();
            _refreshTimer.Start();
        }

        [RelayCommand]
        private async Task CarregarHistoricoAsync()
        {
            if (_userSession.UsuarioId == null) return;

            try
            {
                var historico = await _apiService.ObterHistoricoChatAsync(_userSession.UsuarioId.Value);

                // ✅ Se há chat atualmente aberto, verificar se houve atualizações
                if (_chatAtualId.HasValue)
                {
                    var chatAtualizado = historico.FirstOrDefault(c => c.Id == _chatAtualId.Value);
                    var chatAnterior = Chats.FirstOrDefault(c => c.Id == _chatAtualId.Value);

                    if (chatAtualizado != null && chatAnterior != null)
                    {
                        // Verifica se houve mudanças no chat atual (novas mensagens do técnico, mudança de status, etc)
                        bool statusMudou = chatAtualizado.Status != chatAnterior.Status;
                        bool respostaMudou = chatAtualizado.Resposta != chatAnterior.Resposta;
                        bool solucaoMudou = chatAtualizado.Solucao != chatAnterior.Solucao;
                        bool chatMudou = statusMudou || respostaMudou || solucaoMudou;

                        if (chatMudou)
                        {
                            // Atualiza o status atual
                            _chatAtualStatus = chatAtualizado.Status;
                            MostrarBotoesAvaliacao = chatAtualizado.Status == 1;
                            StatusAtual = chatAtualizado.StatusTexto;
                            MostrarStatusBadge = true;

                            // ✅ NÃO fazer Clear() - apenas adicionar mensagens novas
                            ParsearMensagensNovas(chatAtualizado);
                        }
                    }
                }

                // Atualiza apenas se houver mudanças
                if (!ChatsIguais(Chats, historico))
                {
                    Chats = new ObservableCollection<ChatHistoricoDTO>(historico);
                }
            }
            catch (Exception ex)
            {
                // Erro silencioso no refresh automático
                System.Diagnostics.Debug.WriteLine($"Erro ao atualizar histórico: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        private bool ChatsIguais(ObservableCollection<ChatHistoricoDTO> lista1, List<ChatHistoricoDTO> lista2)
        {
            if (lista1.Count != lista2.Count) return false;

            for (int i = 0; i < lista1.Count; i++)
            {
                if (lista1[i].Id != lista2[i].Id ||
                    lista1[i].Status != lista2[i].Status ||
                    lista1[i].Resposta != lista2[i].Resposta ||
                    lista1[i].Solucao != lista2[i].Solucao) // ✅ Compara Solucao
                {
                    return false;
                }
            }

            return true;
        }

        [RelayCommand]
        private void NovoChat()
        {
            ChatSelecionado = null;
            _chatAtualId = null;
            _chatAtualStatus = null;
            _mensagensProcessadas.Clear(); // ✅ Limpa rastreamento
            MostrarBotoesAvaliacao = false; // Esconde botões em novo chat
            MostrarStatusBadge = false; // Esconde badge de status
            StatusAtual = string.Empty;
            Mensagens.Clear();
            Pergunta = string.Empty;
            Resposta = string.Empty;
        }

        [RelayCommand]
        private async Task SelecionarChatAsync(ChatHistoricoDTO? chat)
        {
            if (chat == null)
            {
                System.Diagnostics.Debug.WriteLine("SelecionarChat: chat é null!");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"SelecionarChat: {chat.Titulo} - IdTicket: {chat.IdTicket}");

            ChatSelecionado = chat;
            // Set current chat ID and status (like Web's chatAtualId)
            _chatAtualId = chat.Id;
            _chatAtualStatus = chat.Status;

            // Mostra botões apenas se chat ainda não foi avaliado (status 1 = em andamento)
            MostrarBotoesAvaliacao = chat.Status == 1;

            // Atualiza status badge
            StatusAtual = chat.StatusTexto;
            MostrarStatusBadge = true;

            // ✅ Bloquear chat se foi concluído (status 2) ou resolvido pelo técnico (status 4)
            ChatBloqueado = chat.Status == 2 || chat.Status == 4;
            System.Diagnostics.Debug.WriteLine($"Chat Status: {chat.Status} - Bloqueado: {ChatBloqueado}");

            Mensagens.Clear();
            _mensagensProcessadas.Clear(); // ✅ Limpa rastreamento ao carregar novo chat

            // Primeiro, parseia as mensagens do histórico (pergunta e resposta da IA)
            ParsearMensagensChat(chat);

            // ✅ SEGUINDO PADRÃO DO WEB: Se tem ticket, buscar soluções do técnico via endpoint separado
            if (chat.IdTicket.HasValue)
            {
                try
                {
                    var respostaTecnico = await _apiService.VerificarRespostaTecnicoAsync(chat.Id);
                    if (respostaTecnico != null && respostaTecnico.TemResposta && !string.IsNullOrWhiteSpace(respostaTecnico.Solucao))
                    {
                        // Processar mensagens do técnico (mesmo código do Web, linhas 1702-1727)
                        var mensagens = respostaTecnico.Solucao.Split(new[] { "\n\n" }, StringSplitOptions.None);

                        foreach (var mensagem in mensagens)
                        {
                            if (!string.IsNullOrWhiteSpace(mensagem))
                            {
                                var mensagemTrimmed = mensagem.Trim();

                                var usuarioRegex = new System.Text.RegularExpressions.Regex(@"^\[USUÁRIO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);
                                var tecnicoRegex = new System.Text.RegularExpressions.Regex(@"^\[TÉCNICO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);

                                var matchUsuario = usuarioRegex.Match(mensagemTrimmed);
                                var matchTecnico = tecnicoRegex.Match(mensagemTrimmed);

                                if (matchUsuario.Success)
                                {
                                    if (DateTime.TryParseExact(matchUsuario.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                        null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                                    {
                                        Mensagens.Add(new ChatMensagem
                                        {
                                            Texto = matchUsuario.Groups[2].Value.Trim(),
                                            IsUsuario = true,
                                            DataHora = dataHora.ToLocalTime(), // ✅ Converte UTC para horário local
                                            NomeRemetente = "Você"
                                        });
                                    }
                                }
                                else if (matchTecnico.Success)
                                {
                                    if (DateTime.TryParseExact(matchTecnico.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                        null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                                    {
                                        Mensagens.Add(new ChatMensagem
                                        {
                                            Texto = matchTecnico.Groups[2].Value.Trim(),
                                            IsUsuario = false,
                                            DataHora = dataHora.ToLocalTime(), // ✅ Converte UTC para horário local
                                            NomeRemetente = "Técnico"
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao buscar resposta do técnico: {ex.Message}");
                }
            }

            // ✅ ORDENAÇÃO ÚNICA NO FINAL após TODAS as mensagens estarem adicionadas
            // (Isso corrige o problema de mensagens fora de ordem ao reabrir chat)
            var mensagensOrdenadas = Mensagens.OrderBy(m => m.DataHora).ToList();
            Mensagens.Clear();
            foreach (var msg in mensagensOrdenadas)
            {
                Mensagens.Add(msg);
                // ✅ Marca mensagens como processadas para o polling (incluindo timestamp)
                var chave = $"{msg.NomeRemetente}:{msg.DataHora:dd/MM/yyyy HH:mm}:{msg.Texto}";
                _mensagensProcessadas.Add(chave);
            }
        }

        private void ParsearMensagensChat(ChatHistoricoDTO chat)
        {
            var regexTimestamp = new System.Text.RegularExpressions.Regex(
                @"\[(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+?)(?=\n\n\[|$)",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Processar perguntas do usuário
            if (!string.IsNullOrWhiteSpace(chat.Pergunta))
            {
                var matchesPerguntas = regexTimestamp.Matches(chat.Pergunta);

                if (matchesPerguntas.Count > 0)
                {
                    // Tem timestamps - mensagens concatenadas
                    foreach (System.Text.RegularExpressions.Match match in matchesPerguntas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                        {
                            Mensagens.Add(new ChatMensagem
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = true,
                                DataHora = dataHora.ToLocalTime(), // ✅ Converte UTC para horário local
                                NomeRemetente = "Você"
                            });
                        }
                    }
                }
                else
                {
                    // Mensagem única original
                    Mensagens.Add(new ChatMensagem
                    {
                        Texto = chat.Pergunta,
                        IsUsuario = true,
                        DataHora = chat.DataHora,
                        NomeRemetente = "Você"
                    });
                }
            }

            // Processar respostas da IA
            if (!string.IsNullOrWhiteSpace(chat.Resposta))
            {
                var matchesRespostas = regexTimestamp.Matches(chat.Resposta);

                if (matchesRespostas.Count > 0)
                {
                    // Tem timestamps - respostas concatenadas
                    foreach (System.Text.RegularExpressions.Match match in matchesRespostas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                        {
                            Mensagens.Add(new ChatMensagem
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = false,
                                DataHora = dataHora.ToLocalTime(), // ✅ Converte UTC para horário local
                                NomeRemetente = "DotIA"
                            });
                        }
                    }
                }
                else
                {
                    // Resposta única original
                    Mensagens.Add(new ChatMensagem
                    {
                        Texto = chat.Resposta,
                        IsUsuario = false,
                        DataHora = chat.DataHora,
                        NomeRemetente = "DotIA"
                    });
                }
            }

            // ✅ NÃO processar chat.Solucao aqui se tem ticket - será processado via API em SelecionarChatAsync
            // (Evita duplicação de mensagens do técnico)

            // ✅ Não ordenar aqui - ordenação única será feita em SelecionarChatAsync após todas mensagens
            // (Problema: ordenar aqui + ordenar lá = mensagens fora de ordem ao reabrir chat)
        }

        // ✅ Adiciona apenas mensagens novas (sem Clear - evita reload visual)
        private async void ParsearMensagensNovas(ChatHistoricoDTO chat)
        {
            var novasMensagens = new List<ChatMensagem>();
            var regexTimestamp = new System.Text.RegularExpressions.Regex(
                @"\[(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+?)(?=\n\n\[|$)",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Processar perguntas do usuário
            if (!string.IsNullOrWhiteSpace(chat.Pergunta))
            {
                var matchesPerguntas = regexTimestamp.Matches(chat.Pergunta);
                if (matchesPerguntas.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match match in matchesPerguntas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                        {
                            var dataHoraLocal = dataHora.ToLocalTime(); // ✅ Converte UTC para horário local
                            var texto = match.Groups[2].Value.Trim();
                            var chave = $"Você:{dataHoraLocal:dd/MM/yyyy HH:mm}:{texto}";

                            // ✅ Verifica HashSet E janela de tempo para evitar duplicação visual
                            var hashSetContains = _mensagensProcessadas.Contains(chave);
                            var jaExiste = MensagemJaExiste(texto, "Você", dataHoraLocal);

                            System.Diagnostics.Debug.WriteLine($"🔍 Polling - Usuário: '{texto.Substring(0, Math.Min(30, texto.Length))}...' | Chave: {chave} | HashSet: {hashSetContains} | JáExiste: {jaExiste}");

                            if (!hashSetContains && !jaExiste)
                            {
                                novasMensagens.Add(new ChatMensagem
                                {
                                    Texto = texto,
                                    IsUsuario = true,
                                    DataHora = dataHoraLocal,
                                    NomeRemetente = "Você"
                                });
                                _mensagensProcessadas.Add(chave);
                                System.Diagnostics.Debug.WriteLine($"✅ Mensagem do usuário adicionada ao polling");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⏭️ Mensagem do usuário ignorada (já existe)");
                            }
                        }
                    }
                }
            }

            // Processar respostas da IA
            if (!string.IsNullOrWhiteSpace(chat.Resposta))
            {
                var matchesRespostas = regexTimestamp.Matches(chat.Resposta);
                if (matchesRespostas.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match match in matchesRespostas)
                    {
                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHora))
                        {
                            var dataHoraLocal = dataHora.ToLocalTime(); // ✅ Converte UTC para horário local
                            var texto = match.Groups[2].Value.Trim();
                            var chave = $"DotIA:{dataHoraLocal:dd/MM/yyyy HH:mm}:{texto}";

                            // ✅ Verifica HashSet E janela de tempo para evitar duplicação visual
                            if (!_mensagensProcessadas.Contains(chave) && !MensagemJaExiste(texto, "DotIA", dataHoraLocal))
                            {
                                novasMensagens.Add(new ChatMensagem
                                {
                                    Texto = texto,
                                    IsUsuario = false,
                                    DataHora = dataHoraLocal,
                                    NomeRemetente = "DotIA"
                                });
                                _mensagensProcessadas.Add(chave);
                            }
                        }
                    }
                }
            }

            // ✅ Processar mensagens do técnico via API
            if (chat.IdTicket.HasValue)
            {
                try
                {
                    var respostaTecnico = await _apiService.VerificarRespostaTecnicoAsync(chat.Id);
                    if (respostaTecnico != null && respostaTecnico.TemResposta && !string.IsNullOrWhiteSpace(respostaTecnico.Solucao))
                    {
                        var mensagens = respostaTecnico.Solucao.Split(new[] { "\n\n" }, StringSplitOptions.None);

                        foreach (var mensagem in mensagens)
                        {
                            if (!string.IsNullOrWhiteSpace(mensagem))
                            {
                                var mensagemTrimmed = mensagem.Trim();

                                var usuarioRegex = new System.Text.RegularExpressions.Regex(@"^\[USUÁRIO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);
                                var tecnicoRegex = new System.Text.RegularExpressions.Regex(@"^\[TÉCNICO\s*-\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})\]\s*(.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);

                                var matchUsuario = usuarioRegex.Match(mensagemTrimmed);
                                var matchTecnico = tecnicoRegex.Match(mensagemTrimmed);

                                if (matchUsuario.Success)
                                {
                                    if (DateTime.TryParseExact(matchUsuario.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                        null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dataHoraUsuario))
                                    {
                                        var dataHoraLocal = dataHoraUsuario.ToLocalTime(); // ✅ Converte UTC para horário local
                                        var texto = matchUsuario.Groups[2].Value.Trim();
                                        var chave = $"Você:{dataHoraLocal:dd/MM/yyyy HH:mm}:{texto}";

                                        // ✅ Verifica se mensagem já existe usando janela de tempo
                                        if (!_mensagensProcessadas.Contains(chave) && !MensagemJaExiste(texto, "Você", dataHoraLocal))
                                        {
                                            novasMensagens.Add(new ChatMensagem
                                            {
                                                Texto = texto,
                                                IsUsuario = true,
                                                DataHora = dataHoraLocal,
                                                NomeRemetente = "Você"
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
                                        var dataHoraLocal = dataHoraTecnico.ToLocalTime(); // ✅ Converte UTC para horário local
                                        var texto = matchTecnico.Groups[2].Value.Trim();
                                        var chave = $"Técnico:{dataHoraLocal:dd/MM/yyyy HH:mm}:{texto}";

                                        // ✅ Verifica se mensagem já existe usando janela de tempo
                                        if (!_mensagensProcessadas.Contains(chave) && !MensagemJaExiste(texto, "Técnico", dataHoraLocal))
                                        {
                                            novasMensagens.Add(new ChatMensagem
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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao buscar mensagens do técnico no polling: {ex.Message}");
                }
            }

            // Adiciona novas mensagens ordenadas
            if (novasMensagens.Count > 0)
            {
                // ✅ OTIMIZAÇÃO: Adiciona novas mensagens na posição correta sem Clear()
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var novaMensagem in novasMensagens.OrderBy(m => m.DataHora))
                    {
                        // Encontra a posição correta para inserir (mantém ordem cronológica)
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
        private async Task EnviarPerguntaAsync()
        {
            if (string.IsNullOrWhiteSpace(Pergunta) || _userSession.UsuarioId == null)
                return;

            var perguntaTexto = Pergunta;
            IsSendingMessage = true;
            Pergunta = string.Empty;

            System.Diagnostics.Debug.WriteLine($"=== ENVIAR PERGUNTA ===");
            System.Diagnostics.Debug.WriteLine($"Pergunta: {perguntaTexto}");
            System.Diagnostics.Debug.WriteLine($"chatAtualId: {_chatAtualId}, chatAtualStatus: {_chatAtualStatus}");

            // ✅ SEGUINDO A LÓGICA DO WEB: Verificar status do chat atual
            if (_chatAtualId.HasValue)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 Verificando status do chat {_chatAtualId}...");
                    var statusInfo = await _apiService.VerificarRespostaTecnicoAsync(_chatAtualId.Value);
                    if (statusInfo != null)
                    {
                        _chatAtualStatus = statusInfo.Status;
                        System.Diagnostics.Debug.WriteLine($"Status verificado: {_chatAtualStatus}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao verificar status: {ex.Message}");
                }
            }

            // ✅ SEGUINDO A LÓGICA DO WEB: Se status é 3 (com técnico), enviar para técnico
            if (_chatAtualStatus == 3 && _chatAtualId.HasValue)
            {
                System.Diagnostics.Debug.WriteLine("📤 Enviando mensagem para técnico (chat pendente)");

                // Adiciona mensagem do usuário
                // ✅ Usa timestamp da última mensagem + 1 segundo para garantir ordem cronológica
                var ultimaMensagem = Mensagens.LastOrDefault();
                var dataHoraEnvio = ultimaMensagem != null && ultimaMensagem.DataHora >= DateTime.Now
                    ? ultimaMensagem.DataHora.AddSeconds(1)
                    : DateTime.Now;

                var mensagemTecnico = new ChatMensagem
                {
                    Texto = perguntaTexto.Trim(),
                    IsUsuario = true,
                    DataHora = dataHoraEnvio,
                    NomeRemetente = "Você"
                };
                Mensagens.Add(mensagemTecnico);

                // ✅ Marca mensagem como processada para evitar duplicação no polling
                var chaveTecnico = $"Você:{dataHoraEnvio:dd/MM/yyyy HH:mm}:{perguntaTexto.Trim()}";
                _mensagensProcessadas.Add(chaveTecnico);
                System.Diagnostics.Debug.WriteLine($"🔑 Chave adicionada ao HashSet (Técnico): {chaveTecnico}");

                try
                {
                    var request = new MensagemUsuarioRequest
                    {
                        ChatId = _chatAtualId.Value,
                        Mensagem = perguntaTexto
                    };

                    var sucesso = await _apiService.EnviarMensagemParaTecnicoAsync(request);

                    if (sucesso)
                    {
                        await Application.Current!.MainPage!.DisplayAlert(
                            "Enviado",
                            "Mensagem enviada ao técnico!",
                            "OK"
                        );
                        // ✅ NÃO chamar CarregarHistoricoAsync aqui - mensagem já foi adicionada localmente
                        // O polling de 10s sincronizará automaticamente
                    }
                    else
                    {
                        await Application.Current!.MainPage!.DisplayAlert(
                            "Erro",
                            "Erro ao enviar mensagem ao técnico",
                            "OK"
                        );
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao enviar para técnico: {ex.Message}");
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Erro",
                        $"Erro: {ex.Message}",
                        "OK"
                    );
                }
                finally
                {
                    IsSendingMessage = false;
                }

                return; // ✅ IMPORTANTE: Retorna aqui, não continua para criar novo chat
            }

            // ✅ SEGUINDO A LÓGICA DO WEB: Enviar para IA
            System.Diagnostics.Debug.WriteLine("🤖 Enviando mensagem para IA");

            // ✅ Usa timestamp da última mensagem + 1 segundo para garantir ordem cronológica
            var ultimaMensagemIA = Mensagens.LastOrDefault();
            var dataHoraEnvioUsuario = ultimaMensagemIA != null && ultimaMensagemIA.DataHora >= DateTime.Now
                ? ultimaMensagemIA.DataHora.AddSeconds(1)
                : DateTime.Now;

            // Adiciona a mensagem do usuário imediatamente
            var mensagemUsuario = new ChatMensagem
            {
                Texto = perguntaTexto.Trim(),
                IsUsuario = true,
                DataHora = dataHoraEnvioUsuario,
                NomeRemetente = "Você"
            };

            System.Diagnostics.Debug.WriteLine($"Criou ChatMensagem: Texto='{mensagemUsuario.Texto}', IsUsuario={mensagemUsuario.IsUsuario}");

            Mensagens.Add(mensagemUsuario);

            // ✅ Marca mensagem como processada para evitar duplicação no polling
            var chaveUsuario = $"Você:{dataHoraEnvioUsuario:dd/MM/yyyy HH:mm}:{perguntaTexto.Trim()}";
            _mensagensProcessadas.Add(chaveUsuario);
            System.Diagnostics.Debug.WriteLine($"🔑 Chave adicionada ao HashSet (IA): {chaveUsuario}");

            System.Diagnostics.Debug.WriteLine($"Total de mensagens após adicionar: {Mensagens.Count}");

            try
            {
                var request = new ChatRequest
                {
                    UsuarioId = _userSession.UsuarioId.Value,
                    Pergunta = perguntaTexto,
                    ChatId = _chatAtualId // ✅ Envia chatId para continuar no mesmo chat
                };

                var response = await _apiService.EnviarPerguntaAsync(request);

                if (response.Sucesso)
                {
                    System.Diagnostics.Debug.WriteLine($"Resposta da API: '{response.Resposta?.Substring(0, Math.Min(100, response.Resposta?.Length ?? 0))}'");

                    // ✅ Armazena chatId e status
                    _chatAtualId = response.ChatId;
                    _chatAtualStatus = 1; // Status 1 = Em andamento
                    System.Diagnostics.Debug.WriteLine($"✅ chatAtualId atualizado para: {_chatAtualId}, status: {_chatAtualStatus}");

                    // ✅ Mostra botões de avaliação fixos
                    MostrarBotoesAvaliacao = true;

                    // ✅ Adiciona a resposta da IA com timestamp +1 segundo da mensagem do usuário
                    var dataHoraRespostaIA = dataHoraEnvioUsuario.AddSeconds(1);
                    var mensagemIA = new ChatMensagem
                    {
                        Texto = response.Resposta,
                        IsUsuario = false,
                        DataHora = dataHoraRespostaIA,
                        NomeRemetente = "DotIA"
                    };

                    System.Diagnostics.Debug.WriteLine($"Criou ChatMensagem IA: Texto='{mensagemIA.Texto?.Substring(0, Math.Min(50, mensagemIA.Texto?.Length ?? 0))}', IsUsuario={mensagemIA.IsUsuario}");

                    Mensagens.Add(mensagemIA);

                    // ✅ Marca mensagem da IA como processada para evitar duplicação no polling
                    var chaveIA = $"DotIA:{dataHoraRespostaIA:dd/MM/yyyy HH:mm}:{response.Resposta?.Trim()}";
                    _mensagensProcessadas.Add(chaveIA);

                    System.Diagnostics.Debug.WriteLine($"Total de mensagens após resposta: {Mensagens.Count}");

                    Resposta = response.Resposta;

                    // ✅ NÃO chamar CarregarHistoricoAsync aqui - mensagens já foram adicionadas localmente
                    // O polling de 10s sincronizará automaticamente
                }
                else
                {
                    var dataHoraErro = Mensagens.LastOrDefault()?.DataHora.AddSeconds(1) ?? DateTime.Now;
                    Mensagens.Add(new ChatMensagem
                    {
                        Texto = "Erro: " + response.Resposta,
                        IsUsuario = false,
                        DataHora = dataHoraErro,
                        NomeRemetente = "DotIA"
                    });
                }
            }
            catch (Exception ex)
            {
                var dataHoraErro = Mensagens.LastOrDefault()?.DataHora.AddSeconds(1) ?? DateTime.Now;
                Mensagens.Add(new ChatMensagem
                {
                    Texto = $"Erro: {ex.Message}",
                    IsUsuario = false,
                    DataHora = dataHoraErro,
                    NomeRemetente = "DotIA"
                });
            }
            finally
            {
                IsSendingMessage = false;
            }
        }

        [RelayCommand]
        private async Task AvaliarRespostaUtilAsync()
        {
            await AvaliarRespostaInternaAsync(true);
        }

        [RelayCommand]
        private async Task AvaliarRespostaNaoUtilAsync()
        {
            await AvaliarRespostaInternaAsync(false);
        }

        private async Task AvaliarRespostaInternaAsync(bool foiUtil)
        {
            if (_chatAtualId == null || _userSession.UsuarioId == null)
                return;

            try
            {
                // Busca o histórico do chat para obter pergunta e resposta
                var chat = Chats.FirstOrDefault(c => c.Id == _chatAtualId.Value);
                if (chat == null) return;

                var request = new AvaliacaoRequest
                {
                    UsuarioId = _userSession.UsuarioId.Value,
                    Pergunta = chat.Pergunta,
                    Resposta = chat.Resposta,
                    FoiUtil = foiUtil,
                    ChatId = _chatAtualId.Value
                };

                await _apiService.AvaliarRespostaAsync(request);

                // Esconde botões fixos após avaliar
                MostrarBotoesAvaliacao = false;

                await CarregarHistoricoAsync();

                if (foiUtil)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Sucesso", "Feedback registrado com sucesso!", "OK");
                    _chatAtualStatus = 2; // Concluído
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Ticket Aberto", "Um ticket foi criado e um técnico irá atendê-lo em breve.", "OK");
                    _chatAtualStatus = 3; // Com técnico
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erro", $"Erro ao avaliar resposta: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EnviarMensagemTecnicoAsync()
        {
            if (ChatSelecionado == null || string.IsNullOrWhiteSpace(MensagemParaTecnico))
                return;

            try
            {
                var request = new MensagemUsuarioRequest
                {
                    ChatId = ChatSelecionado.Id,
                    Mensagem = MensagemParaTecnico
                };

                var sucesso = await _apiService.EnviarMensagemParaTecnicoAsync(request);

                if (sucesso)
                {
                    MensagemParaTecnico = string.Empty;
                    await Shell.Current.DisplayAlert("Sucesso", "Mensagem enviada ao técnico!", "OK");
                    await CarregarHistoricoAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erro", "Erro ao enviar mensagem.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task AbrirTicketDiretoAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📝 AbrirTicketDireto: Iniciando...");

                // Verifica se Application.Current e MainPage estão disponíveis
                if (Application.Current?.MainPage == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ AbrirTicketDireto: Application.Current.MainPage é null");
                    return;
                }

                // Obtém a página modal via DI
                var abrirTicketPage = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AbrirTicketPage>();

                System.Diagnostics.Debug.WriteLine("✅ AbrirTicketDireto: Página criada, abrindo modal...");

                // Abre a página modal
                await Application.Current.MainPage.Navigation.PushModalAsync(abrirTicketPage);

                System.Diagnostics.Debug.WriteLine("✅ AbrirTicketDireto: Modal aberta com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AbrirTicketDireto Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");

                if (Application.Current?.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert("Erro", $"Erro ao abrir página: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task EditarTituloChatAsync(ChatHistoricoDTO chat)
        {
            if (chat == null) return;

            var novoTitulo = await Application.Current!.MainPage!.DisplayPromptAsync(
                "Editar Título",
                "Digite o novo título:",
                initialValue: chat.Titulo,
                maxLength: 100,
                keyboard: Keyboard.Text
            );

            if (string.IsNullOrWhiteSpace(novoTitulo)) return;

            try
            {
                var sucesso = await _apiService.EditarTituloChatAsync(chat.Id, novoTitulo);

                if (sucesso)
                {
                    await CarregarHistoricoAsync();
                    await Application.Current!.MainPage!.DisplayAlert("Sucesso", "Título atualizado com sucesso!", "OK");
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Erro", "Erro ao editar título.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ExcluirChatAsync(int chatId)
        {
            var confirmacao = await Shell.Current.DisplayAlert(
                "Confirmação",
                "Deseja realmente excluir este chat?",
                "Sim",
                "Não"
            );

            if (!confirmacao) return;

            try
            {
                var sucesso = await _apiService.ExcluirChatAsync(chatId);

                if (sucesso)
                {
                    await CarregarHistoricoAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erro", "Erro ao excluir chat.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            StopAutoRefresh();
            _userSession.ClearSession();

            // Navegar para login substituindo a página principal
            var loginPage = App.Current?.Handler?.MauiContext?.Services.GetService<LoginPage>();
            if (loginPage != null)
            {
                Application.Current!.MainPage = new NavigationPage(loginPage);
            }
            else
            {
                // Fallback: tentar navegação relativa
                await Shell.Current.GoToAsync("../LoginPage");
            }
        }
    }
}
