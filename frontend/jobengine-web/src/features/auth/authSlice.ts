import { createSlice, type PayloadAction } from "@reduxjs/toolkit";
import { readStoredAuth, writeStoredAuth, type StoredAuth } from "./authStorage";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  tenantId: string | null;
  tenantSlug: string | null;
  email: string | null;
  expiresAt?: string;
}

const persisted = readStoredAuth();

const initialState: AuthState = {
  accessToken: persisted?.accessToken ?? null,
  refreshToken: persisted?.refreshToken ?? null,
  tenantId: persisted?.tenantId ?? null,
  tenantSlug: persisted?.tenantSlug ?? null,
  email: persisted?.email ?? null,
  expiresAt: persisted?.expiresAt,
};

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    setCredentials: (state, action: PayloadAction<StoredAuth>) => {
      state.accessToken = action.payload.accessToken;
      state.refreshToken = action.payload.refreshToken;
      state.tenantId = action.payload.tenantId;
      state.tenantSlug = action.payload.tenantSlug;
      state.email = action.payload.email;
      state.expiresAt = action.payload.expiresAt;
      writeStoredAuth(action.payload);
    },
    logout: (state) => {
      state.accessToken = null;
      state.refreshToken = null;
      state.tenantId = null;
      state.tenantSlug = null;
      state.email = null;
      state.expiresAt = undefined;
      writeStoredAuth(null);
    },
  },
});

export const { setCredentials, logout } = authSlice.actions;
export default authSlice.reducer;
