using Microsoft.AspNetCore.Mvc;
using DotIA.Web.Services;

namespace DotIA.Web.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApiClient _apiClient;

        public LoginController(HttpClient httpClient, IConfiguration configuration)
        {
            _apiClient = new ApiClient(httpClient, configuration);
        }

        public IActionResult Index()
        {
            // Se já estiver logado, redireciona
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId.HasValue)
            {
                var tipoUsuario = HttpContext.Session.GetString("TipoUsuario");
                if (tipoUsuario == "Tecnico")
                    return RedirectToAction("Index", "Tecnico");
                else
                    return RedirectToAction("Index", "Chat");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Entrar(string email, string senha)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                ViewBag.Erro = "Email e senha são obrigatórios";
                return View("Index");
            }

            var resposta = await _apiClient.LoginAsync(email, senha);

            if (resposta.Sucesso)
            {
                // Salvar na sessão
                HttpContext.Session.SetInt32("UsuarioId", resposta.UsuarioId);
                HttpContext.Session.SetString("Nome", resposta.Nome);
                HttpContext.Session.SetString("TipoUsuario", resposta.TipoUsuario);

                // Redirecionar baseado no tipo
                if (resposta.TipoUsuario == "Tecnico")
                    return RedirectToAction("Index", "Tecnico");
                else
                    return RedirectToAction("Index", "Chat");
            }

            ViewBag.Erro = resposta.Mensagem ?? "Email ou senha incorretos";
            return View("Index");
        }

        public IActionResult Sair()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}