using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class DataCollector : MonoBehaviour {
    [SerializeReference] private CollectedData collectedData = new CollectedData();

    public void SetMapSize(int sizeX, int sizeZ) {
        collectedData.mapSizeX = sizeX;
        collectedData.mapSizeZ = sizeZ;
    }

    public void AddMapData(string blockType) => collectedData.recordedMap.Add(blockType);

    public void AddMoveToRecords(string input) => collectedData.recordedMoves.Add(input);

    public void CreateJSONFile(string fileName, float exploration) {
        collectedData.explorationPercentage = (int)exploration;
        string data = JsonUtility.ToJson(collectedData);
        StreamWriter sw = new StreamWriter(fileName + ".txt");
        sw.WriteLine(data);
        sw.Flush();
        sw.Close();

        collectedData.recordedMap.Clear();
        collectedData.recordedMoves.Clear();
    }
}

[Serializable]
public class CollectedData {
    public int explorationPercentage = 0;
    public int mapSizeX = 0;
    public int mapSizeZ = 0;
    public List<string> recordedMap = new List<string>();
    public List<string> recordedMoves = new List<string>();
}