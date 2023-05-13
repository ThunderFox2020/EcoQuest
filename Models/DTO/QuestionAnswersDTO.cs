namespace EcoQuest.Models.DTO
{
    public class QuestionAnswersDTO
    {
        public QuestionAnswersDTO()
        {
            AllAnswers = new List<string>();
            CorrectAnswers = new List<string>();
        }

        public ICollection<string> AllAnswers { get; set; }
        public ICollection<string> CorrectAnswers { get; set; }
    }
}