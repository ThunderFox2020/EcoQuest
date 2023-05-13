namespace EcoQuest.Models.DTO
{
    public class GameBoardDTO
    {
        public GameBoardDTO()
        {
            Products = new HashSet<ProductDTO>();
        }

        public long GameBoardId { get; set; }
        public string Name { get; set; } = null!;
        public int NumFields { get; set; }
        public long UserId { get; set; }

        public virtual ICollection<ProductDTO> Products { get; set; }
    }
}