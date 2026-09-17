import {
  ArrowLeft,
  CheckCircle2,
  Clock3,
  LoaderCircle,
  MessageCircleMore,
  RefreshCw,
  XCircle,
} from "lucide-react";

import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  useNavigate,
  useSearchParams,
} from "react-router";

import {
  getMyPlatformRequests,
  type MerchantPlatformRequest,
} from "../admin/requests/merchantRequestsApi";

import {
  getMyTenants,
  type MyTenant,
} from "../auth/authSession";

import {
  resolvePostAuthDestination,
} from "../auth/postAuth";

type ViewState =
  | "loading"
  | "missing-store"
  | "missing-request"
  | "ready"
  | "error";

export function RegistrationPendingPage() {
  const navigate =
    useNavigate();

  const [searchParams] =
    useSearchParams();

  const suspended =
    searchParams.get(
      "state",
    ) === "suspended";

  const [state, setState] =
    useState<ViewState>(
      "loading",
    );

  const [request, setRequest] =
    useState<
      MerchantPlatformRequest | null
    >(null);

  const [store, setStore] =
    useState<MyTenant | null>(
      null,
    );

  const [error, setError] =
    useState<string | null>(
      null,
    );

  const [checking, setChecking] =
    useState(false);

  useEffect(() => {
    let cancelled = false;
    let timer:
      number | null =
      null;

    async function load(
      silent = false,
    ) {
      if (!silent) {
        setChecking(true);
      }

      try {
        const stores =
          await getMyTenants();

        const current =
          stores.find(
            (item) =>
              item.role
                .trim()
                .toLowerCase() ===
              "owner",
          ) ??
          stores[0] ??
          null;

        if (cancelled) {
          return;
        }

        setStore(
          current,
        );

        if (!current) {
          setState(
            "missing-store",
          );
          return;
        }

        window.localStorage.setItem(
          "ofoq.admin.current-store",
          JSON.stringify({
            tenantId:
              current.tenantId,
            name:
              current.name,
            slug:
              current.slug,
            status:
              current.status,
          }),
        );

        if (
          current.status ===
          "Active"
        ) {
          const destination =
            await resolvePostAuthDestination();

          if (!cancelled) {
            navigate(
              destination,
              {
                replace: true,
              },
            );
          }

          return;
        }

        const requests =
          await getMyPlatformRequests(
            current.tenantId,
          );

        if (cancelled) {
          return;
        }

        const registration =
          requests.find(
            (item) =>
              item.type ===
              "StoreRegistration",
          ) ?? null;

        setRequest(
          registration,
        );

        if (
          registration?.status ===
          "Approved"
        ) {
          const destination =
            await resolvePostAuthDestination();

          if (!cancelled) {
            navigate(
              destination,
              {
                replace: true,
              },
            );
          }

          return;
        }

        setState(
          registration
            ? "ready"
            : "missing-request",
        );

        setError(null);
      } catch (caught) {
        if (cancelled) {
          return;
        }

        setError(
          caught instanceof Error
            ? caught.message
            : "تعذر تحميل حالة الطلب.",
        );

        setState(
          "error",
        );
      } finally {
        if (
          !cancelled &&
          !silent
        ) {
          setChecking(false);
        }
      }
    }

    void load();

    timer =
      window.setInterval(
        () => {
          void load(true);
        },
        5000,
      );

    return () => {
      cancelled = true;

      if (timer !== null) {
        window.clearInterval(
          timer,
        );
      }
    };
  }, [navigate]);

  const status =
    request?.status ??
    "Pending";

  const content =
    useMemo(
      () =>
        getStatusContent(
          status,
          request?.reviewReason ??
            null,
        ),
      [
        request?.reviewReason,
        status,
      ],
    );

  if (suspended) {
    return (
      <PageFrame>
        <StateCard
          icon={
            <XCircle
              size={24}
            />
          }
          eyebrow="المتجر موقوف"
          title="الوصول إلى لوحة التحكم موقوف حاليًا"
          body="حسابك موجود، لكن المتجر موقوف من الإدارة. تواصل مع إدارة RUKN إذا كنت تحتاج مراجعة الحالة."
          note="لن نعيدك لخطوات التسجيل ولن نطلب منك إنشاء متجر جديد."
        />
      </PageFrame>
    );
  }

  if (state === "loading") {
    return (
      <PageFrame>
        <section className="flex min-h-[420px] items-center justify-center border border-black/[0.08] bg-white">
          <div className="text-center">
            <LoaderCircle
              size={25}
              className="mx-auto animate-spin text-[#8c6432]"
            />
            <p className="mt-4 text-[11px] text-black/40">
              جارٍ قراءة حالة طلبك…
            </p>
          </div>
        </section>
      </PageFrame>
    );
  }

  if (state === "missing-store") {
    return (
      <PageFrame>
        <StateCard
          icon={
            <Clock3
              size={24}
            />
          }
          eyebrow="الإعداد غير مكتمل"
          title="نكمّل إعداد متجرك"
          body="ما في متجر مرتبط بالحساب بعد. نرجع لمسار الإعداد بدل ما نعيدك للبداية."
          actionLabel="متابعة إعداد المتجر"
          onAction={() =>
            navigate(
              "/start/onboarding",
            )
          }
        />
      </PageFrame>
    );
  }

  if (state === "missing-request") {
    return (
      <PageFrame>
        <StateCard
          icon={
            <Clock3
              size={24}
            />
          }
          eyebrow="خطوة أخيرة"
          title="المتجر محفوظ، والطلب لم يُرسل بعد"
          body="ارجع لخطوة تقديم الطلب وأكمل الإرسال للإدارة."
          actionLabel="إكمال الطلب"
          onAction={() =>
            navigate(
              "/start/onboarding",
            )
          }
        />
      </PageFrame>
    );
  }

  if (state === "error") {
    return (
      <PageFrame>
        <StateCard
          icon={
            <XCircle
              size={24}
            />
          }
          eyebrow="تعذر تحديث الحالة"
          title="ما قدرنا نقرأ حالة الطلب"
          body={
            error ??
            "حاول مرة ثانية."
          }
          actionLabel="إعادة المحاولة"
          onAction={() =>
            window.location.reload()
          }
        />
      </PageFrame>
    );
  }

  return (
    <PageFrame>
      <StateCard
        icon={content.icon}
        eyebrow={content.eyebrow}
        title={content.title}
        body={content.body}
        note={content.note}
        actionLabel={
          content.actionLabel
        }
        onAction={
          content.actionLabel
            ? () =>
                navigate(
                  content.actionHref,
                )
            : undefined
        }
        footer={
          status === "Pending" ? (
            <div className="mt-7 border-t border-black/[0.07] pt-5">
              <div className="flex items-center justify-center gap-2 text-[10px] text-black/38">
                <RefreshCw
                  size={13}
                  className={
                    checking
                      ? "animate-spin"
                      : ""
                  }
                />
                الحالة تتحدث تلقائيًا كل بضع ثوانٍ
              </div>

              {store ? (
                <p className="mt-2 text-[10px] text-black/30">
                  {store.name}
                  {" · "}
                  {store.slug}.ofoq.store
                </p>
              ) : null}
            </div>
          ) : null
        }
      />
    </PageFrame>
  );
}

function PageFrame({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f5f3ed] px-5 py-10 text-[#15211d]"
    >
      <main className="mx-auto max-w-[760px]">
        {children}
      </main>
    </div>
  );
}

function StateCard({
  icon,
  eyebrow,
  title,
  body,
  note,
  actionLabel,
  onAction,
  footer,
}: {
  icon: React.ReactNode;
  eyebrow: string;
  title: string;
  body: string;
  note?: string | null;
  actionLabel?: string | null;
  onAction?: () => void;
  footer?: React.ReactNode;
}) {
  return (
    <section className="border border-black/[0.08] bg-white p-7 text-center md:p-11">
      <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-[#f0e7d8] text-[#8c6432]">
        {icon}
      </div>

      <p className="mt-6 text-[10px] font-semibold tracking-[0.08em] text-[#91672f]">
        {eyebrow}
      </p>

      <h1 className="mt-3 text-[30px] font-semibold tracking-[-0.045em] md:text-[38px]">
        {title}
      </h1>

      <p className="mx-auto mt-4 max-w-xl text-[13px] leading-7 text-black/55">
        {body}
      </p>

      {note ? (
        <div className="mx-auto mt-6 max-w-xl border border-black/[0.07] bg-[#faf9f6] p-4 text-[11px] leading-6 text-black/55">
          {note}
        </div>
      ) : null}

      {actionLabel &&
      onAction ? (
        <button
          type="button"
          onClick={onAction}
          className="mt-8 inline-flex h-12 items-center justify-center gap-2 bg-[#152a23] px-6 text-[12px] font-semibold text-white"
        >
          {actionLabel}
          <ArrowLeft
            size={16}
          />
        </button>
      ) : null}

      {footer}
    </section>
  );
}

function getStatusContent(
  status: string,
  reviewReason: string | null,
) {
  switch (status) {
    case "Approved":
      return {
        icon:
          <CheckCircle2
            size={24}
          />,
        eyebrow:
          "تمت الموافقة",
        title:
          "تم اعتماد متجرك",
        body:
          "نعمل الآن على فتح لوحة الإدارة لحسابك.",
        note:
          reviewReason,
        actionLabel:
          "متابعة",
        actionHref:
          "/admin",
      };

    case "MoreInfoRequested":
      return {
        icon:
          <MessageCircleMore
            size={24}
          />,
        eyebrow:
          "مطلوب معلومات إضافية",
        title:
          "الإدارة تحتاج منك توضيحًا",
        body:
          "راجع ملاحظة الإدارة ثم عدّل بيانات الطلب وأعد الإرسال.",
        note:
          reviewReason ??
          "راجع بيانات المتجر.",
        actionLabel:
          "مراجعة بيانات المتجر",
        actionHref:
          "/start/onboarding",
      };

    case "Rejected":
      return {
        icon:
          <XCircle
            size={24}
          />,
        eyebrow:
          "الطلب يحتاج مراجعة",
        title:
          "تعذر اعتماد الطلب حاليًا",
        body:
          "راجع سبب الرفض، وبعد تعديل البيانات تقدر تبدأ طلبًا جديدًا.",
        note:
          reviewReason,
        actionLabel:
          "مراجعة البيانات",
        actionHref:
          "/start/onboarding",
      };

    default:
      return {
        icon:
          <Clock3
            size={24}
          />,
        eyebrow:
          "الطلب قيد المراجعة",
        title:
          "وصلنا طلبك",
        body:
          "سيتم مراجعة طلبك خلال 24 ساعة. ما تحتاج تعيد التسجيل أو ترسل الطلب مرة ثانية.",
        note:
          "بمجرد الموافقة سنحوّلك تلقائيًا إلى إعداد الأمان ثم لوحة الإدارة.",
        actionLabel:
          null,
        actionHref:
          "/start/review",
      };
  }
}
