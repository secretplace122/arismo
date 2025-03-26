using CorporatePortal.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Загрузка токена из data.json
var botTokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "data.json");
Console.WriteLine($"Путь к файлу: {botTokenFilePath}");

if (!File.Exists(botTokenFilePath))
{
    Console.WriteLine("Файл data.json не найден.");
    return;
}

var jsonContent = File.ReadAllText(botTokenFilePath);
var botConfig = JsonConvert.DeserializeObject<BotConfig>(jsonContent);

if (string.IsNullOrEmpty(botConfig?.Token))
{
    Console.WriteLine("Токен бота не найден в data.json.");
    return;
}

// Регистрация TelegramBot с использованием токена
builder.Services.AddSingleton<TelegramBot>(provider => new TelegramBot(botConfig.Token));

// Регистрация Database
builder.Services.AddSingleton<Database>();

// Регистрация фоновой задачи
builder.Services.AddSingleton<LunchOrderBackgroundService>();
builder.Services.AddHostedService<LunchOrderBackgroundService>(provider =>
    provider.GetRequiredService<LunchOrderBackgroundService>());

// Добавление сервисов
builder.Services.AddControllersWithViews();

// Настройка аутентификации
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// Инициализация базы данных
var database = app.Services.GetRequiredService<Database>();
database.InitializeDatabase();

// Запуск TelegramBot
var telegramBot = app.Services.GetRequiredService<TelegramBot>();
await telegramBot.StartAsync(); // Запуск бота

// Конфигурация middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Логирование запросов
app.Use(async (context, next) =>
{
    Console.WriteLine($"Запрос: {context.Request.Path}");
    await next();
});

app.Run();