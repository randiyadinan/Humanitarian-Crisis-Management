namespace HumanitarianAPI.Models
{
    public class Resource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // උදා: වතුර බෝතල්, ඖෂධ
        public string Category { get; set; } = string.Empty; // උදා: Water, Medicine, Food
        public int Quantity { get; set; }
        public int AllocatedQuantity { get; set; } // Quantity currently committed to a crisis/shelter
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // උදා: Available, Dispatched
        public int? CrisisEventId { get; set; } // වෙන්කර ඇති Crisis Event ID
        public int? ShelterId { get; set; } // වෙන්කර ඇති Shelter ID (optional)
    }
}
