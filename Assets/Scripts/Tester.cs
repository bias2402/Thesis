using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MachineLearning;

public class Tester : MonoBehaviour {
    private CNN cnn = new CNN();

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
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("Running...");
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
            Debug.Log("Padded map's size: [" + map.GetLength(0) + "," + map.GetLength(1) + "]");
            string s = "";
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

            float[,] pooledMap = cnn.Pooling(maps[0]);
            Debug.Log("Pooled map's size: [" + pooledMap.GetLength(0) + "," + pooledMap.GetLength(1) + "]");
            s = "";
            for (int i = 0; i < pooledMap.GetLength(0); i++) {
                for (int j = 0; j < pooledMap.GetLength(1); j++) {
                    s += pooledMap[i, j].ToString();
                    if (j != pooledMap.GetLength(1) - 1) s += ",";
                }
                s += "\n";
            }
            Debug.Log(s);
        }
    }
}