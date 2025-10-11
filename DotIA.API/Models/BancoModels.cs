using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TabelasDoBanco
{
    [Table("DEPARTAMENTOS")]
    public class Departamento
    {
        [Key]
        [Column("ID_DEPARTAMENTO")]
        public int Id { get; set; }

        [Column("NOME_DEPARTAMENTO")]
        public string Nome { get; set; } = string.Empty;
    }

    [Table("SOLICITANTES")]
    public class Solicitante
    {
        [Key]
        [Column("ID_SOLICITANTE")]
        public int Id { get; set; }

        [Column("NOME")]
        public string Nome { get; set; } = string.Empty;

        [Column("EMAIL")]
        public string Email { get; set; } = string.Empty;

        [Column("SENHA")]
        public string Senha { get; set; } = string.Empty;

        [Column("ID_DEPARTAMENTO")]
        public int IdDepartamento { get; set; }
    }

    [Table("TECNICOS")]
    public class Tecnico
    {
        [Key]
        [Column("ID_TECNICO")]
        public int Id { get; set; }

        [Column("NOME")]
        public string Nome { get; set; } = string.Empty;

        [Column("EMAIL")]
        public string Email { get; set; } = string.Empty;

        [Column("SENHA")]
        public string Senha { get; set; } = string.Empty;

        [Column("ID_ESPECIALIDADE")]
        public int IdEspecialidade { get; set; }
    }

    [Table("CATEGORIAS")]
    public class Categoria
    {
        [Key]
        [Column("ID_CATEGORIA")]
        public int Id { get; set; }

        [Column("NOME_CATEGORIA")]
        public string Nome { get; set; } = string.Empty;
    }

    [Table("SUBCATEGORIAS")]
    public class Subcategoria
    {
        [Key]
        [Column("ID_SUBCATEGORIA")]
        public int Id { get; set; }

        [Column("NOME_SUBCATEGORIA")]
        public string Nome { get; set; } = string.Empty;

        [Column("ID_CATEGORIA")]
        public int IdCategoria { get; set; }
    }

    [Table("NIVEIS_ATENDIMENTO")]
    public class NivelAtendimento
    {
        [Key]
        [Column("ID_NIVEL")]
        public int Id { get; set; }

        [Column("DESCRICAO")]
        public string Descricao { get; set; } = string.Empty;
    }

    [Table("TICKETS")]
    public class Ticket
    {
        [Key]
        [Column("ID_TICKET")]
        public int Id { get; set; }

        [Column("ID_SOLICITANTE")]
        public int IdSolicitante { get; set; }

        [Column("ID_TECNICO")]
        public int IdTecnico { get; set; }

        [Column("ID_SUBCATEGORIA")]
        public int IdSubcategoria { get; set; }

        [Column("ID_NIVEL")]
        public int IdNivel { get; set; }

        [Column("DESCRICAO_PROBLEMA")]
        public string DescricaoProblema { get; set; } = string.Empty;

        [Column("ID_STATUS")]
        public int IdStatus { get; set; }

        [Column("DATA_ABERTURA")]
        public DateTime DataAbertura { get; set; }

        [Column("DATA_ENCERRAMENTO")]
        public DateTime? DataEncerramento { get; set; }

        [Column("SOLUCAO")]
        public string? Solucao { get; set; }
    }

    [Table("HISTORICO_UTIL")]
    public class HistoricoUtil
    {
        [Key]
        public int Id { get; set; }

        [Column("ID_SOLICITANTE")]
        public int IdSolicitante { get; set; }

        [Column("PERGUNTA")]
        public string Pergunta { get; set; } = string.Empty;

        [Column("RESPOSTA")]
        public string Resposta { get; set; } = string.Empty;

        [Column("DATAHORA")]
        public DateTime DataHora { get; set; }
    }

    [Table("CHAT_HISTORICO")]
    public class ChatHistorico
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("ID_SOLICITANTE")]
        public int IdSolicitante { get; set; }

        [Column("TITULO")]
        public string Titulo { get; set; } = string.Empty;

        [Column("PERGUNTA")]
        public string Pergunta { get; set; } = string.Empty;

        [Column("RESPOSTA")]
        public string Resposta { get; set; } = string.Empty;

        [Column("DATA_HORA")]
        public DateTime DataHora { get; set; }
    }

    [Table("AVALIACAO_RESPOSTA")]
    public class AvaliacaoResposta
    {
        [Key]
        public int Id { get; set; }

        [Column("ID_SOLICITANTE")]
        public int IdSolicitante { get; set; }

        [Column("PERGUNTA")]
        public string Pergunta { get; set; } = string.Empty;

        [Column("FOI_UTIL")]
        public bool FoiUtil { get; set; }

        [Column("DATAHORA")]
        public DateTime DataHora { get; set; }
    }
}