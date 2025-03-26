using CorporatePortal.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class ProfileController : Controller
{
    private readonly Database _database;

    public ProfileController(Database database)
    {
        _database = database;
    }

    public IActionResult Index()
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

        var model = new ProfileViewModel
        {
            FullName = user.FullName ?? "Не указано",
            CompletedTests = completedTests ?? new List<string>(),
            TotalPoints = user.KnowledgePoints,
            CurrentTime = DateTime.Now.ToString("HH:mm:ss"),
            WorkSchedule = workSchedule,
            WorkScheduleType = user.WorkScheduleType,
            ShiftGroup = user.ShiftGroup,
            ScheduleStartDate = user.ScheduleStartDate
        };

        return View(model);
    }
}