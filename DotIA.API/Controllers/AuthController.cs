using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Models;
using TabelasDoBanco;

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
                // Verifica se � solicitante
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
                        Mensagem = "Login realizado com sucesso!"
                    });
                }

                // Verifica se � t�cnico
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
                        Mensagem = "Login realizado com sucesso!"
                    });
                }

                return Ok(new LoginResponse
                {
                    Sucesso = false,
                    Mensagem = "Email ou senha incorretos."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro: {ex.Message}"
                });
            }
        }

        [HttpPost("registro")]
        public async Task<ActionResult<RegistroResponse>> Registro([FromBody] RegistroRequest request)
        {
            try
            {
                // Valida��es
                if (string.IsNullOrWhiteSpace(request.Nome))
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Nome � obrigat�rio."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Email � obrigat�rio."
                    });
                }

                if (!request.Email.Contains("@"))
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Email inv�lido."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Senha))
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Senha � obrigat�ria."
                    });
                }

                if (request.Senha.Length < 6)
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Senha deve ter no m�nimo 6 caracteres."
                    });
                }

                if (request.Senha != request.ConfirmacaoSenha)
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "As senhas n�o coincidem."
                    });
                }

                if (request.IdDepartamento <= 0)
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Selecione um departamento."
                    });
                }

                // Verifica se o departamento existe
                var departamento = await _context.Departamentos.FindAsync(request.IdDepartamento);
                if (departamento == null)
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Departamento n�o encontrado."
                    });
                }

                // Verifica se email j� existe
                var emailExiste = await _context.Solicitantes
                    .AnyAsync(s => s.Email == request.Email);

                if (emailExiste)
                {
                    return Ok(new RegistroResponse
                    {
                        Sucesso = false,
                        Mensagem = "Este email j� est� cadastrado."
                    });
                }

                // Cria novo solicitante
                var novoSolicitante = new Solicitante
                {
                    Nome = request.Nome,
                    Email = request.Email,
                    Senha = request.Senha, // Em produ��o, use hash de senha!
                    IdDepartamento = request.IdDepartamento
                };

                _context.Solicitantes.Add(novoSolicitante);
                await _context.SaveChangesAsync();

                return Ok(new RegistroResponse
                {
                    Sucesso = true,
                    Mensagem = "Cadastro realizado com sucesso! Voc� j� pode fazer login.",
                    UsuarioId = novoSolicitante.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RegistroResponse
                {
                    Sucesso = false,
                    Mensagem = $"Erro ao realizar cadastro: {ex.Message}"
                });
            }
        }

        [HttpGet("departamentos")]
        public async Task<ActionResult<List<DepartamentoDTO>>> ObterDepartamentos()
        {
            try
            {
                var departamentos = await _context.Departamentos
                    .Select(d => new DepartamentoDTO
                    {
                        Id = d.Id,
                        Nome = d.Nome
                    })
                    .OrderBy(d => d.Nome)
                    .ToListAsync();

                return Ok(departamentos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = $"Erro ao buscar departamentos: {ex.Message}" });
            }
        }
    }
}