namespace EcoQuest.Models.DTO
{
    public class StatisticsResultsDTO
    {
        public StatisticsResultsDTO()
        {
            Teams = new List<Team>();
        }

        public ICollection<Team> Teams { get; set; }

        public class Team
        {
            public Team()
            {
                Players = new List<string>();
            }

            public string? Name { get; set; }
            public ICollection<string> Players { get; set; }
            public int Score { get; set; }
            public int Place { get; set; }
        }
    }
}