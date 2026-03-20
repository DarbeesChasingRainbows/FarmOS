using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NSubstitute;
using FarmOS.Flora.Domain;
using FarmOS.Flora.Domain.Aggregates;
using Xunit;

namespace FarmOS.Flora.API.Tests;

public class RecipeEndpointsTests : IClassFixture<FloraWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FloraWebApplicationFactory _factory;

    public RecipeEndpointsTests(FloraWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_CreateRecipe_ShouldReturn201()
    {
        var response = await _client.PostAsJsonAsync("/api/flora/recipes", new
        {
            name = "Summer Market Bouquet",
            category = "market"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task Post_AddRecipeItem_ShouldReturn204()
    {
        var recipe = BouquetRecipe.Create("Test Bouquet", "wedding");
        recipe.ClearEvents();

        _factory.MockFloraEventStore
            .LoadBouquetRecipeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipe));

        var recipeId = recipe.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/recipes/{recipeId}/items", new
        {
            recipeId,
            item = new
            {
                species = "Dahlia",
                cultivar = "Café au Lait",
                stemCount = 3,
                color = "Blush",
                role = "focal"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_RemoveRecipeItem_ShouldReturn204()
    {
        var recipe = BouquetRecipe.Create("Test Bouquet", "market");
        recipe.AddItem(new RecipeItem("Ammi", "Dara", 5, "White", "filler"));
        recipe.ClearEvents();

        _factory.MockFloraEventStore
            .LoadBouquetRecipeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipe));

        var recipeId = recipe.Id.Value;
        var response = await _client.DeleteAsync($"/api/flora/recipes/{recipeId}/items/Ammi/Dara");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_MakeBouquet_ShouldReturn204()
    {
        var recipe = BouquetRecipe.Create("Saturday Special", "market");
        recipe.AddItem(new RecipeItem("Dahlia", "Café au Lait", 3, "Blush", "focal"));
        recipe.ClearEvents();

        _factory.MockFloraEventStore
            .LoadBouquetRecipeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipe));

        var recipeId = recipe.Id.Value;
        var response = await _client.PostAsJsonAsync($"/api/flora/recipes/{recipeId}/make", new
        {
            recipeId,
            quantity = 10,
            date = "2026-07-25",
            notes = "Saturday farmers market"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
