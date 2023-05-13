namespace EcoQuest.Models.Entities
{
    public partial class User
    {
        public User()
        {
            GameBoards = new HashSet<GameBoard>();
            Games = new HashSet<Game>();
        }

        public long UserId { get; set; }
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string Patronymic { get; set; } = null!;
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;

        public virtual ICollection<GameBoard> GameBoards { get; set; }
        public virtual ICollection<Game> Games { get; set; }
    }
}