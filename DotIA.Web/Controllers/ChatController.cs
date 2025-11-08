using Microsoft.AspNetCore.Mvc;
using DotIA.Web.Services;

namespace DotIA.Web.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApiClient _apiClient;

        public ChatController(HttpClient httpClient, IConfiguration configuration)
        {
            _apiClient = new ApiClient(httpClient, configuration);
        }

        public IActionResult Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return RedirectToAction("Index", "Login");

            ViewBag.Nome = HttpContext.Session.GetString("Nome");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarPergunta([FromBody] PerguntaRequest request)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var resposta = await _apiClient.EnviarPerguntaAsync(usuarioId.Value, request.Pergunta, request.ChatId);
            return Json(resposta);
        }

        [HttpGet]
        public async Task<IActionResult> ObterHistorico()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var historico = await _apiClient.ObterHistoricoAsync(usuarioId.Value);
            return Json(historico);
        }

        [HttpPost]
        public async Task<IActionResult> AvaliarResposta([FromBody] AvaliacaoRequest request)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var sucesso = await _apiClient.AvaliarRespostaAsync(
                usuarioId.Value,
                request.Pergunta,
                request.Resposta,
                request.FoiUtil,
                request.ChatId
            );

            return Json(new { sucesso });
        }

        [HttpGet("Chat/VerificarResposta/{chatId}")]
        public async Task<IActionResult> VerificarResposta(int chatId)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var resposta = await _apiClient.VerificarRespostaAsync(chatId);
            return Json(resposta);
        }

        // ✅ NOVO: Editar título do chat
        [HttpPut("Chat/EditarTitulo/{chatId}")]
        public async Task<IActionResult> EditarTitulo(int chatId, [FromBody] EditarTituloRequest request)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var sucesso = await _apiClient.EditarTituloChatAsync(chatId, request.NovoTitulo);
            return Json(new { sucesso });
        }

        [HttpPost("Chat/EnviarParaTecnico")]
        public async Task<IActionResult> EnviarParaTecnico([FromBody] EnviarParaTecnicoRequest request)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var sucesso = await _apiClient.EnviarMensagemParaTecnicoAsync(request.ChatId, request.Mensagem);
            return Json(new { sucesso });
        }

        // ✅ NOVO: Excluir chat
        [HttpDelete("Chat/Excluir/{chatId}")]
        public async Task<IActionResult> ExcluirChat(int chatId)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var sucesso = await _apiClient.ExcluirChatAsync(chatId);
            return Json(new { sucesso });
        }

        [HttpPost("Chat/AbrirTicketDireto")]
        public async Task<IActionResult> AbrirTicketDireto([FromBody] AbrirTicketDiretoWebRequest request)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId == null)
                return Unauthorized();

            var resultado = await _apiClient.AbrirTicketDiretoAsync(
                usuarioId.Value,
                request.Titulo,
                request.Descricao
            );

            return Json(resultado);
        }
    }

    public class PerguntaRequest
    {
        public string Pergunta { get; set; } = string.Empty;
        public int? ChatId { get; set; }
    }

    public class AvaliacaoRequest
    {
        public string Pergunta { get; set; } = string.Empty;
        public string Resposta { get; set; } = string.Empty;
        public bool FoiUtil { get; set; }
        public int ChatId { get; set; }
    }

    public class EditarTituloRequest
    {
        public string NovoTitulo { get; set; } = string.Empty;
    }

    public class EnviarParaTecnicoRequest
    {
        public int ChatId { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }
    public class AbrirTicketDiretoWebRequest
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
    }
}