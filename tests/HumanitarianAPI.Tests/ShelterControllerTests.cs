using System.Reflection;
using HumanitarianAPI.Controllers;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAPI.Tests;

public class ShelterControllerTests
{
    [Fact]
    public async Task CreateShelter_ComputesAvailableCapacityAndStatus()
    {
        using var db = TestDb.CreateContext();
        var controller = new ShelterController(db);

        var result = await controller.CreateShelter(new ShelterCreateDto
        {
            Name = "School shelter",
            Location = "Agalawatta",
            Capacity = 100,
            CurrentOccupancy = 20
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ShelterResponseDto>(created.Value);
        Assert.Equal(80, response.AvailableCapacity);
        Assert.Equal("Open", response.Status);
        Assert.Single(db.Shelters);
    }

    [Fact]
    public async Task CreateShelter_RejectsOccupancyAboveCapacity()
    {
        using var db = TestDb.CreateContext();
        var controller = new ShelterController(db);

        var result = await controller.CreateShelter(new ShelterCreateDto
        {
            Name = "Hall",
            Location = "Colombo",
            Capacity = 10,
            CurrentOccupancy = 11
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.Shelters);
    }

    [Fact]
    public async Task GetAvailableShelters_FiltersClosedAndInsufficientCapacity()
    {
        using var db = TestDb.CreateContext();
        SeedShelter(db, "Open large", 10, 0, "Open");
        SeedShelter(db, "Almost full", 10, 9, "Full");
        SeedShelter(db, "Closed", 20, 0, "Closed");
        SeedShelter(db, "Small", 5, 0, "Open");
        var controller = new ShelterController(db);

        var result = Assert.IsType<OkObjectResult>(
            (await controller.GetAvailableShelters(minCapacity: 5)).Result);
        var shelters = Assert.IsAssignableFrom<List<ShelterResponseDto>>(result.Value);

        Assert.Equal(2, shelters.Count);
        Assert.Contains(shelters, s => s.Name == "Open large");
        Assert.Contains(shelters, s => s.Name == "Small");
    }

    [Fact]
    public async Task GetCapacity_ReturnsRemainingSlots()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "School", 50, 30, "Open");
        var controller = new ShelterController(db);

        var result = Assert.IsType<OkObjectResult>(await controller.GetCapacity(shelter.Id));

        Assert.NotNull(result.Value);
        var available = result.Value!.GetType().GetProperty("AvailableCapacity")!.GetValue(result.Value);
        Assert.Equal(20, available);
    }

    [Fact]
    public async Task AssignOccupants_UpdatesOccupancyAndMarksShelterFull()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "School", 10, 0, "Open");
        var controller = new ShelterController(db);

        var first = Assert.IsType<OkObjectResult>((await controller.AssignOccupants(shelter.Id,
            new ShelterAssignmentDto { HouseholdName = "Perera family", PersonCount = 6 })).Result);
        Assert.Equal(6, Assert.IsType<ShelterResponseDto>(first.Value).CurrentOccupancy);
        Assert.Equal("Open", Assert.IsType<ShelterResponseDto>(first.Value).Status);

        var second = Assert.IsType<OkObjectResult>((await controller.AssignOccupants(shelter.Id,
            new ShelterAssignmentDto { HouseholdName = "Silva family", PersonCount = 4 })).Result);
        var response = Assert.IsType<ShelterResponseDto>(second.Value);
        Assert.Equal(10, response.CurrentOccupancy);
        Assert.Equal(0, response.AvailableCapacity);
        Assert.Equal("Full", response.Status);
        Assert.Equal(2, db.ShelterOccupants.Count());
    }

    [Fact]
    public async Task AssignOccupants_RejectsOverCapacityAssignment()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "School", 10, 8, "Open");
        var controller = new ShelterController(db);

        var result = await controller.AssignOccupants(shelter.Id,
            new ShelterAssignmentDto { HouseholdName = "Large family", PersonCount = 3 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(8, db.Shelters.Single().CurrentOccupancy);
        Assert.Empty(db.ShelterOccupants);
    }

    [Fact]
    public async Task AssignOccupants_RejectsClosedShelter()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "Closed hall", 10, 0, "Closed");
        var controller = new ShelterController(db);

        var result = await controller.AssignOccupants(shelter.Id,
            new ShelterAssignmentDto { PersonCount = 1 });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task RemoveOccupants_RestoresAvailableCapacity()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "School", 10, 0, "Open");
        var controller = new ShelterController(db);
        await controller.AssignOccupants(shelter.Id,
            new ShelterAssignmentDto { HouseholdName = "Family", PersonCount = 5 });
        var occupant = db.ShelterOccupants.Single();

        var result = Assert.IsType<OkObjectResult>(
            (await controller.RemoveOccupants(shelter.Id, occupant.Id)).Result);
        var response = Assert.IsType<ShelterResponseDto>(result.Value);

        Assert.Equal(0, response.CurrentOccupancy);
        Assert.Equal(10, response.AvailableCapacity);
        Assert.Equal("Open", response.Status);
        Assert.Empty(db.ShelterOccupants);
    }

    [Fact]
    public async Task UpdateShelter_RejectsOccupancyBelowAssignedPersons()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "School", 10, 4, "Open");
        db.ShelterOccupants.Add(new ShelterOccupant
        {
            ShelterId = shelter.Id,
            HouseholdName = "Family",
            PersonCount = 4
        });
        db.SaveChanges();
        var controller = new ShelterController(db);

        var result = await controller.UpdateShelter(shelter.Id, new ShelterUpdateDto
        {
            Name = "School",
            Location = "Agalawatta",
            Capacity = 10,
            CurrentOccupancy = 3,
            Status = "Open"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(4, db.Shelters.Single().CurrentOccupancy);
    }

    [Fact]
    public async Task UpdateStatus_ValidatesAllowedValues()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "School", 10, 0, "Open");
        var controller = new ShelterController(db);

        var invalid = await controller.UpdateStatus(shelter.Id, new ShelterStatusDto { Status = "Unknown" });
        Assert.IsType<BadRequestObjectResult>(invalid.Result);

        var valid = Assert.IsType<OkObjectResult>(
            (await controller.UpdateStatus(shelter.Id, new ShelterStatusDto { Status = "closed" })).Result);
        Assert.Equal("Closed", Assert.IsType<ShelterResponseDto>(valid.Value).Status);
    }

    [Fact]
    public async Task DeleteShelter_RemovesAssignmentsAndUnlinksReferences()
    {
        using var db = TestDb.CreateContext();
        var shelter = SeedShelter(db, "School", 10, 2, "Open");
        db.ShelterOccupants.Add(new ShelterOccupant
        {
            ShelterId = shelter.Id,
            HouseholdName = "Family",
            PersonCount = 2
        });
        db.Resources.Add(new Resource
        {
            Name = "Water",
            Category = "Water",
            Quantity = 10,
            Location = "School",
            Status = "Available",
            ShelterId = shelter.Id
        });
        db.Volunteers.Add(new Volunteer
        {
            Name = "Kamal",
            Skills = "First Aid",
            ContactNumber = "0771234567",
            Status = "Available",
            ShelterId = shelter.Id
        });
        db.SaveChanges();
        var controller = new ShelterController(db);

        var result = await controller.DeleteShelter(shelter.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(db.Shelters);
        Assert.Empty(db.ShelterOccupants);
        Assert.Null(db.Resources.Single().ShelterId);
        Assert.Null(db.Volunteers.Single().ShelterId);
    }

    [Fact]
    public void ShelterEndpoints_RequireAuthorisationAndAdminForMutations()
    {
        var type = typeof(ShelterController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());

        var mutations = new[]
        {
            nameof(ShelterController.CreateShelter),
            nameof(ShelterController.UpdateShelter),
            nameof(ShelterController.UpdateStatus),
            nameof(ShelterController.AssignOccupants),
            nameof(ShelterController.RemoveOccupants),
            nameof(ShelterController.DeleteShelter)
        };

        foreach (var mutation in mutations)
        {
            var attribute = type.GetMethod(mutation)!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
            Assert.Contains("Admin", attribute!.Roles!.Split(','));
        }
    }

    private static Shelter SeedShelter(
        ApplicationDbContext db, string name, int capacity, int occupancy, string status)
    {
        var shelter = new Shelter
        {
            Name = name,
            Location = "Kalutara",
            Capacity = capacity,
            CurrentOccupancy = occupancy,
            Status = status
        };
        db.Shelters.Add(shelter);
        db.SaveChanges();
        return shelter;
    }
}
