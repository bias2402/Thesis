using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CounterVisualization : MonoBehaviour {
    [SerializeField] private Text counter = null;
    
    public void SetCounterValue(int value) {
        counter.text = value.ToString();
    }
}