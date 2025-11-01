using Microsoft.AspNetCore.Mvc;
using DotIA.Web.Services;

namespace DotIA.Web.Controllers
{
    public class RegistroController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<RegistroController> _logger;

        public RegistroController(HttpClient httpClient, IConfiguration configuration, ILogger<RegistroController> logger)
        {
            _apiClient = new ApiClient(httpClient, configuration);
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Carregando página de registro...");

                var departamentos = await _apiClient.ObterDepartamentosAsync();

                _logger.LogInformation($"Departamentos carregados: {departamentos?.Count ?? 0}");

                if (departamentos == null || departamentos.Count == 0)
                {
                    _logger.LogWarning("Nenhum departamento foi retornado pela API");
                    ViewBag.Erro = "Erro ao carregar departamentos. Por favor, tente novamente.";
                }

                ViewBag.Departamentos = departamentos;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar departamentos");
                ViewBag.Erro = "Erro ao carregar departamentos: " + ex.Message;
                ViewBag.Departamentos = new List<DepartamentoDTO>();
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Registrar(RegistroRequestWeb request)
        {
            try
            {
                _logger.LogInformation($"Tentando registrar usuário: {request.Email}");

                var resultado = await _apiClient.RegistrarUsuarioAsync(request);

                if (resultado.Sucesso)
                {
                    _logger.LogInformation($"Usuário registrado com sucesso: {request.Email}");
                    TempData["Sucesso"] = resultado.Mensagem;
                    return RedirectToAction("Index", "Login");
                }
                else
                {
                    _logger.LogWarning($"Falha no registro: {resultado.Mensagem}");
                    ViewBag.Erro = resultado.Mensagem;
                    var departamentos = await _apiClient.ObterDepartamentosAsync();
                    ViewBag.Departamentos = departamentos;
                    return View("Index", request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar cadastro");
                ViewBag.Erro = "Erro ao processar cadastro: " + ex.Message;
                var departamentos = await _apiClient.ObterDepartamentosAsync();
                ViewBag.Departamentos = departamentos;
                return View("Index", request);
            }
        }
    }

    public class RegistroRequestWeb
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string ConfirmacaoSenha { get; set; } = string.Empty;
        public int IdDepartamento { get; set; }
    }
}