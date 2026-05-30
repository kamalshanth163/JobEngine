import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "../app/hooks";
import { setCredentials } from "../features/auth/authSlice";
import { pushActivity } from "../features/activity/activitySlice";
import { useLoginMutation, useRegisterTenantMutation } from "../services/api";

const defaultRegister = {
  tenantName: "",
  slug: "",
  adminEmail: "",
  adminPassword: "",
};

const defaultLogin = {
  tenantSlug: "",
  email: "",
  password: "",
};

export const AuthPage = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const isAuthenticated = useAppSelector((state) => Boolean(state.auth.accessToken));

  const [mode, setMode] = useState<"login" | "register">("login");
  const [registerForm, setRegisterForm] = useState(defaultRegister);
  const [loginForm, setLoginForm] = useState(defaultLogin);

  const [registerTenant, registerResult] = useRegisterTenantMutation();
  const [login, loginResult] = useLoginMutation();

  const busy = registerResult.isLoading || loginResult.isLoading;

  const errorText = useMemo(() => {
    const error = registerResult.error ?? loginResult.error;
    if (!error || typeof error !== "object" || !("data" in error)) {
      return null;
    }

    const data = error.data as Record<string, unknown>;
    return (
      (typeof data.detail === "string" && data.detail) ||
      (typeof data.message === "string" && data.message) ||
      "Authentication request failed."
    );
  }, [loginResult.error, registerResult.error]);

  const onRegisterSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const result = await registerTenant(registerForm).unwrap();
    dispatch(
      setCredentials({
        accessToken: result.accessToken,
        refreshToken: "",
        tenantId: result.tenantId,
        tenantSlug: result.slug,
        email: registerForm.adminEmail,
      }),
    );
    dispatch(
      pushActivity({
        title: "Tenant registered",
        detail: `Tenant ${result.slug} is active with admin ${registerForm.adminEmail}.`,
      }),
    );

    navigate("/dashboard");
  };

  const onLoginSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const result = await login(loginForm).unwrap();
    dispatch(
      setCredentials({
        accessToken: result.accessToken,
        refreshToken: result.refreshToken,
        tenantId: result.tenantId,
        tenantSlug: loginForm.tenantSlug,
        email: loginForm.email,
        expiresAt: result.expiresAt,
      }),
    );

    dispatch(
      pushActivity({
        title: "Tenant login",
        detail: `Welcome back ${loginForm.email}.`,
      }),
    );

    navigate("/dashboard");
  };

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <section className="auth-page">
      <div className="auth-hero">
        <p className="eyebrow">Multi-tenant orchestration</p>
        <h1>Control every job from one tenant cockpit.</h1>
        <p>
          Register a tenant admin, sign in, submit jobs, inspect execution lifecycle,
          and generate API keys for secure integration.
        </p>
      </div>

      <div className="auth-card card">
        <div className="tab-row">
          <button
            type="button"
            className={mode === "login" ? "tab active" : "tab"}
            onClick={() => setMode("login")}
          >
            Login
          </button>
          <button
            type="button"
            className={mode === "register" ? "tab active" : "tab"}
            onClick={() => setMode("register")}
          >
            Register Tenant
          </button>
        </div>

        {mode === "register" ? (
          <form onSubmit={onRegisterSubmit} className="form-grid">
            <label>
              Tenant Name
              <input
                required
                value={registerForm.tenantName}
                onChange={(event) =>
                  setRegisterForm((prev) => ({ ...prev, tenantName: event.target.value }))
                }
              />
            </label>
            <label>
              Tenant Slug
              <input
                required
                value={registerForm.slug}
                placeholder="acme-jobs"
                pattern="^[a-z0-9-]+$"
                onChange={(event) =>
                  setRegisterForm((prev) => ({ ...prev, slug: event.target.value }))
                }
              />
            </label>
            <label>
              Admin Email
              <input
                required
                type="email"
                value={registerForm.adminEmail}
                onChange={(event) =>
                  setRegisterForm((prev) => ({ ...prev, adminEmail: event.target.value }))
                }
              />
            </label>
            <label>
              Admin Password
              <input
                required
                type="password"
                minLength={8}
                value={registerForm.adminPassword}
                onChange={(event) =>
                  setRegisterForm((prev) => ({ ...prev, adminPassword: event.target.value }))
                }
              />
            </label>
            <button className="btn primary" type="submit" disabled={busy}>
              {busy ? "Working..." : "Create Tenant"}
            </button>
          </form>
        ) : (
          <form onSubmit={onLoginSubmit} className="form-grid">
            <label>
              Tenant Slug
              <input
                required
                value={loginForm.tenantSlug}
                onChange={(event) =>
                  setLoginForm((prev) => ({ ...prev, tenantSlug: event.target.value }))
                }
              />
            </label>
            <label>
              Email
              <input
                required
                type="email"
                value={loginForm.email}
                onChange={(event) =>
                  setLoginForm((prev) => ({ ...prev, email: event.target.value }))
                }
              />
            </label>
            <label>
              Password
              <input
                required
                type="password"
                value={loginForm.password}
                onChange={(event) =>
                  setLoginForm((prev) => ({ ...prev, password: event.target.value }))
                }
              />
            </label>
            <button className="btn primary" type="submit" disabled={busy}>
              {busy ? "Signing in..." : "Sign In"}
            </button>
          </form>
        )}

        {errorText && <p className="error-text">{errorText}</p>}
      </div>
    </section>
  );
};
