import {
  lazy,
  Suspense,
} from "react";

import {
  Navigate,
  Route,
  Routes,
} from "react-router";

import {
  ProtectedRoute,
} from "../features/auth/ProtectedRoute";

const SecuritySetupPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/auth/SecuritySetupPage"
        );

      return {
        default:
          module.SecuritySetupPage,
      };
    },
  );

const PlatformLandingPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/marketing/PlatformLandingPage"
        );

      return {
        default:
          module.PlatformLandingPage,
      };
    },
  );

const RegistrationPendingPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/onboarding/RegistrationPendingPage"
        );

      return {
        default:
          module.RegistrationPendingPage,
      };
    },
  );

const AuthPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/onboarding/AuthPage"
        );

      return {
        default:
          module.AuthPage,
      };
    },
  );

const MerchantOnboardingPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/onboarding/MerchantOnboardingPage"
        );

      return {
        default:
          module.MerchantOnboardingPage,
      };
    },
  );
const AdminDashboardPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminDashboardPage"
        );

      return {
        default:
          module.AdminDashboardPage,
      };
    },
  );

const AdminProductsPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminProductsPage"
        );

      return {
        default:
          module.AdminProductsPage,
      };
    },
  );

const AdminInventoryPage = lazy(
  async () => {
    const module = await import("../features/admin/AdminInventoryPage");
    return { default: module.AdminInventoryPage };
  },
);
const AdminCategoriesPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminCategoriesPage"
        );

      return {
        default:
          module.AdminCategoriesPage,
      };
    },
  );

const AdminPagesPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminPagesPage"
        );

      return {
        default:
          module.AdminPagesPage,
      };
    },
  );

const AdminOrdersPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminOrdersPage"
        );

      return {
        default:
          module.AdminOrdersPage,
      };
    },
  );

const AdminPaymentsPage = lazy(async () => {
  const module = await import("../features/admin/payments/AdminPaymentsPage");
  return { default: module.AdminPaymentsPage };
});

const AdminCouponsPage = lazy(async () => {
  const module = await import("../features/admin/coupons/AdminCouponsPage");
  return { default: module.AdminCouponsPage };
});

const AdminShippingPage = lazy(async () => {
  const module = await import("../features/admin/shipping/AdminShippingPage");
  return { default: module.AdminShippingPage };
});

const AdminSettingsPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminSettingsPage"
        );

      return {
        default:
          module.AdminSettingsPage,
      };
    },
  );
const AdminShell =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminShell"
        );

      return {
        default:
          module.AdminShell,
      };
    },
  );

const AdminStorefrontPresentationPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminStorefrontPresentationPage"
        );

      return {
        default:
          module.AdminStorefrontPresentationPage,
      };
    },
  );

const StorefrontHomePage =
  lazy(
    async () => {
      const module =
        await import(
          "../storefront/pages/StorefrontHomePage"
        );

      return {
        default:
          module.StorefrontHomePage,
      };
    },
  );

const StorefrontContentPage =
  lazy(
    async () => {
      const module =
        await import(
          "../storefront/pages/StorefrontContentPage"
        );

      return {
        default:
          module.StorefrontContentPage,
      };
    },
  );

const StorefrontProductPage =
  lazy(
    async () => {
      const module =
        await import(
          "../storefront/pages/StorefrontProductPage"
        );

      return {
        default:
          module.StorefrontProductPage,
      };
    },
  );

const StorefrontCategoryPage =
  lazy(
    async () => {
      const module =
        await import(
          "../storefront/pages/StorefrontCategoryPage"
        );

      return {
        default:
          module.StorefrontCategoryPage,
      };
    },
  );

const StorefrontAccountPage = lazy(
  async () => {
    const module = await import(
      "../storefront/pages/StorefrontAccountPage"
    );

    return {
      default: module.StorefrontAccountPage,
    };
  },
);

const StorefrontCustomerAuthPage = lazy(
  async () => {
    const module = await import(
      "../storefront/pages/StorefrontCustomerAuthPage"
    );

    return {
      default: module.StorefrontCustomerAuthPage,
    };
  },
);

const StorefrontCartPage = lazy(
  async () => {
    const module = await import(
      "../storefront/pages/StorefrontCartPage"
    );

    return {
      default: module.StorefrontCartPage,
    };
  },
);

const StorefrontCheckoutPage = lazy(
  async () => {
    const module = await import(
      "../storefront/pages/StorefrontCheckoutPage"
    );

    return {
      default: module.StorefrontCheckoutPage,
    };
  },
);

const StorefrontManualPaymentPage = lazy(async () => {
  const module = await import("../storefront/pages/StorefrontManualPaymentPage");
  return { default: module.StorefrontManualPaymentPage };
});

const StorefrontContactPage =
  lazy(
    async () => {
      const module =
        await import(
          "../storefront/pages/StorefrontContactPage"
        );

      return {
        default:
          module.StorefrontContactPage,
      };
    },
  );

const PlatformShell =
  lazy(
    async () => {
      const module =
        await import(
          "../features/platform/PlatformShell"
        );

      return {
        default:
          module.PlatformShell,
      };
    },
  );

const PlatformDashboardPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/platform/PlatformDashboardPage"
        );

      return {
        default:
          module.PlatformDashboardPage,
      };
    },
  );

const PlatformStoresPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/platform/PlatformStoresPage"
        );

      return {
        default:
          module.PlatformStoresPage,
      };
    },
  );

const PlatformStoreDetailPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/platform/PlatformStoreDetailPage"
        );

      return {
        default:
          module.PlatformStoreDetailPage,
      };
    },
  );

const PlatformRequestsPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/platform/PlatformRequestsPage"
        );

      return {
        default:
          module.PlatformRequestsPage,
      };
    },
  );

const PlatformAdministratorsPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/platform/PlatformAdministratorsPage"
        );

      return {
        default:
          module.PlatformAdministratorsPage,
      };
    },
  );

export function App() {
  return (
    <Suspense
      fallback={
        <RouteLoadingFallback />
      }
    >
      <Routes>
        <Route
          path="/"
          element={
            <PlatformLandingPage />
          }
        />

        <Route
          path="/start"
          element={
            <AuthPage />
          }
        />

        <Route
          path="/start/login"
          element={
            <AuthPage />
          }
        />
        <Route
          element={
            <ProtectedRoute kind="authenticated" />
          }
        >
          <Route
            path="/security/setup"
            element={
              <SecuritySetupPage />
            }
          />

          <Route
            path="/start/onboarding"
            element={
              <MerchantOnboardingPage />
            }
          />

          <Route
            path="/start/review"
            element={
              <RegistrationPendingPage />
            }
          />

          <Route
            path="/start/discovery"
            element={
              <Navigate
                replace
                to="/start/onboarding"
              />
            }
          />

          <Route
            path="/start/recommendation"
            element={
              <Navigate
                replace
                to="/start/onboarding"
              />
            }
          />

          <Route
            path="/start/plans"
            element={
              <Navigate
                replace
                to="/start/onboarding"
              />
            }
          />

          <Route
            path="/start/application"
            element={
              <Navigate
                replace
                to="/start/onboarding"
              />
            }
          />

          <Route
            path="/start/store"
            element={
              <Navigate
                replace
                to="/start/onboarding"
              />
            }
          />
        </Route>

        <Route
          path="/store/:storeSlug"
          element={
            <StorefrontHomePage />
          }
        />

        <Route
          path="/store/:storeSlug/categories/:categorySlug"
          element={
            <StorefrontCategoryPage />
          }
        />

        <Route
          path="/store/:storeSlug/products/:productSlug"
          element={
            <StorefrontProductPage />
          }
        />

        <Route
          path="/store/:storeSlug/pages/:pageSlug"
          element={
            <StorefrontContentPage />
          }
        />

        <Route
          path="/store/:storeSlug/account/login"
          element={
            <StorefrontCustomerAuthPage />
          }
        />

        <Route
          path="/store/:storeSlug/account"
          element={
            <StorefrontAccountPage />
          }
        />

        <Route
          path="/store/:storeSlug/cart"
          element={
            <StorefrontCartPage />
          }
        />
        <Route
          path="/store/:storeSlug/checkout"
          element={<StorefrontCheckoutPage />}
        />
        <Route
          path="/store/:storeSlug/orders/:orderId/payment"
          element={<StorefrontManualPaymentPage />}
        />

        <Route
          path="/store/:storeSlug/contact"
          element={
            <StorefrontContactPage />
          }
        />

        <Route
          element={
            <ProtectedRoute kind="platform" />
          }
        >
          <Route
            path="/platform"
            element={
              <PlatformShell />
            }
          >
          <Route
            index
            element={
              <PlatformDashboardPage />
            }
          />

          <Route
            path="stores"
            element={
              <PlatformStoresPage />
            }
          />

          <Route
            path="stores/:tenantId"
            element={
              <PlatformStoreDetailPage />
            }
          />

          <Route
            path="requests"
            element={
              <PlatformRequestsPage />
            }
          />

          <Route
            path="access"
            element={
              <PlatformAdministratorsPage />
            }
          />
          </Route>
        </Route>
        <Route
          element={
            <ProtectedRoute kind="tenant-backoffice" />
          }
        >
          <Route
            path="/admin"
          element={
            <AdminShell />
          }
        >
          <Route
            index
            element={
              <AdminDashboardPage />
            }
          />

          <Route
            path="products"
            element={
              <AdminProductsPage />
            }
          />

          <Route
            path="inventory"
            element={<AdminInventoryPage />}
          />
          <Route
            path="categories"
            element={
              <AdminCategoriesPage />
            }
          />

          <Route
            path="pages"
            element={
              <AdminPagesPage />
            }
          />

          <Route
            path="orders"
            element={
              <AdminOrdersPage />
            }
          />

          <Route
            path="setup"
            element={
              <Navigate
                replace
                to="/admin/settings"
              />
            }
          />

          <Route
            path="store"
            element={
              <AdminStorefrontPresentationPage />
            }
          />

          <Route path="coupons" element={<AdminCouponsPage />} />
          <Route path="payments" element={<AdminPaymentsPage />} />
          <Route path="shipping" element={<AdminShippingPage />} />

          <Route
            path="settings"
            element={
              <AdminSettingsPage />
            }
          />
          </Route>
        </Route>

        <Route
          path="*"
          element={
            <Navigate
              to="/store/demo"
              replace
            />
          }
        />
      </Routes>
    </Suspense>
  );
}

function RouteLoadingFallback() {
  return (
    <div
      className="flex min-h-screen items-center justify-center bg-[#f7f7f4]"
      aria-live="polite"
      aria-busy="true"
    >
      <div className="w-[min(88vw,420px)]">
        <div className="h-3 w-20 animate-pulse rounded-full bg-black/10" />
        <div className="mt-5 h-8 w-2/3 animate-pulse rounded-[8px] bg-black/10" />
        <div className="mt-3 h-4 w-full animate-pulse rounded-[6px] bg-black/[0.06]" />
        <div className="mt-2 h-4 w-4/5 animate-pulse rounded-[6px] bg-black/[0.06]" />
      </div>
    </div>
  );
}