using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Cache.CacheManager;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Загрузка конфигурации Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Настройка JWT
var jwtKey = "Medyan_Super_Secret_Key_2026_Project_Gateway"; 
var jwtIssuer = "Medyan.Dev";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("MedyanKey", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Извлекаем токен из куки для Ocelot
                if (context.Request.Cookies.ContainsKey("token"))
                    context.Token = context.Request.Cookies["token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Регистрация Ocelot с поддержкой Consul и Кэширования
builder.Services
    .AddOcelot(builder.Configuration)
    .AddConsul()
    .AddCacheManager(x => x.WithDictionaryHandle());

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// --- ВНУТРЕННИЕ МАРШРУТЫ ГЕЙТВЕЯ ---

// Страница входа (без автоматического перенаправления)
app.MapGet("/login", () => Results.Content(@"
    <html>
    <head><title>Medyan Auth</title></head>
    <body style='background:#121212;color:white;font-family:sans-serif;display:flex;justify-content:center;padding-top:100px;'>
        <form id='f' style='background:#1e1e1e;padding:30px;border-radius:12px;display:flex;flex-direction:column;gap:15px;width:300px;'>
            <h2 style='text-align:center;color:#007bff;'>Система Шлюза</h2>
            <input id='u' placeholder='Логин' style='padding:10px;border-radius:5px;border:none;'>
            <input id='p' type='password' placeholder='Пароль' style='padding:10px;border-radius:5px;border:none;'>
            <button type='submit' style='padding:10px;background:#007bff;color:white;border:none;border-radius:5px;cursor:pointer;'>Войти</button>
        </form>
        <script>
            f.onsubmit = async (e) => {
                e.preventDefault();
                const r = await fetch('/auth/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username: u.value, password: p.value })
                });
                if(r.ok) {
                    const d = await r.json();
                    document.cookie = 'token=' + d.token + '; path=/; max-age=3600;';
                    alert('Авторизация успешна! Теперь вы можете вручную перейти на /hello или /bye.');
                } else alert('Неверный логин или пароль!');
            };
        </script>
    </body></html>", "text/html; charset=utf-8"));

// Эндпоинт генерации токена
app.MapPost("/auth/login", (LoginModel login) =>
{
    if (login.username == "admin" && login.password == "medyan2026")
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, login.username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = jwtIssuer,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        return Results.Ok(new { token = handler.WriteToken(handler.CreateToken(descriptor)) });
    }
    return Results.Unauthorized();
});

// Ocelot обрабатывает только те запросы, которые не относятся к логину
app.MapWhen(context => !context.Request.Path.Value.StartsWith("/login") &&
                       !context.Request.Path.Value.StartsWith("/auth"),
            builder => builder.UseOcelot().Wait());

app.Run();

public record LoginModel(string username, string password);
