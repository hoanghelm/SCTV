import { configureStore } from "@reduxjs/toolkit";
import camerasReducer from "./slices/camerasSlice";
import streamingReducer from "./slices/streamingSlice";
import dashboardReducer from "./slices/dashboardSlice";

export const store = configureStore({
  reducer: {
    cameras: camerasReducer,
    streaming: streamingReducer,
    dashboard: dashboardReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ["streaming/setSignalRConnection"],
        ignoredPaths: ["streaming.signalRConnection"],
      },
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
