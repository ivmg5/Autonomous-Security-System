import logging
from flask import Flask, request, jsonify, after_this_request
import agentpy as ap
import threading
import queue
import time
from owlready2 import get_ontology, Thing, DataProperty, ObjectProperty, FunctionalProperty, onto_path
import os
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np
import plotly.express as px

onto_path.append(os.path.dirname(os.path.abspath(__file__)))

onto = get_ontology("ontology.owl")
if not onto.loaded:
    with onto:
        class Agent(Thing):
            pass
        class DroneAgent(Agent):
            pass
        class CameraAgent(Agent):
            pass
        class GuardAgent(Agent):
            pass
        class RobberAgent(Agent):
            pass
        class Message(Thing):
            pass
        class hasStatus(DataProperty, FunctionalProperty):
            domain = [Agent]
            range = [str]
        class hasPositionX(DataProperty, FunctionalProperty):
            domain = [Agent]
            range = [float]
        class hasPositionY(DataProperty, FunctionalProperty):
            domain = [Agent]
            range = [float]
        class hasPositionZ(DataProperty, FunctionalProperty):
            domain = [Agent]
            range = [float]
        class hasBatteryLevel(DataProperty, FunctionalProperty):
            domain = [DroneAgent]
            range = [float]
        class performative(DataProperty, FunctionalProperty):
            domain = [Message]
            range = [str]
        class content(DataProperty, FunctionalProperty):
            domain = [Message]
            range = [str]
        class sender(ObjectProperty, FunctionalProperty):
            domain = [Message]
            range = [Agent]
        class receiver(ObjectProperty, FunctionalProperty):
            domain = [Message]
            range = [Agent]
    onto.save()
else:
    onto.load()

logging.getLogger('werkzeug').disabled = True
logging.basicConfig(
    level=logging.INFO,
    format='%(message)s'
)
logger = logging.getLogger(__name__)
app = Flask(__name__)
message_queue = queue.Queue()

class DroneAgent(ap.Agent):
    def setup(self):
        self.battery_level = 100
        self.position = (0.0, 0.0, 0.0)
        self.status = "Idle"
        self.onto_agent = onto.DroneAgent(f"DroneAgent_{id(self)}")
        self.update_ontology()
    
    def update_ontology(self):
        self.onto_agent.hasStatus = self.status
        x, y, z = self.position
        self.onto_agent.hasPositionX = x
        self.onto_agent.hasPositionY = y
        self.onto_agent.hasPositionZ = z
        self.onto_agent.hasBatteryLevel = self.battery_level
        onto.save()

    def process_message(self, message):
        performative = message['performative']
        content = message['content']
        sender = message['sender']
        if performative == "call_for_proposal":
            bid = self.model.calculate_bid(self)
            response = {
                'sender': 'DroneAgent',
                'receiver': sender,
                'performative': 'propose',
                'content': f'Bid:{bid}'
            }
            message_queue.put(response)
        elif performative == "accept_proposal":
            self.status = "Performing Task"
            self.update_ontology()
            threading.Timer(2.0, self.task_completed).start()
        elif performative == "call_for_vote":
            vote = self.model.guard_vote()
            response = {
                'sender': 'DroneAgent',
                'receiver': sender,
                'performative': 'inform',
                'content': f'Vote:{vote}'
            }
            message_queue.put(response)
        onto_message = onto.Message(f"Message_{time.time()}")
        onto_message.performative = performative
        onto_message.content = content
        sender_agent = self.get_agent_by_name(sender)
        receiver_agent = self
        if sender_agent:
            onto_message.sender = sender_agent.onto_agent
        onto_message.receiver = receiver_agent.onto_agent
        onto.save()

    def get_agent_by_name(self, name):
        return self.model.agent_dict.get(name, None)

    def task_completed(self):
        self.status = "Task Completed"
        self.update_ontology()

class CameraAgent(ap.Agent):
    def setup(self):
        self.position = (0.0, 0.0, 0.0)
        self.status = "Monitoring"
        self.onto_agent = onto.CameraAgent(f"CameraAgent_{id(self)}")
        self.update_ontology()
    
    def update_ontology(self):
        self.onto_agent.hasStatus = self.status
        x, y, z = self.position
        self.onto_agent.hasPositionX = x
        self.onto_agent.hasPositionY = y
        self.onto_agent.hasPositionZ = z
        onto.save()
    
    def process_message(self, message):
        performative = message['performative']
        content = message['content']
        sender = message['sender']
        if performative == "inform":
            if "Thief detected" in content:
                cfp_message = {
                    'sender': 'CameraAgent',
                    'receiver': 'DroneAgent',
                    'performative': 'call_for_proposal',
                    'content': 'Search and confirm thief'
                }
                message_queue.put(cfp_message)
        onto_message = onto.Message(f"Message_{time.time()}")
        onto_message.performative = performative
        onto_message.content = content
        sender_agent = self.get_agent_by_name(sender)
        receiver_agent = self
        if sender_agent:
            onto_message.sender = sender_agent.onto_agent
        onto_message.receiver = receiver_agent.onto_agent
        onto.save()

    def get_agent_by_name(self, name):
        return self.model.agent_dict.get(name, None)

class GuardAgent(ap.Agent):
    def setup(self):
        self.status = "Idle"
        self.onto_agent = onto.GuardAgent(f"GuardAgent_{id(self)}")
        self.update_ontology()

    def update_ontology(self):
        self.onto_agent.hasStatus = self.status
        onto.save()

    def process_message(self, message):
        performative = message['performative']
        content = message['content']
        sender = message['sender']
        if performative == "call_for_vote":
            vote = self.model.guard_vote()
            response = {
                'sender': 'GuardAgent',
                'receiver': sender,
                'performative': 'inform',
                'content': f'Vote:{vote}'
            }
            message_queue.put(response)
        onto_message = onto.Message(f"Message_{time.time()}")
        onto_message.performative = performative
        onto_message.content = content
        sender_agent = self.get_agent_by_name(sender)
        receiver_agent = self
        if sender_agent:
            onto_message.sender = sender_agent.onto_agent
        onto_message.receiver = receiver_agent.onto_agent
        onto.save()

    def get_agent_by_name(self, name):
        return self.model.agent_dict.get(name, None)

class RobberAgent(ap.Agent):
    def setup(self):
        self.position = (5.0, 5.0, 0.0)
        self.status = "Active"
        self.onto_agent = onto.RobberAgent(f"RobberAgent_{id(self)}")
        self.update_ontology()
    
    def update_ontology(self):
        self.onto_agent.hasStatus = self.status
        x, y, z = self.position
        self.onto_agent.hasPositionX = x
        self.onto_agent.hasPositionY = y
        self.onto_agent.hasPositionZ = z
        onto.save()

class SimulationModel(ap.Model):
    def setup(self):
        self.drone = DroneAgent(self)
        self.camera = CameraAgent(self)
        self.guard = GuardAgent(self)
        self.robber = RobberAgent(self)
        self.agents = [self.drone, self.camera, self.guard, self.robber]
        self.agent_dict = {
            'DroneAgent': self.drone,
            'CameraAgent': self.camera,
            'GuardAgent': self.guard,
            'RobberAgent': self.robber,
        }

    def step(self):
        while not message_queue.empty():
            message = message_queue.get()
            receiver = message['receiver']
            for agent in self.agents:
                if agent.__class__.__name__ == receiver:
                    agent.process_message(message)

    def calculate_bid(self, agent):
        agent.update_ontology()
        return agent.battery_level * 0.1

    def guard_vote(self):
        import random
        vote = random.choice(['yes', 'no'])
        return vote

model = SimulationModel()
model.setup()

def run_model():
    while True:
        model.step()
        time.sleep(0.1)

model_thread = threading.Thread(target=run_model)
model_thread.daemon = True
model_thread.start()

@app.route('/agent_action', methods=['POST'])
def agent_action():
    data = request.get_json()
    if not data or 'agent_id' not in data or 'action' not in data:
        return jsonify({'error': 'Invalid Data'}), 400
    agent_id = data['agent_id']
    action = data['action']
    return jsonify({'status': 'success'}), 200

@app.route('/log_message', methods=['POST'])
def log_message():
    data = request.get_json()
    if not data or 'agent_id' not in data or 'message' not in data:
        return jsonify({'error': 'Invalid Data'}), 400
    agent_id = data['agent_id']
    message = data['message']
    log_level = data.get('log_level', 'INFO').upper()
    if log_level not in ['DEBUG', 'INFO', 'WARNING', 'ERROR', 'CRITICAL']:
        log_level = 'INFO'
    logger.info(f"{agent_id}: {message}")
    return jsonify({'status': 'success'}), 200

@app.route('/kqml_message', methods=['POST'])
def kqml_message():
    data = request.get_json()
    if not data or 'sender' not in data or 'performative' not in data:
        return jsonify({'error': 'Invalid Data'}), 400
    sender = data['sender']
    receiver = data.get('receiver', 'SimulationServer')
    performative = data['performative']
    content = data.get('content', '')
    sender_name = sender.replace("1", "Agent")
    receiver_name = receiver.replace("1", "Agent")
    message = {
        'sender': sender_name,
        'receiver': receiver_name,
        'performative': performative,
        'content': content
    }
    message_queue.put(message)
    logger.info(f"{sender} ({performative}): {content}")
    return jsonify({'status': 'success'}), 200

@app.route('/final_report', methods=['POST'])
def final_report():
    import numpy as np
    import plotly.express as px
    data = request.get_json()
    if not data or 'time' not in data or 'battery' not in data or 'distance' not in data:
        return jsonify({'error': 'Invalid Data'}), 400
    time_data = np.array(data['time'])
    battery_data = np.array(data['battery'])
    distance_data = np.array(data['distance'])
    import pandas as pd
    df = pd.DataFrame({
        "Time (s)": time_data,
        "Battery Level (%)": battery_data,
        "Distance Traveled (m)": distance_data
    })
    fig = px.scatter(
        df,
        x="Battery Level (%)",
        y="Distance Traveled (m)",
        color="Time (s)",
        color_continuous_scale="Viridis",
        title="Drone Performance: Battery vs Distance Over Time",
        labels={"Battery Level (%)": "Battery Level (%)", "Distance Traveled (m)": "Distance (m)", "Time (s)": "Time (s)"},
        range_x=[max(battery_data), min(battery_data)]
    )
    fig.write_html("utility_graph.html")
    return jsonify({'status': 'success', 'message': 'Report generated successfully.'}), 200

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5002)