using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "ANNSaver", menuName = "ScriptableObjects/ANNSaver", order = 3)]
public class ANNSaver : ScriptableObject {
    [TextArea(5, 58)] public string serializedANN = "";
}