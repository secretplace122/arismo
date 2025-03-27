using CorporatePortal.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Telegram.Bot;

public class FeedbackController : Controller
{
    private readonly Database _database;
    private readonly TelegramBot _telegramBot;

    public FeedbackController(Database database, TelegramBot telegramBot)
    {
        _database = database;
        _telegramBot = telegramBot;
    }
    [HttpPost]
    public async Task<IActionResult> Submit(string name, string message, bool anonymous)
    {
        // Если выбрана анонимность, используем "Анонимный пользователь"
        if (anonymous)
        {
            name = "Анонимный пользователь";
        }

        // Сохранение сообщения в файл
        var feedbackMessage = $"Имя: {name}\nСообщение: {message}\n\n";
        System.IO.File.AppendAllText("feedback.txt", feedbackMessage);

        // Отправка сообщения в Telegram
        var telegramMessage = $"Новое обращение:\nИмя: {name}\nСообщение: {message}";
        await _telegramBot.SendMessageToChatAsync("-1002686774060", telegramMessage);

        // Перенаправление на страницу с благодарностью
        return RedirectToAction("ThankYou");
    }

    public IActionResult ThankYou()
    {
        return View();
    }
}