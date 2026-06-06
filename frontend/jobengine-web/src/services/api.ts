import { createApi, fetchBaseQuery } from "@reduxjs/toolkit/query/react";
import type {
  CreateApiKeyRequest,
  CreateApiKeyResponse,
  ExecuteRequest,
  ExecuteResponse,
  JobDto,
  LoginRequest,
  LoginResponse,
  RegisterTenantRequest,
  RegisterTenantResponse,
  SubmitJobRequest,
  TenantResponse,
} from "../types/contracts";
import type { RootState } from "../app/store";

const configuredApiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").trim();
const apiBaseUrl = configuredApiBaseUrl || "http://localhost:8080";

export const api = createApi({
  reducerPath: "api",
  baseQuery: fetchBaseQuery({
    baseUrl: apiBaseUrl,
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.accessToken;
      if (token) {
        headers.set("Authorization", `Bearer ${token}`);
      }

      headers.set("Content-Type", "application/json");
      return headers;
    },
  }),
  tagTypes: ["Jobs", "Tenant"],
  endpoints: (builder) => ({
    registerTenant: builder.mutation<RegisterTenantResponse, RegisterTenantRequest>({
      query: (body) => ({
        url: "/api/v1/auth/register",
        method: "POST",
        body,
      }),
    }),
    login: builder.mutation<LoginResponse, LoginRequest>({
      query: (body) => ({
        url: "/api/v1/auth/login",
        method: "POST",
        body,
      }),
    }),
    getTenant: builder.query<TenantResponse, string>({
      query: (tenantId) => `/api/v1/auth/tenants/${tenantId}`,
      providesTags: ["Tenant"],
    }),
    createApiKey: builder.mutation<
      CreateApiKeyResponse,
      { tenantId: string; payload: CreateApiKeyRequest }
    >({
      query: ({ tenantId, payload }) => ({
        url: `/api/v1/auth/tenants/${tenantId}/keys`,
        method: "POST",
        body: payload,
      }),
      invalidatesTags: ["Tenant"],
    }),
    listJobs: builder.query<JobDto[], void>({
      query: () => "/api/v1/jobs",
      providesTags: (result) =>
        result
          ? [
              ...result.map((job) => ({ type: "Jobs" as const, id: job.id })),
              { type: "Jobs" as const, id: "LIST" },
            ]
          : [{ type: "Jobs", id: "LIST" }],
    }),
    getJob: builder.query<JobDto, string>({
      query: (jobId) => `/api/v1/jobs/${jobId}`,
      providesTags: (_, __, jobId) => [{ type: "Jobs", id: jobId }],
    }),
    submitJob: builder.mutation<string, SubmitJobRequest>({
      query: (body) => ({
        url: "/api/v1/jobs",
        method: "POST",
        body,
      }),
      invalidatesTags: [{ type: "Jobs", id: "LIST" }],
    }),
    executeJobType: builder.mutation<ExecuteResponse, ExecuteRequest>({
      query: (body) => ({
        url: "/api/v1/execute",
        method: "POST",
        body,
      }),
    }),
  }),
});

export const {
  useRegisterTenantMutation,
  useLoginMutation,
  useGetTenantQuery,
  useCreateApiKeyMutation,
  useListJobsQuery,
  useGetJobQuery,
  useSubmitJobMutation,
  useExecuteJobTypeMutation,
} = api;
