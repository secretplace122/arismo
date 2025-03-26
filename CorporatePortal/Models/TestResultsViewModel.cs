namespace CorporatePortal.Models
{
    public class TestResultsViewModel
    {
        public string TestName { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public int Percentage { get; set; }
        public bool IsFailedByTime { get; set; }
        public TimeSpan CompletionTime { get; set; }
    }
}