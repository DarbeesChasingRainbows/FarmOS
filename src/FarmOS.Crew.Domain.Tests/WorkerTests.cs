using System;
using System.Linq;
using FarmOS.Crew.Domain;
using FarmOS.Crew.Domain.Aggregates;
using FarmOS.Crew.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Crew.Domain.Tests;

public class WorkerTests
{
    private static WorkerProfile CreateProfile(string name = "Jane Doe") =>
        new(name, "jane@farm.com", "555-0100", WorkerRole.Employee, null, null, DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public void Register_ShouldCreateActiveWorkerAndRaiseEvent()
    {
        // Arrange
        var profile = CreateProfile();

        // Act
        var worker = Worker.Register(profile);

        // Assert
        worker.Status.Should().Be(WorkerStatus.Active);
        worker.Profile.Should().Be(profile);

        var @event = worker.UncommittedEvents.OfType<WorkerRegistered>().Single();
        @event.Profile.Should().Be(profile);
        @event.Id.Should().Be(worker.Id);
    }

    [Fact]
    public void Deactivate_ShouldSucceed_WhenActive()
    {
        // Arrange
        var worker = Worker.Register(CreateProfile());
        worker.ClearEvents();

        // Act
        var result = worker.Deactivate(WorkerStatus.OnLeave, "Vacation");

        // Assert
        result.IsSuccess.Should().BeTrue();
        worker.Status.Should().Be(WorkerStatus.OnLeave);
        worker.UncommittedEvents.Should().ContainSingle(e => e is WorkerDeactivated);
    }

    [Fact]
    public void Deactivate_ShouldFail_WhenAlreadyInactive()
    {
        // Arrange
        var worker = Worker.Register(CreateProfile());
        worker.Deactivate(WorkerStatus.OnLeave, "Vacation");
        worker.ClearEvents();

        // Act
        var result = worker.Deactivate(WorkerStatus.Terminated, "Policy violation");

        // Assert
        result.IsFailure.Should().BeTrue();
        worker.Status.Should().Be(WorkerStatus.OnLeave);
        worker.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddCertification_ShouldSucceed_WhenActive()
    {
        // Arrange
        var worker = Worker.Register(CreateProfile());
        worker.ClearEvents();
        var cert = new Certification(CertificationType.FirstAid, "First Aid", DateOnly.FromDateTime(DateTime.UtcNow), null, "Red Cross", null);

        // Act
        var result = worker.AddCertification(cert);

        // Assert
        result.IsSuccess.Should().BeTrue();
        worker.Certifications.Should().ContainSingle().Which.Should().Be(cert);
        worker.UncommittedEvents.Should().ContainSingle(e => e is CertificationAdded);
    }

    [Fact]
    public void AddCertification_ShouldFail_WhenInactive()
    {
        // Arrange
        var worker = Worker.Register(CreateProfile());
        worker.Deactivate(WorkerStatus.Terminated, "End of contract");
        worker.ClearEvents();
        var cert = new Certification(CertificationType.CPR, "CPR", DateOnly.FromDateTime(DateTime.UtcNow), null, null, null);

        // Act
        var result = worker.AddCertification(cert);

        // Assert
        result.IsFailure.Should().BeTrue();
        worker.Certifications.Should().BeEmpty();
        worker.UncommittedEvents.Should().BeEmpty();
    }
}
