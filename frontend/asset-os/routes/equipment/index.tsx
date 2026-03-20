import { Head } from "fresh/runtime";
import { define } from "../../utils.ts";
import EquipmentPanel from "../../islands/EquipmentPanel.tsx";
import RegisterEquipmentForm from "../../islands/RegisterEquipmentForm.tsx";

export default define.page(function EquipmentPage() {
  return (
    <div class="p-8">
      <Head>
        <title>Equipment — Asset OS</title>
      </Head>

      <div class="flex items-start justify-between mb-8">
        <div>
          <h1 class="text-3xl font-extrabold text-stone-800 tracking-tight">
            🚜 Equipment
          </h1>
          <p class="text-stone-500 mt-1">
            Register, track and maintain all farm equipment.
          </p>
        </div>
        <RegisterEquipmentForm />
      </div>

      <EquipmentPanel />
    </div>
  );
});
