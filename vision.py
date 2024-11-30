import os
from flask import Flask, request, jsonify
import base64
import torch
from PIL import Image
import io
import warnings

warnings.filterwarnings("ignore", category=FutureWarning)
app = Flask(__name__)
model = torch.hub.load('ultralytics/yolov5', 'yolov5s')

@app.route('/detect', methods=['POST'])
def detect():
    data = request.get_json()
    if not data or 'image_base64' not in data or 'camera_id' not in data:
        return jsonify({'error': 'Invalid Data'}), 400
    image_base64 = data['image_base64']
    camera_id = data['camera_id']
    try:
        image_data = base64.b64decode(image_base64)
        image = Image.open(io.BytesIO(image_data)).convert('RGB')
        results = model(image)
        objects_detected = []
        for *box, conf, cls in results.xyxy[0]:
            class_name = results.names[int(cls)]
            confidence = float(conf)
            coordinates = [float(coord) for coord in box]
            objects_detected.append({
                'class_name': class_name,
                'confidence': confidence,
                'coordinates': coordinates
            })
        return jsonify({
            'camera_id': camera_id,
            'detected_objects': objects_detected
        })
    except Exception as e:
        return jsonify({'error': 'Error processing image:'}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001)