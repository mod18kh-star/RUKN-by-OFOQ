import {
  lazy,
  Suspense,
} from "react";

import {
  Navigate,
  Route,
  Routes,
} from "react-router";

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

const AdminPlaceholderPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/AdminPlaceholderPage"
        );

      return {
        default:
          module.AdminPlaceholderPage,
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

const StoreBuilderPage =
  lazy(
    async () => {
      const module =
        await import(
          "../features/admin/StoreBuilderPage"
        );

      return {
        default:
          module.StoreBuilderPage,
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
            <Navigate
              to="/store/demo"
              replace
            />
          }
        />

        <Route
          path="/store/:storeSlug"
          element={
            <StorefrontHomePage />
          }
        />

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
              <AdminPlaceholderPage
                title="المنتجات"
                description="إدارة المنتجات والمخزون والصور والمواصفات."
              />
            }
          />

          <Route
            path="orders"
            element={
              <AdminPlaceholderPage
                title="الطلبات"
                description="متابعة الطلبات والتجهيز والشحن."
              />
            }
          />

          <Route
            path="store"
            element={
              <StoreBuilderPage />
            }
          />

          <Route
            path="settings"
            element={
              <AdminPlaceholderPage
                title="الإعدادات"
                description="إعدادات المتجر والحساب."
              />
            }
          />
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