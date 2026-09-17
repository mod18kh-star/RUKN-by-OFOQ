import {
  Check,
  Clipboard,
  KeyRound,
  ShieldCheck,
} from "lucide-react";

import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";

import {
  Navigate,
  useNavigate,
  useSearchParams,
} from "react-router";

import {
  AuthSessionError,
  authRequest,
  getCurrentUser,
  saveAccessSession,
} from "./authSession";

import {
  developmentMfaBypassEnabled,
} from "./developmentSecurity";

import {
  safeInternalPath,
} from "./postAuth";

interface StartEnrollmentResponse {
  manualEntryKey: string;
  provisioningUri: string;
}

interface ConfirmEnrollmentResponse {
  recoveryCodes: string[];
  accessToken: string;
  accessTokenExpiresAtUtc: string;
}

export function SecuritySetupPage() {
  const navigate =
    useNavigate();

  const [searchParams] =
    useSearchParams();

  const returnTo =
    safeInternalPath(
      searchParams.get(
        "returnTo",
      ),
    ) ?? "/admin";

  const [loading, setLoading] =
    useState(true);

  const [loginRequired, setLoginRequired] =
    useState(false);

  const [alreadyReady, setAlreadyReady] =
    useState(false);

  const [enrollment, setEnrollment] =
    useState<StartEnrollmentResponse | null>(
      null,
    );

  const [code, setCode] =
    useState("");

  const [busy, setBusy] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null,
    );

  const [recoveryCodes, setRecoveryCodes] =
    useState<string[] | null>(
      null,
    );

  useEffect(() => {
    let cancelled = false;

    async function load() {
      if (
        developmentMfaBypassEnabled()
      ) {
        if (!cancelled) {
          setAlreadyReady(true);
          setLoading(false);
        }

        return;
      }
      try {
        const user =
          await getCurrentUser();

        if (
          user.mfaEnabled &&
          user.sessionMfaVerified
        ) {
          if (!cancelled) {
            setAlreadyReady(true);
            setLoading(false);
          }

          return;
        }

        if (user.mfaEnabled) {
          if (!cancelled) {
            setError(
              "التحقق بخطوتين مفعّل على حسابك، لكن الجلسة الحالية لم تُتحقق به بعد. سجل الدخول من جديد لإكمال التحقق.",
            );
            setLoading(false);
          }

          return;
        }

        const result =
          await authRequest<StartEnrollmentResponse>(
            "/api/auth/mfa/enrollment/start",
            {
              method: "POST",
            },
          );

        if (!cancelled) {
          setEnrollment(result);
          setLoading(false);
        }
      } catch (caught) {
        if (
          caught instanceof AuthSessionError &&
          caught.status === 401
        ) {
          if (!cancelled) {
            setLoginRequired(true);
            setLoading(false);
          }

          return;
        }

        if (!cancelled) {
          setError(
            caught instanceof Error
              ? caught.message
              : "تعذر بدء إعداد التحقق بخطوتين.",
          );
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  const formattedKey =
    useMemo(
      () =>
        enrollment?.manualEntryKey
          .replace(
            /(.{4})/g,
            "$1 ",
          )
          .trim() ?? "",
      [enrollment],
    );

  async function confirm(
    event:
      FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (!code.trim()) {
      setError(
        "أدخل الرمز الظاهر في تطبيق المصادقة.",
      );
      return;
    }

    setBusy(true);
    setError(null);

    try {
      const result =
        await authRequest<ConfirmEnrollmentResponse>(
          "/api/auth/mfa/enrollment/confirm",
          {
            method: "POST",
            body: JSON.stringify({
              code:
                code.trim(),
            }),
          },
        );

      saveAccessSession(
        result.accessToken,
        result.accessTokenExpiresAtUtc,
      );

      setRecoveryCodes(
        result.recoveryCodes,
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تأكيد التحقق بخطوتين.",
      );
    } finally {
      setBusy(false);
    }
  }

  if (loginRequired) {
    return (
      <Navigate
        replace
        to={`/start/login?mode=login&returnTo=${encodeURIComponent(
          returnTo,
        )}`}
      />
    );
  }

  if (alreadyReady) {
    return (
      <Navigate
        replace
        to={returnTo}
      />
    );
  }

  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f5f3ed] px-5 py-10 text-[#15211d]"
    >
      <div className="mx-auto max-w-[760px]">
        <div className="flex items-center gap-3">
          <div className="flex size-11 items-center justify-center rounded-full bg-[#111611] text-white">
            <ShieldCheck size={19} />
          </div>
          <div>
            <p className="text-[9px] font-semibold tracking-[0.08em] text-[#9a713f]">
              ACCOUNT SECURITY
            </p>
            <h1 className="mt-1 text-[28px] font-semibold tracking-[-0.04em]">
              حماية حساب الإدارة
            </h1>
          </div>
        </div>

        <p className="mt-5 max-w-[620px] text-[11px] leading-6 text-black/50">
          إدارة المنتجات والطلبات والبيانات الحساسة في ركن تتطلب تسجيل دخول بكلمة المرور مع تحقق بخطوتين.
        </p>

        <div className="mt-7 rounded-[18px] border border-black/[0.075] bg-white p-6 md:p-8">
          {loading ? (
            <div className="py-12 text-center text-[10px] text-black/40">
              جاري تجهيز الحماية…
            </div>
          ) : recoveryCodes ? (
            <div>
              <div className="flex size-10 items-center justify-center rounded-full bg-[#eee5d6] text-[#684b25]">
                <Check size={18} />
              </div>
              <h2 className="mt-5 text-[20px] font-semibold">
                تم تفعيل التحقق بخطوتين
              </h2>
              <p className="mt-2 text-[10px] leading-6 text-black/48">
                احفظ رموز الاسترداد في مكان آمن. ستظهر لك الآن فقط.
              </p>

              <div
                dir="ltr"
                className="mt-5 grid gap-2 rounded-[13px] bg-[#f6f4ef] p-4 sm:grid-cols-2"
              >
                {recoveryCodes.map(
                  (item) => (
                    <code
                      key={item}
                      className="rounded-[7px] bg-white px-3 py-2 text-center text-[11px]"
                    >
                      {item}
                    </code>
                  ),
                )}
              </div>

              <button
                type="button"
                onClick={() =>
                  navigate(
                    returnTo,
                    {
                      replace: true,
                    },
                  )
                }
                className="mt-6 h-11 w-full rounded-[9px] bg-[#0b0f0d] text-[10px] font-semibold text-white"
              >
                المتابعة إلى الإدارة
              </button>
            </div>
          ) : enrollment ? (
            <div>
              <div className="grid gap-5 md:grid-cols-2">
                <div className="rounded-[14px] border border-black/[0.07] bg-[#fbfaf7] p-5">
                  <KeyRound
                    size={18}
                    className="text-[#9a713f]"
                  />
                  <h2 className="mt-4 text-[13px] font-semibold">
                    1. أضف الحساب إلى تطبيق المصادقة
                  </h2>
                  <p className="mt-2 text-[9px] leading-5 text-black/45">
                    افتح Google Authenticator أو Microsoft Authenticator وأدخل المفتاح يدويًا.
                  </p>

                  <div className="mt-4 flex items-center gap-2 rounded-[9px] border border-black/[0.08] bg-white p-3">
                    <code
                      dir="ltr"
                      className="min-w-0 flex-1 break-all text-[11px] font-semibold tracking-[0.14em]"
                    >
                      {formattedKey}
                    </code>
                    <button
                      type="button"
                      aria-label="نسخ المفتاح"
                      onClick={() =>
                        void navigator.clipboard.writeText(
                          enrollment.manualEntryKey,
                        )
                      }
                      className="flex size-8 shrink-0 items-center justify-center rounded-[7px] border border-black/[0.07]"
                    >
                      <Clipboard size={13} />
                    </button>
                  </div>

                  <a
                    href={enrollment.provisioningUri}
                    className="mt-3 inline-flex text-[9px] font-semibold text-[#75552d] underline underline-offset-4"
                  >
                    فتح رابط الإعداد على جهاز مدعوم
                  </a>
                </div>

                <form
                  onSubmit={confirm}
                  className="rounded-[14px] border border-black/[0.07] p-5"
                >
                  <ShieldCheck
                    size={18}
                    className="text-[#9a713f]"
                  />
                  <h2 className="mt-4 text-[13px] font-semibold">
                    2. أكد الرمز
                  </h2>
                  <p className="mt-2 text-[9px] leading-5 text-black/45">
                    اكتب الرمز المكون من 6 أرقام الظاهر في التطبيق.
                  </p>

                  <input
                    dir="ltr"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    value={code}
                    onChange={(event) =>
                      setCode(
                        event.target.value.replace(
                          /\D/g,
                          "",
                        ),
                      )
                    }
                    maxLength={6}
                    className="mt-4 h-12 w-full rounded-[9px] border border-black/10 bg-[#fbfbf9] px-4 text-center text-[18px] tracking-[0.25em] outline-none focus:border-[#111611]"
                  />

                  <button
                    disabled={busy}
                    className="mt-4 h-11 w-full rounded-[9px] bg-[#111611] text-[10px] font-semibold text-white disabled:opacity-50"
                  >
                    {busy
                      ? "جاري التأكيد…"
                      : "تفعيل الحماية"}
                  </button>
                </form>
              </div>

              {error ? (
                <div className="mt-4 rounded-[10px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] leading-5 text-red-700">
                  {error}
                </div>
              ) : null}
            </div>
          ) : (
            <div className="py-6">
              <p className="text-[11px] leading-6 text-red-700">
                {error ??
                  "تعذر تجهيز إعداد الحماية."}
              </p>
              <button
                type="button"
                onClick={() =>
                  navigate(
                    `/start/login?mode=login&returnTo=${encodeURIComponent(
                      returnTo,
                    )}`,
                    {
                      replace: true,
                    },
                  )
                }
                className="mt-4 h-10 rounded-[9px] bg-[#111611] px-5 text-[10px] font-semibold text-white"
              >
                تسجيل الدخول من جديد
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
