namespace CorporatePortal.Models

{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string WorkScheduleType { get; set; } = "2/2"; // '5/2' или '2/2'
        public int ShiftGroup { get; set; } = 1; // 1 или 2
        public string ScheduleStartDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public int KnowledgePoints { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class Test
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int MaxScore { get; set; }
        public List<Question> Questions { get; set; }
    }

    public class Question
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public string Text { get; set; }
        public List<Answer> Answers { get; set; }
    }

    public class Answer
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class TestResult
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TestId { get; set; }
        public int Score { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsFailedByTime { get; set; }
        public Test Test { get; set; }
    }

    public class Schedule
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string WorkDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class ProfileViewModel
    {
        public string FullName { get; set; }
        public List<string> CompletedTests { get; set; }
        public int? TotalPoints { get; set; }
        public string CurrentTime { get; set; }
        public string WorkSchedule { get; set; }
        public string WorkScheduleType { get; set; }
        public int? ShiftGroup { get; set; }
        public string ScheduleStartDate { get; set; }
        public int? TestPercentage { get; set; }
    }
    public class BotConfig
    {
        public string Token { get; set; }
    }
    public class LunchOrder
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; } 
        public string FullName { get; set; }
        public int Portions { get; set; } // Количество порций
        public DateTime OrderDate { get; set; } // Дата заказа
    }
}
