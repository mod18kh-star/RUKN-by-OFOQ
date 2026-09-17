import {
  useEffect,
  useState,
} from "react";

import {
  useNavigate,
} from "react-router";

import {
  DiscoveryWizardPage as DiscoveryWizardContent,
} from "./DiscoveryWizardPage";

import {
  findExistingMerchantStore,
} from "./onboardingResume";

export function DiscoveryWizardPage() {
  const navigate =
    useNavigate();

  const [resolved, setResolved] =
    useState(false);

  useEffect(
    () => {
      let active =
        true;

      const timer =
        window.setTimeout(
          () => {
            void findExistingMerchantStore()
              .then(
                (store) => {
                  if (!active) {
                    return;
                  }

                  if (store) {
                    navigate(
                      "/admin",
                      {
                        replace:
                          true,
                      },
                    );

                    return;
                  }

                  setResolved(
                    true,
                  );
                },
              )
              .catch(
                () => {
                  if (active) {
                    setResolved(
                      true,
                    );
                  }
                },
              );
          },
          0,
        );

      return () => {
        active =
          false;

        window.clearTimeout(
          timer,
        );
      };
    },
    [navigate],
  );

  if (!resolved) {
    return (
      <div
        dir="rtl"
        className="flex min-h-screen items-center justify-center bg-[#f5f3ed] px-6 text-[#080b14]"
      >
        <div className="text-center">
          <div className="mx-auto size-8 animate-pulse rounded-full bg-[#080b14]" />

          <p className="mt-5 text-[12px] font-semibold">
            جاري تجهيز حسابك
          </p>

          <p className="mt-2 text-[10px] text-black/42">
            نتأكد من متجرك الحالي قبل المتابعة
          </p>
        </div>
      </div>
    );
  }

  return (
    <DiscoveryWizardContent />
  );
}