using System;
using System.Linq;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.Domain.Tests;

public class HACCPPlanTests
{
    private static HACCPPlan CreatePlan() =>
        HACCPPlan.Create("Sourdough HACCP Plan", "Main Kitchen");

    private static CCPDefinition SampleCCPDefinition(string product = "Sourdough Bread", string ccpName = "Baking Temperature") =>
        new(
            product,
            ccpName,
            HazardType.Biological,
            "InternalTemp >= 200",
            "Probe thermometer at center of loaf",
            "Extend bake time and re-check temperature"
        );

    [Fact]
    public void Create_ShouldSetPlanNameAndFacility()
    {
        // Arrange & Act
        var plan = HACCPPlan.Create("Sourdough HACCP Plan", "Main Kitchen");

        // Assert
        plan.PlanName.Should().Be("Sourdough HACCP Plan");
        plan.FacilityName.Should().Be("Main Kitchen");
        plan.Id.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldRaiseHACCPPlanCreatedEvent()
    {
        // Arrange & Act
        var plan = HACCPPlan.Create("Sourdough HACCP Plan", "Main Kitchen");

        // Assert
        plan.UncommittedEvents.Should().ContainSingle(e => e is HACCPPlanCreated);
        var @event = plan.UncommittedEvents.OfType<HACCPPlanCreated>().Single();
        @event.PlanName.Should().Be("Sourdough HACCP Plan");
    }

    [Fact]
    public void AddCCPDefinition_ShouldSucceed()
    {
        // Arrange
        var plan = CreatePlan();
        plan.ClearEvents();
        var definition = SampleCCPDefinition();

        // Act
        var result = plan.AddCCPDefinition(definition);

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.CCPDefinitions.Should().ContainSingle();
        plan.CCPDefinitions.First().CCPName.Should().Be("Baking Temperature");
    }

    [Fact]
    public void AddCCPDefinition_Duplicate_ShouldFail()
    {
        // Arrange
        var plan = CreatePlan();
        plan.AddCCPDefinition(SampleCCPDefinition());
        plan.ClearEvents();

        // Act — add the same product + CCP name again
        var result = plan.AddCCPDefinition(SampleCCPDefinition());

        // Assert
        result.IsFailure.Should().BeTrue();
        plan.CCPDefinitions.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveCCPDefinition_Existing_ShouldSucceed()
    {
        // Arrange
        var plan = CreatePlan();
        plan.AddCCPDefinition(SampleCCPDefinition());
        plan.ClearEvents();

        // Act
        var result = plan.RemoveCCPDefinition("Sourdough Bread", "Baking Temperature");

        // Assert
        result.IsSuccess.Should().BeTrue();
        plan.CCPDefinitions.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCCPDefinition_NonExistent_ShouldFail()
    {
        // Arrange
        var plan = CreatePlan();
        plan.ClearEvents();

        // Act
        var result = plan.RemoveCCPDefinition("Nonexistent Product", "Nonexistent CCP");

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
