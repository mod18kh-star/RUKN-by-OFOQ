import type {
  CSSProperties,
  ReactNode,
} from "react";

import {
  Image as ImageIcon,
  LockKeyhole,
} from "lucide-react";

export type PreviewTarget =
  | "store.name"
  | "announcement"
  | "hero.eyebrow"
  | "hero.title"
  | "hero.description"
  | "hero.cta"
  | "hero.primaryImage"
  | "hero.secondaryImage"
  | "categories.eyebrow"
  | "categories.title"
  | "products.eyebrow"
  | "products.title"
  | "banner.eyebrow"
  | "banner.title"
  | "banner.body"
  | "banner.cta"
  | "banner.image"
  | "story.eyebrow"
  | "story.title"
  | "story.body"
  | "story.cta"
  | "story.image";

interface PreviewHighlightDetail {
  target: PreviewTarget;
  label: string;
  value: string;
}

function notifyPreviewHighlight(
  detail: PreviewHighlightDetail,
) {
  window.dispatchEvent(
    new CustomEvent(
      "ofoq-builder-preview-highlight",
      {
        detail,
      },
    ),
  );
}

export function BuilderPanel({
  title,
  description,
  children,
  action,
}: {
  title: string;
  description?: string;
  children: ReactNode;
  action?: ReactNode;
}) {
  return (
    <section className="overflow-hidden rounded-[14px] border border-black/[0.07] bg-[#fbfbf9]">
      <div className="flex items-start justify-between gap-4 border-b border-black/[0.06] px-5 py-5">
        <div>
          <h2 className="text-[15px] font-semibold tracking-[-0.02em]">
            {title}
          </h2>

          {description ? (
            <p className="mt-1.5 max-w-2xl text-[11px] leading-6 text-[var(--ink-muted)]">
              {description}
            </p>
          ) : null}
        </div>

        {action}
      </div>

      <div className="p-5">
        {children}
      </div>
    </section>
  );
}

export function BuilderField({
  label,
  value,
  onChange,
  placeholder,
  helper,
  previewTarget,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  helper?: string;
  previewTarget?: PreviewTarget;
}) {
  function highlight(
    nextValue = value,
  ) {
    if (!previewTarget) {
      return;
    }

    notifyPreviewHighlight({
      target: previewTarget,
      label,
      value: nextValue,
    });
  }

  return (
    <label className="block">
      <span className="mb-2 block text-[11px] font-semibold">
        {label}
      </span>

      <input
        value={value}
        placeholder={placeholder}
        onFocus={() =>
          highlight()
        }
        onClick={() =>
          highlight()
        }
        onChange={(event) => {
          const nextValue =
            event.target.value;

          onChange(
            nextValue,
          );

          window.setTimeout(
            () =>
              highlight(
                nextValue,
              ),
            60,
          );
        }}
        className="h-11 w-full rounded-[8px] border border-black/[0.09] bg-white px-3 text-[12px] outline-none transition placeholder:text-black/25 focus:border-[#315a49] focus:ring-2 focus:ring-[#315a49]/10"
      />

      {helper ? (
        <span className="mt-1.5 block text-[9px] leading-5 text-[var(--ink-muted)]">
          {helper}
        </span>
      ) : null}
    </label>
  );
}

export function BuilderTextarea({
  label,
  value,
  onChange,
  placeholder,
  rows = 4,
  previewTarget,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  rows?: number;
  previewTarget?: PreviewTarget;
}) {
  function highlight(
    nextValue = value,
  ) {
    if (!previewTarget) {
      return;
    }

    notifyPreviewHighlight({
      target: previewTarget,
      label,
      value: nextValue,
    });
  }

  return (
    <label className="block">
      <span className="mb-2 block text-[11px] font-semibold">
        {label}
      </span>

      <textarea
        value={value}
        rows={rows}
        placeholder={placeholder}
        onFocus={() =>
          highlight()
        }
        onClick={() =>
          highlight()
        }
        onChange={(event) => {
          const nextValue =
            event.target.value;

          onChange(
            nextValue,
          );

          window.setTimeout(
            () =>
              highlight(
                nextValue,
              ),
            60,
          );
        }}
        className="w-full resize-y rounded-[8px] border border-black/[0.09] bg-white px-3 py-3 text-[12px] leading-6 outline-none transition placeholder:text-black/25 focus:border-[#315a49] focus:ring-2 focus:ring-[#315a49]/10"
      />
    </label>
  );
}

export function ImageUrlField({
  label,
  value,
  onChange,
  helper,
  previewTarget,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  helper?: string;
  previewTarget?: PreviewTarget;
}) {
  function highlight(
    nextValue = value,
  ) {
    if (!previewTarget) {
      return;
    }

    notifyPreviewHighlight({
      target: previewTarget,
      label,
      value: nextValue,
    });
  }

  return (
    <div>
      <span className="mb-2 block text-[11px] font-semibold">
        {label}
      </span>

      <div className="grid gap-3 sm:grid-cols-[96px_1fr]">
        <button
          type="button"
          onClick={() =>
            highlight()
          }
          className="flex aspect-square items-center justify-center overflow-hidden rounded-[9px] border border-black/[0.08] bg-[#f0f0ec]"
        >
          {value ? (
            <img
              src={value}
              alt=""
              className="h-full w-full object-cover"
            />
          ) : (
            <ImageIcon
              size={21}
              strokeWidth={1.5}
              className="text-black/30"
            />
          )}
        </button>

        <div>
          <input
            value={value}
            onFocus={() =>
              highlight()
            }
            onClick={() =>
              highlight()
            }
            onChange={(event) => {
              const nextValue =
                event.target.value;

              onChange(
                nextValue,
              );

              window.setTimeout(
                () =>
                  highlight(
                    nextValue,
                  ),
                80,
              );
            }}
            placeholder="https://..."
            className="h-11 w-full rounded-[8px] border border-black/[0.09] bg-white px-3 text-left text-[11px] outline-none transition placeholder:text-black/25 focus:border-[#315a49] focus:ring-2 focus:ring-[#315a49]/10"
            dir="ltr"
          />

          {helper ? (
            <span className="mt-1.5 block text-[9px] leading-5 text-[var(--ink-muted)]">
              {helper}
            </span>
          ) : null}
        </div>
      </div>
    </div>
  );
}

export function ChoiceCard({
  name,
  description,
  selected,
  allowed,
  onSelect,
  onLocked,
  titleStyle,
  preview,
}: {
  name: string;
  description?: string;
  selected: boolean;
  allowed: boolean;
  onSelect: () => void;
  onLocked: () => void;
  titleStyle?: CSSProperties;
  preview?: ReactNode;
}) {
  function handleClick() {
    if (!allowed) {
      onLocked();
      return;
    }

    onSelect();
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      className={[
        "relative min-h-[122px] w-full overflow-hidden rounded-[10px] border p-4 text-right transition",
        selected
          ? "border-[#315a49] bg-[#edf4f0] shadow-[0_0_0_1px_rgba(49,90,73,.08)]"
          : "border-black/[0.08] bg-white hover:border-black/[0.18]",
        !allowed
          ? "bg-[#f6f5f2]"
          : "",
      ].join(" ")}
    >
      {preview ? (
        <div className="mb-4">
          {preview}
        </div>
      ) : null}

      {!allowed ? (
        <span className="absolute left-3 top-3 inline-flex items-center gap-1 rounded-full bg-[#ecebe7] px-2 py-1 text-[9px] font-semibold text-[var(--ink-muted)]">
          <LockKeyhole
            size={10}
          />

          ترقية
        </span>
      ) : null}

      <p
        style={titleStyle}
        className="text-[13px] font-semibold"
      >
        {name}
      </p>

      {description ? (
        <p className="mt-1.5 max-w-xs text-[9px] leading-5 text-[var(--ink-muted)]">
          {description}
        </p>
      ) : null}

      {selected ? (
        <span className="absolute bottom-3 left-3 text-[9px] font-semibold text-[#315a49]">
          مستخدم حاليا
        </span>
      ) : null}
    </button>
  );
}

export function BuilderSwitch({
  checked,
  onChange,
  label,
  description,
}: {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label: string;
  description?: string;
}) {
  return (
    <button
      type="button"
      onClick={() =>
        onChange(
          !checked,
        )
      }
      className="flex w-full items-center justify-between gap-5 rounded-[10px] border border-black/[0.08] bg-white px-4 py-3.5 text-right"
    >
      <div>
        <p className="text-[11px] font-semibold">
          {label}
        </p>

        {description ? (
          <p className="mt-1 text-[9px] leading-5 text-[var(--ink-muted)]">
            {description}
          </p>
        ) : null}
      </div>

      <span
        className={[
          "relative h-6 w-11 shrink-0 rounded-full transition",
          checked
            ? "bg-[#315a49]"
            : "bg-black/15",
        ].join(" ")}
      >
        <span
          className={[
            "absolute top-1 size-4 rounded-full bg-white shadow-sm transition",
            checked
              ? "right-6"
              : "right-1",
          ].join(" ")}
        />
      </span>
    </button>
  );
}