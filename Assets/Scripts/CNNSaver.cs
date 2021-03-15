using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "CNNSaver", menuName = "ScriptableObjects/CNNSaver", order = 2)]
public class CNNSaver : ScriptableObject {
    [TextArea(5, 58)] public string serializedCNN = "";
}