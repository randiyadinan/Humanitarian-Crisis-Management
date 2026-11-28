namespace HumanitarianAPI.Models
{
    public class Volunteer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty; // උදා: ප්‍රථමාධාර, පිහිනුම්
        public string Specialisation { get; set; } = string.Empty; // Primary volunteer specialisation
        public string ContactNumber { get; set; } = string.Empty;
        public string AssignedTask { get; set; } = string.Empty; // Current deployment task
        public string Status { get; set; } = string.Empty; // උදා: Available, Deployed
        public int? CrisisEventId { get; set; } // යොදවා ඇති Crisis Event ID
        public int? ShelterId { get; set; } // යොදවා ඇති Shelter ID (optional)
    }
}
