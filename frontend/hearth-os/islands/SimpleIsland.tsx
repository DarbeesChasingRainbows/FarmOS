import { useEffect, useState } from "preact/hooks";

export default function SimpleIsland() {
  const [count, setCount] = useState(0);

  useEffect(() => {
    console.log("SimpleIsland useEffect fired!");
  }, []);

  return (
    <div class="p-4 bg-red-100 border border-red-500 rounded my-4">
      <h2 class="text-xl font-bold">Simple Island Test</h2>
      <button
        type="button"
        class="bg-red-500 text-white px-4 py-2 rounded mt-2 cursor-pointer"
        onClick={() => {
          console.log("Button clicked!");
          setCount(count + 1);
        }}
      >
        Clicks: {count}
      </button>
    </div>
  );
}
