using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tree", menuName = "ScriptableObjects/TreeObject", order = 1)]
public class TreeObject : ScriptableObject {
    public List<Node> nodes = new List<Node>();
    public List<NodeConnection> nodeConnections = new List<NodeConnection>();
    public int leafCount = 0;
}