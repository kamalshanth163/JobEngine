import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { StatusPill } from "../components/jobs/StatusPill";
import { useListJobsQuery } from "../services/api";

export const JobsPage = () => {
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [search, setSearch] = useState("");

  const { data: jobs = [], isLoading, refetch } = useListJobsQuery(undefined, {
    pollingInterval: 5000,
  });

  const filteredJobs = useMemo(() => {
    return jobs.filter((job) => {
      const matchesStatus = statusFilter === "all" || job.status === statusFilter;
      const normalizedSearch = search.trim().toLowerCase();
      const matchesSearch =
        normalizedSearch.length === 0 ||
        job.type.toLowerCase().includes(normalizedSearch) ||
        job.id.toLowerCase().includes(normalizedSearch);

      return matchesStatus && matchesSearch;
    });
  }, [jobs, search, statusFilter]);

  return (
    <section className="stack-lg">
      <div className="card row-spread wrap">
        <div>
          <p className="eyebrow">Job Service</p>
          <h3>Tenant Job Queue</h3>
        </div>

        <div className="row gap-sm">
          <button className="btn secondary" type="button" onClick={() => refetch()}>
            Refresh
          </button>
          <Link className="btn primary" to="/jobs/new">
            Submit Job
          </Link>
        </div>
      </div>

      <section className="card">
        <div className="row gap-sm wrap">
          <input
            className="inline-input"
            placeholder="Search by id or type"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <select
            className="inline-input"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
          >
            <option value="all">All statuses</option>
            <option value="Pending">Pending</option>
            <option value="Queued">Queued</option>
            <option value="Running">Running</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="DeadLetter">DeadLetter</option>
          </select>
        </div>

        <div className="table-shell">
          <table>
            <thead>
              <tr>
                <th>Job Id</th>
                <th>Type</th>
                <th>Status</th>
                <th>Attempts</th>
                <th>Priority</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {filteredJobs.map((job) => (
                <tr key={job.id}>
                  <td>{job.id}</td>
                  <td>{job.type}</td>
                  <td>
                    <StatusPill status={job.status} />
                  </td>
                  <td>
                    {job.attempt}/{job.maxAttempts}
                  </td>
                  <td>{job.priority}</td>
                  <td>{new Date(job.createdAt).toLocaleString()}</td>
                  <td>
                    <Link className="text-link" to={`/jobs/${job.id}`}>
                      Details
                    </Link>
                  </td>
                </tr>
              ))}
              {isLoading && (
                <tr>
                  <td colSpan={7}>Loading jobs...</td>
                </tr>
              )}
              {!isLoading && filteredJobs.length === 0 && (
                <tr>
                  <td colSpan={7} className="muted">
                    No jobs match your filters.
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
