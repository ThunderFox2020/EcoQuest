namespace EcoQuest.Models.Entities
{
    public partial class GameBoard
    {
        public GameBoard()
        {
            GameBoardsProducts = new HashSet<GameBoardsProduct>();
            Questions = new HashSet<Question>();
        }

        public long GameBoardId { get; set; }
        public string Name { get; set; } = null!;
        public int NumFields { get; set; }
        public long UserId { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual ICollection<GameBoardsProduct> GameBoardsProducts { get; set; }

        public virtual ICollection<Question> Questions { get; set; }
    }
}