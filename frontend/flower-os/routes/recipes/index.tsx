import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import NavBar from "../../components/NavBar.tsx";
import RecipeDesigner from "../../islands/RecipeDesigner.tsx";

export default define.page(function RecipesPage() {
  return (
    <>
      <Head>
        <title>Bouquet Recipes — Flower OS</title>
      </Head>
      <NavBar current="/recipes" />

      <div class="p-8">
        <div class="flex items-center justify-between mb-8">
          <div>
            <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
              💐 Bouquet Recipes
            </h1>
            <p class="text-stone-500 mt-1">
              Design bouquets with focal, filler, greenery, and accent stems.
            </p>
          </div>
        </div>

        <RecipeDesigner />
      </div>
    </>
  );
});
