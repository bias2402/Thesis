using System.Collections.Generic;
using UnityEngine;
using MachineLearning;

public class Tester : MonoBehaviour {
    private CNN cnn = new CNN();
    [SerializeField] private CNNSaver cnnSaver = null;
    string test = "";
    Configuration.CNNConfig cnnConfig = null;

    void Start() {
        float[,] filter1 = new float[3, 3] {
                { 0, 0, 1 },
                { 0, 1, 0 },
                { 1, 0, 0 }
            };
        cnn.AddNewFilter(filter1);

        float[,] filter2 = new float[3, 3] {
                { 1, 0, 1 },
                { 0, 1, 0 },
                { 1, 0, 1 }
            };
        cnn.AddNewFilter(filter2);

        float[,] filter3 = new float[3, 3] {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 1 }
            };
        cnn.AddNewFilter(filter3);

        Configuration.ANNConfig annConfig = new Configuration.ANNConfig(AITypes.ANN, "Test - Internal ANN", 0, 0, 4,
            ActivationFunctionHandler.ActivationFunction.None, ActivationFunctionHandler.ActivationFunction.Sigmoid, 1, 0.05);
        cnnConfig = new Configuration.CNNConfig(AITypes.CNN, "Test");
        cnnConfig.AddLayer();                                                           //Padding layer
        cnnConfig.AddLayer(ActivationFunctionHandler.ActivationFunction.Sigmoid, 1);    //Convolutional layer
        cnnConfig.AddLayer(ActivationFunctionHandler.ActivationFunction.ReLU, 1);       //Convolutioanl layer
        cnnConfig.AddLayer(ActivationFunctionHandler.ActivationFunction.Sigmoid, 2, 2); //Max-pooling layer
        cnnConfig.AddLayer(annConfig);                                                  //Fully-connected layer
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            cnnSaver.serializedCNN = MLSerializer.SerializeCNN(cnn);
            Debug.Log("Saved");
        }

        if (Input.GetKeyDown(KeyCode.L)) {
            cnn = MLSerializer.DeserializeCNN(cnnSaver.serializedCNN);
            Debug.Log("Loaded");
        }

        if (Input.GetKeyDown(KeyCode.C)) {
            Debug.Log(cnn.GetANN().GetNeuronCount());
        }

        if (Input.GetKeyDown(KeyCode.Return)) {
            for (int i = 0; i < 100; i++) {
                float[,] map = new float[7, 7] {
                    { 0, 0, 0, 1, 0, 1, 0 },
                    { 0, 0, 1, 0, 0, 1, 0 },
                    { 0, 1, 0, 1, 0, 0, 0 },
                    { 1, 0, 0, 1, 0, 0, 0 },
                    { 0, 0, 1, 0, 1, 1, 1 },
                    { 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 1, 0, 1, 0, 0, 0 }
                };
                List<double> desiredOutputs = new List<double>() { 0, 0.25, -0.1, 0.75 };
                cnn.Train(map, desiredOutputs);
            }
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            float[,] map = new float[7, 7] {
                    { 0, 0, 0, 1, 0, 1, 0 },
                    { 0, 0, 1, 0, 0, 1, 0 },
                    { 0, 1, 0, 1, 0, 0, 0 },
                    { 1, 0, 0, 1, 0, 0, 0 },
                    { 0, 0, 1, 0, 1, 1, 1 },
                    { 0, 0, 0, 0, 0, 0, 0 },
                    { 0, 1, 0, 1, 0, 0, 0 }
                };
            List<double> desiredOutputs = new List<double>() { 0, 0.25, -0.1, 0.75 };
            cnn.Train(map, desiredOutputs, cnnConfig);
        }

        if (Input.GetKeyDown(KeyCode.T)) {
            MLDebugger.EnableDebugging(cnn, 3);

            float[,] map = new float[7, 7] {
                { 0, 0, 0, 1, 0, 1, 0 },
                { 0, 0, 1, 0, 0, 1, 0 },
                { 0, 1, 0, 1, 0, 0, 0 },
                { 1, 0, 0, 1, 0, 0, 0 },
                { 0, 0, 1, 0, 1, 1, 1 },
                { 0, 0, 0, 0, 0, 0, 0 },
                { 0, 1, 0, 1, 0, 0, 0 }
            };
            List<double> desiredOutputs = new List<double>() { 0, 0.25, -0.1, 0.75 };
            List<double> outputs = cnn.Train(map, desiredOutputs);

            test += MLDebugger.GetOutputAndReset();
            test += MLSerializer.SerializeCNN(cnn);
            cnnSaver.serializedCNN = test;
            test = "";
        }
    }
}