import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";

import {
  Mail,
  Plus,
  ShieldCheck,
  UserRoundCheck,
  Users,
} from "lucide-react";

import {
  addPlatformAdministrator,
  getPlatformAdministrators,
  type PlatformAdministrator,
} from "./platformApi";

function formatDate(value: string) {
  return new Intl.DateTimeFormat(
    "ar-SA",
    {
      dateStyle: "medium",
      timeStyle: "short",
    },
  ).format(
    new Date(value),
  );
}

export function PlatformAdministratorsPage() {
  const [administrators, setAdministrators] =
    useState<PlatformAdministrator[]>([]);

  const [email, setEmail] =
    useState("");

  const [loading, setLoading] =
    useState(true);

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null,
    );

  const [message, setMessage] =
    useState<string | null>(
      null,
    );

  const activeCount =
    useMemo(
      () =>
        administrators.filter(
          (administrator) =>
            administrator.status ===
            "Active",
        ).length,
      [administrators],
    );

  async function load() {
    setLoading(true);
    setError(null);

    try {
      setAdministrators(
        await getPlatformAdministrators(),
      );
    } catch (caughtError) {
      setError(
        caughtError instanceof Error
          ? caughtError.message
          : "تعذر تحميل فريق ركن.",
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(
    () => {
      let cancelled =
        false;

      void getPlatformAdministrators()
        .then(
          (items) => {
            if (cancelled) {
              return;
            }

            setAdministrators(
              items,
            );

            setError(
              null,
            );
          },
        )
        .catch(
          (caughtError) => {
            if (cancelled) {
              return;
            }

            setError(
              caughtError instanceof Error
                ? caughtError.message
                : "تعذر تحميل فريق ركن.",
            );
          },
        )
        .finally(
          () => {
            if (!cancelled) {
              setLoading(
                false,
              );
            }
          },
        );

      return () => {
        cancelled =
          true;
      };
    },
    [],
  );

  async function submit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    const normalizedEmail =
      email.trim();

    if (!normalizedEmail) {
      setError(
        "اكتب البريد الإلكتروني أولًا.",
      );

      return;
    }

    setSaving(true);
    setError(null);
    setMessage(null);

    try {
      const result =
        await addPlatformAdministrator(
          normalizedEmail,
        );

      setEmail("");

      setMessage(
        result.alreadyAdministrator
          ? "هذا الحساب يملك صلاحية Super Admin مسبقًا."
          : "تمت إضافة الحساب إلى فريق إدارة ركن.",
      );

      await load();
    } catch (caughtError) {
      setError(
        caughtError instanceof Error
          ? caughtError.message
          : "تعذر إضافة الحساب.",
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      dir="rtl"
      className="mx-auto max-w-[1180px]"
    >
      <div className="flex flex-col gap-5 border-b border-black/[0.08] pb-7 md:flex-row md:items-end md:justify-between">
        <div>
          <div className="flex items-center gap-2 text-[10px] font-semibold text-[#966f3b]">
            <ShieldCheck size={14} />

            PLATFORM ACCESS
          </div>

          <h1 className="mt-3 text-[28px] font-semibold tracking-[-0.04em] md:text-[32px]">
            فريق إدارة ركن
          </h1>

          <p className="mt-3 max-w-[660px] text-[12px] leading-7 text-black/48">
            الحسابات هنا تملك صلاحية إدارة المنصة نفسها وليست صلاحية متجر محدد.
            أضف هذه الصلاحية فقط لحسابات فريق ركن الموثوقة.
          </p>
        </div>

        <div className="flex items-center gap-3 rounded-[12px] border border-black/[0.08] bg-white px-4 py-3">
          <Users
            size={17}
            className="text-[#956b35]"
          />

          <div>
            <p className="text-[9px] text-black/42">
              المدراء الفعالون
            </p>

            <p className="mt-0.5 text-[18px] font-semibold">
              {activeCount}
            </p>
          </div>
        </div>
      </div>

      <div className="mt-7 grid gap-6 lg:grid-cols-[360px_minmax(0,1fr)]">
        <section className="self-start rounded-[16px] border border-black/[0.08] bg-white p-5 shadow-[0_10px_35px_rgba(16,18,25,0.035)]">
          <div className="flex size-10 items-center justify-center rounded-[10px] bg-[#f0e7da] text-[#8b6332]">
            <Plus size={18} />
          </div>

          <h2 className="mt-5 text-[16px] font-semibold">
            إضافة Super Admin
          </h2>

          <p className="mt-2 text-[11px] leading-6 text-black/45">
            للحماية، يجب أن يكون البريد مسجلًا مسبقًا كحساب في ركن.
            بعدها يمكنك منحه صلاحية إدارة المنصة من هنا.
          </p>

          <form
            onSubmit={submit}
            className="mt-5"
          >
            <label className="text-[10px] font-semibold text-black/65">
              البريد الإلكتروني
            </label>

            <div className="mt-2 flex h-11 items-center gap-2 rounded-[9px] border border-black/[0.1] bg-[#faf9f6] px-3 focus-within:border-[#9c7441]">
              <Mail
                size={15}
                className="text-black/30"
              />

              <input
                dir="ltr"
                type="email"
                value={email}
                onChange={(event) => {
                  setEmail(
                    event.target.value,
                  );
                }}
                placeholder="name@example.com"
                className="min-w-0 flex-1 bg-transparent text-left text-[11px] outline-none placeholder:text-black/25"
              />
            </div>

            <button
              type="submit"
              disabled={saving}
              className="mt-4 inline-flex h-11 w-full items-center justify-center gap-2 rounded-[9px] bg-[#0a0d15] px-4 text-[11px] font-semibold text-white transition hover:bg-[#151a25] disabled:cursor-not-allowed disabled:opacity-50"
            >
              <UserRoundCheck size={15} />

              {saving
                ? "جاري الإضافة..."
                : "منح صلاحية Super Admin"}
            </button>
          </form>

          {error ? (
            <p className="mt-4 rounded-[9px] border border-red-200 bg-red-50 px-3 py-2.5 text-[10px] leading-5 text-red-700">
              {error}
            </p>
          ) : null}

          {message ? (
            <p className="mt-4 rounded-[9px] border border-emerald-200 bg-emerald-50 px-3 py-2.5 text-[10px] leading-5 text-emerald-800">
              {message}
            </p>
          ) : null}

          <div className="mt-5 border-t border-black/[0.07] pt-4">
            <p className="text-[9px] leading-5 text-black/38">
              في الإنتاج سنربط إضافة موظفي المنصة بدعوات بريدية وMFA إلزامي.
              حاليًا لا يتم عرض أو تخزين كلمات مرور الموظفين داخل لوحة Super Admin.
            </p>
          </div>
        </section>

        <section className="overflow-hidden rounded-[16px] border border-black/[0.08] bg-white shadow-[0_10px_35px_rgba(16,18,25,0.035)]">
          <div className="flex items-center justify-between border-b border-black/[0.07] px-5 py-4">
            <div>
              <p className="text-[13px] font-semibold">
                الحسابات المخولة
              </p>

              <p className="mt-1 text-[9px] text-black/38">
                وصول على مستوى منصة ركن
              </p>
            </div>

            <ShieldCheck
              size={17}
              className="text-[#956b35]"
            />
          </div>

          {loading ? (
            <div className="p-6">
              <div className="h-12 animate-pulse rounded-[10px] bg-black/[0.04]" />
              <div className="mt-3 h-12 animate-pulse rounded-[10px] bg-black/[0.04]" />
            </div>
          ) : administrators.length === 0 ? (
            <div className="p-8 text-center">
              <p className="text-[12px] font-semibold">
                لا يوجد مديرو منصة ظاهرون
              </p>

              <p className="mt-2 text-[10px] text-black/40">
                أضف أول حساب مخول من النموذج.
              </p>
            </div>
          ) : (
            <div>
              {administrators.map(
                (administrator) => (
                  <div
                    key={administrator.userId}
                    className="flex flex-col gap-3 border-b border-black/[0.06] px-5 py-4 last:border-b-0 sm:flex-row sm:items-center sm:justify-between"
                  >
                    <div className="flex min-w-0 items-center gap-3">
                      <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[#f1ece3] text-[12px] font-semibold text-[#8a6334]">
                        {administrator.email
                          .slice(0, 1)
                          .toUpperCase()}
                      </div>

                      <div className="min-w-0">
                        <p
                          dir="ltr"
                          className="truncate text-left text-[11px] font-semibold"
                        >
                          {administrator.email}
                        </p>

                        <p className="mt-1 text-[9px] text-black/36">
                          أضيف في{" "}
                          {formatDate(
                            administrator.addedAtUtc,
                          )}
                        </p>
                      </div>
                    </div>

                    <div className="flex items-center gap-2">
                      <span className="rounded-full bg-[#edf4ef] px-2.5 py-1 text-[9px] font-semibold text-[#366044]">
                        {administrator.status === "Active"
                          ? "فعال"
                          : administrator.status}
                      </span>

                      <span
                        className={[
                          "rounded-full px-2.5 py-1 text-[9px] font-semibold",
                          administrator.emailVerified
                            ? "bg-[#f2eee6] text-[#77552c]"
                            : "bg-[#fff5e8] text-[#91601d]",
                        ].join(" ")}
                      >
                        {administrator.emailVerified
                          ? "البريد موثق"
                          : "البريد غير موثق"}
                      </span>
                    </div>
                  </div>
                ),
              )}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
