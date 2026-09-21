import {
  useState,
  type FormEvent,
} from "react";

import {
  ArrowLeft,
  Eye,
  EyeOff,
  LockKeyhole,
  Mail,
  Phone,
  UserRound,
} from "lucide-react";

import {
  Link,
  useNavigate,
  useSearchParams,
} from "react-router";

import {
  saveAccessSession,
} from "../auth/authSession";

import {
  resolvePostAuthDestination,
  safeInternalPath,
} from "../auth/postAuth";

interface ApiProblem {
  code?: string;
  message?: string;
}

interface LoginResponse {
  userId: string;
  email: string;
  requiresMfa: boolean;
  accessToken: string | null;
  accessTokenExpiresAtUtc: string | null;
  mfaChallengeToken: string | null;
  mfaChallengeExpiresAtUtc: string | null;
}

interface MfaResponse {
  userId: string;
  email: string;
  accessToken: string;
  expiresAtUtc: string;
}

function apiBaseUrl() {
  return (
    import.meta.env.VITE_API_BASE_URL ??
    ""
  )
    .trim()
    .replace(/\/+$/, "");
}

async function readProblem(
  response: Response,
): Promise<ApiProblem> {
  try {
    return (
      (await response.json()) as ApiProblem
    );
  } catch {
    return {};
  }
}

function friendlyError(
  problem: ApiProblem,
  fallback: string,
) {
  switch (problem.code) {
    case "invalid_credentials":
      return "البريد الإلكتروني أو كلمة المرور غير صحيحة.";
    case "user_email_already_exists":
      return "يوجد حساب بهذا البريد الإلكتروني بالفعل.";
    case "invalid_email":
      return "تأكد من كتابة البريد الإلكتروني بشكل صحيح.";
    case "invalid_password":
      return (
        problem.message ??
        "كلمة المرور لا تحقق المتطلبات المطلوبة."
      );
    case "invalid_full_name":
      return "اكتب اسمًا واضحًا من حرفين على الأقل.";
    case "invalid_phone_number":
      return "تأكد من رقم الجوال ورمز الدولة.";
    case "invalid_mfa_verification":
      return "رمز التحقق غير صحيح أو انتهت صلاحيته.";
    default:
      return (
        problem.message ??
        fallback
      );
  }
}

function phoneLooksValid(
  value: string,
) {
  const normalized =
    value
      .replace(
        /[\s\-()]/g,
        "",
      );

  const digits =
    normalized.startsWith("+")
      ? normalized.slice(1)
      : normalized;

  return (
    digits.length >= 8 &&
    digits.length <= 20 &&
    /^\d+$/.test(
      digits,
    )
  );
}

export function AuthPage() {
  const navigate =
    useNavigate();

  const [searchParams] =
    useSearchParams();

  const requestedMode =
    searchParams.get("mode");

  const requestedReturnTo =
    safeInternalPath(
      searchParams.get("returnTo"),
    );

  const [mode, setMode] =
    useState<
      "login" | "register"
    >(
      requestedMode === "login"
        ? "login"
        : "register",
    );

  const [fullName, setFullName] =
    useState("");

  const [phoneNumber, setPhoneNumber] =
    useState("");

  const [email, setEmail] =
    useState("");

  const [password, setPassword] =
    useState("");

  const [showPassword, setShowPassword] =
    useState(false);

  const [pending, setPending] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null,
    );

  const [challengeToken, setChallengeToken] =
    useState<string | null>(
      null,
    );

  const [mfaValue, setMfaValue] =
    useState("");

  async function finishAuthentication(
    token: string,
    expiresAtUtc: string,
    accountEmail: string,
    accountUserId: string,
  ) {
    saveAccessSession(
      token,
      expiresAtUtc,
      accountEmail,
      accountUserId,
    );

    const destination =
      await resolvePostAuthDestination(
        requestedReturnTo,
      );

    navigate(
      destination,
      {
        replace: true,
      },
    );
  }

  async function login(
    accountEmail: string,
    accountPassword: string,
  ) {
    const response =
      await fetch(
        `${apiBaseUrl()}/api/auth/login`,
        {
          method: "POST",
          credentials: "include",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            email:
              accountEmail,
            password:
              accountPassword,
          }),
        },
      );

    if (!response.ok) {
      throw new Error(
        friendlyError(
          await readProblem(
            response,
          ),
          "تعذر تسجيل الدخول الآن.",
        ),
      );
    }

    const result =
      (await response.json()) as LoginResponse;

    if (result.requiresMfa) {
      if (!result.mfaChallengeToken) {
        throw new Error(
          "تعذر بدء التحقق بخطوتين.",
        );
      }

      setChallengeToken(
        result.mfaChallengeToken,
      );

      return;
    }

    if (
      !result.accessToken ||
      !result.accessTokenExpiresAtUtc
    ) {
      throw new Error(
        "لم يرجع الخادم جلسة دخول صالحة.",
      );
    }

    await finishAuthentication(
      result.accessToken,
      result.accessTokenExpiresAtUtc,
      result.email,
      result.userId,
    );

    return;
  }

  async function submit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    const cleanEmail =
      email.trim();

    if (
      !cleanEmail ||
      !password
    ) {
      setError(
        "اكتب البريد الإلكتروني وكلمة المرور.",
      );
      return;
    }

    if (mode === "register") {
      if (
        fullName.trim().length <
        2
      ) {
        setError(
          "اكتب اسمك الكامل.",
        );
        return;
      }

      if (
        !phoneLooksValid(
          phoneNumber,
        )
      ) {
        setError(
          "اكتب رقم جوال صحيحًا مع رمز الدولة إن أمكن.",
        );
        return;
      }
    }

    setPending(true);
    setError(null);

    try {
      if (
        mode === "register"
      ) {
        const response =
          await fetch(
            `${apiBaseUrl()}/api/auth/register`,
            {
              method: "POST",
              credentials: "include",
              headers: {
                "Content-Type":
                  "application/json",
              },
              body:
                JSON.stringify({
                  fullName:
                    fullName.trim(),
                  phoneNumber:
                    phoneNumber.trim(),
                  email:
                    cleanEmail,
                  password,
                }),
            },
          );

        if (!response.ok) {
          throw new Error(
            friendlyError(
              await readProblem(
                response,
              ),
              "تعذر إنشاء الحساب الآن.",
            ),
          );
        }
      }

      await login(
        cleanEmail,
        password,
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "حدث خطأ غير متوقع.",
      );
    } finally {
      setPending(false);
    }
  }

  async function submitMfa(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (
      !challengeToken ||
      !mfaValue.trim()
    ) {
      return;
    }

    setPending(true);
    setError(null);

    try {
      const response =
        await fetch(
          `${apiBaseUrl()}/api/auth/mfa/totp`,
          {
            method: "POST",
            credentials: "include",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify({
              challengeToken,
              code:
                mfaValue.trim(),
              rememberDevice: false,
            }),
          },
        );

      if (!response.ok) {
        throw new Error(
          friendlyError(
            await readProblem(
              response,
            ),
            "تعذر إكمال التحقق.",
          ),
        );
      }

      const result =
        (await response.json()) as MfaResponse;

      await finishAuthentication(
        result.accessToken,
        result.expiresAtUtc,
        result.email,
        result.userId,
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر إكمال التحقق.",
      );
    } finally {
      setPending(false);
    }
  }

  if (challengeToken) {
    return (
      <AuthFrame>
        <div className="mx-auto w-full max-w-[500px] overflow-hidden rounded-[22px] border border-black/[0.07] bg-white shadow-[0_20px_60px_rgba(35,28,18,0.07)]">
          <div className="border-b border-black/[0.06] bg-[#faf8f3] px-7 py-6 md:px-9">
            <div className="flex items-center justify-between gap-4">
              <div className="flex size-11 items-center justify-center rounded-full bg-[#18201d] text-white">
                <LockKeyhole
                  size={18}
                />
              </div>
              <span className="rounded-full border border-[#a57a43]/20 bg-[#efe6d8] px-3 py-1.5 text-[10px] font-semibold text-[#7c5a30]">
                حماية تسجيل الدخول
              </span>
            </div>

            <h1 className="mt-6 text-[30px] font-semibold tracking-[-0.045em]">
              أدخل رمز التحقق
            </h1>

            <p className="mt-3 text-[12px] leading-6 text-black/50">
              افتح تطبيق Google Authenticator على جوالك، ثم اكتب الرمز الحالي المكوّن من 6 أرقام لحساب ركن.
            </p>
          </div>

          <form
            onSubmit={submitMfa}
            className="p-7 md:p-9"
          >
            <label
              htmlFor="mfa-code"
              className="text-[11px] font-semibold text-black/70"
            >
              رمز Google Authenticator
            </label>

            <input
              id="mfa-code"
              dir="ltr"
              inputMode="numeric"
              autoFocus
              maxLength={6}
              value={mfaValue}
              onChange={(event) =>
                setMfaValue(
                  event.target.value.replace(
                    /\D/g,
                    "",
                  ),
                )
              }
              autoComplete="one-time-code"
              placeholder="000000"
              className="mt-2 h-14 w-full rounded-[12px] border border-black/10 bg-[#fbfaf7] px-4 text-center text-[22px] font-semibold tracking-[0.32em] outline-none transition placeholder:text-black/15 focus:border-[#7f6038]/50 focus:bg-white"
            />

            <p className="mt-2 text-[10px] leading-5 text-black/35">
              الرمز يتغير تلقائيًا داخل التطبيق. استخدم الرمز الظاهر حاليًا.
            </p>

            <ErrorBox
              error={error}
            />

            <button
              disabled={pending || mfaValue.length !== 6}
              className="mt-5 flex h-12 w-full items-center justify-center rounded-[12px] bg-[#18201d] text-[11px] font-semibold text-white transition hover:bg-[#0f1714] disabled:cursor-not-allowed disabled:opacity-40"
            >
              {pending
                ? "جاري التحقق…"
                : "تأكيد الدخول"}
            </button>

            <button
              type="button"
              onClick={() => {
                setChallengeToken(null);
                setMfaValue("");
                setError(null);
              }}
              className="mt-4 w-full text-center text-[10px] font-medium text-black/40 transition hover:text-black/65"
            >
              العودة إلى تسجيل الدخول
            </button>
          </form>
        </div>
      </AuthFrame>
    );
  }

  return (
    <AuthFrame>
      <div className="grid w-full max-w-[1040px] overflow-hidden border border-black/[0.08] bg-white lg:grid-cols-[.82fr_1.18fr]">
        <aside className="hidden bg-[#142720] p-10 text-white lg:flex lg:flex-col">
          <div>
            <div className="text-[23px] font-bold tracking-[-0.055em]">
              RUKN
            </div>

            <p className="mt-16 text-[11px] font-semibold text-[#d4bb92]">
              البداية الصحيحة
            </p>

            <h2 className="mt-3 max-w-[320px] text-[34px] font-semibold leading-[1.25] tracking-[-0.055em]">
              حساب واحد،
              وكل متجرك من مكان واحد.
            </h2>

            <p className="mt-5 max-w-[320px] text-[12px] leading-7 text-white/60">
              نبدأ ببياناتك الأساسية، وبعدها نجهّز المتجر خطوة بخطوة بدون صفحات مزدحمة أو أسئلة مكررة.
            </p>
          </div>

          <div className="mt-auto border-t border-white/10 pt-6 text-[10px] leading-6 text-white/45">
            بعد إنشاء الحساب سنطلب اسم المتجر، النشاط، الرابط، الهوية، ثم الباقة المناسبة قبل إرسال طلب الاعتماد.
          </div>
        </aside>

        <section className="p-7 md:p-10 lg:p-12">
          <div className="flex items-start justify-between gap-5">
            <div>
              <p className="text-[10px] font-semibold tracking-[0.08em] text-[#926b3a]">
                {mode ===
                "register"
                  ? "إنشاء حساب جديد"
                  : "مرحبًا بعودتك"}
              </p>

              <h1 className="mt-3 text-[31px] font-semibold tracking-[-0.05em] md:text-[38px]">
                {mode ===
                "register"
                  ? "ابدأ متجرك بهدوء."
                  : "سجّل دخولك."}
              </h1>

              <p className="mt-3 max-w-[470px] text-[12px] leading-6 text-black/48">
                {mode ===
                "register"
                  ? "نحتاج معلوماتك الأساسية فقط في هذه الخطوة."
                  : "استخدم نفس البريد وكلمة المرور المرتبطين بحسابك."}
              </p>
            </div>

            <Link
              to="/"
              className="mt-1 text-[10px] font-semibold text-black/40"
            >
              الرئيسية
            </Link>
          </div>

          <div className="mt-8 grid grid-cols-2 border border-black/[0.08] bg-[#f7f6f2] p-1">
            <button
              type="button"
              onClick={() => {
                setMode(
                  "register",
                );
                setError(null);
              }}
              className={[
                "h-10 text-[11px] font-semibold transition",
                mode ===
                "register"
                  ? "bg-white text-black shadow-sm"
                  : "text-black/40",
              ].join(" ")}
            >
              إنشاء حساب
            </button>

            <button
              type="button"
              onClick={() => {
                setMode(
                  "login",
                );
                setError(null);
              }}
              className={[
                "h-10 text-[11px] font-semibold transition",
                mode ===
                "login"
                  ? "bg-white text-black shadow-sm"
                  : "text-black/40",
              ].join(" ")}
            >
              تسجيل الدخول
            </button>
          </div>

          <form
            onSubmit={submit}
            className="mt-7 space-y-4"
          >
            {mode === "register" ? (
              <>
                <Field
                  icon={
                    <UserRound
                      size={16}
                    />
                  }
                  label="الاسم الكامل"
                >
                  <input
                    value={fullName}
                    onChange={(event) =>
                      setFullName(
                        event.target.value,
                      )
                    }
                    autoComplete="name"
                    placeholder="مثال: محمد الأحمد"
                    className={inputClass}
                  />
                </Field>

                <Field
                  icon={
                    <Phone
                      size={16}
                    />
                  }
                  label="رقم الجوال"
                >
                  <input
                    value={phoneNumber}
                    onChange={(event) =>
                      setPhoneNumber(
                        event.target.value,
                      )
                    }
                    autoComplete="tel"
                    dir="ltr"
                    placeholder="+9665xxxxxxxx"
                    className={`${inputClass} text-left`}
                  />
                </Field>
              </>
            ) : null}

            <Field
              icon={
                <Mail
                  size={16}
                />
              }
              label="البريد الإلكتروني"
            >
              <input
                type="email"
                value={email}
                onChange={(event) =>
                  setEmail(
                    event.target.value,
                  )
                }
                autoComplete="email"
                dir="ltr"
                placeholder="name@example.com"
                className={`${inputClass} text-left`}
              />
            </Field>

            <div>
              <div className="mb-2 flex items-center gap-2 text-[11px] font-semibold">
                <LockKeyhole
                  size={16}
                  className="text-black/35"
                />
                كلمة المرور
              </div>

              <div className="relative">
                <input
                  type={
                    showPassword
                      ? "text"
                      : "password"
                  }
                  value={password}
                  onChange={(event) =>
                    setPassword(
                      event.target.value,
                    )
                  }
                  autoComplete={
                    mode ===
                    "register"
                      ? "new-password"
                      : "current-password"
                  }
                  dir="ltr"
                  className={`${inputClass} pl-12 text-left`}
                />

                <button
                  type="button"
                  onClick={() =>
                    setShowPassword(
                      (value) =>
                        !value,
                    )
                  }
                  aria-label="إظهار أو إخفاء كلمة المرور"
                  className="absolute left-1 top-1 flex size-10 items-center justify-center text-black/35"
                >
                  {showPassword ? (
                    <EyeOff
                      size={17}
                    />
                  ) : (
                    <Eye
                      size={17}
                    />
                  )}
                </button>
              </div>

              {mode ===
              "register" ? (
                <p className="mt-2 text-[9px] leading-5 text-black/35">
                  8 أحرف على الأقل. لا تستخدم كلمة مرور لحساب آخر.
                </p>
              ) : null}
            </div>

            <ErrorBox
              error={error}
            />

            <button
              disabled={pending}
              className="mt-2 flex h-12 w-full items-center justify-center gap-2 bg-[#152a23] px-5 text-[12px] font-semibold text-white transition hover:bg-[#0e211b] disabled:cursor-not-allowed disabled:opacity-50"
            >
              {pending
                ? mode ===
                  "register"
                  ? "جار إنشاء الحساب..."
                  : "جار تسجيل الدخول..."
                : mode ===
                  "register"
                  ? "إنشاء الحساب والمتابعة"
                  : "تسجيل الدخول"}
              {!pending ? (
                <ArrowLeft
                  size={16}
                />
              ) : null}
            </button>
          </form>
        </section>
      </div>
    </AuthFrame>
  );
}

const inputClass =
  "h-12 w-full border border-black/[0.1] bg-[#fbfaf7] px-4 text-[13px] outline-none transition placeholder:text-black/25 focus:border-black/35";

function AuthFrame({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f5f3ed] px-5 py-8 text-[#15211d] md:py-12"
    >
      <main className="mx-auto flex min-h-[calc(100vh-96px)] max-w-[1180px] items-center justify-center">
        {children}
      </main>
    </div>
  );
}

function Field({
  icon,
  label,
  children,
}: {
  icon: React.ReactNode;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-2 flex items-center gap-2 text-[11px] font-semibold">
        <span className="text-black/35">
          {icon}
        </span>
        {label}
      </span>
      {children}
    </label>
  );
}

function ErrorBox({
  error,
}: {
  error: string | null;
}) {
  if (!error) {
    return null;
  }

  return (
    <div className="mt-4 border border-red-200 bg-red-50 px-4 py-3 text-[11px] leading-5 text-red-700">
      {error}
    </div>
  );
}
