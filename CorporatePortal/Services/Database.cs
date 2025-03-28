using CorporatePortal.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public class Database
{
    public const string ConnectionString = "Data Source=App_Data/databaseusers.db";

    public void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();

            // Таблица пользователей
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    KnowledgePoints INTEGER DEFAULT 0,
                    FullName TEXT NOT NULL,
                    IsActive BOOLEAN DEFAULT TRUE
                );";
            command.ExecuteNonQuery();
        }
    }

    // ============ МЕТОДЫ ДЛЯ РАБОТЫ С ПОЛЬЗОВАТЕЛЯМИ ============

    public void AddUser(string username, string password, int knowledgePoints = 0, string fullName = null)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (Username, Password, KnowledgePoints, FullName, IsActive)
                VALUES ($username, $password, $knowledgePoints, $fullName, TRUE);";
            command.Parameters.AddWithValue("$username", username);
            command.Parameters.AddWithValue("$password", password);
            command.Parameters.AddWithValue("$knowledgePoints", knowledgePoints);
            command.Parameters.AddWithValue("$fullName", fullName ?? string.Empty);
            command.ExecuteNonQuery();
        }
    }

    public void DeleteUser(int userId)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET IsActive = FALSE WHERE Id = $userId;";
            command.Parameters.AddWithValue("$userId", userId);
            command.ExecuteNonQuery();
        }
    }

    public void UpdateUser(int userId, string newPassword = null, int? newKnowledgePoints = null)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Users SET ";

            if (!string.IsNullOrEmpty(newPassword))
            {
                command.CommandText += "Password = $newPassword, ";
                command.Parameters.AddWithValue("$newPassword", newPassword);
            }

            if (newKnowledgePoints.HasValue)
            {
                command.CommandText += "KnowledgePoints = $newKnowledgePoints, ";
                command.Parameters.AddWithValue("$newKnowledgePoints", newKnowledgePoints.Value);
            }

            command.CommandText = command.CommandText.TrimEnd(',', ' ');
            command.CommandText += " WHERE Id = $userId;";
            command.Parameters.AddWithValue("$userId", userId);
            command.ExecuteNonQuery();
        }
    }

    public List<User> GetAllUsers()
    {
        var users = new List<User>();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Username, Password, KnowledgePoints, FullName FROM Users WHERE IsActive = TRUE;";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Password = reader.GetString(2),
                        KnowledgePoints = reader.GetInt32(3),
                        FullName = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
            }
        }
        return users;
    }

    public User AuthenticateUser(string username, string password)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Username, Password, KnowledgePoints, FullName, IsActive
                FROM Users
                WHERE Username = $username AND IsActive = TRUE;";
            command.Parameters.AddWithValue("$username", username);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    var storedPassword = reader.GetString(2);
                    if (password == storedPassword)
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Password = storedPassword,
                            KnowledgePoints = reader.GetInt32(3),
                            FullName = reader.GetString(4),
                            IsActive = reader.GetBoolean(5)
                        };
                    }
                }
            }
        }
        return null;
    }

    public void UpdateKnowledgePoints(int userId, int knowledgePoints)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Users
                SET KnowledgePoints = $knowledgePoints
                WHERE Id = $userId;";
            command.Parameters.AddWithValue("$knowledgePoints", knowledgePoints);
            command.Parameters.AddWithValue("$userId", userId);
            command.ExecuteNonQuery();
        }
    }

    public int GetKnowledgePoints(int userId)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT KnowledgePoints
                FROM Users
                WHERE Id = $userId;";
            command.Parameters.AddWithValue("$userId", userId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }
        }
        return 0;
    }

    public User GetUserById(int userId)
    {
        try
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT Id, Username, Password, FullName, 
                       WorkScheduleType, ShiftGroup, ScheduleStartDate,
                       KnowledgePoints, IsActive
                FROM Users
                WHERE Id = $userId;";
                command.Parameters.AddWithValue("$userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Password = reader.GetString(2),
                            FullName = reader.GetString(3),
                            WorkScheduleType = reader.GetString(4),
                            ShiftGroup = reader.GetInt32(5),
                            ScheduleStartDate = reader.GetString(6),
                            KnowledgePoints = reader.GetInt32(7),
                            IsActive = reader.GetBoolean(8)
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении пользователя: {ex.Message}");
        }
        return null;
    }
    public void UpdateWorkSchedule(int userId, string scheduleType, int shiftGroup)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Users
            SET WorkScheduleType = $scheduleType,
                ShiftGroup = $shiftGroup,
                ScheduleStartDate = $startDate
            WHERE Id = $userId;";
            command.Parameters.AddWithValue("$scheduleType", scheduleType);
            command.Parameters.AddWithValue("$shiftGroup", shiftGroup);
            command.Parameters.AddWithValue("$startDate", DateTime.Now.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$userId", userId);
            command.ExecuteNonQuery();
        }
    }

    // ============ МЕТОДЫ ДЛЯ РАБОТЫ С ТЕСТАМИ ============

    public int StartTest(int userId, int testId)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TestResults (UserId, TestId, StartTime)
                VALUES ($userId, $testId, $startTime);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$testId", testId);
            command.Parameters.AddWithValue("$startTime", DateTime.Now);

            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public void CompleteTest(int resultId, int score, bool isFailedByTime = false)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE TestResults
            SET Score = $score,
                EndTime = $endTime,
                IsCompleted = $isCompleted,
                IsFailedByTime = $isFailedByTime
            WHERE Id = $id";
            command.Parameters.AddWithValue("$score", score);
            command.Parameters.AddWithValue("$endTime", DateTime.Now);
            command.Parameters.AddWithValue("$isCompleted", !isFailedByTime);
            command.Parameters.AddWithValue("$isFailedByTime", isFailedByTime);
            command.Parameters.AddWithValue("$id", resultId);
            command.ExecuteNonQuery();

            // Начисляем баллы только если тест завершен успешно
            if (!isFailedByTime && score > 0)
            {
                command.CommandText = @"
                UPDATE Users
                SET KnowledgePoints = KnowledgePoints + $score
                WHERE Id = (SELECT UserId FROM TestResults WHERE Id = $id)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("$score", score);
                command.Parameters.AddWithValue("$id", resultId);
                command.ExecuteNonQuery();
            }
        }
    }

    public Test GetTestWithQuestions(int testId)
    {
        try
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                // 1. Получаем основной тест
                var test = GetTestById(connection, testId);
                if (test == null)
                {
                    Console.WriteLine($"Тест с ID {testId} не найден");
                    return null;
                }

                // 2. Получаем вопросы для теста
                test.Questions = GetQuestionsForTest(connection, testId);
                if (test.Questions == null || test.Questions.Count == 0)
                {
                    Console.WriteLine($"Для теста {testId} не найдено вопросов");
                    return test; // Возвращаем тест без вопросов
                }

                // 3. Получаем ответы для каждого вопроса
                foreach (var question in test.Questions)
                {
                    question.Answers = GetAnswersForQuestion(connection, question.Id);
                    if (question.Answers == null || question.Answers.Count == 0)
                    {
                        Console.WriteLine($"Для вопроса {question.Id} не найдено ответов");
                    }
                }

                return test;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении теста: {ex.Message}");
            return null;
        }
    }

    private Test GetTestById(SqliteConnection connection, int testId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description, TimeLimitMinutes, TotalQuestions, MaxScore FROM Tests WHERE Id = $id";
        command.Parameters.AddWithValue("$id", testId);

        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                return new Test
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    TimeLimitMinutes = reader.GetInt32(3),
                    TotalQuestions = reader.GetInt32(4),
                    MaxScore = reader.GetInt32(5),
                    Questions = new List<Question>() // Инициализация списка вопросов
                };
            }
        }
        return null;
    }

    private List<Question> GetQuestionsForTest(SqliteConnection connection, int testId)
    {
        var questions = new List<Question>();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, TestId, Text FROM Questions WHERE TestId = $testId";
        command.Parameters.AddWithValue("$testId", testId);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                questions.Add(new Question
                {
                    Id = reader.GetInt32(0),
                    TestId = reader.GetInt32(1),
                    Text = reader.GetString(2),
                    Answers = new List<Answer>() // Инициализация списка ответов
                });
            }
        }
        return questions;
    }

    private List<Answer> GetAnswersForQuestion(SqliteConnection connection, int questionId)
    {
        var answers = new List<Answer>();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, QuestionId, Text, IsCorrect FROM Answers WHERE QuestionId = $questionId";
        command.Parameters.AddWithValue("$questionId", questionId);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                answers.Add(new Answer
                {
                    Id = reader.GetInt32(0),
                    QuestionId = reader.GetInt32(1),
                    Text = reader.GetString(2),
                    IsCorrect = reader.GetBoolean(3)
                });
            }
        }
        return answers;
    }

    public TestResult GetTestResult(int resultId)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM TestResults
                WHERE Id = $id";
            command.Parameters.AddWithValue("$id", resultId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new TestResult
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        TestId = reader.GetInt32(2),
                        Score = reader.GetInt32(3),
                        StartTime = reader.GetDateTime(4),
                        EndTime = reader.IsDBNull(5) ? null : (DateTime?)reader.GetDateTime(5),
                        IsCompleted = reader.GetBoolean(6),
                        IsFailedByTime = reader.GetBoolean(7)
                    };
                }
            }
            return null;
        }
    }

    public List<Test> GetAvailableTests(int userId)
    {
        var tests = new List<Test>();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT t.* FROM Tests t
            WHERE NOT EXISTS (
                SELECT 1 FROM TestResults tr
                WHERE tr.TestId = t.Id AND tr.UserId = $userId AND (tr.IsCompleted = 1 OR tr.IsFailedByTime = 1)
            )";
            command.Parameters.AddWithValue("$userId", userId);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tests.Add(new Test
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        TimeLimitMinutes = reader.GetInt32(3),
                        TotalQuestions = reader.GetInt32(4),
                        MaxScore = reader.GetInt32(5)
                    });
                }
            }
        }
        return tests;
    }
    public List<TestResult> GetTestResultsByUser(int userId)
    {
        var results = new List<TestResult>();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT tr.*, t.Name, t.Description 
            FROM TestResults tr
            JOIN Tests t ON tr.TestId = t.Id
            WHERE tr.UserId = $userId";
            command.Parameters.AddWithValue("$userId", userId);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(new TestResult
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        TestId = reader.GetInt32(2),
                        Score = reader.GetInt32(3),
                        StartTime = reader.GetDateTime(4),
                        EndTime = reader.IsDBNull(5) ? null : (DateTime?)reader.GetDateTime(5),
                        IsCompleted = reader.GetBoolean(6),
                        IsFailedByTime = reader.GetBoolean(7),
                        Test = new Test
                        {
                            Id = reader.GetInt32(2),
                            Name = reader.GetString(8),
                            Description = reader.IsDBNull(9) ? null : reader.GetString(9)
                        }
                    });
                }
            }
        }
        return results;
    }
    public void ResetTestResults(int userId, int testId = 0)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();

            if (testId == 0)
            {
                // Сбросить все тесты пользователя
                command.CommandText = @"
                DELETE FROM TestResults 
                WHERE UserId = $userId";
            }
            else
            {
                // Сбросить конкретный тест пользователя
                command.CommandText = @"
                DELETE FROM TestResults 
                WHERE UserId = $userId AND TestId = $testId";
                command.Parameters.AddWithValue("$testId", testId);
            }

            command.Parameters.AddWithValue("$userId", userId);
            command.ExecuteNonQuery();
        }
    }
    public TestResult GetActiveTestResult(int userId, int testId)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT * FROM TestResults
            WHERE UserId = $userId AND TestId = $testId 
            AND IsCompleted = 0 AND IsFailedByTime = 0
            ORDER BY StartTime DESC
            LIMIT 1";
            command.Parameters.AddWithValue("$userId", userId);
            command.Parameters.AddWithValue("$testId", testId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new TestResult
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        TestId = reader.GetInt32(2),
                        Score = reader.GetInt32(3),
                        StartTime = reader.GetDateTime(4),
                        EndTime = reader.IsDBNull(5) ? null : (DateTime?)reader.GetDateTime(5),
                        IsCompleted = reader.GetBoolean(6),
                        IsFailedByTime = reader.GetBoolean(7)
                    };
                }
            }
            return null;
        }
    }
    // ============ МЕТОДЫ ДЛЯ РАБОТЫ С ОБЕДАМИ ============
    public void AddOrder(LunchOrder order)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO LunchOrders 
                (EmployeeId, FullName, Portions, OrderDate, DeliveryDate)
            VALUES 
                ($employeeId, $fullName, $portions, $orderDate, $deliveryDate)";

            command.Parameters.AddWithValue("$employeeId", order.EmployeeId);
            command.Parameters.AddWithValue("$fullName", order.FullName);
            command.Parameters.AddWithValue("$portions", order.Portions);
            command.Parameters.AddWithValue("$orderDate", order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$deliveryDate", order.DeliveryDate.ToString("yyyy-MM-dd"));

            command.ExecuteNonQuery();
        }
    }
    public List<LunchOrder> GetTodayOrders()
    {
        var today = DateTime.Now.Date;
        using (var connection = new SqliteConnection(ConnectionString))
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

    public void ClearTodayOrders()
    {
        var today = DateTime.Now.Date;
        using (var connection = new SqliteConnection(ConnectionString))
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

    public void DeleteOrder(int orderId)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM LunchOrders WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", orderId);
            command.ExecuteNonQuery();
        }
    }

    public void UpdateOrder(LunchOrder order)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE LunchOrders
                SET Portions = $portions
                WHERE Id = $id;";
            command.Parameters.AddWithValue("$portions", order.Portions);
            command.Parameters.AddWithValue("$id", order.Id);
            command.ExecuteNonQuery();
        }
    }

    // ============ МЕТОДЫ ДЛЯ РАБОТЫ С ГРАФИКОМ ============

    public string GetWorkSchedule(int userId)
    {
        try
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT WorkScheduleType, ShiftGroup
                FROM Users
                WHERE Id = $userId;";
                command.Parameters.AddWithValue("$userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return $"График: {reader.GetString(0)}, Смена: {reader.GetInt32(1)}";
                    }
                }
                return "График не указан";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении графика работы: {ex.Message}");
            return "Ошибка при получении графика";
        }
    }

    // ============ ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ============

    public List<string> GetCompletedTests(int userId)
    {
        try
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT t.Name 
                    FROM TestResults tr
                    JOIN Tests t ON tr.TestId = t.Id
                    WHERE tr.UserId = $userId AND tr.IsCompleted = 1";
                command.Parameters.AddWithValue("$userId", userId);

                var completedTests = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        completedTests.Add(reader.GetString(0));
                    }
                }
                return completedTests;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении пройденных тестов: {ex.Message}");
            return new List<string>();
        }
    }
}