# Simulation Project: Autonomous Security System

This project simulates a coordinated security system using Unity, Python, and YOLOv5 for object detection. It involves multiple agents, including surveillance cameras, a drone, a security guard, and a simulated intruder. Follow the steps below to set up and run the simulation.

---

## Prerequisites

### Software Required:
- Unity (version 2020.3 or higher recommended)
- Python 3.8 or higher
- Pip (Python package manager)
- Git
- Required Python libraries (`requirements.txt` for YOLOv5 and additional libraries)

### Hardware Recommended:
- Processor: Modern CPU with AI support
- GPU: CUDA-compatible GPU for YOLOv5 acceleration
- Minimum RAM: 8 GB

---

## Installation

### 1. Setting Up YOLOv5

1. Open a terminal and clone the YOLOv5 repository:
   ```bash
   git clone https://github.com/ultralytics/yolov5
   ```
2. Navigate to the YOLOv5 directory:
   ```bash
   cd yolov5
   ```
3. Install the required dependencies:
   ```bash
   pip install -r requirements.txt
   ```
4. Verify the installation by running:
   ```bash
   python detect.py --source 0
   ```
   This should activate your webcam and display object detection results.

---

### 2. Setting Up the Python Servers

#### a. Simulation Server (`main.py`)

1. Install the required Python libraries:
   ```bash
   pip install flask agentpy owlready2 plotly matplotlib
   ```
2. Save `main.py` in your project directory.
3. Start the server:
   ```bash
   python main.py
   ```
   The server will run at `http://localhost:5002`.

#### b. YOLOv5 Detection Server (`vision.py`)

1. Save `vision.py` in your project directory.
2. Start the YOLOv5 server:
   ```bash
   python vision.py
   ```
   The server will run at `http://localhost:5001`.

---

### 3. Setting Up Unity

#### a. Importing the Unity Package

1. Open Unity Hub and create a new 3D project or open an existing one.
2. Go to `Assets > Import Package > Custom Package...`.
3. Select the provided Unity Package file and click `Import`.
4. Ensure all items are selected and confirm the import.

#### b. Verifying the Imported Scene

1. Open the scene file located in `Assets/Scenes/SimulationScene.unity`.
2. Check the following GameObjects and their configurations:
   - **Cameras (Camera1, Camera2, Camera3, Camera4):**
     - Each camera should have the `CameraController.cs` script attached.
     - Set `serverUrl` to `http://localhost:5001/detect`.
     - Assign unique `cameraId` values (e.g., "Camera1", "Camera2").
   - **Drone (Drone1):**
     - The drone should have the `DroneController.cs` script attached.
     - Configure properties such as `landingStationPosition`, `takeOffHeight`, and `patrolPoints`.
   - **Security Guard (Security1):**
     - Attach the `SecurityGuardController.cs` script.
   - **Robber (Robber1):**
     - A GameObject representing the simulated intruder.

#### c. Running the Simulation

1. Ensure all scripts and configurations are correct.
2. Press the **Play** button in the Unity Editor to start the simulation.

---

## Running the Full System

1. **Start the YOLOv5 Detection Server:**
   ```bash
   python vision.py
   ```

2. **Start the Simulation Server:**
   ```bash
   python main.py
   ```

3. **Run the Unity Simulation:**
   - Open Unity and press the **Play** button.

4. **Monitor the Workflow:**
   - Surveillance cameras capture images and send detections to the YOLOv5 server.
   - The simulation server logs interactions and coordinates agents.
   - The drone responds to alerts, verifies threats, and interacts with the security guard for decision-making.
   - The security guard evaluates data and makes final decisions.

---

## Viewing Results

- **Logs:**
  - Check the consoles of Unity and the Python servers for real-time logs.
- **Final Report:**
  - The simulation server (`main.py`) generates a report as an HTML file (`utility_graph.html`) containing metrics such as battery levels, distance traveled, and time.
  - Open the file in a web browser to visualize the performance metrics.

---

## Troubleshooting

- **Connection Issues:**
  - Ensure both servers (`main.py` and `vision.py`) are running before starting Unity.
  - Verify that ports `5001` and `5002` are not in use by other processes.
- **Unity Errors:**
  - Check the Unity Console for errors related to scripts or configurations.
- **YOLOv5 Detection Issues:**
  - Ensure the images sent to YOLOv5 meet the model's requirements (e.g., resolution of 224x224).
  - Confirm the YOLOv5 model is correctly installed and functional.

---

## Summary of System Workflow

1. Cameras capture and send images to the YOLOv5 server.
2. YOLOv5 detects objects and sends results to the simulation server.
3. The drone responds to alerts, verifies threats, and updates the simulation server.
4. The security guard evaluates findings and makes final decisions.
5. The system generates a performance report for analysis.

By following these steps, you can successfully run the simulation and analyze the performance of your autonomous security system.