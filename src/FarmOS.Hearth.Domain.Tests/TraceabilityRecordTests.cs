using System;
using System.Linq;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Aggregates;
using FarmOS.Hearth.Domain.Events;
using FarmOS.SharedKernel;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.Domain.Tests;

public class TraceabilityRecordTests
{
    private static readonly Quantity SampleAmount = new(100m, "lbs", "weight");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void LogReceiving_ShouldCreateRecord_WithCorrectKDEs()
    {
        var record = TraceabilityRecord.LogReceiving(
            ProductCategory.Wheat, "Heritage Red Fife", "WHT-20260316-01", SampleAmount, "Local Mill Co.", Now);

        record.EventType.Should().Be(CriticalTrackingEvent.Receiving);
        record.Category.Should().Be(ProductCategory.Wheat);
        record.ProductDescription.Should().Be("Heritage Red Fife");
        record.LotId.Should().Be("WHT-20260316-01");
        record.Amount.Should().Be(SampleAmount);
        record.SourceLocation.Should().Be("Local Mill Co.");
        record.DestinationLocation.Should().BeNull();
        record.SourceLotId.Should().BeNull();
        record.RecordedAt.Should().Be(Now);
    }

    [Fact]
    public void LogTransformation_ShouldLinkSourceLot()
    {
        var record = TraceabilityRecord.LogTransformation(
            ProductCategory.Sourdough, "Sourdough Starter Batch", "SOUR-20260316-01", SampleAmount, "WHT-20260316-01", Now);

        record.EventType.Should().Be(CriticalTrackingEvent.Transformation);
        record.LotId.Should().Be("SOUR-20260316-01");
        record.SourceLotId.Should().Be("WHT-20260316-01");
        record.SourceLocation.Should().BeNull();
        record.DestinationLocation.Should().BeNull();
    }

    [Fact]
    public void LogShipping_ShouldSetDestination()
    {
        var record = TraceabilityRecord.LogShipping(
            ProductCategory.Sourdough, "Baked Loaves", "SOUR-SHP-001", SampleAmount, "B2C EdgePortal", Now);

        record.EventType.Should().Be(CriticalTrackingEvent.Shipping);
        record.DestinationLocation.Should().Be("B2C EdgePortal");
        record.SourceLocation.Should().BeNull();
        record.SourceLotId.Should().BeNull();
    }

    [Fact]
    public void LogReceiving_ShouldRaiseTraceabilityEventLogged()
    {
        var record = TraceabilityRecord.LogReceiving(
            ProductCategory.Mushroom, "Lion's Mane Spawn", "MUSH-20260316-01", SampleAmount, "Spawn Supplier", Now);

        var @event = record.UncommittedEvents.OfType<TraceabilityEventLogged>().Single();
        @event.EventType.Should().Be(CriticalTrackingEvent.Receiving);
        @event.Category.Should().Be(ProductCategory.Mushroom);
        @event.ProductDescription.Should().Be("Lion's Mane Spawn");
        @event.LotId.Should().Be("MUSH-20260316-01");
        @event.Amount.Should().Be(SampleAmount);
        @event.SourceLocation.Should().Be("Spawn Supplier");
    }

    [Fact]
    public void FactoryMethod_ShouldSetVersionToOne()
    {
        var record = TraceabilityRecord.LogReceiving(
            ProductCategory.Ingredients, "Organic Flour", "INGR-20260316-01", SampleAmount, "Flour Mill", Now);

        record.Version.Should().Be(1);
        record.UncommittedEvents.Should().HaveCount(1);
    }

    [Fact]
    public void AllThreeEventTypes_ShouldProduceDistinctRecords()
    {
        var receiving = TraceabilityRecord.LogReceiving(
            ProductCategory.Wheat, "Red Fife", "WHT-01", SampleAmount, "Mill", Now);
        var transformation = TraceabilityRecord.LogTransformation(
            ProductCategory.Sourdough, "Starter", "SOUR-01", SampleAmount, "WHT-01", Now);
        var shipping = TraceabilityRecord.LogShipping(
            ProductCategory.Sourdough, "Loaves", "SOUR-SHP-01", SampleAmount, "Market", Now);

        receiving.Id.Should().NotBe(transformation.Id);
        transformation.Id.Should().NotBe(shipping.Id);
        receiving.EventType.Should().Be(CriticalTrackingEvent.Receiving);
        transformation.EventType.Should().Be(CriticalTrackingEvent.Transformation);
        shipping.EventType.Should().Be(CriticalTrackingEvent.Shipping);
    }

    // ─── Retention / Expiration Tests ────────────────────────────────

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenPastTwoYears()
    {
        var twoYearsAgo = DateTimeOffset.UtcNow.AddDays(-731);
        var record = TraceabilityRecord.LogReceiving(
            ProductCategory.Wheat, "Old Flour", "WHT-OLD-001", SampleAmount, "Mill", twoYearsAgo);

        record.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenWithinRetentionWindow()
    {
        var sixMonthsAgo = DateTimeOffset.UtcNow.AddDays(-180);
        var record = TraceabilityRecord.LogReceiving(
            ProductCategory.Wheat, "Recent Flour", "WHT-NEW-001", SampleAmount, "Mill", sixMonthsAgo);

        record.IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void RetentionDays_ShouldDefaultTo730_ForStandardRecords()
    {
        var record = TraceabilityRecord.LogReceiving(
            ProductCategory.Sourdough, "Starter", "SOUR-001", SampleAmount, "Kitchen", Now);

        record.RetentionDays.Should().Be(730);
        record.IsDirectToConsumer.Should().BeFalse();
    }
}
