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
            if (usuarioId != null)
            {
                var tipoUsuario = HttpContext.Session.GetString("TipoUsuario");
                if (tipoUsuario == "tecnico")
                    return RedirectToAction("Index", "Tecnico");
                else
                    return RedirectToAction("Index", "Chat");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Entrar(string email, string senha)
        {
            try
            {
                var resultado = await _apiClient.LoginAsync(email, senha);

                if (resultado.Sucesso)
                {
                    // Salva informações na sessão
                    HttpContext.Session.SetInt32("UsuarioId", resultado.UsuarioId);
                    HttpContext.Session.SetString("Nome", resultado.Nome);
                    HttpContext.Session.SetString("TipoUsuario", resultado.TipoUsuario);

                    // Redireciona de acordo com o tipo de usuário
                    if (resultado.TipoUsuario == "tecnico")
                        return RedirectToAction("Index", "Tecnico");
                    else
                        return RedirectToAction("Index", "Chat");
                }
                else
                {
                    ViewBag.Erro = resultado.Mensagem;
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Erro = $"Erro ao fazer login: {ex.Message}";
                return View("Index");
            }
        }

        public IActionResult Sair()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}