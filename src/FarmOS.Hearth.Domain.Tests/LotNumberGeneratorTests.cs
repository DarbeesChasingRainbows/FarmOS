using System;
using FarmOS.Hearth.Domain;
using FarmOS.Hearth.Domain.Services;
using FluentAssertions;
using Xunit;

namespace FarmOS.Hearth.Domain.Tests;

public class LotNumberGeneratorTests
{
    private static readonly DateOnly SampleDate = new(2026, 3, 16);

    [Fact]
    public void GenerateLotNumber_ShouldFormatCorrectly()
    {
        var lot = LotNumberGenerator.GenerateLotNumber(ProductCategory.Mushroom, SampleDate, 1);

        lot.Should().Be("MUSH-20260316-01");
    }

    [Theory]
    [InlineData(ProductCategory.Mushroom, "MUSH")]
    [InlineData(ProductCategory.Jun, "JUN")]
    [InlineData(ProductCategory.Kombucha, "KOMB")]
    [InlineData(ProductCategory.Sourdough, "SOUR")]
    [InlineData(ProductCategory.Beef, "BEEF")]
    [InlineData(ProductCategory.Wheat, "WHEAT")]
    [InlineData(ProductCategory.Ingredients, "INGR")]
    [InlineData(ProductCategory.Other, "MISC")]
    public void GenerateLotNumber_ShouldUseCorrectPrefix_ForEachCategory(ProductCategory category, string expectedPrefix)
    {
        var lot = LotNumberGenerator.GenerateLotNumber(category, SampleDate, 1);

        lot.Should().StartWith($"{expectedPrefix}-");
    }

    [Theory]
    [InlineData(1, "01")]
    [InlineData(5, "05")]
    [InlineData(12, "12")]
    [InlineData(99, "99")]
    public void GenerateLotNumber_ShouldPadBatchNumber(int batchNumber, string expectedSuffix)
    {
        var lot = LotNumberGenerator.GenerateLotNumber(ProductCategory.Sourdough, SampleDate, batchNumber);

        lot.Should().EndWith($"-{expectedSuffix}");
    }

    [Fact]
    public void GenerateLotNumber_ShouldIncludeDateInYYYYMMDD()
    {
        var date = new DateOnly(2025, 12, 1);
        var lot = LotNumberGenerator.GenerateLotNumber(ProductCategory.Jun, date, 3);

        lot.Should().Be("JUN-20251201-03");
    }
}
