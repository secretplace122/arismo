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

        // Получаем информацию о дате доставки
        var dateInfo = GetOrderTargetDateInfo();

        ViewBag.IsOrderAvailable = IsOrderAvailable();
        ViewBag.CurrentDay = DateTime.Now.DayOfWeek;
        ViewBag.CurrentHour = DateTime.Now.Hour;
        ViewBag.OrderDate = dateInfo.Date;  // Формат "dd.MM.yyyy"
        ViewBag.OrderDay = dateInfo.DayOfWeek; // "понедельник", "вторник" и т.д.

        return View(orders);
    }
    private bool IsOrderAvailable()
    {

        // === ТЕСТОВЫЕ ПЕРЕМЕННЫЕ ===
        //DayOfWeek testDay = DayOfWeek.Thursday; // День недели для теста
        //int testHour = 16;                     // Час для теста (0-23)
        // === ВРЕМЕННАЯ ЗАМЕНА ДАТЫ ===
        //var now = new DateTime(2024, 7, 13 + (int)testDay, testHour, 0, 0);
        // Раскомментируйте для боевого режима:


        var now = DateTime.Now; // Коммент для теста
        var currentDay = now.DayOfWeek;
        var currentHour = now.Hour;

        // Полностью недоступные дни
        if (currentDay == DayOfWeek.Friday || currentDay == DayOfWeek.Saturday)
        {
            return false;
        }

        // В четверг после 17:00 - недоступно
        if (currentDay == DayOfWeek.Thursday && currentHour >= 17)
        {
            return false;
        }

        // В воскресенье после 17:00 - недоступно ( для того что бы не заказывали на вторник(может уберу в последующем))
        if (currentDay == DayOfWeek.Sunday && currentHour >= 17)
        {
            return false;
        }

        // Все остальные случаи - доступно
        return true;
    }

    /// <summary>
    /// Обрабатывает заказ обеда.
    /// </summary>
    [HttpPost]
    public IActionResult PlaceOrder(int portions)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = _database.GetUserById(userId);

        if (user == null) return RedirectToAction("Error", "Home");

        // Получаем текущий заказ (если есть)
        var existingOrder = _database.GetTodayOrders()
            .FirstOrDefault(o => o.EmployeeId == userId);

        // Если порций 0 - удаляем заказ
        if (portions <= 0)
        {
            if (existingOrder != null)
            {
                _database.DeleteOrder(existingOrder.Id);
            }
            return RedirectToAction("Lunch");
        }

        // Если заказ уже существует - обновляем
        if (existingOrder != null)
        {
            existingOrder.Portions = portions;
            _database.UpdateOrder(existingOrder);
            return RedirectToAction("Lunch");
        }

        // Создаем новый заказ
        var deliveryDate = CalculateDeliveryDate(DateTime.Now);

        var newOrder = new LunchOrder
        {
            EmployeeId = userId,
            FullName = user.FullName,
            Portions = portions,
            OrderDate = DateTime.Now,
            DeliveryDate = deliveryDate
        };

        _database.AddOrder(newOrder);
        return RedirectToAction("Lunch");
    }

    /// <summary>
    /// Единственный метод расчета даты доставки
    /// </summary>
    private DateTime CalculateDeliveryDate(DateTime orderDate)
    {
        var deliveryDate = orderDate.AddDays(1);

        // Если заказ после 17:00 - добавляем дополнительный день
        if (orderDate.Hour >= 17)
        {
            deliveryDate = deliveryDate.AddDays(1);
        }

        // Пропускаем выходные (суббота и воскресенье)
        while (deliveryDate.DayOfWeek == DayOfWeek.Saturday ||
               deliveryDate.DayOfWeek == DayOfWeek.Sunday)
        {
            deliveryDate = deliveryDate.AddDays(1);
        }

        return deliveryDate.Date;
    }

    /// <summary>
    /// Сохраняет заказ в базу данных.
    /// </summary>
    private void SaveOrder(LunchOrder order)
    {
        var now = DateTime.Now;
        var deliveryDate = CalculateDeliveryDate(now);

        using (var connection = new SqliteConnection(Database.ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO LunchOrders 
                (EmployeeId, FullName, Portions, OrderDate, DeliveryDate)
            VALUES 
                ($employeeId, $fullName, $portions, $orderDate, $deliveryDate);";

            command.Parameters.AddWithValue("$employeeId", order.EmployeeId);
            command.Parameters.AddWithValue("$fullName", order.FullName);
            command.Parameters.AddWithValue("$portions", order.Portions);
            command.Parameters.AddWithValue("$orderDate", now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$deliveryDate", deliveryDate.ToString("yyyy-MM-dd"));

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
    private (string Date, string DayOfWeek) GetOrderTargetDateInfo()
    {
        var now = DateTime.Now;
        var targetDate = now;

        // Воскресенье - заказ на понедельник
        if (now.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDate = now.AddDays(1);
        }
        // Рабочие дни до 17:00 - на завтра
        else if (now.Hour < 17)
        {
            targetDate = now.AddDays(1);
        }
        // Рабочие дни после 17:00 - на послезавтра
        else
        {
            targetDate = now.AddDays(2);
        }

        // Пропускаем выходные (если попали на субботу)
        if (targetDate.DayOfWeek == DayOfWeek.Saturday)
        {
            targetDate = targetDate.AddDays(2);
        }
        else if (targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDate = targetDate.AddDays(1);
        }

        var dayOfWeek = targetDate.DayOfWeek switch
        {
            DayOfWeek.Monday => "понедельник",
            DayOfWeek.Tuesday => "вторник",
            DayOfWeek.Wednesday => "среду",
            DayOfWeek.Thursday => "четверг",
            DayOfWeek.Friday => "пятницу",
            _ => ""
        };

        return (targetDate.ToString("dd.MM.yyyy"), dayOfWeek);
    }
}