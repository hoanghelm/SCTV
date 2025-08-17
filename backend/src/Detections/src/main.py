#!/usr/bin/env python3
"""
AI Detection Service for SCTV Backend
Monitors video streams from the Streaming API and performs person detection
"""

import asyncio
import os
import sys
from pathlib import Path
import logging
from dotenv import load_dotenv

# Add the src directory to Python path
sys.path.insert(0, str(Path(__file__).parent))

from detection.person_detector import StreamProcessor

# Load environment variables
load_dotenv()

def setup_logging():
    """Setup logging configuration"""
    log_level = os.getenv('LOG_LEVEL', 'INFO').upper()
    
    logging.basicConfig(
        level=getattr(logging, log_level),
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[
            logging.StreamHandler(sys.stdout),
            logging.FileHandler('ai_detection.log')
        ]
    )

async def main():
    """Main entry point"""
    setup_logging()
    logger = logging.getLogger(__name__)
    
    logger.info("=" * 60)
    logger.info("Starting AI Detection Service for SCTV")
    logger.info("=" * 60)
    
    # Configuration from environment variables
    api_base_url = os.getenv('API_BASE_URL', 'https://localhost:44322')
    logger.info(f"Streaming API URL: {api_base_url}")
    
    # Create and start stream processor
    processor = StreamProcessor(api_base_url=api_base_url)
    
    try:
        await processor.start()
    except KeyboardInterrupt:
        logger.info("Received shutdown signal (Ctrl+C)")
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
        raise
    finally:
        logger.info("AI Detection Service stopped")

if __name__ == "__main__":
    asyncio.run(main())