using Microsoft.AspNetCore.Mvc;
using DotIA.Web.Services;

namespace DotIA.Web.Controllers
{
    public class GerenteController : Controller
    {
        private readonly ApiClient _apiClient;

        public GerenteController(HttpClient httpClient, IConfiguration configuration)
        {
            _apiClient = new ApiClient(httpClient, configuration);
        }

        public IActionResult Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var tipoUsuario = HttpContext.Session.GetString("TipoUsuario");

            if (usuarioId == null || tipoUsuario != "Gerente")
                return RedirectToAction("Index", "Login");

            ViewBag.Nome = HttpContext.Session.GetString("Nome");
            return View();
        }

        // ═══════════════════════════════════════════════════════════
        // DASHBOARD
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> ObterDashboard()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var dashboard = await _apiClient.ObterDashboardAsync();
            return Json(dashboard);
        }

        // ═══════════════════════════════════════════════════════════
        // USUÁRIOS
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> ObterUsuarios()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var usuarios = await _apiClient.ObterUsuariosAsync();
            return Json(usuarios);
        }

        [HttpGet("Gerente/ObterUsuario/{usuarioId}")]
        public async Task<IActionResult> ObterUsuario(int usuarioId)
        {
            var gerenteId = HttpContext.Session.GetInt32("UsuarioId");
            if (gerenteId == null)
                return Unauthorized();

            var usuario = await _apiClient.ObterUsuarioAsync(usuarioId);
            return Json(usuario);
        }

        [HttpPut("Gerente/AtualizarUsuario")]
        public async Task<IActionResult> AtualizarUsuario([FromBody] AtualizarUsuarioRequest request)
        {
            var gerenteId = HttpContext.Session.GetInt32("UsuarioId");
            if (gerenteId == null)
                return Unauthorized();

            var sucesso = await _apiClient.AtualizarUsuarioAsync(
                request.UsuarioId,
                request.Nome,
                request.Email,
                request.IdDepartamento
            );

            return Json(new { sucesso });
        }

        [HttpDelete("Gerente/ExcluirUsuario/{usuarioId}")]
        public async Task<IActionResult> ExcluirUsuario(int usuarioId)
        {
            var gerenteId = HttpContext.Session.GetInt32("UsuarioId");
            if (gerenteId == null)
                return Unauthorized();

            var sucesso = await _apiClient.ExcluirUsuarioAsync(usuarioId);
            return Json(new { sucesso });
        }

        [HttpGet("Gerente/ObterTicketsUsuario/{usuarioId}")]
        public async Task<IActionResult> ObterTicketsUsuario(int usuarioId)
        {
            var gerenteId = HttpContext.Session.GetInt32("UsuarioId");
            if (gerenteId == null)
                return Unauthorized();

            var tickets = await _apiClient.ObterTicketsUsuarioAsync(usuarioId);
            return Json(tickets);
        }

        // ═══════════════════════════════════════════════════════════
        // TICKETS
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> ObterTodosTickets()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var tickets = await _apiClient.ObterTodosTicketsAsync();
            return Json(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> ObterTicketsAbertos()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var tickets = await _apiClient.ObterTicketsAbertosAsync();
            return Json(tickets);
        }

        [HttpPost("Gerente/ResponderTicket")]
        public async Task<IActionResult> ResponderTicket([FromBody] ResponderTicketRequest request)
        {
            var gerenteId = HttpContext.Session.GetInt32("UsuarioId");
            if (gerenteId == null)
                return Unauthorized();

            var sucesso = await _apiClient.ResponderTicketGerenteAsync(
                request.TicketId,
                request.Resposta,
                request.MarcarComoResolvido
            );

            return Json(new { sucesso });
        }

        [HttpPut("Gerente/AlterarSenha")]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
        {
            var gerenteId = HttpContext.Session.GetInt32("UsuarioId");
            if (gerenteId == null)
                return Unauthorized();

            var sucesso = await _apiClient.AlterarSenhaUsuarioAsync(
                request.UsuarioId,
                request.NovaSenha
            );

            return Json(new { sucesso });
        }

        // ═══════════════════════════════════════════════════════════
        // RELATÓRIOS
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> ObterRelatorioDepartamentos()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var relatorio = await _apiClient.ObterRelatorioDepartamentosAsync();
            return Json(relatorio);
        }

        [HttpGet]
        public async Task<IActionResult> ObterDepartamentos()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var departamentos = await _apiClient.ObterDepartamentosAsync();
            return Json(departamentos);
        }

        [HttpPut("Gerente/AlterarCargo")]
        public async Task<IActionResult> AlterarCargo([FromBody] AlterarCargoWebRequest request)
        {
            var gerenteId = HttpContext.Session.GetInt32("UsuarioId");
            if (gerenteId == null)
                return Unauthorized();

            var sucesso = await _apiClient.AlterarCargoUsuarioAsync(
                request.UsuarioId,
                request.Cargo
            );

            return Json(new { sucesso });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MODELS
    // ═══════════════════════════════════════════════════════════

    public class AtualizarUsuarioRequest
    {
        public int UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
    }

    public class ResponderTicketRequest
    {
        public int TicketId { get; set; }
        public string Resposta { get; set; } = string.Empty;
        public bool MarcarComoResolvido { get; set; }
    }

    public class AlterarSenhaRequest
    {
        public int UsuarioId { get; set; }
        public string NovaSenha { get; set; } = string.Empty;
    }

    public class AlterarCargoWebRequest
    {
        public int UsuarioId { get; set; }
        public string Cargo { get; set; } = string.Empty;
    }
}