using System;
using System.Linq;
using FarmOS.Compliance.Domain;
using FarmOS.Compliance.Domain.Aggregates;
using FarmOS.Compliance.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Compliance.Domain.Tests;

public class PermitTests
{
    private static Permit CreatePermit() =>
        Permit.Register(PermitType.BusinessLicense, "Farm Business License", "County Clerk",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            150m, null);

    [Fact]
    public void Register_ShouldCreateActivePermitAndRaiseEvent()
    {
        // Arrange & Act
        var permit = CreatePermit();

        // Assert
        permit.Status.Should().Be(PermitStatus.Active);
        permit.Type.Should().Be(PermitType.BusinessLicense);
        permit.Name.Should().Be("Farm Business License");
        permit.IssuingAuthority.Should().Be("County Clerk");
        permit.Fee.Should().Be(150m);

        var @event = permit.UncommittedEvents.OfType<PermitRegistered>().Single();
        @event.Id.Should().Be(permit.Id);
        @event.Type.Should().Be(PermitType.BusinessLicense);
    }

    [Fact]
    public void Renew_ShouldSucceed_WhenActive()
    {
        // Arrange
        var permit = CreatePermit();
        permit.ClearEvents();
        var renewal = new RenewalInfo(DateOnly.FromDateTime(DateTime.UtcNow), 175m, "Annual renewal");
        var newExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));

        // Act
        var result = permit.Renew(renewal, newExpiry);

        // Assert
        result.IsSuccess.Should().BeTrue();
        permit.ExpiryDate.Should().Be(newExpiry);
        permit.Renewals.Should().ContainSingle().Which.Should().Be(renewal);
        permit.UncommittedEvents.Should().ContainSingle(e => e is PermitRenewed);
    }

    [Fact]
    public void Renew_ShouldFail_WhenRevoked()
    {
        // Arrange
        var permit = CreatePermit();
        permit.Revoke("Violation");
        permit.ClearEvents();
        var renewal = new RenewalInfo(DateOnly.FromDateTime(DateTime.UtcNow), 175m, null);

        // Act
        var result = permit.Renew(renewal, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));

        // Assert
        result.IsFailure.Should().BeTrue();
        permit.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Revoke_ShouldSucceed_WhenActive()
    {
        // Arrange
        var permit = CreatePermit();
        permit.ClearEvents();

        // Act
        var result = permit.Revoke("Health code violation");

        // Assert
        result.IsSuccess.Should().BeTrue();
        permit.Status.Should().Be(PermitStatus.Revoked);
        permit.UncommittedEvents.Should().ContainSingle(e => e is PermitRevoked);
    }

    [Fact]
    public void Revoke_ShouldFail_WhenAlreadyRevoked()
    {
        // Arrange
        var permit = CreatePermit();
        permit.Revoke("First violation");
        permit.ClearEvents();

        // Act
        var result = permit.Revoke("Second violation");

        // Assert
        result.IsFailure.Should().BeTrue();
        permit.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkExpired_ShouldSetStatusToExpired()
    {
        // Arrange
        var permit = CreatePermit();
        permit.ClearEvents();

        // Act
        permit.MarkExpired();

        // Assert
        permit.Status.Should().Be(PermitStatus.Expired);
        permit.UncommittedEvents.Should().ContainSingle(e => e is PermitExpired);
    }
}
