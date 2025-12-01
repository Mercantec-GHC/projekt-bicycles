namespace Blazor.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int BikeId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // user which sent the message
        public User? FromUser { get; set; }

        // user which received the message
        public User? ToUser { get; set; }

        // bike related to the message
        public Bike? Bike { get; set; }
    }
}
