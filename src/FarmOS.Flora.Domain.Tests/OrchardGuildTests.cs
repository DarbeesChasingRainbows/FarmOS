using System;
using System.Linq;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FarmOS.SharedKernel;
using FluentAssertions;
using Xunit;

namespace FarmOS.Flora.Domain.Tests;

public class OrchardGuildTests
{
    private static OrchardGuild CreateGuild() =>
        OrchardGuild.Create("Apple Guild #1", GuildType.Trio, new GeoPosition(40.7, -74.0), new DateOnly(2024, 4, 15));

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var guild = CreateGuild();

        guild.Name.Should().Be("Apple Guild #1");
        guild.Type.Should().Be(GuildType.Trio);
        guild.Position.Latitude.Should().Be(40.7);
        guild.Planted.Should().Be(new DateOnly(2024, 4, 15));
    }

    [Fact]
    public void Create_ShouldRaiseGuildCreatedEvent()
    {
        var guild = CreateGuild();

        guild.UncommittedEvents.Should().ContainSingle(e => e is GuildCreated);
    }

    [Fact]
    public void AddMember_ShouldAddToMembersList()
    {
        var guild = CreateGuild();
        guild.ClearEvents();

        guild.AddMember(new GuildMember(PlantId.New(), "Comfrey", "Bocking 14", GuildRole.DynamicAccumulator));

        guild.Members.Should().HaveCount(1);
        guild.Members[0].Species.Should().Be("Comfrey");
        guild.Members[0].Role.Should().Be(GuildRole.DynamicAccumulator);
    }

    [Fact]
    public void AddMember_ShouldRaiseGuildMemberAddedEvent()
    {
        var guild = CreateGuild();
        guild.ClearEvents();

        guild.AddMember(new GuildMember(PlantId.New(), "White Clover", "Dutch", GuildRole.NitrogenFixer));

        guild.UncommittedEvents.Should().ContainSingle(e => e is GuildMemberAdded);
    }
}
