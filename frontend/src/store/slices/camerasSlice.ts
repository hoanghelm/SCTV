import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import { Camera } from "../../types";
import { cameraService } from "../../services/cameraService";

interface CamerasState {
  cameras: Camera[];
  selectedCameras: string[];
  loading: boolean;
  error: string | null;
}

const initialState: CamerasState = {
  cameras: [],
  selectedCameras: [],
  loading: false,
  error: null,
};

export const loadCameras = createAsyncThunk("cameras/loadCameras", async () => {
  const cameras = await cameraService.getCameras();
  return cameras;
});

export const createTestStream = createAsyncThunk(
  "cameras/createTestStream",
  async (name: string) => {
    await cameraService.createTestStream(name);
    const cameras = await cameraService.getCameras();
    return cameras;
  },
);

const camerasSlice = createSlice({
  name: "cameras",
  initialState,
  reducers: {
    setSelectedCameras: (state, action: PayloadAction<string[]>) => {
      state.selectedCameras = action.payload;
    },
    addSelectedCamera: (state, action: PayloadAction<string>) => {
      if (!state.selectedCameras.includes(action.payload)) {
        state.selectedCameras.push(action.payload);
      }
    },
    removeSelectedCamera: (state, action: PayloadAction<string>) => {
      state.selectedCameras = state.selectedCameras.filter(
        (id) => id !== action.payload,
      );
    },
    updateCameraStatus: (
      state,
      action: PayloadAction<{ cameraId: string; status: string }>,
    ) => {
      const camera = state.cameras.find(
        (c) => c.id === action.payload.cameraId,
      );
      if (camera) {
        camera.status = action.payload.status as any;
      }
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(loadCameras.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadCameras.fulfilled, (state, action) => {
        state.loading = false;
        state.cameras = action.payload;
        state.error = null;
      })
      .addCase(loadCameras.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || "Failed to load cameras";
      })
      .addCase(createTestStream.fulfilled, (state, action) => {
        state.cameras = action.payload;
      })
      .addCase(createTestStream.rejected, (state, action) => {
        state.error = action.error.message || "Failed to create test stream";
      });
  },
});

export const {
  setSelectedCameras,
  addSelectedCamera,
  removeSelectedCamera,
  updateCameraStatus,
  clearError,
} = camerasSlice.actions;

export default camerasSlice.reducer;
