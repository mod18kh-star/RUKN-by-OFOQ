import QRCode from "qrcode";
import {
  Check,
  Clipboard,
  ExternalLink,
  KeyRound,
  ShieldCheck,
  Smartphone,
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
  accessToken: string;
  accessTokenExpiresAtUtc: string;
}

const googleAuthenticatorAndroidUrl =
  "https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2";

const googleAuthenticatorIosUrl =
  "https://apps.apple.com/sa/app/google-authenticator/id388497605";

const googleAuthenticatorHelpUrl =
  "https://support.google.com/accounts/answer/1066447?hl=ar";

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

  const [mfaQr, setMfaQr] = useState<{
    uri: string;
    dataUrl: string | null;
    error: boolean;
  }>({ uri: "", dataUrl: null, error: false });
  const currentQrUri = enrollment?.provisioningUri ?? "";
  const mfaQrDataUrl = mfaQr.uri === currentQrUri ? mfaQr.dataUrl : null;
  const mfaQrError = mfaQr.uri === currentQrUri && mfaQr.error;

  const [code, setCode] =
    useState("");

  const [busy, setBusy] =
    useState(false);

  const [copied, setCopied] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null,
    );

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const user =
          await getCurrentUser();

        /*
         * Development MFA bypass is reserved for the local
         * Platform Administrator bootstrap flow only.
         *
         * Normal merchant Back Office users must complete
         * real MFA because the API requires pwd + mfa.
         */
        if (
          developmentMfaBypassEnabled() &&
          user.platformRoles.includes(
            "PlatformAdministrator",
          ) &&
          !user.mfaEnabled
        ) {
          if (!cancelled) {
            setAlreadyReady(true);
            setLoading(false);
          }

          return;
        }

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
          /*
           * This account is already enrolled in Google Authenticator.
           * Never show enrollment again.
           *
           * Send the user back through the normal login flow so
           * password verification can produce an MFA challenge,
           * then AuthPage asks only for the current 6-digit code.
           */
          if (!cancelled) {
            setLoginRequired(true);
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

  // RUKN_MFA_LOCAL_QR_V1: render the TOTP provisioning URI entirely in the browser.
  // Never send this secret-bearing URI to a third-party QR endpoint or to analytics.
  useEffect(() => {
    let cancelled = false;
    const uri = enrollment?.provisioningUri;
    if (!uri) return;

    void QRCode.toDataURL(uri, {
      width: 248,
      margin: 2,
      errorCorrectionLevel: "M",
      color: { dark: "#18201d", light: "#ffffff" },
    }).then((dataUrl) => {
      if (!cancelled) setMfaQr({ uri, dataUrl, error: false });
    }).catch(() => {
      if (!cancelled) setMfaQr({ uri, dataUrl: null, error: true });
    });

    return () => {
      cancelled = true;
    };
  }, [enrollment?.provisioningUri]);

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

  async function copyManualKey() {
    if (!enrollment) {
      return;
    }

    await navigator.clipboard.writeText(
      enrollment.manualEntryKey,
    );

    setCopied(true);
    window.setTimeout(
      () => setCopied(false),
      1800,
    );
  }

  async function confirm(
    event:
      FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (code.trim().length !== 6) {
      setError(
        "أدخل رمز التحقق المكوّن من 6 أرقام.",
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
      navigate(
        returnTo,
        {
          replace: true,
        },
      );

      return;
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تأكيد رمز التحقق.",
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
    <main
      dir="rtl"
      className="min-h-screen bg-[#f4f1e9] px-4 py-7 text-[#18201d] sm:px-6 sm:py-10"
    >
      <div className="mx-auto w-full max-w-[920px]">
        <header className="flex items-start justify-between gap-5 border-b border-black/[0.07] pb-6">
          <div>
            <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-[#a57a43]/20 bg-[#efe6d8] px-3 py-1.5 text-[11px] font-semibold text-[#7c5a30]">
              <ShieldCheck size={14} />
              خطوة أمان لمرة واحدة
            </div>
            <h1 className="text-[30px] font-semibold tracking-[-0.045em] sm:text-[36px]">
              أمّن دخولك إلى ركن
            </h1>
            <p className="mt-3 max-w-[620px] text-[13px] leading-7 text-black/55">
              نستخدم Google Authenticator لحماية إدارة المتجر. الإعداد الأول يستغرق أقل من دقيقة، وبعدها ستدخل رمزًا من 6 أرقام مع كلمة المرور عند كل تسجيل دخول.
            </p>
          </div>

          <div className="hidden size-12 shrink-0 items-center justify-center rounded-full bg-[#18201d] text-white sm:flex">
            <ShieldCheck size={20} />
          </div>
        </header>

        <section className="mt-6 overflow-hidden rounded-[24px] border border-black/[0.07] bg-white shadow-[0_18px_55px_rgba(41,33,18,0.055)]">
          {loading ? (
            <div className="px-6 py-20 text-center text-[12px] text-black/45">
              جاري تجهيز حماية الحساب…
            </div>
          ) : enrollment ? (
            <div className="grid lg:grid-cols-[1.08fr_.92fr]">
              <div className="border-b border-black/[0.07] p-6 sm:p-8 lg:border-b-0 lg:border-l">
                <div className="flex items-start gap-4">
                  <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[#f1ece2] text-[#846236]">
                    <Smartphone size={18} />
                  </div>
                  <div>
                    <p className="text-[11px] font-semibold text-[#8a6738]">
                      الخطوة 1
                    </p>
                    <h2 className="mt-1 text-[18px] font-semibold tracking-[-0.025em]">
                      نزّل Google Authenticator
                    </h2>
                    <p className="mt-2 text-[12px] leading-6 text-black/50">
                      إذا كان التطبيق موجودًا عندك، انتقل مباشرة إلى ربط الحساب. وإذا لم يكن موجودًا، نزّله من المتجر الرسمي لجهازك.
                    </p>
                  </div>
                </div>

                <div className="mt-5 grid gap-2 sm:grid-cols-2">
                  <a
                    href={googleAuthenticatorAndroidUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex min-h-12 items-center justify-between rounded-[12px] border border-black/[0.08] bg-[#fbfaf7] px-4 text-[11px] font-semibold transition hover:border-black/20 hover:bg-white"
                  >
                    <span>تحميل لأجهزة Android</span>
                    <ExternalLink size={14} className="text-black/35" />
                  </a>
                  <a
                    href={googleAuthenticatorIosUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="flex min-h-12 items-center justify-between rounded-[12px] border border-black/[0.08] bg-[#fbfaf7] px-4 text-[11px] font-semibold transition hover:border-black/20 hover:bg-white"
                  >
                    <span>تحميل لأجهزة iPhone</span>
                    <ExternalLink size={14} className="text-black/35" />
                  </a>
                </div>

                <a
                  href={googleAuthenticatorHelpUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-3 inline-flex items-center gap-1.5 text-[11px] font-medium text-[#74552f] underline decoration-[#74552f]/30 underline-offset-4"
                >
                  شرح Google الرسمي لاستخدام التطبيق
                  <ExternalLink size={12} />
                </a>

                <div className="my-7 h-px bg-black/[0.07]" />

                <div className="flex items-start gap-4">
                  <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[#f1ece2] text-[#846236]">
                    <KeyRound size={18} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-[11px] font-semibold text-[#8a6738]">
                      الخطوة 2
                    </p>
                    <h2 className="mt-1 text-[18px] font-semibold tracking-[-0.025em]">
                      اربط حساب ركن بالتطبيق
                    </h2>
                    <p className="mt-2 text-[12px] leading-6 text-black/50">
                      من جوالك يمكنك فتح رابط الربط مباشرة. وإذا كنت تعمل من الكمبيوتر، أضف حسابًا جديدًا في Google Authenticator واختر إدخال مفتاح الإعداد يدويًا.
                    </p>
                  </div>
                </div>

                <div className="mt-5 rounded-[14px] border border-black/[0.07] bg-[#faf8f3] p-4 sm:p-5">
                  <p className="text-[12px] font-semibold text-[#18201d]">
                    امسح رمز QR من تطبيق Google Authenticator
                  </p>
                  <p className="mt-2 text-[11px] leading-6 text-black/55">
                    من جوالك افتح التطبيق، اضغط + ثم اختر مسح رمز QR. لا تشارك هذا الرمز مع أي شخص.
                  </p>
                  <div className="mx-auto mt-4 flex min-h-[248px] w-full max-w-[280px] items-center justify-center rounded-[14px] border border-black/[0.08] bg-white p-3">
                    {mfaQrDataUrl ? (
                      <img
                        src={mfaQrDataUrl}
                        width={248}
                        height={248}
                        alt="رمز QR السري لربط حسابك بتطبيق المصادقة"
                        className="aspect-square w-full max-w-[248px]"
                        referrerPolicy="no-referrer"
                      />
                    ) : (
                      <p role="status" className="text-center text-[11px] text-black/50">
                        {mfaQrError
                          ? "تعذر عرض رمز QR. استخدم مفتاح الإعداد اليدوي أدناه."
                          : "جاري إعداد رمز QR على جهازك…"}
                      </p>
                    )}
                  </div>
                  <p className="mt-3 text-center text-[10px] leading-5 text-black/45">
                    المفتاح اليدوي موجود أدناه كخيار احتياطي.
                  </p>
                </div>

                <a
                  href={enrollment.provisioningUri}
                  className="mt-5 flex min-h-12 w-full items-center justify-center gap-2 rounded-[12px] bg-[#18201d] px-5 text-[11px] font-semibold text-white transition hover:bg-[#0f1714]"
                >
                  فتح Google Authenticator وربط الحساب
                  <ExternalLink size={14} />
                </a>

                <div className="mt-4 rounded-[14px] border border-black/[0.07] bg-[#faf8f3] p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-[10px] font-semibold text-black/50">
                        مفتاح الإعداد اليدوي
                      </p>
                      <code
                        dir="ltr"
                        className="mt-1 block break-all text-[12px] font-semibold tracking-[0.12em] text-[#18201d]"
                      >
                        {formattedKey}
                      </code>
                    </div>
                    <button
                      type="button"
                      aria-label="نسخ مفتاح الإعداد"
                      onClick={() => void copyManualKey()}
                      className="flex h-9 shrink-0 items-center gap-1.5 rounded-[9px] border border-black/[0.08] bg-white px-3 text-[10px] font-semibold transition hover:border-black/20"
                    >
                      {copied ? <Check size={13} /> : <Clipboard size={13} />}
                      {copied ? "تم النسخ" : "نسخ"}
                    </button>
                  </div>
                </div>
              </div>

              <form
                onSubmit={confirm}
                className="flex flex-col p-6 sm:p-8"
              >
                <div>
                  <p className="text-[11px] font-semibold text-[#8a6738]">
                    الخطوة 3
                  </p>
                  <h2 className="mt-1 text-[18px] font-semibold tracking-[-0.025em]">
                    أدخل رمز التحقق
                  </h2>
                  <p className="mt-2 text-[12px] leading-6 text-black/50">
                    بعد إضافة حساب ركن، سيظهر في Google Authenticator رمز من 6 أرقام. اكتبه هنا للتأكد أن الربط تم بشكل صحيح.
                  </p>
                </div>

                <div className="mt-7">
                  <label
                    htmlFor="security-code"
                    className="text-[11px] font-semibold text-black/70"
                  >
                    رمز Google Authenticator
                  </label>
                  <input
                    id="security-code"
                    dir="ltr"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    autoFocus
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
                    placeholder="000000"
                    className="mt-2 h-14 w-full rounded-[12px] border border-black/10 bg-[#fbfaf7] px-4 text-center text-[22px] font-semibold tracking-[0.32em] outline-none transition placeholder:text-black/15 focus:border-[#7f6038]/50 focus:bg-white"
                  />
                </div>

                {error ? (
                  <div className="mt-4 rounded-[11px] border border-red-200 bg-red-50 px-4 py-3 text-[11px] leading-5 text-red-700">
                    {error}
                  </div>
                ) : null}

                <button
                  disabled={busy || code.length !== 6}
                  className="mt-5 h-12 w-full rounded-[12px] bg-[#18201d] text-[11px] font-semibold text-white transition hover:bg-[#0f1714] disabled:cursor-not-allowed disabled:opacity-40"
                >
                  {busy
                    ? "جاري التحقق…"
                    : "تأكيد وحماية الحساب"}
                </button>

                <p className="mt-4 text-center text-[10px] leading-5 text-black/35">
                  لن نطلب منك ربط التطبيق مرة أخرى. في تسجيلات الدخول القادمة ستحتاج فقط إلى الرمز الحالي من التطبيق.
                </p>
              </form>
            </div>
          ) : (
            <div className="p-7 sm:p-9">
              <p className="text-[12px] leading-6 text-red-700">
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
                className="mt-4 h-11 rounded-[10px] bg-[#18201d] px-5 text-[11px] font-semibold text-white"
              >
                تسجيل الدخول من جديد
              </button>
            </div>
          )}
        </section>

        <p className="mt-4 text-center text-[10px] leading-5 text-black/35">
          لا تشارك رمز التحقق أو مفتاح الإعداد مع أي شخص.
        </p>
      </div>
    </main>
  );
}
