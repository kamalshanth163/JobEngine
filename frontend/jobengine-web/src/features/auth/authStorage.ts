import type { LoginResponse } from "../../types/contracts";

const AUTH_STORAGE_KEY = "jobengine.auth";

export interface StoredAuth {
  accessToken: string;
  refreshToken: string;
  tenantId: string;
  tenantSlug: string;
  email: string;
  expiresAt?: string;
}

export const readStoredAuth = (): StoredAuth | null => {
  const raw = localStorage.getItem(AUTH_STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as StoredAuth;
  } catch {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    return null;
  }
};

export const writeStoredAuth = (
  auth: StoredAuth | null,
  loginPayload?: LoginResponse,
): void => {
  if (!auth) {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    return;
  }

  const value: StoredAuth = {
    ...auth,
    expiresAt: loginPayload?.expiresAt ?? auth.expiresAt,
  };

  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(value));
};
