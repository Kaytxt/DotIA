using Microsoft.EntityFrameworkCore;
using TabelasDoBanco;

namespace DotIA.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets para as tabelas do banco
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Solicitante> Solicitantes { get; set; }
        public DbSet<Tecnico> Tecnicos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Subcategoria> Subcategorias { get; set; }
        public DbSet<NivelAtendimento> NiveisAtendimento { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<HistoricoUtil> HistoricoUtil { get; set; }
        public DbSet<ChatHistorico> ChatsHistorico { get; set; }
        public DbSet<AvaliacaoResposta> AvaliacoesResposta { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurações adicionais se necessário
            modelBuilder.Entity<Solicitante>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<Tecnico>()
                .HasIndex(t => t.Email)
                .IsUnique();
        }
    }
}