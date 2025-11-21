using Microsoft.EntityFrameworkCore;
using DotIA.API.Data;
using DotIA.API.Services;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// configs dos services

// db context com postgres
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ConexaoDotIA")));

// adiciona os controllers
builder.Services.AddControllers();

// swagger pra testar a api
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// httpclient e service do openai
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();

// cors liberado (depois ver se restringe)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// checa se o banco ta funcionando

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ Conexão com banco de dados estabelecida!");
        }
        else
        {
            Console.WriteLine("⚠️  Criando banco de dados...");
            context.Database.EnsureCreated();
            Console.WriteLine("✅ Banco de dados criado com sucesso!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao conectar com o banco: {ex.Message}");
        Console.WriteLine("Verifique:");
        Console.WriteLine("1. PostgreSQL está rodando?");
        Console.WriteLine("2. String de conexão está correta no appsettings.json?");
        Console.WriteLine("3. Banco 'dotia' existe?");
    }
}

// middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 DotIA API iniciada!");
Console.WriteLine($"📍 Swagger UI: http://localhost:5100/swagger");
Console.WriteLine($"📍 API Base: http://localhost:5100/api");

app.Run();