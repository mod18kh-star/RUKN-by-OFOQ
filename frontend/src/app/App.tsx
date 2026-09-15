import {
  Navigate,
  Route,
  Routes,
} from "react-router";

import {
  AdminDashboardPage,
} from "../features/admin/AdminDashboardPage";

import {
  AdminPlaceholderPage,
} from "../features/admin/AdminPlaceholderPage";

import {
  AdminShell,
} from "../features/admin/AdminShell";

import {
  StoreBuilderPage,
} from "../features/admin/StoreBuilderPage";

import {
  StorefrontHomePage,
} from "../storefront/pages/StorefrontHomePage";

export function App() {
  return (
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
  );
}