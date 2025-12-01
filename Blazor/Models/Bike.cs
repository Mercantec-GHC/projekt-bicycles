namespace Blazor.Models
{
    public class Bike
    {
        public int Id { get; set; }

        // user who listed the bike (just ID)
        public int UserId { get; set; }
        public string? Title { get; set; }
        public decimal Price { get; set; }
        public string? Color { get; set; }
        public string? Type { get; set; }
        public int ModelYear { get; set; }
        public string? GearType { get; set; }
        public string? BreakType { get; set; }
        public decimal Weight { get; set; }
        public string? BikeCondition { get; set; }
        public string? TargetAudience { get; set; }
        public string? Material { get; set; }
        public string? ImageUrl { get; set; }
        public string? Brand { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }


        // messages related to the bike
        public List<Message>? Messages { get; set; }

        // user who listed the bike (User instance to work with it like bikeName.User.Name)
        public User? User { get; set; }
    }

}
