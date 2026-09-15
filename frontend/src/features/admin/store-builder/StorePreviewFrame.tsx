import {
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";

import {
  ExternalLink,
  Monitor,
  Smartphone,
  Tablet,
} from "lucide-react";

import type {
  PreviewTarget,
} from "./BuilderControls";

import type {
  StorefrontConfig,
} from "../../../storefront/theme/theme.types";

type PreviewDevice =
  | "desktop"
  | "tablet"
  | "mobile";

interface PreviewHighlightDetail {
  target: PreviewTarget;
  label: string;
  value: string;
}

const devices: Array<{
  id: PreviewDevice;
  label: string;
  icon: typeof Monitor;
}> = [
  {
    id: "desktop",
    label: "Desktop",
    icon: Monitor,
  },

  {
    id: "tablet",
    label: "Tablet",
    icon: Tablet,
  },

  {
    id: "mobile",
    label: "Mobile",
    icon: Smartphone,
  },
];

export function StorePreviewFrame({
  config,
}: {
  config: StorefrontConfig;
}) {
  const [
    device,
    setDevice,
  ] =
    useState<PreviewDevice>(
      "desktop",
    );

  const [
    activeField,
    setActiveField,
  ] =
    useState<string | null>(
      null,
    );

  const iframeRef =
    useRef<HTMLIFrameElement>(
      null,
    );

  const sendPreviewConfig =
    useCallback(() => {
      iframeRef.current
        ?.contentWindow
        ?.postMessage(
          {
            type:
              "OFOQ_STOREFRONT_PREVIEW_CONFIG",
            config,
          },
          window.location.origin,
        );
    }, [
      config,
    ]);

  const sendHighlight =
    useCallback(
      (
        detail:
          PreviewHighlightDetail,
      ) => {
        iframeRef.current
          ?.contentWindow
          ?.postMessage(
            {
              type:
                "OFOQ_STOREFRONT_HIGHLIGHT",
              highlight:
                detail,
            },
            window.location.origin,
          );
      },
      [],
    );

  useEffect(() => {
    sendPreviewConfig();
  }, [
    sendPreviewConfig,
  ]);

  useEffect(() => {
    function handleHighlight(
      event: Event,
    ) {
      const customEvent =
        event as CustomEvent<PreviewHighlightDetail>;

      if (
        !customEvent.detail
      ) {
        return;
      }

      const detail =
        customEvent.detail;

      setActiveField(
        detail.label,
      );

      sendHighlight(
        detail,
      );

      window.setTimeout(
        () =>
          sendHighlight(
            detail,
          ),
        120,
      );

      window.setTimeout(
        () =>
          sendHighlight(
            detail,
          ),
        260,
      );
    }

    window.addEventListener(
      "ofoq-builder-preview-highlight",
      handleHighlight,
    );

    return () => {
      window.removeEventListener(
        "ofoq-builder-preview-highlight",
        handleHighlight,
      );
    };
  }, [
    sendHighlight,
  ]);

  const width =
    device === "desktop"
      ? "100%"
      : device === "tablet"
        ? "820px"
        : "390px";

  return (
    <section className="overflow-hidden rounded-[14px] border border-black/[0.08] bg-[#e9e9e4]">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-black/[0.08] bg-[#f8f8f5] px-4 py-3">
        <div>
          <p className="text-[11px] font-semibold">
            معاينة مباشرة
          </p>

          <p className="mt-0.5 text-[9px] text-[var(--ink-muted)]">
            {activeField
              ? `تعديل الآن: ${activeField}`
              : "اضغط على حقل ليتم تحديد مكانه في المتجر"}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <div className="flex rounded-[8px] border border-black/[0.08] bg-white p-1">
            {devices.map(
              (item) => {
                const Icon =
                  item.icon;

                return (
                  <button
                    key={item.id}
                    type="button"
                    title={item.label}
                    onClick={() =>
                      setDevice(
                        item.id,
                      )
                    }
                    className={[
                      "flex size-8 items-center justify-center rounded-[6px] transition",
                      device ===
                      item.id
                        ? "bg-[#e8ece8] text-[#315a49]"
                        : "text-black/45 hover:bg-black/[0.035]",
                    ].join(" ")}
                  >
                    <Icon
                      size={15}
                    />
                  </button>
                );
              },
            )}
          </div>

          <a
            href="/store/demo"
            target="_blank"
            rel="noreferrer"
            title="فتح المتجر في نافذة جديدة"
            className="flex size-9 items-center justify-center rounded-[8px] border border-black/[0.08] bg-white"
          >
            <ExternalLink
              size={14}
            />
          </a>
        </div>
      </div>

      <div className="h-[760px] overflow-auto p-3">
        <div
          className="mx-auto h-full overflow-hidden bg-white shadow-sm transition-[width] duration-300"
          style={{
            width,
            maxWidth: "100%",
          }}
        >
          <iframe
            ref={iframeRef}
            title="معاينة المتجر"
            src="/store/demo?builderPreview=1"
            onLoad={
              sendPreviewConfig
            }
            className="h-full w-full border-0 bg-white"
          />
        </div>
      </div>
    </section>
  );
}