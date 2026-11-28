using System.ComponentModel.DataAnnotations;

namespace HumanitarianAPI.Models
{
    public class ShelterCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Capacity { get; set; }

        [Range(0, int.MaxValue)]
        public int CurrentOccupancy { get; set; }

        public string Status { get; set; } = string.Empty;

        public int? CrisisEventId { get; set; }
    }

    public class ShelterUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Capacity { get; set; }

        [Range(0, int.MaxValue)]
        public int CurrentOccupancy { get; set; }

        public string Status { get; set; } = string.Empty;

        public int? CrisisEventId { get; set; }
    }

    public class ShelterStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class ShelterAssignmentDto
    {
        public string HouseholdName { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int PersonCount { get; set; }
    }

    public class ShelterResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int CurrentOccupancy { get; set; }
        public int AvailableCapacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? CrisisEventId { get; set; }
    }
}
