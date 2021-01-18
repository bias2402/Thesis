using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

[Serializable]
public class Neuron {
    public delegate void NeuronVisualUpdate();
    public NeuronVisualUpdate neuronVisualUpdate;

    [Header("Neuron Settings")]
    [SerializeField] private bool isInputNeuron = false;
    [SerializeField] private double inputValue = 0;
    [SerializeField] private double bias = 0;
    [SerializeReference] private List<double> weights = new List<double>();
    [SerializeReference] private List<double> inputs = new List<double>();
    [SerializeField] private double output = 0;
    [SerializeField] private double errorGradient = 0;
    [SerializeField] private string name = "";

    [SerializeField] private ActivationFunctions activationFunction = ActivationFunctions.Sigmoid;

    //Get & Set methods
    #region
    public void SetName(string name) => this.name = name;

    public string GetName() { return name; }

    public void SetBias(double delta) => bias += delta;

    public List<double> GetWeights() { return weights; }

    public List<double> GetInputs() { return inputs; }

    public double GetOutput() { return output; }

    public double GetErrorGradient() { return errorGradient; }

    public void SetErrorGradient(double delta) => errorGradient = delta;

    public void SetInputValueForNeuron(double inputValue) => this.inputValue = inputValue;

    public void SetActivationFunction(ActivationFunctions activationFunction) => this.activationFunction = activationFunction;

    public ActivationFunctions GetActivationFunction() { return activationFunction; }
    #endregion

    //Input neuron constructor
    public Neuron() => isInputNeuron = true;

    //Hidden or Output neuron constructor
    public Neuron(int numberOfInputs) {
        if (numberOfInputs <= 0) {
            Debug.LogError("A neuron must have a positive number of inputs!");
            return;
        }
        bias = UnityEngine.Random.Range(-1f, 1f);                                                           //Randomize the start bias
        for (int i = 0; i < numberOfInputs; i++) {
            inputs.Add(0);                                                                                      //Add an input for each number of inputs
            weights.Add(UnityEngine.Random.Range(-1f, 1f));                                                     //Add a weight for each number of inputs and randomize the start value
        }
    }

    public void CalculateOutput(Layer layer = null) {
        if (isInputNeuron) {                                                                                //If the neuron is an input neuron, set its output to input
            output = inputValue;
            return;
        }
        inputs.Clear();
        foreach (Neuron n in layer.GetNeurons()) {
            inputs.Add(n.GetOutput());
        }

        double value = 0;
        if (inputs.Count != weights.Count) Debug.LogError("Inputs and weights must be the same amount! Inputs: " + 
            inputs.Count + ", Weights: " + weights.Count + ", Neuron: " + name);                            //Ensure the lists of weights and inputs are equal in size
        for (int i = 0; i < inputs.Count; i++) {
            value += inputs[i] * weights[i];                                                                    //Calculate the output value by multiplying weights and inputs
        }
        value -= bias;                                                                                      //Subtract the bias to apply the bias of the neuron
        output = ActivationFunctionHandler.TriggerActivationFunction(activationFunction, value);            //Throw the output through the chosen activation function to calculate the final output
    }

    public void CallNeuronVisualUpdateEvent() {
        neuronVisualUpdate();
    }
}