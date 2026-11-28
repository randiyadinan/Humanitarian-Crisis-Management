using System.ComponentModel.DataAnnotations;

namespace HumanitarianAPI.Models
{
    public class VolunteerCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public string Specialisation { get; set; } = string.Empty;

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int? CrisisEventId { get; set; }

        public int? ShelterId { get; set; }
    }

    public class VolunteerUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public string Specialisation { get; set; } = string.Empty;

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int? CrisisEventId { get; set; }

        public int? ShelterId { get; set; }
    }

    public class VolunteerAvailabilityDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class VolunteerAssignmentDto
    {
        [Range(1, int.MaxValue)]
        public int CrisisEventId { get; set; }

        public int? ShelterId { get; set; }

        public string Task { get; set; } = string.Empty;
    }

    public class VolunteerResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string Specialisation { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string AssignedTask { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? CrisisEventId { get; set; }
        public int? ShelterId { get; set; }
    }
}
