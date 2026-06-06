import { configureStore } from "@reduxjs/toolkit";
import { setupListeners } from "@reduxjs/toolkit/query";
import { api } from "../services/api";
import authReducer from "../features/auth/authSlice";
import uiReducer from "../features/ui/uiSlice";
import activityReducer from "../features/activity/activitySlice";

export const store = configureStore({
  reducer: {
    auth: authReducer,
    ui: uiReducer,
    activity: activityReducer,
    [api.reducerPath]: api.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});

setupListeners(store.dispatch);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
