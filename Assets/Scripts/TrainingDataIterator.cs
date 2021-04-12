using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "TrainingDataIterator", menuName = "ScriptableObjects/TrainingDataIterator", order = 4)]
public class TrainingDataIterator : ScriptableObject {
    [SerializeField] private PlayStyles playStyles = PlayStyles.Done;
    [SerializeField] private string path = "Assets/";
    [SerializeField] private int index = 0;
    [SerializeField] private TextAsset[] dataFiles = null;

    public bool GetNextFile(out TextAsset file) {
        if (index >= dataFiles.Length) {
            file = null;
            return false;
        }
        file = dataFiles[index];
        index++;
        return true;
    }

    public string GetPath() { return path; }
}