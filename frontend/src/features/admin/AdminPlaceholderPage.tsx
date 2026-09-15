interface AdminPlaceholderPageProps {
  title: string;
  description: string;
}

export function AdminPlaceholderPage({
  title,
  description,
}: AdminPlaceholderPageProps) {
  return (
    <div className="mx-auto max-w-[1320px]">
      <p className="mb-2 text-[11px] text-[var(--ink-muted)]">
        OFOQ Admin
      </p>

      <h1 className="text-[30px] font-semibold tracking-[-0.045em]">
        {title}
      </h1>

      <p className="mt-3 max-w-xl text-[12px] leading-7 text-[var(--ink-soft)]">
        {description}
      </p>

      <div className="mt-9 min-h-[380px] border border-dashed border-black/[0.13] bg-white/40" />
    </div>
  );
}