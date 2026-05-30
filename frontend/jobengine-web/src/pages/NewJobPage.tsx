import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAppDispatch } from "../app/hooks";
import { pushActivity } from "../features/activity/activitySlice";
import { useSubmitJobMutation } from "../services/api";

export const NewJobPage = () => {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const [submitJob, submitResult] = useSubmitJobMutation();
  const [form, setForm] = useState({
    type: "email.send",
    payload: '{"to":"tenant@example.com","template":"welcome"}',
    priority: 0,
    maxAttempts: 3,
    scheduledAt: "",
  });

  const errorText = useMemo(() => {
    const error = submitResult.error;
    if (!error || typeof error !== "object" || !("data" in error)) {
      return null;
    }

    const data = error.data as Record<string, unknown>;
    return (
      (typeof data.detail === "string" && data.detail) ||
      (typeof data.message === "string" && data.message) ||
      "Job submission failed."
    );
  }, [submitResult.error]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const jobId = await submitJob({
      type: form.type,
      payload: form.payload,
      priority: Number(form.priority),
      maxAttempts: Number(form.maxAttempts),
      scheduledAt: form.scheduledAt || undefined,
    }).unwrap();

    dispatch(
      pushActivity({
        title: "Job submitted",
        detail: `Job ${jobId} (${form.type}) was queued.`,
      }),
    );

    navigate(`/jobs/${jobId}`);
  };

  return (
    <section className="card stack-lg">
      <div>
        <p className="eyebrow">Job Service</p>
        <h3>Submit a Tenant Job</h3>
        <p className="muted">Payload is forwarded as JSON string to downstream execution handlers.</p>
      </div>

      <form className="form-grid" onSubmit={handleSubmit}>
        <label>
          Job Type
          <input
            required
            value={form.type}
            onChange={(event) => setForm((prev) => ({ ...prev, type: event.target.value }))}
          />
        </label>

        <label>
          Payload JSON
          <textarea
            rows={8}
            required
            value={form.payload}
            onChange={(event) => setForm((prev) => ({ ...prev, payload: event.target.value }))}
          />
        </label>

        <div className="two-col-grid">
          <label>
            Priority
            <input
              type="number"
              value={form.priority}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, priority: Number(event.target.value) }))
              }
            />
          </label>

          <label>
            Max Attempts
            <input
              type="number"
              min={1}
              value={form.maxAttempts}
              onChange={(event) =>
                setForm((prev) => ({ ...prev, maxAttempts: Number(event.target.value) }))
              }
            />
          </label>
        </div>

        <label>
          Scheduled At (optional)
          <input
            type="datetime-local"
            value={form.scheduledAt}
            onChange={(event) =>
              setForm((prev) => ({ ...prev, scheduledAt: event.target.value }))
            }
          />
        </label>

        <button className="btn primary" type="submit" disabled={submitResult.isLoading}>
          {submitResult.isLoading ? "Submitting..." : "Submit Job"}
        </button>
      </form>

      {errorText && <p className="error-text">{errorText}</p>}
    </section>
  );
};
