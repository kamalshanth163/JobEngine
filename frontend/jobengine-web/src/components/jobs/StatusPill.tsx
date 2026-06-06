import clsx from "clsx";
import type { JobStatus } from "../../types/contracts";

interface StatusPillProps {
  status: JobStatus;
}

export const StatusPill = ({ status }: StatusPillProps) => {
  return <span className={clsx("status-pill", `status-${status.toLowerCase()}`)}>{status}</span>;
};
