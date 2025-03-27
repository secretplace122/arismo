using CorporatePortal.Models;
using System;

namespace CorporatePortal.Models
{
    public class TestViewModel
    {
        public Test Test { get; set; }
        public int ResultId { get; set; }
        public TimeSpan TimeLeft { get; set; }
        public Dictionary<int, int> UserAnswers { get; set; } = new Dictionary<int, int>();
        public int CurrentQuestionIndex { get; set; } = 0;
    }
}