import type { ReactNode } from "react";

export interface PagePlaceholderProps {
  title: string;
  subtitle?: string;
  note: ReactNode;
}

/** Shared page scaffold for screens whose full build lands in a later epic. */
export function PagePlaceholder({ title, subtitle, note }: PagePlaceholderProps) {
  return (
    <div className="mx-auto max-w-6xl px-8 py-8">
      <header className="mb-8">
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">{title}</h1>
        {subtitle ? <p className="mt-1 text-sm text-muted-foreground">{subtitle}</p> : null}
      </header>
      <div className="grid place-items-center rounded-lg border border-dashed bg-card/50 p-16 text-center">
        <p className="max-w-md text-sm text-muted-foreground">{note}</p>
      </div>
    </div>
  );
}
