import {
  useEffect,
} from "react";

import {
  ArrowLeft,
  LockKeyhole,
  X,
} from "lucide-react";

export function UpgradeNoticeDialog({
  open,
  featureName,
  onClose,
}: {
  open: boolean;
  featureName: string;
  onClose: () => void;
}) {
  useEffect(() => {
    if (!open) {
      return;
    }

    function handleKeyDown(
      event: KeyboardEvent,
    ) {
      if (
        event.key ===
        "Escape"
      ) {
        onClose();
      }
    }

    window.addEventListener(
      "keydown",
      handleKeyDown,
    );

    return () => {
      window.removeEventListener(
        "keydown",
        handleKeyDown,
      );
    };
  }, [
    open,
    onClose,
  ]);

  if (!open) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-[100] flex items-center justify-center bg-black/35 p-5 backdrop-blur-[2px]"
      onMouseDown={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="ميزة تحتاج إلى ترقية"
        onMouseDown={(event) =>
          event.stopPropagation()
        }
        className="w-full max-w-[430px] rounded-[16px] bg-white p-6 shadow-2xl"
      >
        <div className="flex items-start justify-between gap-4">
          <div className="flex size-11 items-center justify-center rounded-full bg-[#eef3f0] text-[#315a49]">
            <LockKeyhole
              size={18}
            />
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="إغلاق"
            className="flex size-9 items-center justify-center rounded-full hover:bg-black/[0.05]"
          >
            <X size={17} />
          </button>
        </div>

        <h2 className="mt-5 text-[21px] font-semibold tracking-[-0.035em]">
          هذا الخيار متاح في باقة أعلى
        </h2>

        <p className="mt-3 text-[12px] leading-7 text-[var(--ink-soft)]">
          خيار
          {" "}
          <strong>
            {featureName}
          </strong>
          {" "}
          غير مشمول في باقتك الحالية. إعدادات متجرك الحالية لن تتغير.
        </p>

        <div className="mt-6 rounded-[10px] bg-[#f5f5f1] p-4">
          <p className="text-[10px] font-semibold">
            في الباقات الأعلى تحصل على
          </p>

          <p className="mt-2 text-[10px] leading-6 text-[var(--ink-muted)]">
            ثيمات أكثر، خطوط إضافية، أشكال بطاقات ووضعيات عرض وتحكم أكبر بتكوين الصفحة.
          </p>
        </div>

        <button
          type="button"
          onClick={onClose}
          className="mt-6 inline-flex h-11 w-full items-center justify-center gap-2 rounded-[9px] bg-[#1d201e] px-5 text-[11px] font-semibold text-white"
        >
          فهمت

          <ArrowLeft size={14} />
        </button>
      </div>
    </div>
  );
}