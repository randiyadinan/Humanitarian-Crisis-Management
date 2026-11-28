using System.ComponentModel.DataAnnotations;

namespace HumanitarianAPI.Models
{
    public class ResourceCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int? CrisisEventId { get; set; }

        public int? ShelterId { get; set; }
    }

    public class ResourceUpdateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int? CrisisEventId { get; set; }

        public int? ShelterId { get; set; }
    }

    public class ResourceAllocationDto
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public int? CrisisEventId { get; set; }

        public int? ShelterId { get; set; }
    }

    public class ResourceReleaseDto
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class ResourceResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int AllocatedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? CrisisEventId { get; set; }
        public int? ShelterId { get; set; }
    }
}
