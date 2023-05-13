namespace EcoQuest.Models.Entities
{
    public partial class Statistic
    {
        public long RecordId { get; set; }
        public long UserId { get; set; }
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string Patronymic { get; set; } = null!;
        public string Login { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string Duration { get; set; } = null!;
        public string Results { get; set; } = null!;
    }
}