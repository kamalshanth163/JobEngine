import { useAppDispatch, useAppSelector } from "../app/hooks";
import { clearActivity } from "../features/activity/activitySlice";

export const SettingsPage = () => {
  const dispatch = useAppDispatch();
  const auth = useAppSelector((state) => state.auth);

  return (
    <section className="stack-lg">
      <div className="card">
        <p className="eyebrow">Tenant profile</p>
        <h3>Session Settings</h3>
      </div>

      <section className="card stack-sm">
        <h4>Authenticated Context</h4>
        <div className="info-grid">
          <article className="info-item">
            <span className="info-label">Email</span>
            <p className="info-value">{auth.email ?? "-"}</p>
          </article>
          <article className="info-item">
            <span className="info-label">Tenant Slug</span>
            <p className="info-value">{auth.tenantSlug ?? "-"}</p>
          </article>
          <article className="info-item">
            <span className="info-label">Tenant Id</span>
            <p className="info-value">{auth.tenantId ?? "-"}</p>
          </article>
          <article className="info-item">
            <span className="info-label">Token Expires</span>
            <p className="info-value">{auth.expiresAt ?? "n/a"}</p>
          </article>
        </div>
      </section>

      <section className="card stack-sm">
        <h4>Environment</h4>
        <div className="info-grid one-col">
          <article className="info-item">
            <span className="info-label">Gateway URL</span>
            <p className="info-value">
              {import.meta.env.VITE_API_BASE_URL || "not-configured"}
            </p>
          </article>
        </div>
      </section>

      <section className="card stack-sm">
        <h4>Activity Feed</h4>
        <button className="btn secondary" type="button" onClick={() => dispatch(clearActivity())}>
          Clear local activity
        </button>
      </section>
    </section>
  );
};
