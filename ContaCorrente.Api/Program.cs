using System.Text.Json.Serialization;
using ContaCorrente.Api.Api;
using ContaCorrente.Api.Application.Dispatch;
using ContaCorrente.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string PoliticaCorsDoFront = "web";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum como texto ("Credito"/"Debito") deixa o contrato legível para o front
        // e para quem lê o Swagger, em vez de 1/2.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<ContaCorrenteDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Padrao")));

// Dispatcher, handlers e validators (registrados por varredura do assembly).
builder.Services.AddApplication();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Conta Corrente API",
        Version = "v1",
        Description = "Controle de entradas, saídas, saldo e histórico de uma conta empresarial."
    });

    var xml = Path.Combine(AppContext.BaseDirectory, "ContaCorrente.Api.xml");
    if (File.Exists(xml))
    {
        options.IncludeXmlComments(xml);
    }
});

builder.Services.AddCors(options =>
    options.AddPolicy(PoliticaCorsDoFront, policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Aplica as migrations no startup para que a API suba pronta para uso, sem passo manual.
using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<ContaCorrenteDbContext>();
    db.Database.Migrate();

    // WAL permite leituras concorrentes durante uma escrita. Sem isso, o SQLite bloqueia
    // os leitores enquanto uma movimentação está sendo gravada. Ignorado em bancos
    // :memory:, que não suportam o modo.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Conta Corrente API v1"));
    app.UseCors(PoliticaCorsDoFront);
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Exposto para que os testes de integração possam instanciar a aplicação
/// via WebApplicationFactory&lt;Program&gt;.
/// </summary>
public partial class Program;
