using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotIA.Mobile.Models;
using DotIA.Mobile.Services;
using System.Collections.ObjectModel;

namespace DotIA.Mobile.ViewModels
{
    public partial class GerenteViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly UserSessionService _userSession;

        // Dashboard Stats
        [ObservableProperty] private int totalUsuarios;
        [ObservableProperty] private int ticketsAbertos;
        [ObservableProperty] private int ticketsResolvidos;
        [ObservableProperty] private int totalChats;
        [ObservableProperty] private int resolvidosHoje;
        [ObservableProperty] private int chatsConcluidos;

        // Tabs
        [ObservableProperty] private int abaAtiva = 0; // 0=Tickets, 1=Usuários, 2=Relatórios

        // Tickets
        [ObservableProperty] private ObservableCollection<TicketGerenteDTO> tickets = new();

        // Usuários
        [ObservableProperty] private ObservableCollection<UsuarioGerenteDTO> usuarios = new();
        [ObservableProperty] private ObservableCollection<DepartamentoDTO> departamentos = new();

        // Relatórios
        [ObservableProperty] private ObservableCollection<RelatorioDepartamentoDTO> relatoriosDepartamentos = new();
        [ObservableProperty] private ObservableCollection<RankingUsuarioDTO> ranking = new();

        [ObservableProperty] private bool isLoading = false;
        [ObservableProperty] private string nomeUsuario = string.Empty;

        public GerenteViewModel(ApiService apiService, UserSessionService userSession)
        {
            _apiService = apiService;
            _userSession = userSession;
            NomeUsuario = _userSession.Nome ?? "Gerente";
        }

        public async Task InitializeAsync()
        {
            System.Diagnostics.Debug.WriteLine("=== GerenteViewModel.InitializeAsync INICIADO ===");
            await CarregarDashboardAsync();
            await CarregarTicketsAsync();
            await CarregarDepartamentosAsync();
            System.Diagnostics.Debug.WriteLine("=== GerenteViewModel.InitializeAsync CONCLUÍDO ===");
        }

        [RelayCommand]
        private async Task MudarAba(string aba)
        {
            System.Diagnostics.Debug.WriteLine($"[MUDAR ABA] Mudando para aba: {aba}");
            switch (aba)
            {
                case "Tickets":
                    AbaAtiva = 0;
                    System.Diagnostics.Debug.WriteLine("[MUDAR ABA] AbaAtiva = 0 (Tickets)");
                    await CarregarTicketsAsync();
                    break;
                case "Usuarios":
                    AbaAtiva = 1;
                    System.Diagnostics.Debug.WriteLine("[MUDAR ABA] AbaAtiva = 1 (Usuarios)");
                    await CarregarUsuariosAsync();
                    break;
                case "Relatorios":
                    AbaAtiva = 2;
                    System.Diagnostics.Debug.WriteLine("[MUDAR ABA] AbaAtiva = 2 (Relatorios)");
                    await CarregarRelatoriosAsync();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // DASHBOARD
        // ═══════════════════════════════════════════════════════════

        private async Task CarregarDashboardAsync()
        {
            System.Diagnostics.Debug.WriteLine("[DASHBOARD] Iniciando carregamento...");
            try
            {
                var dashboard = await _apiService.ObterDashboardAsync();
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD] API retornou: {(dashboard != null ? "dados" : "NULL")}");

                if (dashboard != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DASHBOARD] TotalUsuarios: {dashboard.TotalUsuarios}");
                    System.Diagnostics.Debug.WriteLine($"[DASHBOARD] TicketsAbertos: {dashboard.TicketsAbertos}");
                    System.Diagnostics.Debug.WriteLine($"[DASHBOARD] TicketsResolvidos: {dashboard.TicketsResolvidos}");
                    System.Diagnostics.Debug.WriteLine($"[DASHBOARD] TotalChats: {dashboard.TotalChats}");
                    System.Diagnostics.Debug.WriteLine($"[DASHBOARD] TopUsuarios count: {dashboard.TopUsuarios?.Count ?? 0}");

                    TotalUsuarios = dashboard.TotalUsuarios;
                    TicketsAbertos = dashboard.TicketsAbertos;
                    TicketsResolvidos = dashboard.TicketsResolvidos;
                    TotalChats = dashboard.TotalChats;
                    ResolvidosHoje = dashboard.TicketsResolvidosHoje;
                    ChatsConcluidos = dashboard.ChatsResolvidos;

                    Ranking.Clear();
                    foreach (var user in dashboard.TopUsuarios)
                    {
                        Ranking.Add(user);
                    }
                    System.Diagnostics.Debug.WriteLine($"[DASHBOARD] Ranking.Count após adicionar: {Ranking.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DASHBOARD] ERRO: Dashboard é NULL!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD] EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD] Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DASHBOARD] StackTrace: {ex.StackTrace}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // TICKETS
        // ═══════════════════════════════════════════════════════════

        private async Task CarregarTicketsAsync()
        {
            System.Diagnostics.Debug.WriteLine("[TICKETS] Iniciando carregamento...");
            if (IsLoading)
            {
                System.Diagnostics.Debug.WriteLine("[TICKETS] JÁ ESTÁ CARREGANDO - retornando");
                return;
            }
            IsLoading = true;

            try
            {
                var lista = await _apiService.ObterTodosTicketsAsync();
                System.Diagnostics.Debug.WriteLine($"[TICKETS] API retornou {lista?.Count ?? 0} tickets");

                Tickets.Clear();
                foreach (var ticket in lista)
                {
                    System.Diagnostics.Debug.WriteLine($"[TICKETS] Adicionando ticket #{ticket.Id}: {ticket.NomeSolicitante} - {ticket.DescricaoProblema?.Substring(0, Math.Min(30, ticket.DescricaoProblema?.Length ?? 0))}");
                    Tickets.Add(ticket);
                }
                System.Diagnostics.Debug.WriteLine($"[TICKETS] Tickets.Count final: {Tickets.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TICKETS] EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[TICKETS] Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TICKETS] StackTrace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("[TICKETS] Carregamento finalizado");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // USUÁRIOS
        // ═══════════════════════════════════════════════════════════

        private async Task CarregarUsuariosAsync()
        {
            System.Diagnostics.Debug.WriteLine("[USUARIOS] Iniciando carregamento...");
            if (IsLoading)
            {
                System.Diagnostics.Debug.WriteLine("[USUARIOS] JÁ ESTÁ CARREGANDO - retornando");
                return;
            }
            IsLoading = true;

            try
            {
                var lista = await _apiService.ObterUsuariosAsync();
                System.Diagnostics.Debug.WriteLine($"[USUARIOS] API retornou {lista?.Count ?? 0} usuários");

                Usuarios.Clear();
                foreach (var user in lista)
                {
                    System.Diagnostics.Debug.WriteLine($"[USUARIOS] Adicionando: {user.Nome} ({user.Email}) - Dept: {user.Departamento}");
                    Usuarios.Add(user);
                }
                System.Diagnostics.Debug.WriteLine($"[USUARIOS] Usuarios.Count final: {Usuarios.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[USUARIOS] EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[USUARIOS] Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[USUARIOS] StackTrace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("[USUARIOS] Carregamento finalizado");
            }
        }

        private async Task CarregarDepartamentosAsync()
        {
            System.Diagnostics.Debug.WriteLine("[DEPARTAMENTOS] Iniciando carregamento...");
            try
            {
                var lista = await _apiService.ObterDepartamentosAsync();
                System.Diagnostics.Debug.WriteLine($"[DEPARTAMENTOS] API retornou {lista?.Count ?? 0} departamentos");

                Departamentos.Clear();
                foreach (var dept in lista)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEPARTAMENTOS] Adicionando: {dept.Nome}");
                    Departamentos.Add(dept);
                }
                System.Diagnostics.Debug.WriteLine($"[DEPARTAMENTOS] Departamentos.Count final: {Departamentos.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEPARTAMENTOS] EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[DEPARTAMENTOS] Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEPARTAMENTOS] StackTrace: {ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task EditarUsuario(UsuarioGerenteDTO usuario)
        {
            if (Application.Current?.MainPage == null) return;

            var nome = await Application.Current.MainPage.DisplayPromptAsync(
                "Editar Usuário",
                "Nome:",
                initialValue: usuario.Nome
            );
            if (string.IsNullOrWhiteSpace(nome)) return;

            var email = await Application.Current.MainPage.DisplayPromptAsync(
                "Editar Usuário",
                "Email:",
                initialValue: usuario.Email,
                keyboard: Keyboard.Email
            );
            if (string.IsNullOrWhiteSpace(email)) return;

            // Selecionar departamento
            var deptNomes = Departamentos.Select(d => d.Nome).ToArray();
            var deptSelecionado = await Application.Current.MainPage.DisplayActionSheet(
                "Selecione o Departamento",
                "Cancelar",
                null,
                deptNomes
            );

            if (deptSelecionado == "Cancelar" || string.IsNullOrEmpty(deptSelecionado)) return;

            var dept = Departamentos.FirstOrDefault(d => d.Nome == deptSelecionado);
            if (dept == null) return;

            var request = new AtualizarUsuarioRequest
            {
                Nome = nome,
                Email = email,
                IdDepartamento = dept.Id
            };

            var sucesso = await _apiService.AtualizarUsuarioAsync(usuario.Id, request);

            if (sucesso)
            {
                await Application.Current.MainPage.DisplayAlert("Sucesso", "Usuário atualizado!", "OK");
                await CarregarUsuariosAsync();
                await CarregarDashboardAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Falha ao atualizar usuário", "OK");
            }
        }

        [RelayCommand]
        private async Task AlterarCargo(UsuarioGerenteDTO usuario)
        {
            if (Application.Current?.MainPage == null) return;

            var cargo = await Application.Current.MainPage.DisplayActionSheet(
                $"Alterar cargo de {usuario.Nome}",
                "Cancelar",
                null,
                "Solicitante",
                "Tecnico",
                "Gerente"
            );

            if (cargo == "Cancelar" || string.IsNullOrEmpty(cargo)) return;

            var confirmar = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                $"Alterar cargo para {cargo}?",
                "Sim",
                "Não"
            );

            if (!confirmar) return;

            var sucesso = await _apiService.AlterarCargoUsuarioAsync(usuario.Id, cargo);

            if (sucesso)
            {
                await Application.Current.MainPage.DisplayAlert("Sucesso", $"Cargo alterado para {cargo}!", "OK");
                await CarregarUsuariosAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Falha ao alterar cargo", "OK");
            }
        }

        [RelayCommand]
        private async Task AlterarSenha(UsuarioGerenteDTO usuario)
        {
            if (Application.Current?.MainPage == null) return;

            var novaSenha = await Application.Current.MainPage.DisplayPromptAsync(
                "Alterar Senha",
                $"Nova senha para {usuario.Nome}:",
                placeholder: "Mínimo 6 caracteres"
            );

            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
            {
                await Application.Current.MainPage.DisplayAlert("Atenção", "Senha deve ter no mínimo 6 caracteres", "OK");
                return;
            }

            var sucesso = await _apiService.AlterarSenhaUsuarioAsync(usuario.Id, novaSenha);

            if (sucesso)
            {
                await Application.Current.MainPage.DisplayAlert("Sucesso", "Senha alterada!", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Falha ao alterar senha", "OK");
            }
        }

        [RelayCommand]
        private async Task VerTicketsUsuario(UsuarioGerenteDTO usuario)
        {
            if (Application.Current?.MainPage == null) return;

            try
            {
                var tickets = await _apiService.ObterTicketsUsuarioAsync(usuario.Id);

                if (!tickets.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "Este usuário não possui tickets", "OK");
                    return;
                }

                var ticketsTitulos = tickets.Select(t => $"#{t.Id} - {t.DescricaoProblema?.Substring(0, Math.Min(50, t.DescricaoProblema.Length))}...").ToArray();

                await Application.Current.MainPage.DisplayActionSheet(
                    $"Tickets de {usuario.Nome} ({tickets.Count})",
                    "Fechar",
                    null,
                    ticketsTitulos
                );
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task ExcluirUsuario(UsuarioGerenteDTO usuario)
        {
            if (Application.Current?.MainPage == null) return;

            var confirmar = await Application.Current.MainPage.DisplayAlert(
                "Atenção",
                $"Deseja realmente excluir {usuario.Nome}? Esta ação não pode ser desfeita!",
                "Sim, Excluir",
                "Cancelar"
            );

            if (!confirmar) return;

            var sucesso = await _apiService.ExcluirUsuarioAsync(usuario.Id);

            if (sucesso)
            {
                await Application.Current.MainPage.DisplayAlert("Sucesso", "Usuário excluído!", "OK");
                await CarregarUsuariosAsync();
                await CarregarDashboardAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Erro", "Falha ao excluir usuário", "OK");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RELATÓRIOS
        // ═══════════════════════════════════════════════════════════

        private async Task CarregarRelatoriosAsync()
        {
            System.Diagnostics.Debug.WriteLine("[RELATORIOS] Iniciando carregamento...");
            if (IsLoading)
            {
                System.Diagnostics.Debug.WriteLine("[RELATORIOS] JÁ ESTÁ CARREGANDO - retornando");
                return;
            }
            IsLoading = true;

            try
            {
                var relatorios = await _apiService.ObterRelatorioDepartamentosAsync();
                System.Diagnostics.Debug.WriteLine($"[RELATORIOS] API retornou {relatorios?.Count ?? 0} relatórios");

                RelatoriosDepartamentos.Clear();
                foreach (var rel in relatorios)
                {
                    System.Diagnostics.Debug.WriteLine($"[RELATORIOS] Adicionando: {rel.Departamento} - {rel.TotalUsuarios} usuários, {rel.TotalTickets} tickets");
                    RelatoriosDepartamentos.Add(rel);
                }
                System.Diagnostics.Debug.WriteLine($"[RELATORIOS] RelatoriosDepartamentos.Count final: {RelatoriosDepartamentos.Count}");

                // Ranking já foi carregado no dashboard
                System.Diagnostics.Debug.WriteLine($"[RELATORIOS] Ranking.Count atual: {Ranking.Count}");
                if (Ranking.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[RELATORIOS] Ranking vazio, carregando do dashboard...");
                    var dashboard = await _apiService.ObterDashboardAsync();
                    if (dashboard != null)
                    {
                        Ranking.Clear();
                        foreach (var user in dashboard.TopUsuarios)
                        {
                            Ranking.Add(user);
                        }
                        System.Diagnostics.Debug.WriteLine($"[RELATORIOS] Ranking.Count após carregar: {Ranking.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RELATORIOS] EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[RELATORIOS] Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RELATORIOS] StackTrace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("[RELATORIOS] Carregamento finalizado");
            }
        }

        [RelayCommand]
        private async Task Sair()
        {
            if (Application.Current?.MainPage == null) return;

            var confirmar = await Application.Current.MainPage.DisplayAlert(
                "Sair",
                "Deseja realmente sair?",
                "Sim",
                "Não"
            );

            if (confirmar)
            {
                _userSession.ClearSession();
                Application.Current.MainPage = new Views.LoginPage(
                    Application.Current!.Handler.MauiContext!.Services.GetRequiredService<ViewModels.LoginViewModel>()
                );
            }
        }
    }
}
