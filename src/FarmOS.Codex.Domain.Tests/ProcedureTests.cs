using System;
using System.Linq;
using FarmOS.Codex.Domain;
using FarmOS.Codex.Domain.Aggregates;
using FarmOS.Codex.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Codex.Domain.Tests;

public class ProcedureTests
{
    private static ProcedureStep CreateStep(int order = 1) =>
        new(order, $"Step {order}", "Do the thing", null, null, 5);

    [Fact]
    public void Create_ShouldProduceDraftProcedureAndRaiseEvent()
    {
        // Arrange & Act
        var proc = Procedure.Create("Fence Inspection", ProcedureCategory.Pasture, AudienceRole.Everyone, "Check fences");

        // Assert
        proc.Status.Should().Be(ProcedureStatus.Draft);
        proc.Title.Should().Be("Fence Inspection");
        proc.Category.Should().Be(ProcedureCategory.Pasture);
        proc.Audience.Should().Be(AudienceRole.Everyone);
        proc.Description.Should().Be("Check fences");

        var @event = proc.UncommittedEvents.OfType<ProcedureCreated>().Single();
        @event.Title.Should().Be("Fence Inspection");
        @event.Id.Should().Be(proc.Id);
    }

    [Fact]
    public void AddStep_ShouldAppendStepAndRaiseEvent()
    {
        // Arrange
        var proc = Procedure.Create("Hive Check", ProcedureCategory.Apiary, AudienceRole.Employee, null);
        proc.ClearEvents();
        var step = CreateStep();

        // Act
        proc.AddStep(step);

        // Assert
        proc.Steps.Should().ContainSingle().Which.Should().Be(step);
        proc.UncommittedEvents.Should().ContainSingle(e => e is ProcedureStepAdded);
    }

    [Fact]
    public void Publish_ShouldSucceed_WhenDraftWithSteps()
    {
        // Arrange
        var proc = Procedure.Create("Harvest SOP", ProcedureCategory.Flora, AudienceRole.Everyone, null);
        proc.AddStep(CreateStep());
        proc.ClearEvents();

        // Act
        var result = proc.Publish();

        // Assert
        result.IsSuccess.Should().BeTrue();
        proc.Status.Should().Be(ProcedureStatus.Published);
        proc.Revision.Should().Be(1);
        proc.UncommittedEvents.Should().ContainSingle(e => e is ProcedurePublished);
    }

    [Fact]
    public void Publish_ShouldFail_WhenNoSteps()
    {
        // Arrange
        var proc = Procedure.Create("Empty SOP", ProcedureCategory.General, AudienceRole.Everyone, null);
        proc.ClearEvents();

        // Act
        var result = proc.Publish();

        // Assert
        result.IsFailure.Should().BeTrue();
        proc.Status.Should().Be(ProcedureStatus.Draft);
        proc.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Publish_ShouldFail_WhenArchived()
    {
        // Arrange
        var proc = Procedure.Create("Old SOP", ProcedureCategory.Safety, AudienceRole.Everyone, null);
        proc.AddStep(CreateStep());
        proc.Archive();
        proc.ClearEvents();

        // Act
        var result = proc.Publish();

        // Assert
        result.IsFailure.Should().BeTrue();
        proc.Status.Should().Be(ProcedureStatus.Archived);
        proc.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Revise_ShouldSucceed_WhenPublished()
    {
        // Arrange
        var proc = Procedure.Create("Published SOP", ProcedureCategory.Compliance, AudienceRole.Manager, null);
        proc.AddStep(CreateStep());
        proc.Publish();
        proc.ClearEvents();

        // Act
        var result = proc.Revise("Updating for 2026 season");

        // Assert
        result.IsSuccess.Should().BeTrue();
        proc.Status.Should().Be(ProcedureStatus.Draft);
        proc.UncommittedEvents.Should().ContainSingle(e => e is ProcedureRevised);
    }

    [Fact]
    public void Revise_ShouldFail_WhenDraft()
    {
        // Arrange
        var proc = Procedure.Create("Draft SOP", ProcedureCategory.General, AudienceRole.Everyone, null);
        proc.ClearEvents();

        // Act
        var result = proc.Revise("No changes needed");

        // Assert
        result.IsFailure.Should().BeTrue();
        proc.Status.Should().Be(ProcedureStatus.Draft);
        proc.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_ShouldSetStatusToArchived()
    {
        // Arrange
        var proc = Procedure.Create("Retiring SOP", ProcedureCategory.Onboarding, AudienceRole.Apprentice, null);
        proc.ClearEvents();

        // Act
        proc.Archive();

        // Assert
        proc.Status.Should().Be(ProcedureStatus.Archived);
        proc.UncommittedEvents.Should().ContainSingle(e => e is ProcedureArchived);
    }
}
