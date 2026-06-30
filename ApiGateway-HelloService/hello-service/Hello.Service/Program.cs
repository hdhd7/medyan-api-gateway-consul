var builder = WebApplication.CreateBuilder(args);

// Настройка порта
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001);
});

// Добавляем встроенную поддержку Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Эндпоинт для Consul (обязательно должен быть доступен)
app.MapHealthChecks("/health");

// Твой основной эндпоинт
app.MapGet("/hello", () => "Hello from Service 1 (IP: 92.51.23.80)!");

app.Run();
