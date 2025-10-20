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
            // Se já está logado, redireciona
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioId != null)
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
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                ViewBag.Erro = "Por favor, preencha todos os campos.";
                return View("Index");
            }

            var resultado = await _apiClient.LoginAsync(email, senha);

            if (resultado.Sucesso)
            {
                // Salva informações na sessão
                HttpContext.Session.SetInt32("UsuarioId", resultado.UsuarioId);
                HttpContext.Session.SetString("TipoUsuario", resultado.TipoUsuario);
                HttpContext.Session.SetString("Nome", resultado.Nome);

                // Redireciona conforme o tipo de usuário
                if (resultado.TipoUsuario == "Tecnico")
                    return RedirectToAction("Index", "Tecnico");
                else
                    return RedirectToAction("Index", "Chat");
            }

            ViewBag.Erro = resultado.Mensagem ?? "Email ou senha inválidos.";
            return View("Index");
        }

        public IActionResult Sair()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}