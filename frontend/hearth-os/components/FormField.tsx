import type { ComponentChildren } from "preact";

interface FormFieldProps {
  label: string;
  error?: string;
  helpText?: string;
  children: ComponentChildren;
  required?: boolean;
}

export default function FormField(
  { label, error, helpText, children, required }: FormFieldProps,
) {
  return (
    <div class="flex flex-col gap-1">
      <label class="text-sm font-medium text-stone-700">
        {label}
        {required && <span class="text-red-500 ml-0.5">*</span>}
      </label>
      {children}
      {error && (
        <p class="text-xs text-red-600 font-medium flex items-center gap-1">
          <span>⚠</span> {error}
        </p>
      )}
      {helpText && !error && <p class="text-xs text-stone-400">{helpText}</p>}
    </div>
  );
}
