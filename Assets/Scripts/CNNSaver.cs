using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MachineLearning;

[Serializable]
[CreateAssetMenu(fileName = "CNNSaver", menuName = "ScriptableObjects/CNNSaver", order = 2)]
public class CNNSaver : ScriptableObject {
    public CNN cnn = null;
}