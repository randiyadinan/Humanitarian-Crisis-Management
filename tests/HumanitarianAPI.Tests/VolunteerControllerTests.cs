using System.Reflection;
using HumanitarianAPI.Controllers;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAPI.Tests;

public class VolunteerControllerTests
{
    [Fact]
    public async Task CreateVolunteer_DefaultsAvailabilityAndSpecialisation()
    {
        using var db = TestDb.CreateContext();
        var controller = new VolunteerController(db);

        var result = await controller.CreateVolunteer(new VolunteerCreateDto
        {
            Name = "Kamal Perera",
            Skills = "First Aid, Swimming",
            ContactNumber = "0771234567"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<VolunteerResponseDto>(created.Value);
        Assert.Equal("Available", response.Status);
        Assert.Equal("General Relief", response.Specialisation);
        Assert.Equal("First Aid, Swimming", response.Skills);
        Assert.Single(db.Volunteers);
    }

    [Fact]
    public async Task CreateVolunteer_RejectsInvalidContactNumber()
    {
        using var db = TestDb.CreateContext();
        var controller = new VolunteerController(db);

        var result = await controller.CreateVolunteer(new VolunteerCreateDto
        {
            Name = "Nimal",
            ContactNumber = "abc"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.Volunteers);
    }

    [Fact]
    public async Task CreateVolunteer_RejectsUnknownShelter()
    {
        using var db = TestDb.CreateContext();
        var controller = new VolunteerController(db);

        var result = await controller.CreateVolunteer(new VolunteerCreateDto
        {
            Name = "Nimal",
            ContactNumber = "0771234567",
            ShelterId = 999
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAvailableVolunteers_FiltersByStatusSkillAndSpecialisation()
    {
        using var db = TestDb.CreateContext();
        SeedVolunteer(db, "Medic", "First Aid", "Medical", "Available");
        SeedVolunteer(db, "Boat operator", "Swimming", "Rescue", "Available");
        SeedVolunteer(db, "Deployed medic", "First Aid", "Medical", "Deployed");
        var controller = new VolunteerController(db);

        var available = Assert.IsType<OkObjectResult>(
            (await controller.GetAvailableVolunteers()).Result);
        Assert.Equal(2, Assert.IsAssignableFrom<List<VolunteerResponseDto>>(available.Value).Count);

        var bySkill = Assert.IsType<OkObjectResult>(
            (await controller.GetAvailableVolunteers(skill: "first aid")).Result);
        var skillList = Assert.IsAssignableFrom<List<VolunteerResponseDto>>(bySkill.Value);
        Assert.Single(skillList);
        Assert.Equal("Medic", skillList[0].Name);

        var bySpecialisation = Assert.IsType<OkObjectResult>(
            (await controller.GetAvailableVolunteers(specialisation: "rescue")).Result);
        Assert.Single(Assert.IsAssignableFrom<List<VolunteerResponseDto>>(bySpecialisation.Value));
    }

    [Fact]
    public async Task AssignVolunteer_SetsDeploymentFields()
    {
        using var db = TestDb.CreateContext();
        var crisis = SeedCrisis(db);
        var shelter = SeedShelter(db);
        var volunteer = SeedVolunteer(db, "Medic", "First Aid", "Medical", "Available");
        var controller = new VolunteerController(db);

        var result = await controller.AssignVolunteer(volunteer.Id, new VolunteerAssignmentDto
        {
            CrisisEventId = crisis.Id,
            ShelterId = shelter.Id,
            Task = "Triage at shelter"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<VolunteerResponseDto>(ok.Value);
        Assert.Equal("Deployed", response.Status);
        Assert.Equal(crisis.Id, response.CrisisEventId);
        Assert.Equal(shelter.Id, response.ShelterId);
        Assert.Equal("Triage at shelter", response.AssignedTask);
    }

    [Fact]
    public async Task AssignVolunteer_RejectsUnavailableVolunteer()
    {
        using var db = TestDb.CreateContext();
        var crisis = SeedCrisis(db);
        var volunteer = SeedVolunteer(db, "Medic", "First Aid", "Medical", "Unavailable");
        var controller = new VolunteerController(db);

        var result = await controller.AssignVolunteer(volunteer.Id, new VolunteerAssignmentDto
        {
            CrisisEventId = crisis.Id
        });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task AssignVolunteer_RequiresValidCrisisEvent()
    {
        using var db = TestDb.CreateContext();
        var volunteer = SeedVolunteer(db, "Medic", "First Aid", "Medical", "Available");
        var controller = new VolunteerController(db);

        var missing = await controller.AssignVolunteer(volunteer.Id, new VolunteerAssignmentDto
        {
            CrisisEventId = 999
        });
        Assert.IsType<BadRequestObjectResult>(missing.Result);

        var zero = await controller.AssignVolunteer(volunteer.Id, new VolunteerAssignmentDto());
        Assert.IsType<BadRequestObjectResult>(zero.Result);
    }

    [Fact]
    public async Task ReleaseVolunteer_ClearsDeployment()
    {
        using var db = TestDb.CreateContext();
        var crisis = SeedCrisis(db);
        var volunteer = SeedVolunteer(db, "Medic", "First Aid", "Medical", "Deployed", crisis.Id);
        volunteer.AssignedTask = "Triage";
        db.SaveChanges();
        var controller = new VolunteerController(db);

        var result = Assert.IsType<OkObjectResult>((await controller.ReleaseVolunteer(volunteer.Id)).Result);
        var response = Assert.IsType<VolunteerResponseDto>(result.Value);

        Assert.Equal("Available", response.Status);
        Assert.Null(response.CrisisEventId);
        Assert.Null(response.ShelterId);
        Assert.Equal(string.Empty, response.AssignedTask);
    }

    [Fact]
    public async Task UpdateAvailability_RequiresCrisisForDeployedStatus()
    {
        using var db = TestDb.CreateContext();
        var volunteer = SeedVolunteer(db, "Medic", "First Aid", "Medical", "Available");
        var controller = new VolunteerController(db);

        var invalid = await controller.UpdateAvailability(volunteer.Id,
            new VolunteerAvailabilityDto { Status = "Deployed" });
        Assert.IsType<BadRequestObjectResult>(invalid.Result);

        var valid = Assert.IsType<OkObjectResult>((await controller.UpdateAvailability(volunteer.Id,
            new VolunteerAvailabilityDto { Status = "Unavailable" })).Result);
        Assert.Equal("Unavailable", Assert.IsType<VolunteerResponseDto>(valid.Value).Status);
    }

    [Fact]
    public async Task UpdateVolunteer_RejectsInvalidStatus()
    {
        using var db = TestDb.CreateContext();
        var volunteer = SeedVolunteer(db, "Medic", "First Aid", "Medical", "Available");
        var controller = new VolunteerController(db);

        var result = await controller.UpdateVolunteer(volunteer.Id, new VolunteerUpdateDto
        {
            Name = "Medic",
            ContactNumber = "0771234567",
            Status = "On holiday"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Available", db.Volunteers.Single().Status);
    }

    [Fact]
    public void VolunteerEndpoints_RequireAuth_AndAdminForManagementOnly()
    {
        var type = typeof(VolunteerController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());

        // Citizen self-registration must remain available to authenticated non-admin users.
        var createAttribute = type.GetMethod(nameof(VolunteerController.CreateVolunteer))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.True(createAttribute == null || createAttribute.Roles == null);

        var mutations = new[]
        {
            nameof(VolunteerController.UpdateVolunteer),
            nameof(VolunteerController.DeleteVolunteer),
            nameof(VolunteerController.UpdateAvailability),
            nameof(VolunteerController.AssignVolunteer),
            nameof(VolunteerController.ReleaseVolunteer)
        };

        foreach (var mutation in mutations)
        {
            var attribute = type.GetMethod(mutation)!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
            Assert.Contains("Admin", attribute!.Roles!.Split(','));
        }
    }

    private static CrisisEvent SeedCrisis(ApplicationDbContext db)
    {
        var crisis = new CrisisEvent
        {
            Title = "Flood",
            EventType = "Flood",
            Location = "Kalutara",
            Severity = "High",
            DateReported = "2026-11-25"
        };
        db.CrisisEvents.Add(crisis);
        db.SaveChanges();
        return crisis;
    }

    private static Shelter SeedShelter(ApplicationDbContext db)
    {
        var shelter = new Shelter
        {
            Name = "School",
            Location = "Kalutara",
            Capacity = 50,
            CurrentOccupancy = 0,
            Status = "Open"
        };
        db.Shelters.Add(shelter);
        db.SaveChanges();
        return shelter;
    }

    private static Volunteer SeedVolunteer(
        ApplicationDbContext db,
        string name,
        string skills,
        string specialisation,
        string status,
        int? crisisEventId = null)
    {
        var volunteer = new Volunteer
        {
            Name = name,
            Skills = skills,
            Specialisation = specialisation,
            ContactNumber = "0771234567",
            Status = status,
            CrisisEventId = crisisEventId
        };
        db.Volunteers.Add(volunteer);
        db.SaveChanges();
        return volunteer;
    }
}
