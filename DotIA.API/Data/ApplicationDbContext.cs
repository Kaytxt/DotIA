using Microsoft.EntityFrameworkCore;

namespace DotIA.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Solicitante> Solicitantes { get; set; }
        public DbSet<Tecnico> Tecnicos { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Subcategoria> Subcategorias { get; set; }
        public DbSet<NivelAtendimento> NiveisAtendimento { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<HistoricoUtil> HistoricoUtil { get; set; }
        public DbSet<ChatHistorico> ChatsHistorico { get; set; }
        public DbSet<AvaliacaoResposta> AvaliacoesRespostas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar schema e nomes de tabelas em lowercase
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<Solicitante>().ToTable("solicitantes");
            modelBuilder.Entity<Tecnico>().ToTable("tecnicos");
            modelBuilder.Entity<Departamento>().ToTable("departamentos");
            modelBuilder.Entity<Categoria>().ToTable("categorias");
            modelBuilder.Entity<Subcategoria>().ToTable("subcategorias");
            modelBuilder.Entity<NivelAtendimento>().ToTable("niveis_atendimento");
            modelBuilder.Entity<Ticket>().ToTable("tickets");
            modelBuilder.Entity<HistoricoUtil>().ToTable("historico_util");
            modelBuilder.Entity<ChatHistorico>().ToTable("chat_historico");
            modelBuilder.Entity<AvaliacaoResposta>().ToTable("avaliacao_resposta");

            // Configurar propriedades nullable corretamente
            modelBuilder.Entity<Ticket>()
                .Property(t => t.DataEncerramento)
                .IsRequired(false);

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Solucao)
                .IsRequired(false);
        }
    }

    // CLASSES DE MODELO (mantém as existentes do BancoModels.cs)
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("solicitantes")]
    public class Solicitante
    {
        [Key]
        [Column("id_solicitante")]
        public int Id { get; set; }

        [Column("nome")]
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Column("email")]
        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Column("senha")]
        [Required]
        [MaxLength(255)]
        public string Senha { get; set; }

        [Column("id_departamento")]
        public int IdDepartamento { get; set; }
    }

    [Table("tecnicos")]
    public class Tecnico
    {
        [Key]
        [Column("id_tecnico")]
        public int Id { get; set; }

        [Column("nome")]
        [Required]
        public string Nome { get; set; }

        [Column("email")]
        [Required]
        public string Email { get; set; }

        [Column("senha")]
        [Required]
        public string Senha { get; set; }

        [Column("id_especialidade")]
        public int IdEspecialidade { get; set; }
    }

    [Table("departamentos")]
    public class Departamento
    {
        [Key]
        [Column("id_departamento")]
        public int Id { get; set; }

        [Column("nome_departamento")]
        [Required]
        public string Nome { get; set; }
    }

    [Table("categorias")]
    public class Categoria
    {
        [Key]
        [Column("id_categoria")]
        public int Id { get; set; }

        [Column("nome_categoria")]
        [Required]
        public string Nome { get; set; }
    }

    [Table("subcategorias")]
    public class Subcategoria
    {
        [Key]
        [Column("id_subcategoria")]
        public int Id { get; set; }

        [Column("nome_subcategoria")]
        [Required]
        public string Nome { get; set; }

        [Column("id_categoria")]
        public int IdCategoria { get; set; }
    }

    [Table("niveis_atendimento")]
    public class NivelAtendimento
    {
        [Key]
        [Column("id_nivel")]
        public int Id { get; set; }

        [Column("descricao")]
        [Required]
        public string Descricao { get; set; }
    }

    [Table("tickets")]
    public class Ticket
    {
        [Key]
        [Column("id_ticket")]
        public int Id { get; set; }

        [Column("id_solicitante")]
        public int IdSolicitante { get; set; }

        [Column("id_tecnico")]
        public int IdTecnico { get; set; }

        [Column("id_subcategoria")]
        public int IdSubcategoria { get; set; }

        [Column("id_nivel")]
        public int IdNivel { get; set; }

        [Column("descricao_problema")]
        [Required]
        public string DescricaoProblema { get; set; }

        [Column("id_status")]
        public int IdStatus { get; set; }

        [Column("data_abertura")]
        public DateTime DataAbertura { get; set; }

        [Column("data_encerramento")]
        public DateTime? DataEncerramento { get; set; }

        [Column("solucao")]
        public string? Solucao { get; set; }
    }

    [Table("historico_util")]
    public class HistoricoUtil
    {
        [Key]
        public int Id { get; set; }

        [Column("id_solicitante")]
        public int IdSolicitante { get; set; }

        [Column("pergunta")]
        [Required]
        public string Pergunta { get; set; }

        [Column("resposta")]
        [Required]
        public string Resposta { get; set; }

        [Column("datahora")]
        public DateTime DataHora { get; set; }
    }

    [Table("chat_historico")]
    public class ChatHistorico
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_solicitante")]
        public int IdSolicitante { get; set; }

        [Column("titulo")]
        [Required]
        public string Titulo { get; set; }

        [Column("pergunta")]
        [Required]
        public string Pergunta { get; set; }

        [Column("resposta")]
        [Required]
        public string Resposta { get; set; }

        [Column("data_hora")]
        public DateTime DataHora { get; set; }
    }

    [Table("avaliacao_resposta")]
    public class AvaliacaoResposta
    {
        [Key]
        public int Id { get; set; }

        [Column("id_solicitante")]
        public int IdSolicitante { get; set; }

        [Column("pergunta")]
        [Required]
        public string Pergunta { get; set; }

        [Column("foi_util")]
        public bool FoiUtil { get; set; }

        [Column("datahora")]
        public DateTime DataHora { get; set; }
    }
}