using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;

namespace DotIA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Validação de entrada
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Senha))
                {
                    return BadRequest(new LoginResponse
                    {
                        Sucesso = false,
                        Mensagem = "Email e senha são obrigatórios"
                    });
                }

                // Tentar login como Solicitante
                var solicitante = await _context.Solicitantes
                    .FirstOrDefaultAsync(s => s.Email == request.Email && s.Senha == request.Senha);

                if (solicitante != null)
                {
                    return Ok(new LoginResponse
                    {
                        Sucesso = true,
                        TipoUsuario = "Solicitante",
                        UsuarioId = solicitante.Id,
                        Nome = solicitante.Nome,
                        Mensagem = "Login realizado com sucesso"
                    });
                }

                // Tentar login como Técnico
                var tecnico = await _context.Tecnicos
                    .FirstOrDefaultAsync(t => t.Email == request.Email && t.Senha == request.Senha);

                if (tecnico != null)
                {
                    return Ok(new LoginResponse
                    {
                        Sucesso = true,
                        TipoUsuario = "Tecnico",
                        UsuarioId = tecnico.Id,
                        Nome = tecnico.Nome,
                        Mensagem = "Login realizado com sucesso"
                    });
                }

                // Credenciais inválidas
                return Unauthorized(new LoginResponse
                {
                    Sucesso = false,
                    Mensagem = "Email ou senha incorretos"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro ao processar login: {ex.Message}"
                });
            }
        }

        [HttpPost("registrar")]
        public async Task<ActionResult<LoginResponse>> Registrar([FromBody] RegistroRequest request)
        {
            try
            {
                // Verificar se email já existe
                var emailExiste = await _context.Solicitantes
                    .AnyAsync(s => s.Email == request.Email);

                if (emailExiste)
                {
                    return BadRequest(new LoginResponse
                    {
                        Sucesso = false,
                        Mensagem = "Este email já está cadastrado"
                    });
                }

                // Criar novo solicitante
                var novoSolicitante = new Solicitante
                {
                    Nome = request.Nome,
                    Email = request.Email,
                    Senha = request.Senha, // ?? TODO: Implementar hash de senha
                    IdDepartamento = request.IdDepartamento
                };

                _context.Solicitantes.Add(novoSolicitante);
                await _context.SaveChangesAsync();

                return Ok(new LoginResponse
                {
                    Sucesso = true,
                    TipoUsuario = "Solicitante",
                    UsuarioId = novoSolicitante.Id,
                    Nome = novoSolicitante.Nome,
                    Mensagem = "Cadastro realizado com sucesso"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro ao processar cadastro: {ex.Message}"
                });
            }
        }
    }

    // ???????????????????????????????????????????????????????????
    // MODELS (adicionar em Models/AuthModels.cs)
    // ???????????????????????????????????????????????????????????

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Senha { get; set; }
    }

    public class LoginResponse
    {
        public bool Sucesso { get; set; }
        public string TipoUsuario { get; set; }
        public int UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Mensagem { get; set; }
    }

    public class RegistroRequest
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public int IdDepartamento { get; set; }
    }
}