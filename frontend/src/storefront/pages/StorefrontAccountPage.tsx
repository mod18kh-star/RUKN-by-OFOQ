import { CustomerAddressesSection } from "./CustomerAddressesSection";
import { CustomerManualOrdersSection } from "./CustomerManualOrdersSection";
import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
  type ReactNode,
} from "react";

import {
  Link,
  useParams,
  useSearchParams,
} from "react-router";

import {
  ArrowLeft,
  House,
  LogOut,
  Mail,
  Phone,
  ShoppingBag,
  Star,
} from "lucide-react";

import {
  AuthSessionError,
  authRequest,
  getCurrentUser,
  logoutSession,
  type CurrentUser,
} from "../../features/auth/authSession";

interface ProfileResponse {
  userId: string;
  email: string;
  fullName: string | null;
  phoneNumber: string | null;
}

function firstNameOf(
  value: string | null | undefined,
) {
  const cleaned = (value ?? "").trim();

  if (!cleaned) return "صديقنا";

  return cleaned.split(/\s+/)[0] || "صديقنا";
}

function profileSectionCard(
  title: string,
  description: string,
  icon: ReactNode,
) {
  return (
    <div className="rounded-2xl border border-black/[0.06] bg-[#fafbf8] p-5">
      <div className="mb-3 flex size-11 items-center justify-center rounded-xl bg-white">
        {icon}
      </div>

      <h3 className="text-sm font-semibold text-[#20382d]">
        {title}
      </h3>

      <p className="mt-2 text-sm leading-7 text-[#748077]">
        {description}
      </p>
    </div>
  );
}

export function StorefrontAccountPage() {
  const { storeSlug = "" } = useParams<{
    storeSlug: string;
  }>();

  const [searchParams] = useSearchParams();

  const storePath =
    `/store/${encodeURIComponent(storeSlug)}`;

  const accountPath =
    `${storePath}/account`;

  const requestedReturnTo =
    searchParams.get("returnTo")?.trim() || "";

  const normalizedReturnTo =
    requestedReturnTo.startsWith("/")
      ? requestedReturnTo
      : storePath;

  const encodedReturnTo =
    encodeURIComponent(normalizedReturnTo);

  const loginPath =
    `${accountPath}/login?mode=login&returnTo=${encodedReturnTo}`;

  const registerPath =
    `${accountPath}/login?mode=register&returnTo=${encodedReturnTo}`;

  const [user, setUser] =
    useState<CurrentUser | null>(null);

  const [loading, setLoading] =
    useState(true);

  const [saving, setSaving] =
    useState(false);

  const [fullName, setFullName] =
    useState("");

  const [phoneNumber, setPhoneNumber] =
    useState("");

  const [error, setError] =
    useState<string | null>(null);

  const [success, setSuccess] =
    useState<string | null>(null);

  useEffect(() => {
    let active = true;

    async function loadAccount() {
      try {
        const result =
          await getCurrentUser();

        if (!active) return;

        setUser(result);
        setFullName(result.fullName ?? "");
        setPhoneNumber(result.phoneNumber ?? "");
      } catch (caught) {
        if (!active) return;

        if (
          caught instanceof AuthSessionError &&
          caught.status === 401
        ) {
          setUser(null);
        } else {
          setError(
            "تعذر تحميل بيانات الحساب. حاول مرة أخرى.",
          );
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void loadAccount();

    return () => {
      active = false;
    };
  }, []);

  async function saveProfile(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (!user || saving) return;

    if (fullName.trim().length < 2) {
      setError("اكتب اسمك الكامل.");
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const result =
        await authRequest<ProfileResponse>(
          "/api/customer/profile",
          {
            method: "PUT",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({
              fullName: fullName.trim(),
              phoneNumber: phoneNumber.trim(),
            }),
          },
        );

      setUser({
        ...user,
        fullName: result.fullName,
        phoneNumber: result.phoneNumber,
      });

      setFullName(result.fullName ?? "");
      setPhoneNumber(result.phoneNumber ?? "");

      setSuccess("تم حفظ معلومات حسابك.");
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر حفظ التعديلات.",
      );
    } finally {
      setSaving(false);
    }
  }

  async function signOut() {
    setError(null);
    setSuccess(null);

    try {
      await logoutSession();

      setUser(null);
    } catch {
      setUser(null);

      setError(
        "تعذر تأكيد الخروج من الخادم. تم مسح الجلسة المحلية، ويُرجى المحاولة مجددًا.",
      );
    }
  }

  const inputClass =
    "mt-2 h-12 w-full rounded-xl border border-black/10 bg-white px-4 text-sm text-[#24352d] outline-none transition focus:border-[#537260]";

  const greetingName = useMemo(
    () => firstNameOf(user?.fullName),
    [user?.fullName],
  );

  return (
    <main
      dir="rtl"
      className="min-h-screen bg-[#f7f7f5] px-4 py-12 text-[#24352d] sm:py-20"
    >
      <div className="mx-auto w-full max-w-3xl">

        <div className="mb-8 flex items-center justify-between gap-4">
          <Link
            to={normalizedReturnTo}
            className="inline-flex items-center gap-2 text-sm font-medium text-[#526557] transition hover:text-black"
          >
            <ArrowLeft size={16} />
            العودة للصفحة السابقة
          </Link>

          <span className="text-xs font-semibold tracking-widest text-[#849187]">
            RUKN
          </span>
        </div>

        <div className="overflow-hidden rounded-2xl border border-black/[0.07] bg-white shadow-sm">

          <div className="border-b border-black/[0.06] px-6 py-8 sm:px-9">
            <div className="mb-5 flex size-14 items-center justify-center rounded-2xl bg-[#edf3ee]">
              <House size={24} />
            </div>

            <h1 className="text-2xl font-semibold tracking-tight">
              حسابك في ركن
            </h1>

            <p className="mt-3 text-sm leading-7 text-[#778178]">
              حساب واحد يرافقك في مختلف متاجر ركن.
            </p>

            {!loading && user ? (
              <div className="mt-5 rounded-2xl bg-[#f5f7f4] px-4 py-4">
                <p className="text-sm font-semibold text-[#20382d]">
                  أهلًا {greetingName}، مرحبًا بعودتك.
                </p>

                <p className="mt-1 text-sm text-[#768078]">
                  يسعدنا وجودك هنا، ويمكنك متابعة طلباتك وإدارة بياناتك من هذه الصفحة.
                </p>
              </div>
            ) : null}
          </div>

          <div className="px-6 py-8 sm:px-9">
            {loading ? (
              <p className="text-sm text-[#778178]">
                جاري تحميل الحساب...
              </p>
            ) : null}

            {error ? (
              <div
                role="alert"
                className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
              >
                {error}
              </div>
            ) : null}

            {success ? (
              <div
                role="status"
                className="mb-5 rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-800"
              >
                {success}
              </div>
            ) : null}

            {!loading && !user && !error ? (
              <div className="space-y-5">
                <p className="text-sm leading-7 text-[#66746a]">
                  سجّل دخولك للوصول إلى حسابك، أو أنشئ حسابًا جديدًا واستخدمه
                  في مختلف متاجر ركن.
                </p>

                <div className="grid gap-3 sm:grid-cols-2">
                  <Link
                    to={loginPath}
                    className="flex h-12 items-center justify-center rounded-xl bg-[#243b2f] px-5 text-sm font-semibold text-white transition hover:bg-[#35513f]"
                    style={{ color: "#ffffff" }}
                  >
                    تسجيل الدخول
                  </Link>

                  <Link
                    to={registerPath}
                    className="flex h-12 items-center justify-center rounded-xl border border-black/10 px-5 text-sm font-semibold transition hover:bg-[#f7f8f6]"
                  >
                    إنشاء حساب
                  </Link>
                </div>

                <Link
                  to={normalizedReturnTo}
                  className="block pt-2 text-center text-sm text-[#66746a] underline underline-offset-4"
                >
                  متابعة التسوق دون تسجيل
                </Link>
              </div>
            ) : null}

            {!loading && user ? (
              <div className="space-y-8">
                <div className="flex items-start gap-3 rounded-xl bg-[#f5f7f4] p-4">
                  <Mail
                    size={18}
                    className="mt-0.5 shrink-0 text-[#697e6d]"
                  />

                  <div className="min-w-0">
                    <p className="text-xs text-[#849187]">
                      البريد الإلكتروني
                    </p>

                    <p
                      dir="ltr"
                      className="mt-1 break-all text-left text-sm font-medium"
                    >
                      {user.email}
                    </p>
                  </div>
                </div>

                <form
                  onSubmit={saveProfile}
                  className="space-y-5"
                >
                  <div>
                    <label
                      htmlFor="customer-name"
                      className="text-sm font-medium"
                    >
                      الاسم الكامل
                    </label>

                    <input
                      id="customer-name"
                      className={inputClass}
                      value={fullName}
                      onChange={(event) =>
                        setFullName(event.target.value)
                      }
                      maxLength={150}
                      autoComplete="name"
                      required
                    />
                  </div>

                  <div>
                    <label
                      htmlFor="customer-phone"
                      className="flex items-center gap-2 text-sm font-medium"
                    >
                      <Phone size={15} />
                      رقم الهاتف
                    </label>

                    <input
                      id="customer-phone"
                      className={inputClass}
                      type="tel"
                      dir="ltr"
                      value={phoneNumber}
                      onChange={(event) =>
                        setPhoneNumber(event.target.value)
                      }
                      maxLength={32}
                      autoComplete="tel"
                      placeholder="+966..."
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={saving}
                    className="flex h-12 w-full items-center justify-center rounded-xl bg-[#243b2f] px-5 text-sm font-semibold text-white transition hover:bg-[#35513f] disabled:cursor-not-allowed disabled:opacity-60"
                    style={{ color: "#ffffff" }}
                  >
                    {saving
                      ? "جاري حفظ التعديلات..."
                      : "حفظ التعديلات"}
                  </button>
                </form>

                <div className="grid gap-4 md:grid-cols-2">
                  {profileSectionCard(
                    "طلباتك السابقة",
                    "ستظهر هنا طلباتك السابقة داخل ركن، مع حالتها وتفاصيلها وروابط المتابعة.",
                    <ShoppingBag size={18} />,
                  )}



                  {profileSectionCard(
                    "تقييماتك",
                    "ستظهر هنا تقييماتك ومراجعاتك السابقة للمنتجات، لتسهيل الرجوع إليها وتحديثها لاحقًا.",
                    <Star size={18} />,
                  )}
                </div>

                <CustomerManualOrdersSection />

                <CustomerAddressesSection />

                <div className="border-t border-black/[0.07] pt-5">
                  <button
                    type="button"
                    onClick={() => void signOut()}
                    className="inline-flex items-center gap-2 text-sm font-medium text-[#8c4949] transition hover:text-red-700"
                  >
                    <LogOut size={16} />
                    تسجيل الخروج
                  </button>
                </div>
              </div>
            ) : null}
          </div>
        </div>

        <p className="mt-6 text-center text-xs leading-6 text-[#89948b]">
          يمكنك استخدام حساب ركن نفسه في أي متجر آخر.
        </p>
      </div>
    </main>
  );
}