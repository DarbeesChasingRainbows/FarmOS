# Arrow.js Island Pattern — FarmOS Frontends

> The standard pattern for all interactive UI components across FarmOS micro-frontends.

---

## Why Arrow.js?

FarmOS migrated from Preact Signals to [Arrow.js](https://www.arrow-js.com/) (`@arrow-js/core@1.0.0-alpha.9`) for island reactivity. Key benefits:

- **Zero dependencies** — ~2KB, no virtual DOM overhead
- **Fine-grained reactivity** — only the DOM nodes that depend on changed state re-render
- **Tagged template literals** — `html` templates compile directly to DOM operations, no JSX transform needed for the reactive layer
- **Co-located state** — `reactive()` objects keep all island state in one place

Arrow.js handles **reactivity and DOM rendering**. The outer Preact shell is only used as the Fresh island entry point (required by Deno Fresh's island hydration system).

---

## The Pattern

Every island follows this exact structure:

```tsx
import { useEffect, useRef } from "preact/hooks";
import { html, reactive } from "@arrow-js/core";

export default function ArrowMyIsland(props: { someParam?: string }) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    containerRef.current.innerHTML = "";

    // 1. Create reactive state
    const state = reactive({
      items: [] as Item[],
      showForm: false,
      selectedId: null as string | null,
      errors: {} as Record<string, string>,
      submitting: false,
    });

    // 2. Define event handlers
    const handleSubmit = async (e: Event) => {
      e.preventDefault();
      const form = e.target as HTMLFormElement;
      const fd = new FormData(form);
      // validate, call API, update state...
    };

    // 3. Mount Arrow template
    html`
      <div>
        <h2>${props.someParam ?? "Default Title"}</h2>
        
        <!-- Reactive text -->
        <p>Count: ${() => state.items.length}</p>
        
        <!-- Conditional rendering -->
        ${() => state.showForm ? html`
          <form @submit="${handleSubmit}">
            <input name="field" class="..." placeholder="...">
            <button type="submit">
              ${() => state.submitting ? "Saving..." : "Submit"}
            </button>
          </form>
        ` : html`<span></span>`}
        
        <!-- List rendering with keys -->
        ${() => state.items.map(item => html`
          <div class="...">
            <p>${item.name}</p>
            <button @click="${() => state.selectedId = item.id}">
              Select
            </button>
          </div>
        `.key(item.id))}
      </div>
    `(containerRef.current);  // <-- Mount to DOM
  }, []);

  return <div ref={containerRef} />;
}
```

---

## Key Concepts

### Reactive State

```tsx
const state = reactive({
  count: 0,
  name: "",
  items: [] as string[],
});

// Mutations trigger re-renders automatically:
state.count++;                        // Reactive — updates DOM
state.items = [...state.items, "x"];  // Reactive — must replace array (no .push())
```

> **Important**: Arrow.js proxies property assignments. You must **replace** arrays/objects rather than mutating them in-place. `state.items.push(x)` will NOT trigger reactivity — use `state.items = [...state.items, x]`.

### Event Binding

Arrow uses `@event` syntax (not `onClick`):

```html
<!-- Click -->
<button @click="${() => state.showForm = true}">Show</button>

<!-- Form submit -->
<form @submit="${handleSubmit}">...</form>

<!-- Input binding -->
<input @input="${(e: Event) => state.name = (e?.target as HTMLInputElement)?.value ?? ''}">
```

### Conditional Rendering

Use a function wrapper `${() => condition ? html`...` : html`<span></span>`}`:

```tsx
${() => state.showForm ? html`
  <form>...</form>
` : html`<span></span>`}
```

> **Note**: The falsy branch must return an Arrow template, not `null` or `""`. Use `html\`<span></span>\`` as the empty placeholder.

### List Rendering

Use `.map()` with `.key()` for efficient DOM diffing:

```tsx
${() => state.items.map(item => html`
  <div>${item.name}</div>
`.key(item.id))}
```

### Dynamic Classes

Use a function wrapper for reactive class strings:

```tsx
<div class="${() => `base-class ${state.active ? 'bg-blue-500' : 'bg-gray-500'}`}">
```

For static classes (no reactivity needed), use plain strings:

```tsx
<div class="px-4 py-2 rounded-lg">
```

---

## Common Patterns

### Sidebar / Slide-out Panel

```tsx
const state = reactive({
  selectedId: null as string | null,
  sidebarOpen: false,
});

const openSidebar = (id: string) => {
  state.selectedId = id;
  state.sidebarOpen = true;
};

const closeSidebar = () => {
  state.sidebarOpen = false;
  setTimeout(() => { state.selectedId = null; }, 300); // Wait for animation
};

html`
  <!-- Main content shifts when sidebar opens -->
  <div class="${() => `flex-1 transition-all ${state.sidebarOpen ? 'mr-[400px]' : ''}`}">
    ...
  </div>

  <!-- Sidebar -->
  <aside class="${() => `fixed right-0 top-0 h-full w-[380px] transform transition-transform ${
    state.sidebarOpen ? 'translate-x-0' : 'translate-x-full'
  }`}">
    ${() => {
      const item = state.items.find(i => i.id === state.selectedId);
      if (!item) return html`<span></span>`;
      return html`<div>...detail view for ${item.name}...</div>`;
    }}
  </aside>
`(containerRef.current);
```

### Modal Dialog

```tsx
${() => state.showModal ? html`
  <div class="fixed inset-0 bg-stone-900/50 backdrop-blur-sm flex items-center justify-center z-50"
    @click="${(e: Event) => { if (e.target === e.currentTarget) state.showModal = false; }}">
    <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4">
      <form @submit="${handleSubmit}" class="p-6">
        ...
      </form>
    </div>
  </div>
` : html`<span></span>`}
```

### Form Validation with Error Display

```tsx
const state = reactive({
  errors: {} as Record<string, string>,
});

const handleSubmit = (e: Event) => {
  e.preventDefault();
  const fd = new FormData(e.target as HTMLFormElement);
  const errs: Record<string, string> = {};
  if (!fd.get("name")) errs.name = "Required";
  if (Object.keys(errs).length) { state.errors = errs; return; }
  state.errors = {};
  // proceed...
};

// In template:
html`
  <input name="name" class="...">
  ${() => state.errors.name
    ? html`<p class="text-red-500 text-xs mt-1">${state.errors.name}</p>`
    : html`<span></span>`}
`
```

### Async Data Fetching

```tsx
useEffect(() => {
  // ... state and template setup ...

  // Fetch data after mount
  async function loadData() {
    try {
      const { MyAPI } = await import("../utils/farmos-client.ts");
      state.items = await MyAPI.getItems();
    } catch { /* handle */ }
  }
  loadData();

  // Optional polling
  const interval = setInterval(loadData, 30_000);
  return () => clearInterval(interval);
}, []);
```

---

## Naming Convention

All island files are prefixed with `Arrow`:
- `ArrowBatchDetailPanel.tsx`
- `ArrowSanitationLog.tsx`
- `ArrowEquipmentPanel.tsx`

This prefix was introduced during the migration from Preact. Now that all islands are Arrow-based, the prefix serves as a clear indicator of the component framework used.

---

## TypeScript Notes

Arrow.js types are strict about `ReactiveProxy`. You may see TS warnings when reassigning properties like:

```tsx
state.errors = {};           // TS warns: '{}' not assignable to ReactiveProxy<...>
state.items = [...newItems];  // TS warns about ReactiveProxy array types
```

These are **type-level only** — Arrow.js handles the proxy wrapping at runtime. The code works correctly. These warnings are acknowledged and accepted across the codebase.

---

## File Structure

```
frontend/hearth-os/
  islands/
    Arrow*.tsx          # All interactive islands (32 files)
  routes/
    _app.tsx            # HTML shell
    _layout.tsx         # Shared chrome (nav, toast, connection)
    batches/            # Sourdough & kombucha routes
    cultures/           # Living culture routes
    compliance/         # HACCP, sanitation, CAPA routes
    ...
  utils/
    farmos-client.ts    # Typed API client
    toastState.ts       # Global toast signal
    connectionState.ts  # WebSocket health signal
    schemas.ts          # Zod validation schemas
  components/
    StatusBadge.tsx     # Shared Preact components (SSR only, not islands)
```
