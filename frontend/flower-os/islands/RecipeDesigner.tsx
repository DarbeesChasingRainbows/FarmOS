import { useSignal } from "@preact/signals";
import type { RecipeDetail, RecipeSummary } from "../utils/farmos-client.ts";
import StatusBadge from "../components/StatusBadge.tsx";

const ROLE_COLORS: Record<string, string> = {
  focal: "bg-rose-100 text-rose-700",
  filler: "bg-amber-100 text-amber-700",
  greenery: "bg-emerald-100 text-emerald-700",
  accent: "bg-violet-100 text-violet-700",
};

export default function RecipeDesigner() {
  const recipes = useSignal<RecipeSummary[]>([]);
  const selectedRecipe = useSignal<RecipeDetail | null>(null);
  const loading = useSignal(true);
  const error = useSignal("");
  const showCreateForm = useSignal(false);

  // Create form
  const newName = useSignal("");
  const newCategory = useSignal("market");

  const loadRecipes = async () => {
    loading.value = true;
    error.value = "";
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const result = await FloraAPI.getRecipes();
      recipes.value = result ?? [];
    } catch (err) {
      error.value = err instanceof Error
        ? err.message
        : "Failed to load recipes";
    } finally {
      loading.value = false;
    }
  };

  const selectRecipe = async (id: string) => {
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      const detail = await FloraAPI.getRecipe(id);
      selectedRecipe.value = detail;
    } catch (err) {
      error.value = err instanceof Error
        ? err.message
        : "Failed to load recipe";
    }
  };

  const createRecipe = async () => {
    if (!newName.value.trim()) return;
    try {
      const { FloraAPI } = await import("../utils/farmos-client.ts");
      await FloraAPI.createRecipe({
        name: newName.value,
        category: newCategory.value,
      });
      showCreateForm.value = false;
      newName.value = "";
      await loadRecipes();
    } catch (err) {
      error.value = err instanceof Error
        ? err.message
        : "Failed to create recipe";
    }
  };

  if (loading.value && recipes.value.length === 0) {
    loadRecipes();
  }

  return (
    <div>
      {error.value && (
        <div class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {error.value}
          <button
            onClick={() => (error.value = "")}
            class="ml-2 text-red-500 hover:text-red-700"
          >
            ✕
          </button>
        </div>
      )}

      <div class="flex gap-6">
        {/* Recipe List */}
        <div class="w-80 flex-shrink-0">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide">
              Recipes
            </h3>
            <button
              onClick={() => (showCreateForm.value = !showCreateForm.value)}
              class="px-3 py-1.5 text-sm font-medium bg-rose-600 text-white rounded-lg hover:bg-rose-700 transition-colors"
            >
              + New Recipe
            </button>
          </div>

          {showCreateForm.value && (
            <div class="mb-4 p-4 bg-white rounded-xl border border-stone-200 space-y-3">
              <input
                type="text"
                placeholder="Recipe name"
                value={newName.value}
                onInput={(
                  e,
                ) => (newName.value = (e.target as HTMLInputElement).value)}
                class="w-full px-3 py-2 text-sm border border-stone-300 rounded-lg focus:ring-2 focus:ring-rose-500 focus:border-rose-500"
              />
              <select
                value={newCategory.value}
                onChange={(
                  e,
                ) => (newCategory.value =
                  (e.target as HTMLSelectElement).value)}
                class="w-full px-3 py-2 text-sm border border-stone-300 rounded-lg"
              >
                <option value="market">Farmers Market</option>
                <option value="wedding">Wedding</option>
                <option value="csa">CSA</option>
                <option value="wholesale">Wholesale</option>
              </select>
              <div class="flex gap-2">
                <button
                  onClick={createRecipe}
                  class="flex-1 px-3 py-2 text-sm font-medium bg-rose-600 text-white rounded-lg hover:bg-rose-700"
                >
                  Create
                </button>
                <button
                  onClick={() => (showCreateForm.value = false)}
                  class="px-3 py-2 text-sm text-stone-500 hover:text-stone-700"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          {loading.value
            ? (
              <div class="text-center py-8 text-stone-400">
                Loading recipes...
              </div>
            )
            : recipes.value.length === 0
            ? (
              <div class="text-center py-8 text-stone-400">
                <p class="text-lg">No recipes yet</p>
                <p class="text-sm mt-1">Design your first bouquet recipe.</p>
              </div>
            )
            : (
              <div class="space-y-2">
                {recipes.value.map((recipe) => (
                  <button
                    key={recipe.id}
                    onClick={() => selectRecipe(recipe.id)}
                    class={`w-full text-left p-4 rounded-xl border transition-all duration-150 ${
                      selectedRecipe.value?.id === recipe.id
                        ? "bg-rose-50 border-rose-300 shadow-sm"
                        : "bg-white border-stone-200 hover:border-rose-200 hover:shadow-sm"
                    }`}
                  >
                    <div class="flex items-center justify-between">
                      <span class="font-semibold text-stone-800">
                        {recipe.name}
                      </span>
                      <StatusBadge status="active" label={recipe.category} />
                    </div>
                    <div class="text-sm text-stone-500 mt-1">
                      {recipe.itemCount} items · {recipe.totalStemsPerBouquet}
                      {" "}
                      stems/bouquet
                    </div>
                  </button>
                ))}
              </div>
            )}
        </div>

        {/* Recipe Detail */}
        <div class="flex-1">
          {selectedRecipe.value
            ? <RecipeDetailView recipe={selectedRecipe.value} />
            : (
              <div class="flex items-center justify-center h-64 text-stone-400 bg-white rounded-xl border border-dashed border-stone-300">
                <p>Select a recipe to view its design</p>
              </div>
            )}
        </div>
      </div>
    </div>
  );
}

function RecipeDetailView({ recipe }: { recipe: RecipeDetail }) {
  return (
    <div
      class="bg-white rounded-xl border border-stone-200 p-6"
      style="animation: slideIn 0.2s ease-out"
    >
      <div class="flex items-center justify-between mb-6">
        <div>
          <h2 class="text-2xl font-bold text-stone-800">{recipe.name}</h2>
          <p class="text-stone-500 text-sm">
            {recipe.category} · {recipe.totalStemsPerBouquet} stems per bouquet
          </p>
        </div>
        <div class="text-right">
          <div class="text-2xl font-bold text-rose-600">
            {recipe.totalBouquetsMade}
          </div>
          <div class="text-xs text-stone-400">bouquets made</div>
        </div>
      </div>

      {/* Recipe Items */}
      <h3 class="text-sm font-semibold text-stone-500 uppercase tracking-wide mb-3">
        Stem Components
      </h3>
      {recipe.items.length === 0
        ? (
          <div class="text-center py-6 text-stone-400 border border-dashed border-stone-200 rounded-lg">
            Add focal, filler, greenery, and accent stems to complete this
            recipe.
          </div>
        )
        : (
          <div class="space-y-2">
            {recipe.items.map((item, i) => (
              <div
                key={i}
                class="flex items-center justify-between p-3 bg-stone-50 rounded-lg border border-stone-100"
              >
                <div class="flex items-center gap-3">
                  <span
                    class={`px-2 py-0.5 rounded text-xs font-medium ${
                      ROLE_COLORS[item.role] ?? "bg-stone-100 text-stone-600"
                    }`}
                  >
                    {item.role}
                  </span>
                  <div>
                    <span class="font-medium text-stone-800">
                      {item.species}
                    </span>
                    <span class="text-stone-500 ml-1">'{item.cultivar}'</span>
                    {item.color && (
                      <span class="text-xs text-stone-400 ml-1">
                        ({item.color})
                      </span>
                    )}
                  </div>
                </div>
                <span class="text-sm font-semibold text-stone-700">
                  {item.stemCount} stems
                </span>
              </div>
            ))}
          </div>
        )}
    </div>
  );
}
