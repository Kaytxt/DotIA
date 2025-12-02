# DotIA.API

API backend do sistema DotIA - Assistente de TI com IA.

## 🚀 Tecnologias

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- OpenAI API (GPT-4o-mini)

## 📋 Pré-requisitos

- .NET SDK 9.0+
- PostgreSQL 13+
- OpenAI API Key

## ⚙️ Configuração Local

### 1. Configurar appsettings.json

Já está configurado com valores padrão. Para produção, use variáveis de ambiente.

### 2. Restaurar dependências

```bash
dotnet restore
```

### 3. Executar

```bash
dotnet run
```

A API estará disponível em:
- API: http://localhost:5100/api
- Swagger: http://localhost:5100/swagger
- Health Check: http://localhost:5100/health

## 🔧 Variáveis de Ambiente

Para produção (Azure), configure estas variáveis:

```bash
# Connection String
ConnectionStrings__ConexaoDotIA="Server=HOST;Port=5432;Database=DotIA;User Id=USER;Password=PASS"

# OpenAI
OpenAI__ApiKey="sk-..."
OpenAI__Model="gpt-4o-mini"

# Ambiente
ASPNETCORE_ENVIRONMENT="Production"
```

## 🌐 Deploy na Azure

Veja o guia completo: [AZURE_DEPLOYMENT_GUIDE.md](../AZURE_DEPLOYMENT_GUIDE.md)

**Quick Start:**
```bash
cd ..
./deploy-azure.sh
```

## 📚 Endpoints Principais

### Auth
- `POST /api/auth/login` - Login
- `POST /api/auth/registro` - Registro
- `GET /api/auth/departamentos` - Listar departamentos

### Chat
- `POST /api/chat/enviar` - Enviar pergunta para IA
- `GET /api/chat/historico/{usuarioId}` - Histórico de chats
- `POST /api/chat/avaliar` - Avaliar resposta
- `POST /api/chat/enviar-para-tecnico` - Escalar para técnico
- `PUT /api/chat/editar-titulo/{chatId}` - Editar título
- `DELETE /api/chat/excluir/{chatId}` - Excluir chat

### Tickets
- `GET /api/tickets/pendentes` - Tickets pendentes
- `POST /api/tickets/resolver` - Resolver ticket
- `GET /api/tickets/{id}` - Detalhes do ticket

### Gerente
- `GET /api/gerente/dashboard` - Dashboard gerencial
- `GET /api/gerente/usuarios` - Listar usuários
- `GET /api/gerente/tickets/todos` - Todos os tickets
- `GET /api/gerente/relatorio-departamentos` - Relatório por departamento

## 🧪 Testes

### Testar conexão com banco

```bash
curl http://localhost:5100/health
```

### Testar endpoint

```bash
curl http://localhost:5100/api/auth/departamentos
```

## 📝 Estrutura

```
DotIA.API/
├── Controllers/          # Endpoints da API
│   ├── AuthController.cs
│   ├── ChatController.cs
│   ├── TicketsController.cs
│   └── GerenteController.cs
├── Data/                 # Contexto EF e Migrations
├── Models/               # Entidades
├── Services/             # Serviços (OpenAI, etc)
├── appsettings.json      # Config Development
├── appsettings.Production.json  # Config Production
└── Program.cs            # Entry point
```

## 🐛 Troubleshooting

### Erro ao conectar no banco

Verifique:
1. PostgreSQL está rodando?
2. String de conexão está correta?
3. Banco 'DotIA' existe?

### API retorna 500

Verifique os logs:
```bash
# Local
dotnet run

# Azure
az webapp log tail --name dotia-api --resource-group DotIA-RG
```

### OpenAI retorna erro

Verifique:
1. API Key está correta?
2. Tem créditos disponíveis na OpenAI?
3. Timeout está adequado?

## 📖 Documentação

- [Guia de Deploy Azure](../AZURE_DEPLOYMENT_GUIDE.md)
- [Quick Start](../QUICK_START.md)
- [Swagger UI](http://localhost:5100/swagger) (quando rodando)
