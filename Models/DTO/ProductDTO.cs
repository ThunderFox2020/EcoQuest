using EcoQuest.Models.Entities;

namespace EcoQuest.Models.DTO
{
    public class ProductDTO
    {
        public ProductDTO()
        {
            AllQuestions = new HashSet<Question>();
            ActiveQuestions = new HashSet<long>();
        }

        public long GameBoardId { get; set; }
        public long ProductId { get; set; }
        public string Colour { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Round { get; set; }
        public string? Logo { get; set; }
        public int NumOfRepeating { get; set; }

        public virtual ICollection<Question> AllQuestions { get; set; }
        public virtual ICollection<long> ActiveQuestions { get; set; }
    }
}