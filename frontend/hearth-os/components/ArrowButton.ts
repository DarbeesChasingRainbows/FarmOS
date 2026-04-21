import { html } from "@arrow-js/core";

export interface ArrowButtonProps {
  id?: string;
  onClick?: (e: Event) => void;
  text: string | (() => string);
  disabled?: boolean | (() => boolean);
  class?: string;
  type?: "button" | "submit" | "reset";
}

export function ArrowButton(props: ArrowButtonProps) {
  return html`
    <button
      id="${props.id || ""}"
      type="${props.type || "button"}"
      @click="${props.onClick}"
      disabled="${props.disabled}"
      class="px-2 py-1 border-stone-500 border-2 rounded-sm bg-white hover:bg-stone-200 transition-colors disabled:opacity-50 ${props
        .class || ""}"
    >
      ${props.text}
    </button>
  `;
}
