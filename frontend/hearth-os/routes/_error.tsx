import { PageProps } from "fresh";
import { ApiError } from "../utils/farmos-client.ts";

export default function ErrorPage(props: PageProps) {
  const error = props.error;

  let status = 500;
  let title = "Something Went Wrong";
  let message = "An unexpected error occurred. Please try again.";
  let icon = "⚠️";

  if (error instanceof ApiError) {
    status = error.status;

    switch (status) {
      case 404:
        title = "Not Found";
        message = error.message ||
          "The page or resource you're looking for doesn't exist.";
        icon = "🔍";
        break;
      case 503:
        title = "Gateway Unreachable";
        message =
          "Unable to reach the FarmOS backend. Check your network connection or try again in a moment.";
        icon = "📡";
        break;
      default:
        title = `Error ${status}`;
        message = error.message ||
          "An error occurred while processing your request.";
    }
  }

  return (
    <div class="min-h-screen bg-stone-50 flex items-center justify-center p-6">
      <div class="max-w-md w-full bg-white rounded-2xl shadow-lg border border-stone-200 p-8 text-center">
        <div class="text-5xl mb-4">{icon}</div>
        <h1 class="text-2xl font-bold text-stone-800 mb-2">{title}</h1>
        <p class="text-stone-500 mb-6 text-sm leading-relaxed">{message}</p>

        {status === 503 && (
          <div class="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-6 text-xs text-amber-700">
            <strong>Kitchen Tip:</strong>{" "}
            If you're in the mushroom room or kitchen, WiFi may be intermittent.
            Your last readings are still visible on the dashboard.
          </div>
        )}

        <div class="flex gap-3 justify-center">
          <a
            href="/"
            class="inline-flex items-center gap-2 px-4 py-2.5 bg-stone-800 text-white rounded-lg text-sm font-semibold hover:bg-stone-700 transition min-h-[48px] min-w-[48px]"
          >
            ← Dashboard
          </a>
          <a
            href={typeof globalThis.window !== "undefined"
              ? globalThis.window.location.href
              : "/"}
            class="inline-flex items-center gap-2 px-4 py-2.5 border border-stone-300 text-stone-700 rounded-lg text-sm font-semibold hover:bg-stone-50 transition min-h-[48px] min-w-[48px]"
          >
            Retry
          </a>
        </div>

        <p class="mt-6 text-xs text-stone-300">HTTP {status}</p>
      </div>
    </div>
  );
}
