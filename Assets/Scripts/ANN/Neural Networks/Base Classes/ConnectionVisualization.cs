using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
[RequireComponent(typeof(LineRenderer))]
public class ConnectionVisualization : MonoBehaviour {
    [SerializeField] private LineRenderer lineRenderer = null;
    [SerializeField] private Shader lineMat = null;
    [SerializeField] private Vector3[] positions = new Vector3[2];

    public void Init(Vector3 start, Vector3 end) {
        lineRenderer.positionCount = 2;
        positions[0] = start;
        positions[1] = end;
             
        if (lineRenderer == null) GetComponent<LineRenderer>();
        lineRenderer.material = new Material(lineMat);
        Draw(Color.yellow, 1);
    }

    public void Draw(Color c, float connectionStrength) {
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
        lineRenderer.startWidth = 1;
        lineRenderer.endWidth = 1;
        lineRenderer.SetPositions(positions);
    }
}