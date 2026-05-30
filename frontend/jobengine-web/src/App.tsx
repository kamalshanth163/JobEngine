import { Navigate, Route, Routes } from "react-router-dom";
import { useAppSelector } from "./app/hooks";
import { AppShell } from "./components/layout/AppShell";
import { ApiKeysPage } from "./pages/ApiKeysPage";
import { AuthPage } from "./pages/AuthPage";
import { DashboardPage } from "./pages/DashboardPage";
import { ExecutionLabPage } from "./pages/ExecutionLabPage";
import { JobDetailsPage } from "./pages/JobDetailsPage";
import { JobsPage } from "./pages/JobsPage";
import { NewJobPage } from "./pages/NewJobPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { SettingsPage } from "./pages/SettingsPage";

const ProtectedRoutes = () => {
  const isAuthenticated = useAppSelector((state) => Boolean(state.auth.accessToken));
  return isAuthenticated ? <AppShell /> : <Navigate to="/auth" replace />;
};

const App = () => {
  const isAuthenticated = useAppSelector((state) => Boolean(state.auth.accessToken));

  return (
    <Routes>
      <Route
        path="/"
        element={<Navigate to={isAuthenticated ? "/dashboard" : "/auth"} replace />}
      />
      <Route path="/auth" element={<AuthPage />} />

      <Route element={<ProtectedRoutes />}>
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/jobs" element={<JobsPage />} />
        <Route path="/jobs/new" element={<NewJobPage />} />
        <Route path="/jobs/:jobId" element={<JobDetailsPage />} />
        <Route path="/api-keys" element={<ApiKeysPage />} />
        <Route path="/execution-lab" element={<ExecutionLabPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
};

export default App;
