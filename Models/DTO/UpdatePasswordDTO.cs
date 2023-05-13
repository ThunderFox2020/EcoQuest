namespace EcoQuest.Models.DTO
{
    public class UpdatePasswordDTO
    {
        public string Login { get; set; } = null!;
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}