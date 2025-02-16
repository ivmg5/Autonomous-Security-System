# **Autonomous Security System**
> *A simulation project integrating Unity, Python, and YOLOv5 to coordinate a multi-agent security solution.*

## **Introduction**
Autonomous Security System is a comprehensive simulation that replicates a coordinated security solution for large open areas. By integrating Unity for a realistic 3D environment, Python for server-side logic and agent communication, and YOLOv5 for real-time object detection, this project demonstrates how multiple agents—such as surveillance cameras, a drone, a security guard, and a simulated intruder—can work together to rapidly detect and respond to potential threats.

## **Project Description**
- **Main Functionality:**  
  - Simulate a multi-agent security system where static cameras continuously monitor an area and send image data to a YOLOv5 detection server.
  - Dispatch a drone to verify alerts by streaming live footage and gathering additional data.
  - Enable a security guard to review the information and make final decisions on whether to escalate or dismiss alerts.
  
- **Technologies Used:**  
  - **Unity:** For creating and visualizing the simulation environment.
  - **Python:** To manage server-side operations and inter-agent communications.
  - **YOLOv5:** For high-performance, real-time object detection.
  - **Additional Libraries:** Flask, Agentpy, Owlready2, Plotly, and Matplotlib for simulation server functionality and data visualization.

- **Challenges Faced:**  
  - Integrating multiple technologies and ensuring smooth communication between diverse agents.
  - Achieving real-time performance with accurate object detection using YOLOv5.
  - Managing resource constraints, such as drone battery life and processing load, within the simulation.

- **Future Improvements:**  
  - Enhancing detection accuracy and response times.
  - Expanding the simulation with more complex scenarios and additional agent types.
  - Incorporating advanced machine learning models for improved threat assessment.

## **Table of Contents**
1. [Introduction](#introduction)
2. [Project Description](#project-description)
3. [Installation](#installation)
4. [Usage](#usage)
5. [Additional Documentation](#additional-documentation)
6. [License](#license)
7. [Status Badges](#status-badges)

## **Installation**

### **1. Prerequisites**
Before starting, ensure your system meets the following requirements:

#### **Software Required:**
- **Unity:** Version 2020.3 or higher is recommended.
- **Python:** Version 3.8 or higher.
- **Pip:** Python package manager.
- **Git**

#### **Hardware Recommended:**
- **Processor:** Modern CPU with support for AI operations.
- **GPU:** CUDA-compatible GPU (recommended for accelerating YOLOv5).
- **RAM:** Minimum 8 GB.

---

### **2. Setting Up YOLOv5**
1. Open a terminal and clone the YOLOv5 repository:
   ```bash
   git clone https://github.com/ultralytics/yolov5
   ```
2. Navigate to the cloned directory:
   ```bash
   cd yolov5
   ```
3. Install the necessary dependencies:
   ```bash
   pip install -r requirements.txt
   ```
4. Verify YOLOv5 is functioning correctly by running:
   ```bash
   python detect.py --source 0
   ```
This command should activate your webcam and display detection results.

### **3. Configuring the Python Servers**

#### **a. Simulation Server (main.py):**
1. Install the required libraries:
   ```bash
   pip install flask agentpy owlready2 plotly matplotlib
   ```
2. Place the main.py file in your project directory.
3. Start the simulation server:
   ```bash
   python main.py
   ```
The server will be available at http://localhost:5002.

#### **b. YOLOv5 Detection Server (vision.py):**
1. Place the vision.py file in the main project directory.
2. Start the YOLOv5 detection server:
   ```bash
   python vision.py
   ```
The detection server will run at http://localhost:5001.

### **4. Setting Up Unity**

#### **a. Importing the Unity Package**
1. Open Unity Hub and either create a new 3D project or open an existing one.
2. Navigate to Assets > Import Package > Custom Package....
3. Select the provided Unity Package file and click Import.
4. Confirm that all items are selected and complete the import process.

#### **b. Verifying the Imported Scene**
1. Open the scene file located at Assets/Scenes/SimulationScene.unity.
2. Ensure the following GameObjects are properly configured:
   - Cameras (Camera1, Camera2, Camera3, Camera4):
     	- Each camera should have the CameraController.cs script attached.
	- Set the serverUrl to http://localhost:5001/detect.
     	- Assign a unique cameraId to each camera (e.g., “Camera1”, “Camera2”).
   - Drone (Drone1):
   	- The drone should have the DroneController.cs script attached.
	- Configure properties such as landingStationPosition, takeOffHeight, and patrolPoints.
   - Security Guard (Security1):
     	- Ensure the SecurityGuardController.cs script is attached.
   - Robber (Robber1):
	- A GameObject representing the simulated intruder.
3. Verify all configurations in the Inspector window.
4. Ensure both the simulation and YOLOv5 servers are running before proceeding to play the scene.

### **5. Running the Full System**
1. Start the YOLOv5 Detection Server:
   ```bash
   python vision.py
   ```
2. Start the Simulation Server:
   ```bash
   python main.py
   ```
3. Launch the Unity Simulation:
   - Open Unity and press the Play button in the Editor.
4. Monitor the Workflow:
   - Surveillance cameras capture images and forward detections to the YOLOv5 server.
   - The simulation server logs all interactions and coordinates agent responses.
   - The drone is activated to verify alerts and relay live footage.
   - The security guard evaluates the information and makes final decisions.

### **6. Visualizing Results**
- Logs: Check the Unity console and the Python server logs for real-time updates.
- Final Report:
  - The simulation server (main.py) generates a performance report saved as utility_graph.html.
  - Open this HTML file in a web browser to view metrics such as battery levels, distance traveled, and response times.

### **7. Troubleshooting**
- Connection Issues:
  - Ensure that both Python servers (main.py and vision.py) are running before starting Unity.
  - Verify that ports 5001 and 5002 are not occupied by other processes.
- Unity Errors:
  - Check the Unity Console for errors related to missing scripts or misconfigurations.
- YOLOv5 Detection Issues:
  - Confirm that the YOLOv5 model is correctly set up and that input images meet the expected specifications.

## **Usage**
1. Start the Servers: Begin by launching both the YOLOv5 detection server and the simulation server.
2. Run the Unity Simulation: Open Unity and press Play to start the simulation.
3. Observe the Process:
   - Cameras capture images and send data to the detection server.
   - The drone is deployed to verify alerts and provide live footage.
   - The security guard reviews the information and makes the final decision on each alert.
4. Review Performance Metrics: Analyze the generated utility_graph.html report for insights into system performance.

## **Additional Documentation**

For more detailed setup instructions, troubleshooting tips, and technical specifications, please refer to the complete documentation provided within the repository.

## **License**

This project is licensed under the MIT License.

## **Status Badges**
[![Build Status](https://img.shields.io/badge/status-active-brightgreen)](#) [![Code Coverage](https://img.shields.io/badge/coverage-80%25-yellowgreen)](#)

# Link to download the complete Unity project

https://drive.google.com/file/d/1rR61e3CQ-W9RmvYFS4vwpq7gyrva65qH/view?usp=sharing
