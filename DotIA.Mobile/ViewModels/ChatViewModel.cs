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

            _refreshTimer = new System.Timers.Timer(5000); // 5 segundos (igual ao web)
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

                System.Diagnostics.Debug.WriteLine($"CarregarHistorico: {historico.Count} chats recebidos");

                // Debug: Mostrar títulos
                foreach (var chat in historico)
                {
                    System.Diagnostics.Debug.WriteLine($"  Chat {chat.Id}: Titulo='{chat.Titulo}' Pergunta='{chat.Pergunta?.Substring(0, Math.Min(50, chat.Pergunta?.Length ?? 0))}'");
                }

                // ✅ Se há chat atualmente aberto, verificar se houve atualizações
                if (_chatAtualId.HasValue)
                {
                    var chatAtualizado = historico.FirstOrDefault(c => c.Id == _chatAtualId.Value);
                    var chatAnterior = Chats.FirstOrDefault(c => c.Id == _chatAtualId.Value);

                    if (chatAtualizado != null && chatAnterior != null)
                    {
                        // Verifica se houve mudanças no chat atual (novas mensagens do técnico, mudança de status, etc)
                        bool chatMudou = chatAtualizado.Status != chatAnterior.Status ||
                                        chatAtualizado.Resposta != chatAnterior.Resposta ||
                                        chatAtualizado.Solucao != chatAnterior.Solucao;

                        if (chatMudou)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Chat {_chatAtualId} teve atualizações! Recarregando mensagens...");
                            // Atualiza o status atual
                            _chatAtualStatus = chatAtualizado.Status;
                            MostrarBotoesAvaliacao = chatAtualizado.Status == 1;
                            StatusAtual = chatAtualizado.StatusTexto;
                            MostrarStatusBadge = true;

                            // ✅ IMPORTANTE: Limpar mensagens antes de recarregar (seguindo padrão do Web)
                            Mensagens.Clear();

                            // Recarrega as mensagens do chat atual
                            ParsearMensagensChat(chatAtualizado);

                            // ✅ Se tem ticket, buscar mensagens do técnico via API (igual ao Web)
                            if (chatAtualizado.IdTicket.HasValue)
                            {
                                try
                                {
                                    var respostaTecnico = await _apiService.VerificarRespostaTecnicoAsync(chatAtualizado.Id);
                                    if (respostaTecnico != null && respostaTecnico.TemResposta && !string.IsNullOrWhiteSpace(respostaTecnico.Solucao))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"✅ Auto-refresh: Mensagens do técnico recebidas!");

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

                                                if (matchUsuario.Success && DateTime.TryParseExact(matchUsuario.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                                    null, System.Globalization.DateTimeStyles.None, out DateTime dataHoraUsuario))
                                                {
                                                    Mensagens.Add(new ChatMensagem
                                                    {
                                                        Texto = matchUsuario.Groups[2].Value.Trim(),
                                                        IsUsuario = true,
                                                        DataHora = dataHoraUsuario,
                                                        NomeRemetente = "Você"
                                                    });
                                                }
                                                else if (matchTecnico.Success && DateTime.TryParseExact(matchTecnico.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                                    null, System.Globalization.DateTimeStyles.None, out DateTime dataHoraTecnico))
                                                {
                                                    Mensagens.Add(new ChatMensagem
                                                    {
                                                        Texto = matchTecnico.Groups[2].Value.Trim(),
                                                        IsUsuario = false,
                                                        DataHora = dataHoraTecnico,
                                                        NomeRemetente = "Técnico 🔧"
                                                    });
                                                    System.Diagnostics.Debug.WriteLine($"  🔧 Auto-refresh adicionou mensagem do técnico!");
                                                }
                                            }
                                        }

                                        // Reordenar por data
                                        var mensagensOrdenadas = Mensagens.OrderBy(m => m.DataHora).ToList();
                                        Mensagens.Clear();
                                        foreach (var msg in mensagensOrdenadas)
                                        {
                                            Mensagens.Add(msg);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Erro ao buscar mensagens do técnico no auto-refresh: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                // Atualiza apenas se houver mudanças
                if (!ChatsIguais(Chats, historico))
                {
                    Chats = new ObservableCollection<ChatHistoricoDTO>(historico);
                    System.Diagnostics.Debug.WriteLine($"CarregarHistorico: Chats atualizados na UI");
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

            // Primeiro, parseia as mensagens do histórico (pergunta e resposta da IA)
            ParsearMensagensChat(chat);

            // ✅ SEGUINDO PADRÃO DO WEB: Se tem ticket, buscar soluções do técnico via endpoint separado
            if (chat.IdTicket.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"📞 Chat tem ticket {chat.IdTicket.Value}, buscando respostas do técnico...");
                try
                {
                    var respostaTecnico = await _apiService.VerificarRespostaTecnicoAsync(chat.Id);
                    if (respostaTecnico != null && respostaTecnico.TemResposta && !string.IsNullOrWhiteSpace(respostaTecnico.Solucao))
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Resposta do técnico recebida via API!");
                        System.Diagnostics.Debug.WriteLine($"Solucao: {respostaTecnico.Solucao}");

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
                                        null, System.Globalization.DateTimeStyles.None, out DateTime dataHora))
                                    {
                                        Mensagens.Add(new ChatMensagem
                                        {
                                            Texto = matchUsuario.Groups[2].Value.Trim(),
                                            IsUsuario = true,
                                            DataHora = dataHora,
                                            NomeRemetente = "Você"
                                        });
                                        System.Diagnostics.Debug.WriteLine($"  ➕ Mensagem do usuário adicionada");
                                    }
                                }
                                else if (matchTecnico.Success)
                                {
                                    if (DateTime.TryParseExact(matchTecnico.Groups[1].Value, "dd/MM/yyyy HH:mm",
                                        null, System.Globalization.DateTimeStyles.None, out DateTime dataHora))
                                    {
                                        Mensagens.Add(new ChatMensagem
                                        {
                                            Texto = matchTecnico.Groups[2].Value.Trim(),
                                            IsUsuario = false,
                                            DataHora = dataHora,
                                            NomeRemetente = "Técnico 🔧"
                                        });
                                        System.Diagnostics.Debug.WriteLine($"  ➕ Mensagem do TÉCNICO adicionada: {matchTecnico.Groups[2].Value.Substring(0, Math.Min(50, matchTecnico.Groups[2].Value.Length))}...");
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ℹ️ Nenhuma resposta do técnico ainda");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erro ao buscar resposta do técnico: {ex.Message}");
                }
            }

            // ✅ ORDENAÇÃO ÚNICA NO FINAL após TODAS as mensagens estarem adicionadas
            // (Isso corrige o problema de mensagens fora de ordem ao reabrir chat)
            var mensagensOrdenadas = Mensagens.OrderBy(m => m.DataHora).ToList();
            Mensagens.Clear();
            foreach (var msg in mensagensOrdenadas)
            {
                Mensagens.Add(msg);
                System.Diagnostics.Debug.WriteLine($"  📩 {msg.NomeRemetente} ({msg.DataHora:dd/MM HH:mm}): {msg.Texto.Substring(0, Math.Min(30, msg.Texto.Length))}...");
            }

            System.Diagnostics.Debug.WriteLine($"SelecionarChat: {Mensagens.Count} mensagens carregadas e ordenadas");
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
                            null, System.Globalization.DateTimeStyles.None, out DateTime dataHora))
                        {
                            Mensagens.Add(new ChatMensagem
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = true,
                                DataHora = dataHora,
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
                            null, System.Globalization.DateTimeStyles.None, out DateTime dataHora))
                        {
                            Mensagens.Add(new ChatMensagem
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
                    Mensagens.Add(new ChatMensagem
                    {
                        Texto = chat.Resposta,
                        IsUsuario = false,
                        DataHora = chat.DataHora,
                        NomeRemetente = "DotIA 🤖"
                    });
                }
            }

            // Processar soluções do técnico (formato: [TÉCNICO - dd/MM/yyyy HH:mm] mensagem)
            if (!string.IsNullOrWhiteSpace(chat.Solucao))
            {
                System.Diagnostics.Debug.WriteLine($"📧 ===== SOLUCAO DO TÉCNICO RECEBIDA =====");
                System.Diagnostics.Debug.WriteLine($"Chat ID: {chat.Id}");
                System.Diagnostics.Debug.WriteLine($"Conteúdo completo da Solucao:");
                System.Diagnostics.Debug.WriteLine(chat.Solucao);
                System.Diagnostics.Debug.WriteLine($"==========================================");

                var regexTecnico = new System.Text.RegularExpressions.Regex(
                    @"\[TÉCNICO\s*-\s*(\d{2}/\d{2}/\d{4}\s\d{2}:\d{2})\]\s*(.+?)(?=\n\n\[|$)",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                var matchesTecnico = regexTecnico.Matches(chat.Solucao);

                System.Diagnostics.Debug.WriteLine($"Regex matches encontrados: {matchesTecnico.Count}");

                if (matchesTecnico.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match match in matchesTecnico)
                    {
                        System.Diagnostics.Debug.WriteLine($"Match {match.Index}: Data='{match.Groups[1].Value}', Texto='{match.Groups[2].Value.Substring(0, Math.Min(50, match.Groups[2].Value.Length))}...'");

                        if (DateTime.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy HH:mm",
                            null, System.Globalization.DateTimeStyles.None, out DateTime dataHora))
                        {
                            var mensagemTecnico = new ChatMensagem
                            {
                                Texto = match.Groups[2].Value.Trim(),
                                IsUsuario = false,
                                DataHora = dataHora,
                                NomeRemetente = "Técnico 🔧"
                            };

                            Mensagens.Add(mensagemTecnico);
                            System.Diagnostics.Debug.WriteLine($"✅ Mensagem do técnico adicionada: '{mensagemTecnico.Texto.Substring(0, Math.Min(50, mensagemTecnico.Texto.Length))}...'");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Falha ao parsear data: '{match.Groups[1].Value}'");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Nenhuma mensagem do técnico encontrada no regex. Testando formato alternativo...");
                    // Debug: Mostrar as primeiras linhas da Solucao para análise
                    var linhas = chat.Solucao.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < Math.Min(5, linhas.Length); i++)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Linha {i}: '{linhas[i]}'");
                    }
                }
            }

            // ✅ Não ordenar aqui - ordenação única será feita em SelecionarChatAsync após todas mensagens
            // (Problema: ordenar aqui + ordenar lá = mensagens fora de ordem ao reabrir chat)
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
                Mensagens.Add(new ChatMensagem
                {
                    Texto = perguntaTexto,
                    IsUsuario = true,
                    DataHora = DateTime.Now
                });

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
                        await CarregarHistoricoAsync();
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

            // Adiciona a mensagem do usuário imediatamente
            var mensagemUsuario = new ChatMensagem
            {
                Texto = perguntaTexto,
                IsUsuario = true,
                DataHora = DateTime.Now
            };

            System.Diagnostics.Debug.WriteLine($"Criou ChatMensagem: Texto='{mensagemUsuario.Texto}', IsUsuario={mensagemUsuario.IsUsuario}");

            Mensagens.Add(mensagemUsuario);

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

                    // Adiciona a resposta da IA
                    var mensagemIA = new ChatMensagem
                    {
                        Texto = response.Resposta,
                        IsUsuario = false,
                        DataHora = DateTime.Now
                    };

                    System.Diagnostics.Debug.WriteLine($"Criou ChatMensagem IA: Texto='{mensagemIA.Texto?.Substring(0, Math.Min(50, mensagemIA.Texto?.Length ?? 0))}', IsUsuario={mensagemIA.IsUsuario}");

                    Mensagens.Add(mensagemIA);

                    System.Diagnostics.Debug.WriteLine($"Total de mensagens após resposta: {Mensagens.Count}");

                    Resposta = response.Resposta;

                    // Atualiza histórico
                    await CarregarHistoricoAsync();
                }
                else
                {
                    Mensagens.Add(new ChatMensagem
                    {
                        Texto = "Erro: " + response.Resposta,
                        IsUsuario = false,
                        DataHora = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                Mensagens.Add(new ChatMensagem
                {
                    Texto = $"Erro: {ex.Message}",
                    IsUsuario = false,
                    DataHora = DateTime.Now
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
