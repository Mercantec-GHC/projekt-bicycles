namespace Blazor.Models
{
    public class User
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }

        // A user can have multiple bikes
        public List<Bike>? Bikes { get; set; }

        // A user can send multiple messages
        public List<Message>? SentMessages { get; set; }

        // A user can receive multiple messages
        public List<Message>? ReceivedMessages { get; set; }
    }
}
