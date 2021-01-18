using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class NeuronVisualization : MonoBehaviour {
    private Neuron neuron = null;
    [SerializeField] private Transform connectionPool = null;
    private Image neuronImage = null;
    private Dictionary<int, ConnectionVisualization> connections = new Dictionary<int, ConnectionVisualization>();

    void Awake() {
        neuronImage = GetComponent<Image>();
    }

    public void PrepareVisualNeuron(Neuron neuron) {
        this.neuron = neuron;
        neuron.neuronVisualUpdate += delegate { UpdateConnectionAndImage((float)this.neuron.GetOutput()); };
    }

    public void CreateNewConnection(GameObject connectionPrefab, int index, Vector3 currentLayerNeuronPosition, Vector3 nextLayerNeuronPosition) {
        ConnectionVisualization conn = Instantiate(connectionPrefab, connectionPool).GetComponent<ConnectionVisualization>();
        connections.Add(index, conn);
        if (connections.Count > 0) conn.Init(currentLayerNeuronPosition, nextLayerNeuronPosition);
    }

    public void UpdateConnectionAndImage(float strength) {
        neuronImage.color = Color.Lerp(Color.red, Color.green, ClampStrength(strength));
        Color c = Color.Lerp(Color.red, Color.green, strength);
        if (connections.Count > 0) {
            foreach (int key in connections.Keys) {
                connections[key].Draw(c, ClampStrength(strength));
            }
        }
    }

    float ClampStrength(float connectionStrength) {
        return connectionStrength < 0.1f ? 0.1f : connectionStrength > 1 ? 1 : connectionStrength;
    }

    public void ResetNeuronColor() => neuronImage.color = Color.white;

    public void NeuronWorking() => neuronImage.color = Color.yellow;
}