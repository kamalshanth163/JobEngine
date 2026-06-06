import { useEffect, useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "../../app/hooks";
import { logout } from "../../features/auth/authSlice";
import { closeMobileMenu, toggleMobileMenu } from "../../features/ui/uiSlice";
import logo from "../../assets/logo.png";

const THEME_STORAGE_KEY = "jobengine.theme";

const navLinks = [
  { to: "/dashboard", label: "Dashboard" },
  { to: "/jobs", label: "Jobs" },
  { to: "/jobs/new", label: "Submit Job" },
  { to: "/api-keys", label: "API Keys" },
  { to: "/execution-lab", label: "Execution Lab" },
  { to: "/settings", label: "Settings" },
];

export const AppShell = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const mobileMenuOpen = useAppSelector((state) => state.ui.mobileMenuOpen);
  const activity = useAppSelector((state) => state.activity.events);
  const auth = useAppSelector((state) => state.auth);
  const [theme, setTheme] = useState<"light" | "dark">(() => {
    if (typeof window === "undefined") {
      return "light";
    }

    const savedTheme = window.localStorage.getItem(THEME_STORAGE_KEY);
    if (savedTheme === "light" || savedTheme === "dark") {
      return savedTheme;
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  });

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", theme);
    window.localStorage.setItem(THEME_STORAGE_KEY, theme);
  }, [theme]);

  const handleLogout = () => {
    dispatch(logout());
    dispatch(closeMobileMenu());
    navigate("/auth");
  };

  return (
    <div className="app-shell">
      <aside className={`sidebar ${mobileMenuOpen ? "sidebar-open" : ""}`}>
        <div className="brand-panel">
          <p className="eyebrow">Tenant Console</p>
          <div className="sidebar-brand-title">
            <img src={logo} alt="JobEngine logo" className="brand-logo" />
            <h1>JobEngine</h1>
          </div>
          <p className="tenant-chip">{auth.tenantSlug ?? "guest-tenant"}</p>
        </div>

        <nav className="main-nav" aria-label="Primary navigation">
          {navLinks.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={() => dispatch(closeMobileMenu())}
              className={({ isActive }) =>
                isActive ? "nav-link nav-link-active" : "nav-link"
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <button
            className="ghost theme-toggle"
            type="button"
            onClick={() => setTheme((prev) => (prev === "light" ? "dark" : "light"))}
          >
            {theme === "light" ? "Switch to Dark" : "Switch to Light"}
          </button>

          <button className="ghost danger" type="button" onClick={handleLogout}>
            Log out
          </button>
        </div>
      </aside>

      <div className="content-column">
        <header className="top-bar">
          <button
            className="menu-button"
            type="button"
            onClick={() => dispatch(toggleMobileMenu())}
            aria-label="Toggle navigation"
          >
            {mobileMenuOpen ? "Close" : "Menu"}
          </button>

          <div className="title-wrap">
            <p className="eyebrow">Realtime workloads</p>
            <h2>Tenant Operations</h2>
          </div>

          <div className="status-group">
            <span className="dot" />
            <span>Gateway: {import.meta.env.VITE_API_BASE_URL ?? "not-configured"}</span>
          </div>
        </header>

        <main className="page-content">
          <Outlet />
        </main>
      </div>

      <aside className="activity-panel">
        <h3>Activity Feed</h3>
        <p className="panel-caption">Latest tenant-level actions in this browser session.</p>

        <div className="activity-list">
          {activity.length === 0 ? (
            <p className="muted">No activity yet. Submit a job or create an API key.</p>
          ) : (
            activity.map((event) => (
              <article key={event.id} className="activity-item">
                <p className="activity-title">{event.title}</p>
                <p className="activity-detail">{event.detail}</p>
                <time>{new Date(event.createdAt).toLocaleTimeString()}</time>
              </article>
            ))
          )}
        </div>
      </aside>
    </div>
  );
};
