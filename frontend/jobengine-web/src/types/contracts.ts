export type JobStatus =
  | "Pending"
  | "Queued"
  | "Running"
  | "Completed"
  | "Failed"
  | "DeadLetter";

export interface RegisterTenantRequest {
  tenantName: string;
  slug: string;
  adminEmail: string;
  adminPassword: string;
}

export interface RegisterTenantResponse {
  tenantId: string;
  slug: string;
  accessToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantSlug: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  tenantId: string;
}

export interface CreateApiKeyRequest {
  name?: string;
}

export interface CreateApiKeyResponse {
  id: string;
  keyPrefix: string;
  rawKey: string;
  name?: string;
}

export interface TenantResponse {
  id: string;
  slug: string;
  name: string;
  adminEmail: string;
}

export interface SubmitJobRequest {
  type: string;
  payload: string;
  priority: number;
  maxAttempts: number;
  scheduledAt?: string;
}

export interface JobDto {
  id: string;
  tenantId: string;
  type: string;
  status: JobStatus;
  attempt: number;
  maxAttempts: number;
  priority: number;
  error?: string;
  result?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

export interface ExecuteRequest {
  jobType: string;
  payload: string;
}

export interface ExecuteResponse {
  output?: string;
  result?: string;
  error?: string;
  durationMs?: number;
}

export interface ApiError {
  title?: string;
  detail?: string;
  message?: string;
  status?: number;
}
