import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import { useAppDispatch } from "../app/hooks";
import { pushActivity } from "../features/activity/activitySlice";
import { useExecuteJobTypeMutation } from "../services/api";

export const ExecutionLabPage = () => {
  const dispatch = useAppDispatch();
  const [execute, executeResult] = useExecuteJobTypeMutation();
  const [form, setForm] = useState({
    jobType: "email.send",
    payload: '{"to":"tenant@example.com","template":"welcome"}',
  });

  const errorText = useMemo(() => {
    const error = executeResult.error;
    if (!error || typeof error !== "object" || !("data" in error)) {
      return null;
    }

    const data = error.data as Record<string, unknown>;
    return (
      (typeof data.detail === "string" && data.detail) ||
      (typeof data.message === "string" && data.message) ||
      "Execution request failed."
    );
  }, [executeResult.error]);

  const handleExecute = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const result = await execute(form).unwrap();
    dispatch(
      pushActivity({
        title: "Execution test run",
        detail: `Executed ${form.jobType} with duration ${result.durationMs ?? 0} ms.`,
      }),
    );
  };

  return (
    <section className="stack-lg">
      <div className="card">
        <p className="eyebrow">Execution Service</p>
        <h3>Execution Lab</h3>
        <p className="muted">
          Validate job handler behavior through gateway route /api/v1/execute before queue submission.
        </p>
      </div>

      <form className="card form-grid" onSubmit={handleExecute}>
        <label>
          Job Type
          <input
            required
            value={form.jobType}
            onChange={(event) => setForm((prev) => ({ ...prev, jobType: event.target.value }))}
          />
        </label>

        <label>
          Payload
          <textarea
            rows={7}
            required
            value={form.payload}
            onChange={(event) => setForm((prev) => ({ ...prev, payload: event.target.value }))}
          />
        </label>

        <button className="btn primary" type="submit" disabled={executeResult.isLoading}>
          {executeResult.isLoading ? "Executing..." : "Run Execution"}
        </button>
        {errorText && <p className="error-text">{errorText}</p>}
      </form>

      <section className="card stack-sm">
        <h4>Execution Response</h4>
        <pre>{JSON.stringify(executeResult.data ?? { info: "No execution yet." }, null, 2)}</pre>
      </section>
    </section>
  );
};
