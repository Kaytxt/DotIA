# DotIA Mobile - Aplicativo Android

## 📱 Sobre o Projeto

Aplicativo mobile Android desenvolvido em **.NET MAUI** (C#) para o sistema DotIA - Sistema Inteligente de Suporte com IA.

### ✨ Funcionalidades

#### 👤 Solicitante
- Chat com IA (Azure OpenAI)
- Histórico de conversas
- Avaliação de respostas
- Abertura de tickets direto
- Envio de mensagens para técnicos
- Sincronização em tempo real (5 segundos)

#### 🛠️ Técnico
- Visualização de tickets pendentes
- Responder tickets
- Marcar tickets como resolvidos
- Auto-refresh a cada 5 segundos

#### 👔 Gerente
- Dashboard completo com estatísticas
- Gerenciar todos os tickets
- Gerenciar usuários
- Ver relatórios e métricas

## 🚀 Pré-requisitos

1. **Visual Studio 2022** (17.8 ou superior) OU **Visual Studio Code** com extensões C#
2. **.NET 9.0 SDK**
3. **Workload do .NET MAUI** instalado
4. **Android SDK** (API Level 21 ou superior recomendado API 34)
5. **Emulador Android** ou dispositivo físico Android

### Instalar .NET MAUI Workload

```bash
dotnet workload install maui
```

## 📦 Configuração

### 1. Configurar URL da API

No arquivo `DotIA.Mobile/Services/ApiService.cs`, linha 11, você precisa configurar a URL da API:

```csharp
// Para Emulador Android (aponta para localhost da máquina host)
private const string BaseUrl = "http://10.0.2.2:5100";

// Para dispositivo físico Android (substitua pelo IP da sua máquina)
private const string BaseUrl = "http://192.168.1.XXX:5100";
```

**Como descobrir seu IP local:**

```bash
# Windows
ipconfig

# Linux/Mac
ip addr show
# ou
ifconfig
```

### 2. Garantir que a API está rodando

A API backend deve estar rodando em `http://localhost:5100` antes de iniciar o app mobile.

```bash
cd DotIA.API
dotnet run
```

### 3. Permitir HTTP no Android (já configurado)

O Android por padrão bloqueia conexões HTTP (não HTTPS). Já configuramos isso no `AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

## 🔧 Compilar e Executar

### Usando Visual Studio 2022

1. Abra a solução `DotIA.Mobile.csproj` no Visual Studio
2. Selecione o target **Android**
3. Escolha um emulador Android ou dispositivo conectado
4. Pressione **F5** ou clique em **Executar**

### Usando Visual Studio Code

1. Abra a pasta `DotIA.Mobile` no VS Code
2. Execute o comando:

```bash
dotnet build
```

3. Para rodar no emulador:

```bash
dotnet build -t:Run -f net9.0-android34.0
```

### Usando Android Studio

1. Abra o Android Studio
2. Selecione **Open an existing Android Studio project**
3. Navegue até a pasta `DotIA.Mobile`
4. Aguarde a sincronização do Gradle
5. Execute o projeto

### Linha de Comando

```bash
cd DotIA.Mobile

# Compilar
dotnet build

# Rodar no emulador
dotnet build -t:Run -f net9.0-android35.0

# Gerar APK de Release
dotnet publish -f net9.0-android35.0 -c Release
```

O APK será gerado em: `bin/Release/net9.0-android35.0/publish/`

## 🎨 Design e UI

O aplicativo segue o mesmo tema da versão Web:

- **Cores principais:** Roxo (#7c3aed) e Verde (#10b981)
- **Background:** Tons escuros (#1a1a2e, #16213e)
- **Padrão:** Material Design adaptado
- **Arquitetura:** MVVM (Model-View-ViewModel)

## 🔄 Sincronização em Tempo Real

O app implementa **polling** a cada 5 segundos (igual à versão Web) para:
- Atualizar histórico de chats
- Verificar respostas de técnicos
- Atualizar lista de tickets
- Atualizar estatísticas do dashboard

## 📚 Estrutura do Projeto

```
DotIA.Mobile/
├── Models/                  # DTOs e modelos de dados
│   └── DTOs.cs
├── Services/               # Serviços (API, Session)
│   ├── ApiService.cs
│   └── UserSessionService.cs
├── ViewModels/            # ViewModels (MVVM)
│   ├── LoginViewModel.cs
│   ├── RegistroViewModel.cs
│   ├── ChatViewModel.cs
│   ├── TecnicoViewModel.cs
│   └── GerenteViewModel.cs
├── Views/                 # Páginas XAML
│   ├── LoginPage.xaml
│   ├── RegistroPage.xaml
│   ├── ChatPage.xaml
│   ├── TecnicoPage.xaml
│   └── GerentePage.xaml
├── Converters/           # Converters XAML
│   └── Converters.cs
├── Resources/            # Recursos (ícones, fontes, estilos)
│   ├── AppIcon/
│   ├── Splash/
│   ├── Styles/
│   └── Fonts/
├── Platforms/           # Código específico de plataforma
│   └── Android/
│       ├── AndroidManifest.xml
│       ├── MainActivity.cs
│       └── MainApplication.cs
├── App.xaml             # Aplicativo principal
├── AppShell.xaml        # Shell de navegação
└── MauiProgram.cs       # Configuração DI
```

## 🐛 Solução de Problemas

### Erro: "Unable to connect to the API"

1. Verifique se a API está rodando em `http://localhost:5100`
2. Se estiver usando dispositivo físico, certifique-se de usar o IP correto da máquina
3. Verifique se o firewall não está bloqueando a porta 5100

### Erro: "DEP0700: Registration of the app failed"

1. Desinstale o app do emulador/dispositivo
2. Limpe a solução: `dotnet clean`
3. Reconstrua: `dotnet build`

### Emulador Android lento

1. Use um emulador com **Hardware Acceleration (HAXM ou Hyper-V)**
2. Configure o emulador com pelo menos **4GB de RAM**
3. Considere usar um dispositivo físico para testes

### Erro de compilação no .NET MAUI

1. Certifique-se de ter o workload MAUI instalado:
   ```bash
   dotnet workload install maui
   ```

2. Atualize o SDK:
   ```bash
   dotnet workload update
   ```

## 📱 Testando no Dispositivo Físico

1. **Ative o modo desenvolvedor** no seu Android:
   - Vá em Configurações > Sobre o telefone
   - Toque 7 vezes em "Número de compilação"

2. **Ative a Depuração USB**:
   - Configurações > Opções do desenvolvedor
   - Ative "Depuração USB"

3. **Conecte o dispositivo** via USB

4. **Configure o IP da API** no código (ApiService.cs)

5. **Execute o projeto** selecionando seu dispositivo

## 🌐 Sincronização com Web e Desktop

O aplicativo mobile se integra perfeitamente com as versões Web e Desktop:

- ✅ **Mesma API backend** (porta 5100)
- ✅ **Mesmo banco de dados PostgreSQL**
- ✅ **Sincronização em tempo real** via polling
- ✅ **Mesmo fluxo de negócio**

**Exemplo de sincronização:**
1. Usuário abre ticket no mobile
2. Técnico responde no desktop
3. Mobile detecta resposta em até 5 segundos
4. Usuário visualiza resposta no mobile

## 📄 Licença

Projeto DotIA - Sistema Inteligente de Suporte
Desenvolvido com .NET MAUI, C# e Azure OpenAI

## 🤝 Contribuindo

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

## 📞 Suporte

Para dúvidas ou problemas:
1. Verifique a documentação completa em `/README.md`
2. Consulte os logs do aplicativo
3. Abra uma issue no repositório
