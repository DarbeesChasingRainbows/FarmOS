using System;
using System.Linq;
using FarmOS.Compliance.Domain;
using FarmOS.Compliance.Domain.Aggregates;
using FarmOS.Compliance.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Compliance.Domain.Tests;

public class InsurancePolicyTests
{
    private static readonly IReadOnlyList<CoverageDetail> DefaultCoverages =
    [
        new("General Liability", 1_000_000m, 5_000m),
        new("Property Damage", 500_000m, 2_500m)
    ];

    private static InsurancePolicy CreatePolicy() =>
        InsurancePolicy.Register(PolicyType.GeneralLiability, "Farm Bureau", "POL-2026-001",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            2_400m, DefaultCoverages);

    [Fact]
    public void Register_ShouldCreateActivePolicyWithCoverages()
    {
        // Arrange & Act
        var policy = CreatePolicy();

        // Assert
        policy.Status.Should().Be(PolicyStatus.Active);
        policy.Type.Should().Be(PolicyType.GeneralLiability);
        policy.Provider.Should().Be("Farm Bureau");
        policy.PolicyNumber.Should().Be("POL-2026-001");
        policy.AnnualPremium.Should().Be(2_400m);
        policy.Coverages.Should().HaveCount(2);

        var @event = policy.UncommittedEvents.OfType<PolicyRegistered>().Single();
        @event.Id.Should().Be(policy.Id);
    }

    [Fact]
    public void Renew_ShouldSucceed_WhenActive()
    {
        // Arrange
        var policy = CreatePolicy();
        policy.ClearEvents();
        var newExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));

        // Act
        var result = policy.Renew(newExpiry, 2_600m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        policy.ExpiryDate.Should().Be(newExpiry);
        policy.AnnualPremium.Should().Be(2_600m);
        policy.UncommittedEvents.Should().ContainSingle(e => e is PolicyRenewed);
    }

    [Fact]
    public void Renew_ShouldFail_WhenCancelled()
    {
        // Arrange — rehydrate a policy into Cancelled state via a PolicyCancelled-like flow
        var policy = CreatePolicy();
        policy.Cancel("No longer needed");
        policy.ClearEvents();

        // Act
        var result = policy.Renew(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), 2_500m);

        // Assert
        result.IsFailure.Should().BeTrue();
        policy.Status.Should().Be(PolicyStatus.Cancelled);
        policy.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateCoverages_ShouldReplaceExistingCoverages()
    {
        // Arrange
        var policy = CreatePolicy();
        policy.ClearEvents();
        var newCoverages = new List<CoverageDetail>
        {
            new("Umbrella", 2_000_000m, 10_000m)
        };

        // Act
        policy.UpdateCoverages(newCoverages);

        // Assert
        policy.Coverages.Should().HaveCount(1);
        policy.Coverages[0].CoverageType.Should().Be("Umbrella");
        policy.UncommittedEvents.Should().ContainSingle(e => e is PolicyCoverageUpdated);
    }
}
