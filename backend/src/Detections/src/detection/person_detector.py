import cv2
import numpy as np
from ultralytics import YOLO
import asyncio
from typing import List, Dict, Tuple, Optional, Union
import json
import base64
from datetime import datetime
import logging
from pathlib import Path
import aiohttp
# from aiokafka import AIOKafkaProducer  # Commented out as requested
import os
from collections import deque
import time

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class PersonDetector:
    """AI-powered person detection service using YOLO for background monitoring
    
    This service runs continuously in the background for all active streams,
    providing person detection and sending notifications to Kafka (currently disabled).
    Runs independently of browser-based real-time detection for comprehensive monitoring.
    """
    
    def __init__(
        self, 
        model_path: str = "yolov8n.pt",
        confidence_threshold: float = 0.5,
        nms_threshold: float = 0.4,
        process_every_n_frames: int = 15
    ):
        self.model = YOLO(model_path)
        self.confidence_threshold = confidence_threshold
        self.nms_threshold = nms_threshold
        self.process_every_n_frames = process_every_n_frames
        # self.kafka_producer: Optional[AIOKafkaProducer] = None  # Commented out
        self.api_base_url = os.getenv('API_BASE_URL', 'http://localhost:5004')
        self.frame_buffer = {}  # Store recent frames for each camera
        self.detection_cooldown = {}  # Prevent spam detections
        self.detection_history = {}  # Track detection consistency
        
    # async def initialize_kafka(self, bootstrap_servers: str = None):
    #     """Initialize Kafka producer for sending detection events"""
    #     if bootstrap_servers is None:
    #         bootstrap_servers = os.getenv('KAFKA_BOOTSTRAP_SERVERS', 'localhost:9092')
    #         
    #     self.kafka_producer = AIOKafkaProducer(
    #         bootstrap_servers=bootstrap_servers,
    #         value_serializer=lambda m: json.dumps(m).encode('utf-8'),
    #         compression_type='gzip'
    #     )
    #     await self.kafka_producer.start()
    #     logger.info(f"Kafka producer initialized: {bootstrap_servers}")
    
    async def close(self):
        """Cleanup resources"""
        # if self.kafka_producer:
        #     await self.kafka_producer.stop()
        logger.info("PersonDetector closed")
    
    def detect_persons(self, frame: np.ndarray) -> List[Dict]:
        """Detect persons in a single frame"""
        try:
            # Run YOLO detection
            results = self.model(frame, classes=[0], conf=self.confidence_threshold)  # class 0 is person
            
            detections = []
            for r in results:
                boxes = r.boxes
                if boxes is not None:
                    for box in boxes:
                        # Get coordinates
                        x1, y1, x2, y2 = box.xyxy[0].tolist()
                        confidence = box.conf[0].item()
                        
                        # Calculate center point
                        center_x = (x1 + x2) / 2
                        center_y = (y1 + y2) / 2
                        
                        # Calculate area for size filtering
                        width = x2 - x1
                        height = y2 - y1
                        area = width * height
                        
                        # Filter by size (ignore very small detections)
                        if area > 1000:  # Minimum area threshold
                            detections.append({
                                'bbox': [float(x1), float(y1), float(x2), float(y2)],
                                'confidence': float(confidence),
                                'center': [float(center_x), float(center_y)],
                                'area': float(area),
                                'timestamp': datetime.utcnow().isoformat()
                            })
            
            return detections
        except Exception as e:
            logger.error(f"Error in person detection: {e}")
            return []
    
    def should_send_detection(self, camera_id: str, detections: List[Dict]) -> bool:
        """Check if we should send this detection event with improved smoothing logic"""
        if not detections:
            # Clear history if no detections
            if camera_id in self.detection_history:
                self.detection_history[camera_id].clear()
            return False
            
        current_time = time.time()
        
        # Initialize history for new camera
        if camera_id not in self.detection_history:
            self.detection_history[camera_id] = deque(maxlen=5)
        
        # Add current detection count to history
        self.detection_history[camera_id].append(len(detections))
        
        # Only send if we have consistent detections over multiple frames
        if len(self.detection_history[camera_id]) < 3:
            return False
        
        # Check if detections are consistent (at least 2 out of last 3 frames had detections)
        recent_detections = list(self.detection_history[camera_id])[-3:]
        detection_frames = sum(1 for count in recent_detections if count > 0)
        
        if detection_frames < 2:
            return False
        
        # Apply cooldown - increased to 10 seconds for smoother experience
        last_detection_time = self.detection_cooldown.get(camera_id, 0)
        if current_time - last_detection_time > 10.0:
            self.detection_cooldown[camera_id] = current_time
            return True
            
        return False
    
    def draw_detections(self, frame: np.ndarray, detections: List[Dict]) -> np.ndarray:
        """Draw bounding boxes on frame for visualization"""
        annotated_frame = frame.copy()
        
        for detection in detections:
            bbox = detection['bbox']
            confidence = detection['confidence']
            
            # Draw bounding box
            cv2.rectangle(
                annotated_frame,
                (int(bbox[0]), int(bbox[1])),
                (int(bbox[2]), int(bbox[3])),
                (0, 255, 0),
                2
            )
            
            # Draw confidence label
            label = f"Person: {confidence:.2f}"
            cv2.putText(
                annotated_frame,
                label,
                (int(bbox[0]), int(bbox[1]) - 10),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.5,
                (0, 255, 0),
                2
            )
        
        return annotated_frame
    
    async def send_detection_event(
        self,
        camera_id: str,
        camera_name: str,
        detections: List[Dict],
        frame: np.ndarray,
        send_frame: bool = True
    ):
        """Log detection event (Kafka functionality commented out)"""
        try:
            event = {
                'camera_id': camera_id,
                'camera_name': camera_name,
                'detections': detections,
                'detection_count': len(detections),
                'timestamp': datetime.utcnow().isoformat(),
                'event_type': 'person_detection'
            }
            
            if send_frame and len(detections) > 0:
                # Annotate frame with detections
                annotated_frame = self.draw_detections(frame, detections)
                
                # Compress frame
                encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), 70]
                _, buffer = cv2.imencode('.jpg', annotated_frame, encode_param)
                frame_base64 = base64.b64encode(buffer).decode('utf-8')
                event['frame'] = frame_base64
            
            # Log the detection event instead of sending to Kafka
            logger.info(f"DETECTION EVENT - Camera: {camera_id} ({camera_name}), Persons: {len(detections)}")
            for i, detection in enumerate(detections):
                logger.info(f"  Person {i+1}: Confidence={detection['confidence']:.2f}, BBox={detection['bbox']}")
            
            # TODO: Uncomment below to send to Kafka when ready
            # await self.kafka_producer.send('person-detection', event)
            # logger.info(f"Sent detection event for camera {camera_id}: {len(detections)} persons")
            
        except Exception as e:
            logger.error(f"Error processing detection event: {e}")
    
    async def process_stream(
        self,
        camera_id: str,
        camera_name: str,
        stream_source: Union[str, int]
    ):
        """Process video stream for person detection"""
        logger.info(f"Starting detection for camera {camera_id}: {stream_source}")
        
        cap = None
        frame_count = 0
        fps_counter = deque(maxlen=30)
        
        try:
            # Open video source
            cap = cv2.VideoCapture(stream_source)
            
            # Set buffer size to reduce latency
            cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)
            
            # Try to set FPS if it's an RTSP stream
            if isinstance(stream_source, str) and stream_source.startswith('rtsp://'):
                cap.set(cv2.CAP_PROP_FPS, 30)
            
            while True:
                start_time = time.time()
                
                # Read frame
                ret, frame = cap.read()
                
                if not ret:
                    logger.warning(f"Failed to read frame from camera {camera_id}")
                    await asyncio.sleep(1)
                    
                    # Try to reconnect
                    cap.release()
                    cap = cv2.VideoCapture(stream_source)
                    continue
                
                frame_count += 1
                
                # Process frame for detection
                if frame_count % self.process_every_n_frames == 0:
                    detections = self.detect_persons(frame)
                    
                    # Send detection event if persons detected and cooldown passed
                    if self.should_send_detection(camera_id, detections):
                        await self.send_detection_event(
                            camera_id,
                            camera_name,
                            detections,
                            frame,
                            send_frame=True
                        )
                
                # Calculate FPS
                process_time = time.time() - start_time
                fps_counter.append(1.0 / process_time if process_time > 0 else 0)
                
                if frame_count % 30 == 0:
                    avg_fps = sum(fps_counter) / len(fps_counter)
                    logger.debug(f"Camera {camera_id} - FPS: {avg_fps:.2f}")
                
                # Control frame rate - slower processing for smoother detection
                await asyncio.sleep(max(0, (1.0/20) - process_time))
                
        except Exception as e:
            logger.error(f"Error processing stream for camera {camera_id}: {e}")
        finally:
            if cap:
                cap.release()
            logger.info(f"Stopped detection for camera {camera_id}")


# Stream Processor for managing multiple camera streams
class StreamProcessor:
    """Manages multiple camera detection streams"""
    
    def __init__(self, api_base_url: str = None):
        self.api_base_url = api_base_url or os.getenv('API_BASE_URL', 'http://localhost:5004')
        self.active_streams: Dict[str, asyncio.Task] = {}
        self.detector = PersonDetector()
        self.running = False
    
    async def start(self):
        """Start the stream processor"""
        logger.info("Starting stream processor...")
        self.running = True
        
        # Initialize Kafka (commented out)
        # await self.detector.initialize_kafka()
        
        # Start monitoring cameras
        monitor_task = asyncio.create_task(self.monitor_cameras())
        
        # Wait for shutdown
        try:
            await monitor_task
        except asyncio.CancelledError:
            pass
        finally:
            await self.stop()
    
    async def stop(self):
        """Stop all processing"""
        logger.info("Stopping stream processor...")
        self.running = False
        
        # Cancel all active streams
        for camera_id, task in self.active_streams.items():
            task.cancel()
            try:
                await task
            except asyncio.CancelledError:
                pass
        
        self.active_streams.clear()
        await self.detector.close()
    
    async def monitor_cameras(self):
        """Periodically check for cameras to process"""
        while self.running:
            try:
                # Fetch active cameras from API
                cameras = await self.fetch_active_cameras()
                
                # Start processing new cameras
                for camera in cameras:
                    camera_id = camera['id']
                    if camera_id not in self.active_streams:
                        task = asyncio.create_task(
                            self.detector.process_stream(
                                camera_id,
                                camera['name'],
                                camera['streamUrl']
                            )
                        )
                        self.active_streams[camera_id] = task
                        logger.info(f"Started processing camera: {camera['name']} ({camera_id})")
                
                # Stop processing removed cameras
                active_camera_ids = {c['id'] for c in cameras}
                for camera_id in list(self.active_streams.keys()):
                    if camera_id not in active_camera_ids:
                        await self.stop_camera(camera_id)
                
            except Exception as e:
                logger.error(f"Error in camera monitoring: {e}")
            
            # Check every 30 seconds
            await asyncio.sleep(30)
    
    async def fetch_active_cameras(self) -> List[Dict]:
        """Fetch list of active cameras from API"""
        try:
            # Create SSL context that allows self-signed certificates for localhost
            import ssl
            ssl_context = ssl.create_default_context()
            if 'localhost' in self.api_base_url or '127.0.0.1' in self.api_base_url:
                ssl_context.check_hostname = False
                ssl_context.verify_mode = ssl.CERT_NONE
            
            connector = aiohttp.TCPConnector(ssl=ssl_context)
            async with aiohttp.ClientSession(connector=connector) as session:
                async with session.get(f"{self.api_base_url}/api/v1/stream/cameras/active") as resp:
                    if resp.status == 200:
                        data = await resp.json()
                        # Handle different response structures
                        if isinstance(data, list):
                            return data
                        elif isinstance(data, dict):
                            # Try different nested structures
                            if 'result' in data:
                                result = data['result']
                                if isinstance(result, list):
                                    return result
                                elif isinstance(result, dict) and 'items' in result:
                                    return result['items']
                            elif 'items' in data:
                                return data['items']
                            elif 'data' in data:
                                return data['data'] if isinstance(data['data'], list) else []
                        return []
                    else:
                        logger.error(f"Failed to fetch cameras: {resp.status}")
                        return []
        except Exception as e:
            logger.error(f"Error fetching cameras: {e}")
            return []
    
    async def stop_camera(self, camera_id: str):
        """Stop processing a specific camera"""
        if camera_id in self.active_streams:
            task = self.active_streams[camera_id]
            task.cancel()
            try:
                await task
            except asyncio.CancelledError:
                pass
            del self.active_streams[camera_id]
            logger.info(f"Stopped processing camera: {camera_id}")


# Main entry point
async def main():
    """Main entry point for the detection service"""
    # Setup logging
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )
    
    # Create and start processor
    processor = StreamProcessor()
    
    try:
        await processor.start()
    except KeyboardInterrupt:
        logger.info("Received shutdown signal")
    except Exception as e:
        logger.error(f"Error in main: {e}")


if __name__ == "__main__":
    asyncio.run(main())