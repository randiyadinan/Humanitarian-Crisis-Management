namespace HumanitarianAPI.Models
{
    public class Donation
    {
        public int Id { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public string DonationType { get; set; } = string.Empty; // Monetary or Material
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int? CrisisEventId { get; set; } // වෙන්කර ඇති Crisis Event ID
    }
}
