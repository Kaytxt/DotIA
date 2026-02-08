# DotIA 🤖 - Projeto Faculdade

**DotIA** é uma plataforma inteligente de Help Desk e Suporte Técnico que utiliza Inteligência Artificial para agilizar o atendimento. O sistema tenta resolver as dúvidas do usuário via chat (integração OpenAI) e, caso não seja possível, escala automaticamente para um ticket, direcionando para técnicos humanos.

O ecossistema é composto por uma **API Central (.NET 9)** que serve clientes **Web**, **Desktop** (Windows Forms) e **Mobile** (MAUI).

## 🚀 Funcionalidades

- **🧠 Atendimento via IA:** Integração com OpenAI para tentar resolver problemas do usuário (Nível 1) instantaneamente.
- **🎫 Gestão de Tickets:** Abertura automática de tickets quando a IA não resolve ou abertura manual direta.
- **💬 Chat em Tempo Real:** Histórico de conversas entre Usuário e IA, ou Usuário e Técnico.
- **👥 Controle de Acesso:**
  - **Solicitantes:** Abrem chamados e avaliam respostas.
  - **Técnicos/Gerentes:** Gerenciam filas, respondem tickets e visualizam métricas.
- **📂 Organização:** Departamentos, Categorias (Hardware, Software, Rede) e Níveis de urgência.
- **📱 Multiplataforma:** Acesso via Web, Desktop e Mobile.

## 🛠️ Tecnologias Utilizadas

### Backend (API)
- **.NET 9.0** (Web API)
- **Entity Framework Core** (ORM)
- **PostgreSQL** (Banco de Dados)
- **OpenAI API** (Inteligência Artificial)
- **Swagger** (Documentação da API)

### Frontends
- **Web:** ASP.NET Core MVC
- **Desktop:** Windows Forms (.NET 8)
- **Mobile:** .NET MAUI (Android/iOS)

## 📦 Estrutura do Projeto

```bash
DotIA/
├── DotIA.API/        # Backend central (Web API)
├── DotIA.Web/        # Interface Web para usuários/técnicos
├── DotIA.Desktop/    # Aplicação administrativa para Windows
├── DotIA.Mobile/     # App mobile para solicitantes
└── Script-Database/  # Scripts SQL de criação
