using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class LunchOrderBackgroundService : BackgroundService
{
    private readonly Database _database;
    private readonly TelegramBot _telegramBot;

    public LunchOrderBackgroundService(Database database, TelegramBot telegramBot)
    {
        _database = database;
        _telegramBot = telegramBot;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var targetTime = new DateTime(now.Year, now.Month, now.Day, 17, 00, 0); // 17:00

            if (now > targetTime)
            {
                targetTime = targetTime.AddDays(1); // Если уже прошло 17:00, планируем на следующий день
            }

            var delay = targetTime - now;
            await Task.Delay(delay, stoppingToken);

            // Выполняем задачу в 17:00
            await SendOrdersAndResetAsync();
        }
    }

    private async Task SendOrdersAndResetAsync()
    {
        var orders = _database.GetTodayOrders();
        if (orders.Any())
        {
            await _telegramBot.SendOrdersToTelegramAsync(orders);
            _database.ClearTodayOrders();
        }
    }
}