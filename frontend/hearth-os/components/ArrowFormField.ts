import { html } from "@arrow-js/core";

export interface ArrowFormFieldProps {
  label: string | (() => string);
  error?: string | (() => string | undefined);
  helpText?: string | (() => string | undefined);
  required?: boolean | (() => boolean);
  // deno-lint-ignore no-explicit-any
  children: any; 
}

export function ArrowFormField(props: ArrowFormFieldProps) {
  return html`
    <div class="flex flex-col gap-1">
      <label class="text-sm font-medium text-stone-700">
        ${props.label}
        ${() => {
          const isReq = typeof props.required === 'function' ? props.required() : props.required;
          return isReq ? html`<span class="text-red-500 ml-0.5">*</span>` : '';
        }}
      </label>
      
      ${props.children}
      
      ${() => {
        const err = typeof props.error === 'function' ? props.error() : props.error;
        if (err) {
          return html`
            <p class="text-xs text-red-600 font-medium flex items-center gap-1">
              <span>⚠</span> ${err}
            </p>
          `;
        }
        
        const help = typeof props.helpText === 'function' ? props.helpText() : props.helpText;
        if (help) {
          return html`<p class="text-xs text-stone-400">${help}</p>`;
        }
        
        return '';
      }}
    </div>
  `;
}
