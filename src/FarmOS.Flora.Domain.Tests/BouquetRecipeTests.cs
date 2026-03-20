using System;
using System.Linq;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using FarmOS.Flora.Domain.Events;
using FluentAssertions;
using Xunit;

namespace FarmOS.Flora.Domain.Tests;

public class BouquetRecipeTests
{
    private static BouquetRecipe CreateRecipe() =>
        BouquetRecipe.Create("Summer Market Bouquet", "market");

    private static RecipeItem FocalItem() =>
        new("Dahlia", "Café au Lait", 3, "Blush", "focal");

    private static RecipeItem FillerItem() =>
        new("Ammi", "Dara", 5, "White", "filler");

    private static RecipeItem GreeneryItem() =>
        new("Eucalyptus", "Silver Dollar", 3, null, "greenery");

    private static RecipeItem AccentItem() =>
        new("Scabiosa", "Fata Morgana", 2, "Lavender", "accent");

    [Fact]
    public void Create_ShouldSetNameAndCategory()
    {
        var recipe = CreateRecipe();

        recipe.Name.Should().Be("Summer Market Bouquet");
        recipe.Category.Should().Be("market");
        recipe.Items.Should().BeEmpty();
        recipe.TotalStemsPerBouquet.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldRaiseBouquetRecipeCreatedEvent()
    {
        var recipe = CreateRecipe();

        recipe.UncommittedEvents.Should().ContainSingle(e => e is BouquetRecipeCreated);
    }

    [Fact]
    public void AddItem_ShouldBuildCompleteRecipe()
    {
        // Industry practice: focal (hero flowers), filler (volume), greenery, accent
        var recipe = CreateRecipe();
        recipe.ClearEvents();

        recipe.AddItem(FocalItem());      // 3 stems
        recipe.AddItem(FillerItem());     // 5 stems
        recipe.AddItem(GreeneryItem());   // 3 stems
        recipe.AddItem(AccentItem());     // 2 stems

        recipe.Items.Should().HaveCount(4);
        recipe.TotalStemsPerBouquet.Should().Be(13);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveBySpeciesAndCultivar()
    {
        var recipe = CreateRecipe();
        recipe.AddItem(FocalItem());
        recipe.AddItem(FillerItem());
        recipe.ClearEvents();

        recipe.RemoveItem("Ammi", "Dara");

        recipe.Items.Should().HaveCount(1);
        recipe.Items[0].Species.Should().Be("Dahlia");
    }

    [Fact]
    public void MakeBouquet_ShouldRaiseBouquetMadeEvent()
    {
        var recipe = CreateRecipe();
        recipe.AddItem(FocalItem());
        recipe.AddItem(FillerItem());
        recipe.ClearEvents();

        recipe.MakeBouquet(10, new DateOnly(2026, 7, 20), "Saturday farmers market");

        var @event = recipe.UncommittedEvents.OfType<BouquetMade>().Single();
        @event.Quantity.Should().Be(10);
        @event.Date.Should().Be(new DateOnly(2026, 7, 20));
        @event.Notes.Should().Be("Saturday farmers market");
    }

    [Fact]
    public void TotalStemsPerBouquet_ShouldRecalculateAfterRemoval()
    {
        var recipe = CreateRecipe();
        recipe.AddItem(FocalItem());      // 3
        recipe.AddItem(FillerItem());     // 5

        recipe.TotalStemsPerBouquet.Should().Be(8);

        recipe.RemoveItem("Ammi", "Dara");

        recipe.TotalStemsPerBouquet.Should().Be(3);
    }
}
