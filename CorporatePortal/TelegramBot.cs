using CorporatePortal.Models;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using System.Threading.Tasks;

public class TelegramBot
{
    private readonly ITelegramBotClient _botClient;
    private readonly string _chatId = "-1002358092301"; // ID чата для уведомлений
    private readonly UserTokenManager _tokenManager;
    private readonly Database _database;
    private readonly List<long> _allowedUserIds = new List<long> { 1251675837 }; // Разрешённые ID пользователей

    public TelegramBot(string token)
    {
        _botClient = new TelegramBotClient(token);
        _tokenManager = new UserTokenManager(Path.Combine("App_Data", "user_token.json"));
        _database = new Database();
    }

    /// <summary>
    /// Запускает бота и начинает прослушивание сообщений.
    /// </summary>
    public async Task StartAsync()
    {
        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"Бот {me.Username} запущен!");

        // Запускаем фоновую задачу для уведомлений о перерывах
        _ = Task.Run(() => SendBreakNotificationsAsync(_botClient, _chatId, CancellationToken.None));

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() }
        );

        Console.WriteLine("Бот готов к работе.");
    }

    /// <summary>
    /// Обрабатывает входящие обновления (сообщения, callback-запросы).
    /// </summary>
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Проверяем, что сообщение пришло из личного чата
        if (update.Message?.Type == MessageType.Text && update.Message.Chat.Type == ChatType.Private)
        {
            var userId = update.Message.From.Id;
            var chatId = update.Message.Chat.Id;
            Console.WriteLine($"ID пользователя: {userId}");
            var messageText = update.Message.Text.ToLowerInvariant().Trim();

            if (_tokenManager.IsUserAuthorized(userId))
            {
                await HandleAuthorizedUser(botClient, update.Message, cancellationToken);
            }
            else
            {
                await HandleUnauthorizedUser(botClient, update.Message, cancellationToken);
            }
        }
        else if (update.CallbackQuery != null && update.CallbackQuery.Message.Chat.Type == ChatType.Private)
        {
            // Обрабатываем callback-запросы только из личных сообщений
            await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
        }
        else
        {
            // Игнорируем сообщения из групповых чатов
            Console.WriteLine("Сообщение из группового чата проигнорировано.");
        }
    }

    /// <summary>
    /// Обрабатывает сообщения от авторизованных пользователей.
    /// </summary>
    private async Task HandleAuthorizedUser(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var userId = message.From.Id;
        var messageText = message.Text.ToLowerInvariant().Trim();

        if (messageText == "/start" || messageText == "старт" || messageText == "меню")
        {
            await ShowMainMenuAsync(chatId, cancellationToken);
        }
        else if (_allowedUserIds.Contains(userId))
        {
            await HandleAdminCommands(botClient, message, cancellationToken);
        }
    }

    /// <summary>
    /// Обрабатывает команды администратора.
    /// </summary>
    private async Task HandleAdminCommands(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var messageText = message.Text.Trim();
        var parts = messageText.Split(' ');

        if (parts[0] == "/adduser" && parts.Length >= 5)
        {
            var username = parts[1];
            var password = parts[2];
            var knowledgePoints = int.Parse(parts[3]);
            var fullName = string.Join(" ", parts.Skip(4)); // parts[4..] объединяем в одно значение

            _database.AddUser(username, password, knowledgePoints, fullName);
            await botClient.SendTextMessageAsync(chatId, "Пользователь добавлен.", cancellationToken: cancellationToken);
        }
        else if (parts[0] == "/deleteuser" && parts.Length == 2)
        {
            var userId = int.Parse(parts[1]);
            _database.DeleteUser(userId);
            await botClient.SendTextMessageAsync(chatId, "Пользователь удалён.", cancellationToken: cancellationToken);
        }
        else if (parts[0] == "/updateuser" && parts.Length == 4)
        {
            var userId = int.Parse(parts[1]);
            var newPassword = parts[2];
            var newKnowledgePoints = int.Parse(parts[3]);
            _database.UpdateUser(userId, newPassword, newKnowledgePoints);
            await botClient.SendTextMessageAsync(chatId, "Пользователь обновлён.", cancellationToken: cancellationToken);
        }
        else if (parts[0] == "/getuser" && parts.Length == 2)
        {
            var userId = int.Parse(parts[1]);
            var user = _database.GetUserById(userId);
            if (user != null)
            {
                await botClient.SendTextMessageAsync(chatId, $"Пользователь: {user.Username}, Полное имя: {user.FullName}, Очки: {user.KnowledgePoints}", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Пользователь не найден.", cancellationToken: cancellationToken);
            }
        }
        else if (parts[0] == "/getallusers")
        {
            var users = _database.GetAllUsers();
            var userList = string.Join("\n", users.Select(u => $"{u.Id}: {u.Username} ({u.FullName}) - Баллы: {u.KnowledgePoints}"));
            await botClient.SendTextMessageAsync(chatId, $"Список пользователей:\n{userList}", cancellationToken: cancellationToken);
        }
        else if (parts[0] == "/updatepoints" && parts.Length == 3)
        {
            var userId = int.Parse(parts[1]);
            var points = int.Parse(parts[2]);
            _database.UpdateKnowledgePoints(userId, points);
            await botClient.SendTextMessageAsync(chatId, "Очки обновлены.", cancellationToken: cancellationToken);
        }
        else if (parts[0] == "/a")
        {
            await ShowAdminHelp(chatId, cancellationToken);
        }
        else if (parts[0] == "/setschedule" && parts.Length == 4)
        {
            var userId = int.Parse(parts[1]);
            var scheduleType = parts[2]; // "5/2" или "2/2"
            var shiftGroup = int.Parse(parts[3]); // 1 или 2

            if ((scheduleType == "5/2" || scheduleType == "2/2") && (shiftGroup == 1 || shiftGroup == 2))
            {
                _database.UpdateWorkSchedule(userId, scheduleType, shiftGroup);
                await botClient.SendTextMessageAsync(chatId, "График работы обновлен.", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Неверные параметры графика. Используйте: /setschedule <userId> <5/2|2/2> <1|2>", cancellationToken: cancellationToken);
            }
        }
        else if (parts[0] == "/getschedule" && parts.Length == 2)
        {
            var userId = int.Parse(parts[1]);
            var user = _database.GetUserById(userId);
            if (user != null)
            {
                await botClient.SendTextMessageAsync(chatId,
                    $"График пользователя {user.FullName}:\n" +
                    $"Тип: {user.WorkScheduleType}\n" +
                    $"Группа: {user.ShiftGroup}\n" +
                    $"Дата начала: {user.ScheduleStartDate}",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Пользователь не найден.", cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Неизвестная команда.", cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Показывает список всех команд администратора.
    /// </summary>
    private async Task ShowAdminHelp(long chatId, CancellationToken cancellationToken)
    {
        var helpMessage = @"
Список команд администратора:
/adduser <username> <password> <knowledgePoints> <FullName>- Добавить пользователя
/deleteuser <userId> - Удалить пользователя
/updateuser <userId> <newPassword> <newKnowledgePoints> - Обновить пользователя
/getuser <userId> - Получить информацию о пользователе
/getallusers - Получить список всех пользователей
/updatepoints <userId> <points> - Обновить очки пользователя
/setschedule <userId> <5/2|2/2> <1|2> - Установить график работы
/getschedule <userId> - Получить график работы пользователя
/a - Показать список команд
";

        await _botClient.SendTextMessageAsync(chatId, helpMessage, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обрабатывает сообщения от неавторизованных пользователей.
    /// </summary>
    private async Task HandleUnauthorizedUser(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From.Id;
        var chatId = message.Chat.Id;
        var messageText = message.Text.ToLowerInvariant().Trim();

        if (messageText == "/start" || messageText == "старт")
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Для работы с ботом требуется уникальный токен. Пожалуйста, введите ваш токен:",
                cancellationToken: cancellationToken);
        }
        else if (_tokenManager.IsTokenValid(messageText))
        {
            if (_tokenManager.IsTokenAlreadyUsed(messageText))
            {
                await botClient.SendTextMessageAsync(chatId, "Этот токен уже использован другим пользователем.", cancellationToken: cancellationToken);
            }
            else
            {
                _tokenManager.AuthorizeUser(userId, messageText);
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Старт", "start")
                });

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Авторизация прошла успешно! Нажмите кнопку 'Старт', чтобы продолжить.",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный токен. Пожалуйста, попробуйте еще раз.", cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Показывает главное меню пользователю.
    /// </summary>
    private async Task ShowMainMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("Чат", "https://t.me/+8EgUeXk-K8pmZGMy"),
                InlineKeyboardButton.WithWebApp("Открыть Mini App", new WebAppInfo
                {
                    Url = "https://cunningly-skilled-pup.cloudpub.ru/"
                })
            }
        });

        var photoStream = new FileStream(Path.Combine("wwwroot", "images", "logo_arismo.jpg"), FileMode.Open);
        await _botClient.SendPhotoAsync(chatId, photoStream, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        await photoStream.DisposeAsync();
    }

    /// <summary>
    /// Обрабатывает callback-запросы (например, нажатие кнопки "Старт").
    /// </summary>
    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var callbackData = callbackQuery.Data;

        if (callbackData == "start")
        {
            await ShowMainMenuAsync(chatId, cancellationToken);
        }

        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обрабатывает ошибки при прослушивании обновлений.
    /// </summary>
    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Отправляет уведомления о перерывах.
    /// </summary>
    private async Task SendBreakNotificationsAsync(ITelegramBotClient bot, string chatId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var jsonFilePath = Path.Combine("App_Data", "breaks.json");
                if (!File.Exists(jsonFilePath))
                {
                    Console.WriteLine("Файл breaks.json не найден.");
                    await Task.Delay(60000, cancellationToken);
                    continue;
                }

                var json = File.ReadAllText(jsonFilePath);
                var breaksData = JsonConvert.DeserializeObject<BreaksData>(json);

                if (breaksData == null || breaksData.Breaks == null)
                {
                    Console.WriteLine("Неверный формат breaks.json.");
                    await Task.Delay(60000, cancellationToken);
                    continue;
                }

                var now = DateTime.Now.ToString("HH:mm");

                foreach (var breakInfo in breaksData.Breaks)
                {
                    if (breakInfo.Time == now)
                    {
                        await bot.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"{breakInfo.Description}",
                            cancellationToken: cancellationToken);

                        Console.WriteLine($"Сообщение отправлено: {breakInfo.Description}");
                    }
                }

                await Task.Delay(60000, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SendBreakNotificationsAsync: {ex.Message}");
                await Task.Delay(60000, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Отправляет список заказов на обед в Telegram-группу.
    /// </summary>
    public async Task SendOrdersToTelegramAsync(List<LunchOrder> orders)
    {
        var message = "Список заказов на обед:\n";
        foreach (var order in orders)
        {
            message += $"{order.FullName} — {order.Portions} порция(и)\n";
        }

        await _botClient.SendTextMessageAsync(_chatId, message);
    }

    public async Task SendMessageToChatAsync(string chatId, string message)
    {
        await _botClient.SendTextMessageAsync(chatId, message);
    }
}

/// <summary>
/// Управляет токенами пользователей.
/// </summary>
public class UserTokenManager
{
    private readonly string _filePath;
    private Dictionary<string, long> _tokens;

    public UserTokenManager(string filePath)
    {
        _filePath = filePath;
        LoadTokens();
    }

    /// <summary>
    /// Проверяет, авторизован ли пользователь.
    /// </summary>
    public bool IsUserAuthorized(long userId) => _tokens.Values.Contains(userId);

    /// <summary>
    /// Проверяет, действителен ли токен.
    /// </summary>
    public bool IsTokenValid(string token) => _tokens.ContainsKey(token) && _tokens[token] == 0;

    /// <summary>
    /// Проверяет, использовался ли токен ранее.
    /// </summary>
    public bool IsTokenAlreadyUsed(string token) => _tokens.ContainsKey(token) && _tokens[token] != 0;

    /// <summary>
    /// Авторизует пользователя по токену.
    /// </summary>
    public void AuthorizeUser(long userId, string token)
    {
        if (_tokens.ContainsKey(token) && _tokens[token] == 0)
        {
            _tokens[token] = userId;
            SaveTokens();
        }
        else
        {
            throw new InvalidOperationException("Токен уже использован или не найден.");
        }
    }

    /// <summary>
    /// Загружает токены из файла.
    /// </summary>
    private void LoadTokens()
    {
        if (File.Exists(_filePath))
        {
            var jsonContent = File.ReadAllText(_filePath);
            _tokens = JsonConvert.DeserializeObject<Dictionary<string, long>>(jsonContent) ?? new Dictionary<string, long>();
        }
        else
        {
            _tokens = new Dictionary<string, long>();
        }
    }

    /// <summary>
    /// Сохраняет токены в файл.
    /// </summary>
    private void SaveTokens()
    {
        var jsonContent = JsonConvert.SerializeObject(_tokens, Formatting.Indented);
        File.WriteAllText(_filePath, jsonContent);
    }
}

/// <summary>
/// Представляет данные о перерывах.
/// </summary>
public class BreaksData
{
    public List<BreakInfo> Breaks { get; set; }
}

/// <summary>
/// Представляет информацию о перерыве.
/// </summary>
public class BreakInfo
{
    public string Time { get; set; }
    public string Description { get; set; }
}