using System.Reflection;
using HumanitarianAPI.Controllers;
using HumanitarianAPI.Data;
using HumanitarianAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HumanitarianAPI.Tests;

public class ResourceControllerTests
{
    [Fact]
    public async Task CreateResource_PersistsCategoryAndAvailability()
    {
        using var db = TestDb.CreateContext();
        var controller = new ResourceController(db);

        var result = await controller.CreateResource(new ResourceCreateDto
        {
            Name = "Drinking water bottles",
            Category = "Water",
            Quantity = 100,
            Location = "Main warehouse",
            Status = "Available"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ResourceResponseDto>(created.Value);

        Assert.Equal("Water", response.Category);
        Assert.Equal(100, response.Quantity);
        Assert.Equal(0, response.AllocatedQuantity);
        Assert.Equal(100, response.AvailableQuantity);
        Assert.Single(db.Resources);
    }

    [Fact]
    public async Task CreateResource_RejectsNegativeQuantity()
    {
        using var db = TestDb.CreateContext();
        var controller = new ResourceController(db);

        var result = await controller.CreateResource(new ResourceCreateDto
        {
            Name = "Blankets",
            Quantity = -1,
            Location = "Warehouse"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.Resources);
    }

    [Fact]
    public async Task CreateResource_RejectsUnknownCrisisEvent()
    {
        using var db = TestDb.CreateContext();
        var controller = new ResourceController(db);

        var result = await controller.CreateResource(new ResourceCreateDto
        {
            Name = "Medicine",
            Quantity = 10,
            Location = "Clinic",
            CrisisEventId = 999
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.Resources);
    }

    [Fact]
    public async Task GetAvailableResources_FiltersByStatusCategoryAndAvailableQuantity()
    {
        using var db = TestDb.CreateContext();
        SeedResource(db, "Water", 10, 0, "Available");
        SeedResource(db, "Water", 10, 10, "Available");
        SeedResource(db, "Food", 20, 0, "Available");
        SeedResource(db, "Water", 100, 0, "Dispatched");

        var controller = new ResourceController(db);

        var allAvailable = Assert.IsType<OkObjectResult>(
            (await controller.GetAvailableResources()).Result);
        var allList = Assert.IsAssignableFrom<List<ResourceResponseDto>>(allAvailable.Value);
        Assert.Equal(2, allList.Count);

        var waterOnly = Assert.IsType<OkObjectResult>(
            (await controller.GetAvailableResources("Water")).Result);
        var waterList = Assert.IsAssignableFrom<List<ResourceResponseDto>>(waterOnly.Value);
        Assert.Single(waterList);
        Assert.Equal("Water", waterList[0].Category);
    }

    [Fact]
    public async Task AllocateResource_UpdatesAllocatedQuantityAndStatus()
    {
        using var db = TestDb.CreateContext();
        var crisis = SeedCrisis(db);
        var resource = SeedResource(db, "Water", 10, 0, "Available");
        var controller = new ResourceController(db);

        var result = await controller.AllocateResource(resource.Id, new ResourceAllocationDto
        {
            Quantity = 4,
            CrisisEventId = crisis.Id
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResourceResponseDto>(ok.Value);
        Assert.Equal(4, response.AllocatedQuantity);
        Assert.Equal(6, response.AvailableQuantity);
        Assert.Equal("Available", response.Status);
        Assert.Equal(crisis.Id, response.CrisisEventId);
    }

    [Fact]
    public async Task AllocateResource_FullyAllocatedResourceIsDispatched()
    {
        using var db = TestDb.CreateContext();
        var resource = SeedResource(db, "Medicine", 5, 0, "Available");
        var controller = new ResourceController(db);

        var result = await controller.AllocateResource(resource.Id, new ResourceAllocationDto
        {
            Quantity = 5
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResourceResponseDto>(ok.Value);
        Assert.Equal(5, response.AllocatedQuantity);
        Assert.Equal(0, response.AvailableQuantity);
        Assert.Equal("Dispatched", response.Status);
    }

    [Fact]
    public async Task AllocateResource_RejectsQuantityAboveAvailability()
    {
        using var db = TestDb.CreateContext();
        var resource = SeedResource(db, "Water", 10, 7, "Available");
        var controller = new ResourceController(db);

        var result = await controller.AllocateResource(resource.Id, new ResourceAllocationDto
        {
            Quantity = 4
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(7, db.Resources.Single().AllocatedQuantity);
    }

    [Fact]
    public async Task AllocateResource_RejectsNonAvailableResource()
    {
        using var db = TestDb.CreateContext();
        var resource = SeedResource(db, "Water", 10, 0, "Damaged");
        var controller = new ResourceController(db);

        var result = await controller.AllocateResource(resource.Id, new ResourceAllocationDto
        {
            Quantity = 1
        });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task ReleaseResource_RestoresAvailabilityAndClearsAssignment()
    {
        using var db = TestDb.CreateContext();
        var crisis = SeedCrisis(db);
        var resource = SeedResource(db, "Water", 10, 10, "Dispatched", crisis.Id);
        var controller = new ResourceController(db);

        var result = await controller.ReleaseResource(resource.Id, new ResourceReleaseDto { Quantity = 10 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ResourceResponseDto>(ok.Value);
        Assert.Equal(0, response.AllocatedQuantity);
        Assert.Equal(10, response.AvailableQuantity);
        Assert.Equal("Available", response.Status);
        Assert.Null(response.CrisisEventId);
    }

    [Fact]
    public async Task ReleaseResource_RejectsQuantityAboveAllocated()
    {
        using var db = TestDb.CreateContext();
        var resource = SeedResource(db, "Water", 10, 3, "Available");
        var controller = new ResourceController(db);

        var result = await controller.ReleaseResource(resource.Id, new ResourceReleaseDto { Quantity = 4 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(3, db.Resources.Single().AllocatedQuantity);
    }

    [Fact]
    public async Task UpdateResource_RejectsQuantityBelowAllocatedQuantity()
    {
        using var db = TestDb.CreateContext();
        var resource = SeedResource(db, "Water", 10, 6, "Available");
        var controller = new ResourceController(db);

        var result = await controller.UpdateResource(resource.Id, new ResourceUpdateDto
        {
            Name = "Water",
            Category = "Water",
            Quantity = 5,
            Location = "Warehouse",
            Status = "Available"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(10, db.Resources.Single().Quantity);
    }

    [Fact]
    public async Task DeleteResource_RejectsResourceWithAllocatedUnits()
    {
        using var db = TestDb.CreateContext();
        var resource = SeedResource(db, "Water", 10, 2, "Available");
        var controller = new ResourceController(db);

        var result = await controller.DeleteResource(resource.Id);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Resources);
    }

    [Fact]
    public void ResourceEndpoints_RequireAuthorisationAndAdminForMutations()
    {
        var type = typeof(ResourceController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());

        var mutations = new[]
        {
            nameof(ResourceController.CreateResource),
            nameof(ResourceController.UpdateResource),
            nameof(ResourceController.DeleteResource),
            nameof(ResourceController.AllocateResource),
            nameof(ResourceController.ReleaseResource)
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

    private static Resource SeedResource(
        ApplicationDbContext db,
        string category,
        int quantity,
        int allocatedQuantity,
        string status,
        int? crisisEventId = null)
    {
        var resource = new Resource
        {
            Name = $"{category} supply",
            Category = category,
            Quantity = quantity,
            AllocatedQuantity = allocatedQuantity,
            Location = "Warehouse",
            Status = status,
            CrisisEventId = crisisEventId
        };
        db.Resources.Add(resource);
        db.SaveChanges();
        return resource;
    }
}
