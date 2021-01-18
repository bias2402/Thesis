using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Layer {
    [SerializeField] private List<Neuron> neurons = new List<Neuron>();

    //Get & Set methods
    #region
    public List<Neuron> GetNeurons() { return neurons; }
    #endregion

    public Layer(int numberOfNeuronsForLayer, Layer prevLayer) {
        for (int i = 0; i < numberOfNeuronsForLayer; i++) {
            if (prevLayer != null) neurons.Add(new Neuron(prevLayer.GetNeurons().Count));                       //Create a hidden or output layer
            else neurons.Add(new Neuron());                                                                     //Create an input layer
        }
    }

    public void PassDataToNeuron(int neuronIndex, double data) {
        neurons[neuronIndex].SetInputValueForNeuron(data);
    }

    public void SetActivationFunctionForLayersNeurons(ActivationFunctions activationFunction) {
        for (int i = 0; i < neurons.Count; i++) {
            neurons[i].SetActivationFunction(activationFunction);
        }
    }
}