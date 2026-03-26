import { useEffect, useRef } from "preact/hooks";
import { reactive, html } from "@arrow-js/core";

export default function ArrowExperiment() {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    
    // Clear in development cleanly to avoid HMR duplication
    containerRef.current.innerHTML = '';

    // Create Arrow.js reactive state
    const state = reactive({
      clicks: 0,
      time: new Date().toLocaleTimeString(),
      mousePos: { x: 0, y: 0 }
    });

    // Simple reactive clock
    const timer = setInterval(() => {
      state.time = new Date().toLocaleTimeString();
    }, 1000);

    const handleMouseMove = (e: MouseEvent) => {
      // Because we are inside a bounded container, we'll track relative mouse moves on the card
      const rect = containerRef.current!.getBoundingClientRect();
      state.mousePos.x = Math.round(e.clientX - rect.left);
      state.mousePos.y = Math.round(e.clientY - rect.top);
    };

    // Build the reactive template using Tagged Template Literals (Arrow.js magic)
    const template = html`
      <div 
        @mousemove="${handleMouseMove}"
        class="bg-white rounded-xl border-2 border-dashed border-amber-300 shadow-sm p-6 relative overflow-hidden"
      >
        <div class="absolute top-0 right-0 bg-amber-100 text-amber-800 text-[10px] uppercase font-bold px-2 py-1 tracking-widest rounded-bl-lg">
          Powered by Arrow.js
        </div>

        <h3 class="text-xl font-bold text-stone-800 mb-2 mt-2">Zero-Vdom UI Experiment</h3>
        <p class="text-stone-500 text-sm mb-6 max-w-xl">
          This entire block is rendered and hydrated completely by <strong>Arrow.js</strong> traversing standard DOM nodes. Preact acts simply as a mounting wrapper. Look at how fast and cleanly it tracks the mouse inside this box!
        </p>
        
        <div class="flex flex-wrap items-center gap-6">
          <div class="bg-stone-50 border border-stone-100 p-4 rounded-xl text-center min-w-32 group cursor-pointer transition" @click="${() => state.clicks++}">
            <div class="text-4xl font-mono font-extrabold text-amber-600 group-hover:scale-110 transition-transform">${() => state.clicks}</div>
            <div class="text-xs font-bold text-stone-400 uppercase tracking-widest mt-2">Clicks</div>
          </div>
          
          <div class="bg-stone-50 border border-stone-100 p-4 rounded-xl text-center min-w-32">
            <div class="text-xl font-mono font-bold text-emerald-600 mt-2">${() => state.mousePos.x}, ${() => state.mousePos.y}</div>
            <div class="text-xs font-bold text-stone-400 uppercase tracking-widest mt-2">Local X/Y</div>
          </div>

          <button 
            @click="${() => state.clicks += 10}"
            class="px-5 py-3 bg-stone-800 text-white font-medium rounded-xl hover:bg-stone-700 transition shadow-sm active:translate-y-0.5"
          >
            Add +10
          </button>
        </div>

        <div class="mt-6 pt-4 border-t border-stone-100 flex items-center justify-between text-xs text-stone-400">
          <span>Sub-node granular updates</span>
          <span class="font-mono bg-stone-100 px-2 py-1 rounded text-stone-500">${() => state.time}</span>
        </div>
      </div>
    `;

    // Initialize/Mount it inside our Preact-controlled div
    template(containerRef.current);

    return () => clearInterval(timer);
  }, []);

  return <div ref={containerRef}></div>;
}
