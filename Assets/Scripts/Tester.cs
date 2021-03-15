using System.Collections.Generic;
using UnityEngine;
using MachineLearning;
using System.IO;

public class Tester : MonoBehaviour {
    private CNN cnn = new CNN();
    [SerializeField] private CNNSaver cnnSaver = null;
    private int saveCounter = 0;

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
            float[,] map = new float[9, 9] {
                { 0, 0, 0, 1, 0, 1, 0, 1, 0 },
                { 0, 0, 1, 0, 0, 1, 0, 0, 1 },
                { 0, 1, 0, 1, 0, 0, 0, 1, 1 },
                { 1, 0, 0, 1, 0, 0, 0, 0, 0 },
                { 0, 0, 1, 0, 1, 1, 1, 1, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                { 0, 1, 0, 1, 0, 0, 0, 1, 0 },
                { 0, 0, 1, 0, 1, 0, 1, 0, 1 },
                { 1, 1, 0, 1, 0, 1, 0, 1, 0 }
            };
            map = cnn.Padding(map);
            List<double> desiredOutputs = new List<double>() { 0, 0.25, -0.1, 0.75 };
            List<double> outputs = cnn.Train(map, desiredOutputs);
            string s = "";
            foreach (double d in outputs) {
                s += d + " ";
            }
            Debug.Log(s);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            cnn.Clear(true, true, false, false);

            Debug.Log("Running...\nStart map size is: [9,9]");
            float[,] map = new float[9, 9] {
                { 0, 0, 0, 1, 0, 1, 0, 1, 0 },
                { 0, 0, 1, 0, 0, 1, 0, 0, 1 },
                { 0, 1, 0, 1, 0, 0, 0, 1, 1 },
                { 1, 0, 0, 1, 0, 0, 0, 0, 0 },
                { 0, 0, 1, 0, 1, 1, 1, 1, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                { 0, 1, 0, 1, 0, 0, 0, 1, 0 },
                { 0, 0, 1, 0, 1, 0, 1, 0, 1 },
                { 1, 1, 0, 1, 0, 1, 0, 1, 0 }
            };
            string s = "";

            map = cnn.Padding(map);
            Debug.Log("Padded map's size: [" + map.GetLength(0) + "," + map.GetLength(1) + "]");
            for (int i = 0; i < map.GetLength(0); i++) {
                for (int j = 0; j < map.GetLength(1); j++) {
                    s += map[i, j].ToString();
                    if (j != map.GetLength(1) - 1) s += ",";
                }
                s += "\n";
            }
            Debug.Log(s);

            List<float[,]> maps = cnn.Convolution(map);
            Debug.Log("Count: " + maps.Count + ", first map's size: [" + maps[0].GetLength(0) + "," + maps[0].GetLength(1) + "]");
            s = "";
            for (int i = 0; i < maps[0].GetLength(0); i++) {
                for (int j = 0; j < maps[0].GetLength(1); j++) {
                    s += maps[0][i, j].ToString();
                    if (j != maps[0].GetLength(1) - 1) s += ",";
                }
                s += "\n";
            }
            Debug.Log(s);

            cnn.Pooling();
            Debug.Log("First pooled map's size: [" + cnn.GetPooledMaps()[0].GetLength(0) + "," + cnn.GetPooledMaps()[0].GetLength(1) + "]");
            s = "";
            for (int i = 0; i < cnn.GetPooledMaps()[0].GetLength(0); i++) {
                for (int j = 0; j < cnn.GetPooledMaps()[0].GetLength(1); j++) {
                    s += cnn.GetPooledMaps()[0][i, j].ToString();
                    if (j != cnn.GetPooledMaps()[0].GetLength(1) - 1) s += ",";
                }
                s += "\n";
            }
            Debug.Log(s);

            List<double> outputs = cnn.FullyConnected(4, cnn.GetPooledMaps(), "pooled maps");
            Debug.Log("Run. #Neurons: " + cnn.GetANN().GetNeuronCount() + ". Outputs generated: " + outputs.Count);
            s = "";
            foreach (double d in outputs) {
                s += d + " ";
            }
            Debug.Log(s);

            List<double> desiredOutputs = new List<double>() { 0, 0.25, -0.1, 0.75 };
            //outputs = cnn.Train(desiredOutputs);
            Debug.Log("Train. #Neurons: " + cnn.GetANN().GetNeuronCount() + ". Outputs generated: " + outputs.Count);
            s = "";
            foreach (double d in outputs) {
                s += d + " ";
            }
            Debug.Log(s);
        }
    }
}