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
        <p>
          <strong>Email:</strong> {auth.email ?? "-"}
        </p>
        <p>
          <strong>Tenant Slug:</strong> {auth.tenantSlug ?? "-"}
        </p>
        <p>
          <strong>Tenant Id:</strong> {auth.tenantId ?? "-"}
        </p>
        <p>
          <strong>Token Expires:</strong> {auth.expiresAt ?? "n/a"}
        </p>
      </section>

      <section className="card stack-sm">
        <h4>Environment</h4>
        <p>
          <strong>Gateway URL:</strong> {import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080"}
        </p>
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
