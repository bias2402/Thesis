using System.Collections.Generic;
using UnityEngine;
using MachineLearning;
using System.Collections;
using System.Configuration;

public class Tester : MonoBehaviour {
    private CNN cnn = new CNN();
    [SerializeField] private CNNSaver cnnSaver = null;
    string test = "";
    [SerializeField] private TextAsset cnnConfigFile = null;
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

        //Configuration.ANNConfig annConfig = new Configuration.ANNConfig(AITypes.ANN, "Test - Internal ANN", 0, 0, 4,
        //    ActivationFunctionHandler.ActivationFunction.None, ActivationFunctionHandler.ActivationFunction.Sigmoid, 1, 0.05);
        //cnnConfig = new Configuration.CNNConfig(AITypes.CNN, "Test");
        //cnnConfig.AddLayer();                                                           //Padding layer
        //cnnConfig.AddLayer(ActivationFunctionHandler.ActivationFunction.Sigmoid, 1);    //Convolutional layer
        //cnnConfig.AddLayer(ActivationFunctionHandler.ActivationFunction.ReLU, 1);       //Convolutioanl layer
        //cnnConfig.AddLayer(ActivationFunctionHandler.ActivationFunction.Sigmoid, 2, 2); //Max-pooling layer
        //cnnConfig.AddLayer(ActivationFunctionHandler.ActivationFunction.Sigmoid, 2, 2); //Max-pooling layer
        //cnnConfig.AddLayer(annConfig);                                                  //Fully-connected layer

        //System.IO.StreamWriter sw = new System.IO.StreamWriter("Assets/config-test.txt");
        //sw.Write(Configuration.Serialize(cnnConfig));
        //sw.Flush();
        //sw.Close();

        cnnConfig = Configuration.DeserializeCNN("Assets/" + cnnConfigFile.name + ".txt");

        //sw = new System.IO.StreamWriter("Assets/config-test2.txt");
        //sw.Write(Configuration.Serialize(cnnConfig));
        //sw.Flush();
        //sw.Close();
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

        if (Input.GetKeyDown(KeyCode.F)) Debug.Log(2.56.ToString());

        if (Input.GetKeyDown(KeyCode.T)) {
            StartCoroutine(ErrorTester(0));
        }
    }

    private IEnumerator ErrorTester(int counter) {
        bool gotError = false;
        List<double> outputs = null;
        try {
            counter++;
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
            outputs = cnn.Train(map, desiredOutputs, cnnConfig);

            //test += MLSerializer.SerializeCNN(cnn);
            if (counter % 100 == 0) Debug.Log(counter + " iterations");
        } catch (System.Exception e) {
            Debug.Log(e);
            Debug.Log("Infinity reached in " + counter + " iterations");
            gotError = true;
        }
        test += MLDebugger.GetOutputAndReset();
        cnnSaver.serializedCNN = test;
        test = "";

        //string output = "";
        //foreach (double d in outputs) {
        //    output += d + ", ";
        //}
        //Debug.Log(output);
        yield return new WaitForSeconds(0.1f);
        if (!gotError) StartCoroutine(ErrorTester(counter));
    }
}