import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import { useAppDispatch, useAppSelector } from "../app/hooks";
import { pushActivity } from "../features/activity/activitySlice";
import { useCreateApiKeyMutation, useGetTenantQuery } from "../services/api";

export const ApiKeysPage = () => {
  const dispatch = useAppDispatch();
  const tenantId = useAppSelector((state) => state.auth.tenantId);
  const [keyName, setKeyName] = useState("integration-key");
  const [latestRawKey, setLatestRawKey] = useState<string | null>(null);

  const { data: tenant } = useGetTenantQuery(tenantId ?? "", {
    skip: !tenantId,
  });
  const [createApiKey, createResult] = useCreateApiKeyMutation();

  const errorText = useMemo(() => {
    const error = createResult.error;
    if (!error || typeof error !== "object" || !("data" in error)) {
      return null;
    }

    const data = error.data as Record<string, unknown>;
    return (
      (typeof data.detail === "string" && data.detail) ||
      (typeof data.message === "string" && data.message) ||
      "Failed to create API key."
    );
  }, [createResult.error]);

  if (!tenantId) {
    return <p className="error-text">You must be logged in as a tenant admin.</p>;
  }

  const handleCreateKey = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const result = await createApiKey({
      tenantId,
      payload: { name: keyName || undefined },
    }).unwrap();

    setLatestRawKey(result.rawKey);
    dispatch(
      pushActivity({
        title: "API key created",
        detail: `Prefix ${result.keyPrefix} for tenant ${tenant?.slug ?? tenantId}.`,
      }),
    );
  };

  return (
    <section className="stack-lg">
      <div className="card">
        <p className="eyebrow">Auth Service</p>
        <h3>Tenant API Keys</h3>
        <p className="muted">
          Generate keys for service-to-service integrations. Raw keys are shown one time.
        </p>
      </div>

      <section className="card stack-sm">
        <h4>Tenant Context</h4>
        <div className="info-grid">
          <article className="info-item">
            <span className="info-label">Name</span>
            <p className="info-value">{tenant?.name ?? "Loading..."}</p>
          </article>
          <article className="info-item">
            <span className="info-label">Slug</span>
            <p className="info-value">{tenant?.slug ?? "Loading..."}</p>
          </article>
          <article className="info-item">
            <span className="info-label">Tenant Id</span>
            <p className="info-value">{tenantId}</p>
          </article>
        </div>
      </section>

      <form className="card form-grid" onSubmit={handleCreateKey}>
        <label>
          Key Name (optional)
          <input value={keyName} onChange={(event) => setKeyName(event.target.value)} />
        </label>

        <button className="btn primary" type="submit" disabled={createResult.isLoading}>
          {createResult.isLoading ? "Creating..." : "Create API Key"}
        </button>

        {errorText && <p className="error-text">{errorText}</p>}
      </form>

      {latestRawKey && (
        <section className="card stack-sm">
          <h4>New Raw Key</h4>
          <p className="warning">Copy this now. It will not be returned again.</p>
          <pre>{latestRawKey}</pre>
        </section>
      )}
    </section>
  );
};
