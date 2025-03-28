using CorporatePortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Telegram.Bot;

public class LunchController : Controller
{
    private readonly Database _database;
    private readonly TelegramBot _telegramBot;

    public LunchController(Database database, TelegramBot telegramBot)
    {
        _database = database;
        _telegramBot = telegramBot;
    }

    /// <summary>
    /// Отображает страницу для заказа обедов.
    /// </summary>
    [HttpGet]
    public IActionResult Lunch()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var orders = _database.GetTodayOrders().Where(o => o.EmployeeId == userId).ToList();

        ViewBag.HasOrder = orders.Any(); // Передаем информацию о наличии заказа
        ViewBag.IsOrderAvailable = IsOrderAvailable(); // Передаем информацию о доступности заказов
        ViewBag.IsSunday = DateTime.Today.DayOfWeek == DayOfWeek.Sunday; // Передаем информацию, если сегодня воскресенье
        return View(orders);
    }
    private bool IsOrderAvailable()
    {
        var today = DateTime.Today.DayOfWeek;

        // Заказы недоступны в пятницу и субботу
        if (today == DayOfWeek.Friday || today == DayOfWeek.Saturday)
        {
            return false;
        }

        // В воскресенье заказы доступны для понедельника
        return true;
    }
    /// <summary>
    /// Обрабатывает заказ обеда.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(int portions)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = _database.GetUserById(userId);

        if (user == null)
        {
            return RedirectToAction("Error", "Home");
        }

        var order = new LunchOrder
        {
            EmployeeId = userId,
            FullName = user.FullName,
            Portions = portions,
            OrderDate = DateTime.Now
        };

        // Сохраняем заказ в базу данных
        SaveOrder(order);

        return RedirectToAction("Lunch", "Lunch");
    }

    /// <summary>
    /// Сохраняет заказ в базу данных.
    /// </summary>
    private void SaveOrder(LunchOrder order)
    {
        using (var connection = new SqliteConnection(Database.ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO LunchOrders (EmployeeId, FullName, Portions, OrderDate)
                VALUES ($employeeId, $fullName, $portions, $orderDate);";
            command.Parameters.AddWithValue("$employeeId", order.EmployeeId);
            command.Parameters.AddWithValue("$fullName", order.FullName);
            command.Parameters.AddWithValue("$portions", order.Portions);
            command.Parameters.AddWithValue("$orderDate", order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Получает список заказов на сегодня.
    /// </summary>
    public List<LunchOrder> GetTodayOrders()
    {
        var today = DateTime.Now.Date;
        using (var connection = new SqliteConnection(Database.ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM LunchOrders
                WHERE date(OrderDate) = date($today);";
            command.Parameters.AddWithValue("$today", today.ToString("yyyy-MM-dd"));

            var orders = new List<LunchOrder>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    orders.Add(new LunchOrder
                    {
                        Id = reader.GetInt32(0),
                        EmployeeId = reader.GetInt32(1),
                        FullName = reader.GetString(2),
                        Portions = reader.GetInt32(3),
                        OrderDate = DateTime.Parse(reader.GetString(4))
                    });
                }
            }
            return orders;
        }
    }

    /// <summary>
    /// Очищает заказы за сегодня.
    /// </summary>
    public void ClearTodayOrders()
    {
        var today = DateTime.Now.Date;
        using (var connection = new SqliteConnection(Database.ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM LunchOrders
                WHERE date(OrderDate) = date($today);";
            command.Parameters.AddWithValue("$today", today.ToString("yyyy-MM-dd"));
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Отправляет список заказов в Telegram.
    /// </summary>
    private async Task SendOrdersToTelegramAsync(List<LunchOrder> orders)
    {
        await _telegramBot.SendOrdersToTelegramAsync(orders);
    }

    /// <summary>
    /// Сбрасывает заказы и отправляет список в Telegram.
    /// </summary>
    public async Task ResetOrdersAsync()
    {
        var orders = GetTodayOrders();
        if (orders.Any())
        {
            await SendOrdersToTelegramAsync(orders);
        }
        ClearTodayOrders();
    }
    [HttpPost]
    public IActionResult EditOrder(int portions)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var order = _database.GetTodayOrders().FirstOrDefault(o => o.EmployeeId == userId);

        if (order != null)
        {
            if (portions <= 0)
            {
                _database.DeleteOrder(order.Id); // Удаляем заказ, если порций 0
            }
            else
            {
                order.Portions = portions;
                _database.UpdateOrder(order); // Обновляем заказ
            }
        }

        return RedirectToAction("Lunch");
    }
}