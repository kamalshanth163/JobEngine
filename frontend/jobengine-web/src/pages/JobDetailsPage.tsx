import { Link, useParams } from "react-router-dom";
import { StatusPill } from "../components/jobs/StatusPill";
import { useGetJobQuery } from "../services/api";

const terminalStates = new Set(["Completed", "Failed", "DeadLetter"]);

export const JobDetailsPage = () => {
  const { jobId } = useParams();

  const {
    data: job,
    isLoading,
    isError,
  } = useGetJobQuery(jobId ?? "", {
    skip: !jobId,
    pollingInterval: 5000,
  });

  if (!jobId) {
    return <p className="error-text">Missing job id.</p>;
  }

  if (isLoading) {
    return <p>Loading job details...</p>;
  }

  if (isError || !job) {
    return (
      <section className="card stack-sm">
        <h3>Job not found</h3>
        <p className="muted">This job might belong to another tenant or is unavailable.</p>
        <Link className="btn secondary" to="/jobs">
          Back to jobs
        </Link>
      </section>
    );
  }

  const isTerminal = terminalStates.has(job.status);

  return (
    <section className="stack-lg">
      <div className="card row-spread wrap">
        <div>
          <p className="eyebrow">Job lifecycle</p>
          <h3>{job.type}</h3>
          <p className="muted">{job.id}</p>
        </div>
        <StatusPill status={job.status} />
      </div>

      <div className="card stack-sm">
        <h4>Execution Summary</h4>
        <div className="meta-grid">
          <div>
            <span>Attempts</span>
            <strong>
              {job.attempt}/{job.maxAttempts}
            </strong>
          </div>
          <div>
            <span>Priority</span>
            <strong>{job.priority}</strong>
          </div>
          <div>
            <span>Created</span>
            <strong>{new Date(job.createdAt).toLocaleString()}</strong>
          </div>
          <div>
            <span>Started</span>
            <strong>{job.startedAt ? new Date(job.startedAt).toLocaleString() : "-"}</strong>
          </div>
          <div>
            <span>Completed</span>
            <strong>{job.completedAt ? new Date(job.completedAt).toLocaleString() : "-"}</strong>
          </div>
          <div>
            <span>Polling</span>
            <strong>{isTerminal ? "Stopped (terminal)" : "Every 5s"}</strong>
          </div>
          <div>
            <span>Webhook</span>
            <strong>{job.webhookUrl ? "Enabled" : "Disabled"}</strong>
          </div>
          <div>
            <span>Webhook URL</span>
            <strong>{job.webhookUrl ?? "-"}</strong>
          </div>
        </div>
      </div>

      <div className="card stack-sm">
        <h4>Result</h4>
        <pre>{job.result ?? "No result yet."}</pre>
      </div>

      <div className="card stack-sm">
        <h4>Error</h4>
        <pre>{job.error ?? "No error."}</pre>
      </div>
    </section>
  );
};
