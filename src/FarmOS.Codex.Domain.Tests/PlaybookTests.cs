using System;
using System.Linq;
using FarmOS.Codex.Domain;
using FarmOS.Codex.Domain.Aggregates;
using FarmOS.Codex.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Codex.Domain.Tests;

public class PlaybookTests
{
    private static PlaybookTask CreateTask(int month = 3, string title = "Seed Dahlia Tubers") =>
        new(month, title, "Plant tubers in prepared beds", ProcedureCategory.Flora, null, "High");

    [Fact]
    public void Create_ShouldProducePlaybookAndRaiseEvent()
    {
        // Arrange & Act
        var playbook = Playbook.Create("Spring Planting Guide", "Tasks for spring season", AudienceRole.Everyone);

        // Assert
        playbook.Title.Should().Be("Spring Planting Guide");
        playbook.Description.Should().Be("Tasks for spring season");
        playbook.Audience.Should().Be(AudienceRole.Everyone);

        var @event = playbook.UncommittedEvents.OfType<PlaybookCreated>().Single();
        @event.Title.Should().Be("Spring Planting Guide");
        @event.Id.Should().Be(playbook.Id);
    }

    [Fact]
    public void AddTask_ShouldAppendTaskAndRaiseEvent()
    {
        // Arrange
        var playbook = Playbook.Create("Annual Plan", null, AudienceRole.Employee);
        playbook.ClearEvents();
        var task = CreateTask();

        // Act
        playbook.AddTask(task);

        // Assert
        playbook.Tasks.Should().ContainSingle().Which.Should().Be(task);
        playbook.UncommittedEvents.Should().ContainSingle(e => e is PlaybookTaskAdded);
    }

    [Fact]
    public void RemoveTask_ShouldSucceed_WhenTaskExists()
    {
        // Arrange
        var playbook = Playbook.Create("Seasonal Guide", null, AudienceRole.Everyone);
        var task = CreateTask();
        playbook.AddTask(task);
        playbook.ClearEvents();

        // Act
        var result = playbook.RemoveTask(task.Month, task.Title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        playbook.Tasks.Should().BeEmpty();
        playbook.UncommittedEvents.Should().ContainSingle(e => e is PlaybookTaskRemoved);
    }

    [Fact]
    public void RemoveTask_ShouldFail_WhenTaskNotFound()
    {
        // Arrange
        var playbook = Playbook.Create("Empty Guide", null, AudienceRole.Everyone);
        playbook.ClearEvents();

        // Act
        var result = playbook.RemoveTask(6, "Nonexistent Task");

        // Assert
        result.IsFailure.Should().BeTrue();
        playbook.UncommittedEvents.Should().BeEmpty();
    }
}
