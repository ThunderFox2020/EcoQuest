namespace EcoQuest.Models.Entities
{
    public partial class GameBoardsProduct
    {
        public long GameBoardId { get; set; }
        public long ProductId { get; set; }
        public int NumOfRepeating { get; set; }

        public virtual GameBoard GameBoard { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}