import {
  useMemo,
  useState,
  type FormEvent,
} from "react";

import {
  Link,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router";

import {
  ArrowRight,
  House,
  LockKeyhole,
  Mail,
  UserRound,
} from "lucide-react";

import {
  apiUrl,
  saveAccessSession,
} from "../../features/auth/authSession";

interface LoginResult {
  userId: string;
  email: string;
  requiresMfa: boolean;
  accessToken: string | null;
  accessTokenExpiresAtUtc: string | null;
  mfaChallengeToken: string | null;
}

interface MfaResult {
  userId: string;
  email: string;
  accessToken: string;
  expiresAtUtc: string;
}

interface ApiError {
  code?: string;
}

async function post<T>(
  path: string,
  body: unknown,
): Promise<T> {
  const response = await fetch(apiUrl(path), {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    const error = await response
      .json()
      .catch(() => ({})) as ApiError;

    if (error.code === "user_email_already_exists") {
      throw new Error(
        "لديك حساب بهذا البريد بالفعل. سجّل دخولك بدلًا من إنشاء حساب جديد.",
      );
    }

    if (error.code === "invalid_credentials") {
      throw new Error(
        "البريد الإلكتروني أو كلمة المرور غير صحيحة.",
      );
    }

    if (error.code === "invalid_password") {
      throw new Error(
        "كلمة المرور لا تحقق المتطلبات المطلوبة.",
      );
    }

    if (error.code === "invalid_mfa_verification") {
      throw new Error(
        "رمز التحقق غير صحيح أو انتهت صلاحيته.",
      );
    }

    if (response.status === 429) {
      throw new Error(
        "محاولات كثيرة. انتظر قليلًا ثم حاول مجددًا.",
      );
    }

    throw new Error(
      "تعذر إتمام العملية. حاول مرة أخرى.",
    );
  }

  return response.json() as Promise<T>;
}

export function StorefrontCustomerAuthPage() {
  const { storeSlug = "" } = useParams<{
    storeSlug: string;
  }>();

  const [params] = useSearchParams();

  const navigate = useNavigate();

  const storePath =
    `/store/${encodeURIComponent(storeSlug)}`;

  const accountPath = `${storePath}/account`;

  const requestedReturnTo =
    params.get("returnTo")?.trim() || "";

  const normalizedReturnTo =
    requestedReturnTo.startsWith("/")
      ? requestedReturnTo
      : accountPath;

  const [mode, setMode] = useState<
    "login" | "register"
  >(
    params.get("mode") === "register"
      ? "register"
      : "login",
  );

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");

  const [challenge, setChallenge] =
    useState<string | null>(null);

  const [code, setCode] = useState("");

  const [pending, setPending] = useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const inputClass =
    "mt-2 h-12 w-full rounded-xl border border-[#dfe4df] bg-white px-4 text-sm text-[#193c30] outline-none focus:border-[#47715a]";

  const primaryButton =
    "flex h-12 w-full items-center justify-center rounded-xl bg-[#193c30] px-5 text-sm font-semibold text-white transition hover:bg-[#295541] disabled:opacity-50";

  const subtitle = useMemo(() => {
    if (challenge) {
      return "أدخل رمز التحقق لإكمال تسجيل الدخول إلى حسابك.";
    }

    if (mode === "register") {
      return "أنشئ حسابك مرة واحدة، واستخدمه للتسوق من مختلف متاجر ركن.";
    }

    return "سجّل دخولك إلى حسابك، أو تابع التسوق كضيف.";
  }, [challenge, mode]);

  function completeLogin(result: LoginResult) {
    if (result.requiresMfa) {
      if (!result.mfaChallengeToken) {
        throw new Error(
          "تعذر بدء التحقق. حاول تسجيل الدخول مجددًا.",
        );
      }

      setChallenge(result.mfaChallengeToken);
      setCode("");

      return;
    }

    if (
      !result.accessToken ||
      !result.accessTokenExpiresAtUtc
    ) {
      throw new Error(
        "لم يصدر الخادم جلسة دخول صالحة.",
      );
    }

    saveAccessSession(
      result.accessToken,
      result.accessTokenExpiresAtUtc,
      result.email,
      result.userId,
    );

    navigate(normalizedReturnTo, { replace: true });
  }

  async function submit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (pending) return;

    setPending(true);
    setError(null);

    try {
      if (mode === "register") {
        if (fullName.trim().length < 2) {
          throw new Error("اكتب اسمك الكامل.");
        }

        await post<unknown>(
          "/api/auth/register",
          {
            fullName: fullName.trim(),
            email: email.trim(),
            password,
            phoneNumber: phone.trim() || null,
          },
        );
      }

      const result = await post<LoginResult>(
        "/api/auth/login",
        {
          email: email.trim(),
          password,
        },
      );

      completeLogin(result);
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

  async function verifyMfa(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (
      pending ||
      !challenge ||
      !/^\d{6}$/.test(code)
    ) {
      return;
    }

    setPending(true);
    setError(null);

    try {
      const result = await post<MfaResult>(
        "/api/auth/mfa/totp",
        {
          challengeToken: challenge,
          code,
          rememberDevice: false,
        },
      );

      saveAccessSession(
        result.accessToken,
        result.expiresAtUtc,
        result.email,
        result.userId,
      );

      navigate(normalizedReturnTo, { replace: true });
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر التحقق من الرمز.",
      );
    } finally {
      setPending(false);
    }
  }

  return (
    <main
      dir="rtl"
      className="min-h-screen bg-[#f7f8f5] px-4 py-10 text-[#193c30] sm:py-16"
    >
      <div className="mx-auto max-w-[540px]">
        <div className="mb-7 flex items-center justify-between">
          <Link
            to={normalizedReturnTo}
            className="inline-flex items-center gap-2 text-sm text-[#617469]"
          >
            <ArrowRight size={16} />
            العودة للصفحة السابقة
          </Link>

          <span className="text-sm font-semibold tracking-widest">
            RUKN
          </span>
        </div>

        <div className="overflow-hidden rounded-2xl border border-black/[0.06] bg-white shadow-sm">
          <div className="border-b border-black/[0.06] px-6 py-8 sm:px-9">
            <div className="mb-5 flex size-12 items-center justify-center rounded-xl bg-[#ecf2ed]">
              <House size={23} />
            </div>

            <p className="mb-2 text-xs font-semibold text-[#678570]">
              حساب ركن الموحد
            </p>

            <h1 className="text-2xl font-semibold">
              {challenge
                ? "التحقق من تسجيل الدخول"
                : mode === "register"
                  ? "أهلًا بك في ركن"
                  : "أهلًا بعودتك إلى ركن"}
            </h1>

            <p className="mt-3 text-sm leading-7 text-[#718077]">
              {subtitle}
            </p>
          </div>

          <div className="px-6 py-8 sm:px-9">
            {error && (
              <div
                role="alert"
                className="mb-5 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700"
              >
                {error}
              </div>
            )}

            {challenge ? (
              <form
                onSubmit={verifyMfa}
                className="space-y-5"
              >
                <div>
                  <label
                    htmlFor="customer-mfa"
                    className="flex items-center gap-2 text-sm font-medium"
                  >
                    <LockKeyhole size={16} />
                    رمز Google Authenticator
                  </label>

                  <input
                    id="customer-mfa"
                    className={`${inputClass} text-center text-xl tracking-[0.3em]`}
                    dir="ltr"
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    maxLength={6}
                    value={code}
                    onChange={(event) =>
                      setCode(
                        event.target.value
                          .replace(/\D/g, "")
                          .slice(0, 6),
                      )
                    }
                    required
                  />
                </div>

                <button
                  type="submit"
                  disabled={pending || code.length !== 6}
                  className={primaryButton}
                  style={{ color: "#ffffff" }}
                >
                  {pending
                    ? "جاري التحقق..."
                    : "متابعة"}
                </button>

                <button
                  type="button"
                  onClick={() => {
                    setChallenge(null);
                    setCode("");
                    setError(null);
                  }}
                  className="w-full text-center text-sm text-[#617469]"
                >
                  العودة إلى تسجيل الدخول
                </button>
              </form>
            ) : (
              <>
                <div className="mb-7 grid grid-cols-2 gap-2 rounded-xl bg-[#f2f4f1] p-1">
                  <button
                    type="button"
                    onClick={() => {
                      setMode("login");
                      setError(null);
                    }}
                    className={`h-10 rounded-lg text-sm font-medium ${
                      mode === "login"
                        ? "bg-white text-[#193c30] shadow-sm"
                        : "text-[#87938a]"
                    }`}
                  >
                    تسجيل الدخول
                  </button>

                  <button
                    type="button"
                    onClick={() => {
                      setMode("register");
                      setError(null);
                    }}
                    className={`h-10 rounded-lg text-sm font-medium ${
                      mode === "register"
                        ? "bg-white text-[#193c30] shadow-sm"
                        : "text-[#87938a]"
                    }`}
                  >
                    إنشاء حساب
                  </button>
                </div>

                <form
                  onSubmit={submit}
                  className="space-y-5"
                >
                  {mode === "register" && (
                    <>
                      <div>
                        <label
                          htmlFor="customer-name"
                          className="flex items-center gap-2 text-sm font-medium"
                        >
                          <UserRound size={16} />
                          الاسم الكامل
                        </label>

                        <input
                          id="customer-name"
                          className={inputClass}
                          value={fullName}
                          onChange={(event) =>
                            setFullName(event.target.value)
                          }
                          maxLength={160}
                          autoComplete="name"
                          required
                        />
                      </div>

                      <div>
                        <label
                          htmlFor="customer-phone"
                          className="text-sm font-medium"
                        >
                          رقم الجوال (اختياري)
                        </label>

                        <input
                          id="customer-phone"
                          type="tel"
                          dir="ltr"
                          className={inputClass}
                          value={phone}
                          onChange={(event) =>
                            setPhone(event.target.value)
                          }
                          maxLength={24}
                          autoComplete="tel"
                        />
                      </div>
                    </>
                  )}

                  <div>
                    <label
                      htmlFor="customer-email"
                      className="flex items-center gap-2 text-sm font-medium"
                    >
                      <Mail size={16} />
                      البريد الإلكتروني
                    </label>

                    <input
                      id="customer-email"
                      type="email"
                      dir="ltr"
                      className={inputClass}
                      value={email}
                      onChange={(event) =>
                        setEmail(event.target.value)
                      }
                      autoComplete="email"
                      required
                    />
                  </div>

                  <div>
                    <label
                      htmlFor="customer-password"
                      className="flex items-center gap-2 text-sm font-medium"
                    >
                      <LockKeyhole size={16} />
                      كلمة المرور
                    </label>

                    <input
                      id="customer-password"
                      type="password"
                      dir="ltr"
                      className={inputClass}
                      value={password}
                      onChange={(event) =>
                        setPassword(event.target.value)
                      }
                      autoComplete={
                        mode === "register"
                          ? "new-password"
                          : "current-password"
                      }
                      minLength={
                        mode === "register" ? 8 : undefined
                      }
                      required
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={pending}
                    className={primaryButton}
                    style={{ color: "#ffffff" }}
                  >
                    {pending
                      ? "جاري المتابعة..."
                      : mode === "register"
                        ? "إنشاء حسابك"
                        : "تسجيل الدخول"}
                  </button>
                </form>

                <Link
                  to={normalizedReturnTo}
                  className="mt-6 block text-center text-sm text-[#617469] underline underline-offset-4"
                >
                  المتابعة كضيف والعودة إلى الصفحة السابقة
                </Link>
              </>
            )}
          </div>
        </div>

        <p className="mt-6 text-center text-xs text-[#89958c]">
          حساب واحد في ركن، وتجربة تسوق مستقلة في كل متجر.
        </p>
      </div>
    </main>
  );
}