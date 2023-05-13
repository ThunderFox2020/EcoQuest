namespace EcoQuest.Models.Entities
{
    public partial class Game
    {
        public long GameId { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string? State { get; set; }
        public long? CurrentQuestionId { get; set; }

        public virtual User User { get; set; } = null!;
    }
}