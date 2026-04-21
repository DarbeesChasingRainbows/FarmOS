import { html } from "@arrow-js/core";

export interface ArrowEmptyStateProps {
  icon?: string;
  title: string;
  message: string;
}

export function ArrowEmptyState(props: ArrowEmptyStateProps) {
  return html`
    <div class="bg-stone-50 border border-stone-200 rounded-2xl p-12 text-center">
      ${props.icon
        ? html`
          <span class="text-4xl block mb-3">${props.icon}</span>
        `
        : html`

        `}
      <p class="text-lg font-medium text-stone-600 mb-2">${props.title}</p>
      <p class="text-sm text-stone-500 max-w-md mx-auto">${props.message}</p>
    </div>
  `;
}
