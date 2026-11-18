namespace HumanitarianAPI.Models
{
    public class AssistanceRequest
    {
        public int Id { get; set; }

        public string Category { get; set; } = string.Empty; 
        // Examples: Food, Medicine, Shelter

        public string Urgency { get; set; } = string.Empty;
        // Examples: High, Medium, Low

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";
        // Examples: Pending, Assigned, Completed
    }
}