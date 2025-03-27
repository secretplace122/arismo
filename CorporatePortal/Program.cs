using CorporatePortal.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// �������� ������ �� data.json
var botTokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "data.json");
Console.WriteLine($"���� � �����: {botTokenFilePath}");

if (!File.Exists(botTokenFilePath))
{
    Console.WriteLine("���� data.json �� ������.");
    return;
}

var jsonContent = File.ReadAllText(botTokenFilePath);
var botConfig = JsonConvert.DeserializeObject<BotConfig>(jsonContent);

if (string.IsNullOrEmpty(botConfig?.Token))
{
    Console.WriteLine("����� ���� �� ������ � data.json.");
    return;
}

// ����������� TelegramBot � �������������� ������
builder.Services.AddSingleton<TelegramBot>(provider => new TelegramBot(botConfig.Token));

// ����������� Database
builder.Services.AddSingleton<Database>();

// ����������� ������� ������
builder.Services.AddSingleton<LunchOrderBackgroundService>();
builder.Services.AddHostedService<LunchOrderBackgroundService>(provider =>
    provider.GetRequiredService<LunchOrderBackgroundService>());

// ���������� ��������
builder.Services.AddControllersWithViews();

// ��������� ��������������
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// ������������� ���� ������
var database = app.Services.GetRequiredService<Database>();
database.InitializeDatabase();

// ������ TelegramBot
var telegramBot = app.Services.GetRequiredService<TelegramBot>();
await telegramBot.StartAsync(); // ������ ����

// ������������ middleware
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

// ����������� ��������
app.Use(async (context, next) =>
{
    Console.WriteLine($"������: {context.Request.Path}");
    await next();
});

app.Run();