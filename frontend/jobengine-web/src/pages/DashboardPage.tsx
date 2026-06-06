import { Link } from "react-router-dom";
import { useListJobsQuery } from "../services/api";

export const DashboardPage = () => {
  const { data: jobs = [], isLoading } = useListJobsQuery(undefined, {
    pollingInterval: 8000,
    refetchOnFocus: true,
  });

  const total = jobs.length;
  const completed = jobs.filter((job) => job.status === "Completed").length;
  const running = jobs.filter((job) => job.status === "Running").length;
  const failed = jobs.filter((job) => job.status === "Failed" || job.status === "DeadLetter").length;

  return (
    <section className="stack-lg">
      <div className="hero card gradient-card">
        <p className="eyebrow">Tenant telemetry</p>
        <h3>Operational pulse for your background workloads</h3>
        <p>
          Track throughput, inspect failures early, and keep retries under control with
          realtime polling against the JobEngine APIs.
        </p>
        <div className="hero-actions">
          <Link to="/jobs/new" className="btn primary">
            Submit New Job
          </Link>
          <Link to="/jobs" className="btn secondary">
            Inspect Queue
          </Link>
        </div>
      </div>

      <div className="kpi-grid">
        <article className="kpi card">
          <span>Total Jobs</span>
          <strong>{isLoading ? "..." : total}</strong>
        </article>
        <article className="kpi card">
          <span>Running</span>
          <strong>{isLoading ? "..." : running}</strong>
        </article>
        <article className="kpi card">
          <span>Completed</span>
          <strong>{isLoading ? "..." : completed}</strong>
        </article>
        <article className="kpi card">
          <span>Failed / DeadLetter</span>
          <strong>{isLoading ? "..." : failed}</strong>
        </article>
      </div>

      <section className="card">
        <div className="row-spread">
          <h3>Queue Snapshot</h3>
          <Link to="/jobs" className="text-link">
            Open full list
          </Link>
        </div>

        <div className="table-shell">
          <table>
            <thead>
              <tr>
                <th>Job Id</th>
                <th>Type</th>
                <th>Status</th>
                <th>Attempt</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {jobs.slice(0, 6).map((job) => (
                <tr key={job.id}>
                  <td>{job.id.slice(0, 8)}</td>
                  <td>{job.type}</td>
                  <td>{job.status}</td>
                  <td>
                    {job.attempt}/{job.maxAttempts}
                  </td>
                  <td>{new Date(job.createdAt).toLocaleString()}</td>
                </tr>
              ))}
              {!isLoading && jobs.length === 0 && (
                <tr>
                  <td colSpan={5} className="muted">
                    No jobs found for this tenant yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </section>
  );
};
