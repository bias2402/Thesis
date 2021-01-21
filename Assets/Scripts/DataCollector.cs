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

    public void AddMoveToRecords(string input) => collectedData.recordedActions.Add(input);

    public void AddDelayToRecords(float delay) => collectedData.recordedActions.Add(delay.ToString()); 

    public void CreateJSONFile(string fileName, float exploration = 0, int treasures = 0) {
        collectedData.explorationPercentage = (int)exploration;
        collectedData.treasures = treasures;
        string data = JsonUtility.ToJson(collectedData);
        StreamWriter sw = new StreamWriter(fileName + ".txt");
        sw.WriteLine(data);
        sw.Flush();
        sw.Close();

        collectedData.recordedMap.Clear();
        collectedData.recordedActions.Clear();
    }
}

[Serializable]
public class CollectedData {
    public int explorationPercentage = 0;
    public int treasures = 0;
    public int mapSizeX = 0;
    public int mapSizeZ = 0;
    public List<string> recordedMap = new List<string>();
    public List<string> recordedActions = new List<string>();
}