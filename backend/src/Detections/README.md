# AI Detection Service for SCTV

This Python service monitors video streams from the SCTV Streaming API and performs continuous background person detection using YOLO (You Only Look Once) deep learning model.

## Dual Detection System

This system provides **two layers of person detection**:

1. **Background Detection (Python)**: Runs continuously for all active streams, sends notifications to Kafka
2. **Real-time Detection (Browser)**: JavaScript-based detection in the web viewer for immediate visual feedback

## Features

- Continuous background person detection using YOLOv8
- Automatic monitoring of active camera streams from Streaming API
- Configurable detection parameters
- Logging of detection events
- Kafka integration (currently commented out for testing)
- Automatic reconnection and error handling
- Runs independently of browser viewers

## Requirements

- Python 3.8 or higher
- CUDA-compatible GPU (optional, for better performance)
- Access to SCTV Streaming API

## Installation

### 1. Create Python Virtual Environment

```bash
# Navigate to the ai-detection directory
cd services/ai-detection

# Create virtual environment
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate
# On Linux/Mac:
source venv/bin/activate
```

### 2. Install Dependencies

```bash
pip install -r requirements.txt
```

### 3. Download YOLO Model

The service will automatically download the YOLOv8 nano model on first run. For better accuracy, you can manually download larger models:

```bash
# Optional: Download larger models for better accuracy
python -c "from ultralytics import YOLO; YOLO('yolov8s.pt')"  # Small model
python -c "from ultralytics import YOLO; YOLO('yolov8m.pt')"  # Medium model
python -c "from ultralytics import YOLO; YOLO('yolov8l.pt')"  # Large model
```

### 4. Configure Environment

```bash
# Copy environment template
cp .env.example .env

# Edit configuration
notepad .env  # On Windows
# or
nano .env     # On Linux
```

Edit the `.env` file to match your setup:

```env
API_BASE_URL=https://localhost:44322
LOG_LEVEL=INFO
CONFIDENCE_THRESHOLD=0.5
```

## Running the Service

### Development Mode

```bash
# Activate virtual environment
venv\Scripts\activate  # Windows
# source venv/bin/activate  # Linux/Mac

# Run the service
python src/main.py
```

### Production Mode

```bash
# Install as a Windows service or Linux daemon
# (Implementation depends on your deployment environment)
```

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `API_BASE_URL` | `https://localhost:44322` | SCTV Streaming API base URL |
| `MODEL_PATH` | `yolov8n.pt` | YOLO model file path |
| `CONFIDENCE_THRESHOLD` | `0.5` | Minimum confidence for detections |
| `PROCESS_EVERY_N_FRAMES` | `5` | Process every N frames (for performance) |
| `LOG_LEVEL` | `INFO` | Logging level (DEBUG, INFO, WARNING, ERROR) |

### YOLO Model Options

| Model | Size | Speed | Accuracy | Use Case |
|-------|------|-------|----------|----------|
| `yolov8n.pt` | Nano | Fastest | Good | Real-time, low resources |
| `yolov8s.pt` | Small | Fast | Better | Balanced performance |
| `yolov8m.pt` | Medium | Medium | High | Higher accuracy needed |
| `yolov8l.pt` | Large | Slow | Highest | Maximum accuracy |

## How It Works

### Background Detection Service (Python)
1. **Stream Monitoring**: Polls the Streaming API endpoint `/api/v1/stream/cameras/active` every 30 seconds
2. **Stream Processing**: For each active camera, opens the video stream using OpenCV
3. **Person Detection**: Every N frames (configurable), runs YOLO person detection
4. **Event Logging**: Detection events are logged and prepared for Kafka notifications
5. **Automatic Recovery**: If a stream fails, automatically attempts to reconnect

### Real-time Detection (Browser)
1. **User-initiated**: Activated when users click "Enable Detection" in the web viewer
2. **TensorFlow.js**: Uses lightweight COCO-SSD model for browser-based detection
3. **Visual Feedback**: Shows green bounding boxes around detected persons
4. **Instant Notifications**: Real-time alerts in the browser interface

### Working Together
- **Python service**: Runs 24/7 for comprehensive monitoring and alerting
- **Browser detection**: Provides immediate visual feedback during active viewing
- **Complementary**: Both systems can run simultaneously without interference

## API Integration

The service integrates with your SCTV Streaming API:

- **GET** `/api/v1/stream/cameras/active` - Fetches list of active cameras
- Supports camera objects with `id`, `name`, and `streamUrl` properties
- Works with RTSP streams, file paths, and test streams

## Performance Tuning

### For Better Performance:
- Use a smaller YOLO model (`yolov8n.pt`)
- Increase `PROCESS_EVERY_N_FRAMES` value
- Reduce `CONFIDENCE_THRESHOLD` if getting too many false positives
- Use GPU acceleration if available

### For Better Accuracy:
- Use a larger YOLO model (`yolov8m.pt` or `yolov8l.pt`)
- Decrease `PROCESS_EVERY_N_FRAMES` value
- Adjust `CONFIDENCE_THRESHOLD` based on your needs

## Troubleshooting

### Common Issues:

1. **"No module named 'cv2'"**
   ```bash
   pip install opencv-python
   ```

2. **"Failed to fetch cameras"**
   - Check if Streaming API is running
   - Verify `API_BASE_URL` in `.env` file
   - Check network connectivity

3. **"Failed to read frame from camera"**
   - Verify camera stream URLs are accessible
   - Check if cameras are actually streaming
   - Ensure proper network access to camera sources

4. **High CPU usage**
   - Increase `PROCESS_EVERY_N_FRAMES` value
   - Use smaller YOLO model
   - Reduce number of concurrent camera streams

### Debug Mode:

```bash
# Set debug logging
echo "LOG_LEVEL=DEBUG" >> .env

# Run with debug output
python src/main.py
```

## Future Enhancements

- Kafka integration for real-time notifications
- Web dashboard for monitoring detections
- Database storage of detection events
- Multiple object detection (not just persons)
- Face recognition capabilities
- Alert system integration

## Logs

- Application logs: `ai_detection.log`
- Detection events are logged with camera ID, timestamp, and confidence scores
- Use `LOG_LEVEL=DEBUG` for detailed troubleshooting