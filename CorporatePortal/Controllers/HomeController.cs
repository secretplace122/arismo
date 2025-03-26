using CorporatePortal.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Security.Claims;
using System.Text;

[Authorize]
public class HomeController : Controller
{
    private readonly Database _database;

    public HomeController(Database database)
    {
        _database = database;
    }
    public IActionResult Index() // �������
    {
        return View();
    }
    [Authorize]
    [Authorize]
    public IActionResult PersonalCabinet()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return RedirectToAction("Error", "Home");
        }

        var user = _database.GetUserById(userId);
        if (user == null)
        {
            return RedirectToAction("Error", "Home");
        }

        var completedTests = _database.GetCompletedTests(userId);
        var workSchedule = _database.GetWorkSchedule(userId);

        // �������� ���������� ������ ������������
        var testResults = _database.GetTestResultsByUser(userId);
        int? testPercentage = null;

        if (testResults != null && testResults.Any())
        {
            double totalPercentage = 0;
            int count = 0;

            foreach (var testResult in testResults)
            {
                if (testResult.IsCompleted)
                {
                    var maxScore = testResult.Test?.Questions?.Count * 5 ?? 15;
                    if (maxScore > 0)
                    {
                        totalPercentage += (double)testResult.Score / maxScore * 100;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                testPercentage = (int)Math.Round(totalPercentage / count);
            }
        }

        var model = new ProfileViewModel
        {
            FullName = user.FullName ?? "�� �������",
            CompletedTests = completedTests ?? new List<string>(),
            TotalPoints = user.KnowledgePoints,
            CurrentTime = DateTime.Now.ToString("HH:mm:ss"),
            WorkSchedule = workSchedule,
            WorkScheduleType = user.WorkScheduleType,
            ShiftGroup = user.ShiftGroup,
            ScheduleStartDate = user.ScheduleStartDate,
            TestPercentage = testPercentage
        };

        return View(model);
    }

    public IActionResult Knowledge_Base() // ���� ������
    {
        return View();
    }

    public IActionResult Test1() // ���� 1 ���������� � ��������
    {
        return View();
    }
    public IActionResult Equipment() // �����������
    {
        return View();
    }
    public IActionResult Device() // ������������
    {
        return View();
    }
    public IActionResult Contacts() // ��������
    {
        return View();
    }
    public IActionResult FireSafety() // ���� �������� ������������
    {
        return View();
    }
    public IActionResult Safety() // ���� ������������
    {
        return View();
    }
    [HttpGet]
    public IActionResult Feedback()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = int.Parse(userIdClaim);
        var user = _database.GetUserById(userId);

        if (user == null)
        {
            user = new User { FullName = "����������� ������������" };
        }

        return View(user);
    }
    public IActionResult Testpage()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var tests = _database.GetAvailableTests(userId);
        return View(tests);
    }

    [HttpGet]
    public IActionResult StartTest(int testId)
    {
        if (testId <= 0)
        {
            return RedirectToAction("Error", new { message = "�������� ID �����" });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var resultId = _database.StartTest(userId, testId);

        // ���������� ����� �������� ���������� ��������
        return RedirectToAction("Test", "Home", new
        {
            id = testId,  // �������� �������� - ���������� "id" ������ "testId"
            resultId = resultId
        });
    }

    public IActionResult Test(int id, int resultId)
    {
        Console.WriteLine($"������� ������ �� ����: ID={id}, ResultID={resultId}");

        var test = _database.GetTestWithQuestions(id);
        if (test == null)
        {
            Console.WriteLine($"���� � ID {id} �� ������ � ���� ������");
            return RedirectToAction("Error", new { message = "���� �� ������" });
        }

        var result = _database.GetTestResult(resultId);
        if (result == null)
        {
            Console.WriteLine($"��������� ����� � ID {resultId} �� ������");
            return RedirectToAction("Error", new { message = "��������� ����� �� ������" });
        }

        if (result.IsCompleted || result.IsFailedByTime)
        {
            return RedirectToAction("TestResults", new { resultId = resultId });
        }

        var timeLeft = result.StartTime.AddMinutes(test.TimeLimitMinutes) - DateTime.Now;

        return View(new TestViewModel
        {
            Test = test,
            ResultId = resultId,
            TimeLeft = timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero
        });
    }

    [HttpPost]
    public IActionResult SubmitTest(int resultId, IFormCollection form)
    {
        var result = _database.GetTestResult(resultId);
        if (result == null) return RedirectToAction("Error");

        var test = _database.GetTestWithQuestions(result.TestId);
        if (test == null) return RedirectToAction("Error");

        // �������� �������
        bool isFailedByTime = DateTime.Now > result.StartTime.AddMinutes(test.TimeLimitMinutes);

        int score = 0;
        if (!isFailedByTime)
        {
            foreach (var question in test.Questions)
            {
                var answerKey = $"answers[{question.Id}]";
                if (form.TryGetValue(answerKey, out var answerValue))
                {
                    if (int.TryParse(answerValue, out int selectedAnswerId))
                    {
                        if (question.Answers.Any(a => a.Id == selectedAnswerId && a.IsCorrect))
                        {
                            score += 5;
                        }
                    }
                }
            }
        }

        _database.CompleteTest(resultId, score, isFailedByTime);
        return RedirectToAction("TestResults", new { resultId });
    }
    public IActionResult TestResults(int resultId)
    {
        var result = _database.GetTestResult(resultId);
        if (result == null) return RedirectToAction("Error");

        var test = _database.GetTestWithQuestions(result.TestId);
        if (test == null) return RedirectToAction("Error");

        int maxScore = test.Questions.Count * 5;
        int percentage = maxScore > 0 ? (int)Math.Round((double)result.Score / maxScore * 100) : 0;

        return View(new TestResultsViewModel
        {
            TestName = test.Name,
            Score = result.Score,
            MaxScore = maxScore,
            Percentage = percentage,
            IsFailedByTime = result.IsFailedByTime,
            CompletionTime = result.EndTime.HasValue
                ? result.EndTime.Value - result.StartTime
                : TimeSpan.Zero
        });
    }
}
