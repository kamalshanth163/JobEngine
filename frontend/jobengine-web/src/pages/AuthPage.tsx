import { useMemo, useRef, useState } from "react";
import type { FormEvent, MouseEvent } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "../app/hooks";
import { setCredentials } from "../features/auth/authSlice";
import { pushActivity } from "../features/activity/activitySlice";
import { useLoginMutation, useRegisterTenantMutation } from "../services/api";
import logo from "../assets/logo.png";

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
  const authPageRef = useRef<HTMLElement | null>(null);
  const idleTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
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

  const onAuthPageMouseMove = (event: MouseEvent<HTMLElement>) => {
    const page = authPageRef.current;
    if (!page) {
      return;
    }

    if (idleTimerRef.current) {
      clearTimeout(idleTimerRef.current);
      idleTimerRef.current = null;
    }

    const rect = page.getBoundingClientRect();
    const xRatio = ((event.clientX - rect.left) / rect.width - 0.5) * 2;
    const yRatio = ((event.clientY - rect.top) / rect.height - 0.5) * 2;

    page.style.setProperty("--grid-react-ms", "55ms");
    page.style.setProperty("--grid-shift-x", `${xRatio * 22}px`);
    page.style.setProperty("--grid-shift-y", `${yRatio * 18}px`);

    idleTimerRef.current = setTimeout(() => {
      page.style.setProperty("--grid-react-ms", "460ms");
      page.style.setProperty("--grid-shift-x", "0px");
      page.style.setProperty("--grid-shift-y", "0px");
    }, 130);
  };

  const onAuthPageMouseLeave = () => {
    const page = authPageRef.current;
    if (!page) {
      return;
    }

    if (idleTimerRef.current) {
      clearTimeout(idleTimerRef.current);
      idleTimerRef.current = null;
    }

    page.style.setProperty("--grid-react-ms", "460ms");
    page.style.setProperty("--grid-shift-x", "0px");
    page.style.setProperty("--grid-shift-y", "0px");
  };

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <section
      ref={authPageRef}
      className="auth-page"
      onMouseMove={onAuthPageMouseMove}
      onMouseLeave={onAuthPageMouseLeave}
    >
      <div className="auth-processing" aria-hidden="true">
        <span className="job-particle particle-a" />
        <span className="job-particle particle-b" />
        <span className="job-particle particle-c" />
        <span className="job-particle particle-d" />
      </div>

      <div className="auth-hero">
        <div className="auth-brand">
          <img src={logo} alt="JobEngine logo" className="brand-logo" />
          <span>JobEngine</span>
        </div>
        <p className="eyebrow">Distributed Job Platform</p>
        <h1>Run every background workload from one modern operations hub.</h1>
        <p>
          Launch jobs across distributed workers, monitor throughput in real time,
          and resolve retries before they become incidents, all with secure
          tenant-level access and API-first automation.
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
