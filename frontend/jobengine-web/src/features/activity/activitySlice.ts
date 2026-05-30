import { createSlice, type PayloadAction } from "@reduxjs/toolkit";

export interface ActivityEvent {
  id: string;
  title: string;
  detail: string;
  createdAt: string;
}

interface ActivityState {
  events: ActivityEvent[];
}

const initialState: ActivityState = {
  events: [],
};

const activitySlice = createSlice({
  name: "activity",
  initialState,
  reducers: {
    pushActivity: (state, action: PayloadAction<Omit<ActivityEvent, "id" | "createdAt">>) => {
      state.events.unshift({
        ...action.payload,
        id: crypto.randomUUID(),
        createdAt: new Date().toISOString(),
      });

      state.events = state.events.slice(0, 40);
    },
    clearActivity: (state) => {
      state.events = [];
    },
  },
});

export const { pushActivity, clearActivity } = activitySlice.actions;
export default activitySlice.reducer;
