namespace HumanitarianAPI.Models
{
    // A household/person group assigned to a shelter.
    public class ShelterOccupant
    {
        public int Id { get; set; }
        public int ShelterId { get; set; }
        public string HouseholdName { get; set; } = string.Empty;
        public int PersonCount { get; set; }
    }
}
