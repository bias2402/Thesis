using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TreeHandler : MonoBehaviour {
    [SerializeField] private TreeObject tree = null;
    [Space]
    [Tooltip("Toggle this to log the tree execution to the console. This will spam the console A LOT and should be toggled off in any release!")]
    [SerializeField] private bool debugExecution = false;

    [Header("Leaf Actions")]
    private List<Node> nodes = new List<Node>();
    [SerializeField] private List<LeafMethod> leafMethods = null;
    private List<Node> leaves = new List<Node>();

    //This method runs when the editor compiles or a variable in a component of this type is changed through the inspector
    private void OnValidate() {
        if (tree != null) {                                                                                 //If the tree ScriptableObject is added to the object
            if (leafMethods.Count != tree.leafCount) {                                                          //Check that the list is the correct size
                leafMethods.Clear();
                foreach (Node n in tree.nodes) {                                                                     //Go through all nodes and find the leaves
                    if (n.GetNodeType() == NodeTypes.Leaf) {
                        leafMethods.Add(new LeafMethod(n.GetNodeName()));                                                   //Create a new LeafMethod for each leaf node and pass the node's name to it
                    }
                }
            }
        } else {
            leafMethods.Clear();
        }
    }

    public void InitTree() {
        foreach (Node n in tree.nodes) {                                                                    //Foreach node in the tree object, create a new node for execution
            nodes.Add(new Node(n.GetNodeName(), n.GetNodeType()));
        }
        foreach (Node n in nodes) {                                                                         //Foreach execution node, set debug mode and pass methods to the leaves
            n.SetDebugMode(debugExecution);
            if (n.GetNodeType() == NodeTypes.Leaf) {
                n.SetLeafMethod(FindLeafMethod(n));
                leaves.Add(n);
            }
        }
        for (int i = 0; i < nodes.Count; i++) {                                                             //Compare execution nodes and tree object nodes to find and set parents and children
            if (tree.nodes[i].GetParent() != null) nodes[i].SetParent(nodes[tree.nodes[i].GetParent().listIndex]);

            List<Node> nodeChildren = new List<Node>(tree.nodes[i].GetChildren());
            if (nodeChildren != null && nodeChildren.Count > 0) {
                foreach (Node n in nodeChildren) {
                    nodes[i].AddChild(nodes[n.listIndex]);
                }
            }
        }
    }

    LeafMethod FindLeafMethod(Node n) {                                                                 //This method is used to find the right leaf for passing a LeafMethod object to
        foreach (LeafMethod m in leafMethods) {
            if (m.GetLeafName().Equals(n.GetNodeName())) {
                return m;
            }
        }
        return null;
    }

    public void Execute() {
        nodes[0].Execute();
    }

    public void Callback(bool result) {                                                             //This method is supposed to be called whenever a leaf completes its task in another script
        foreach (Node l in leaves) {                                                                        //Iterate the leaves and find the currently running leaf to call its callback function with result
            if (l.IsNodeRunning()) {                                                                            
                l.Callback(result);
                return;
            }
        }
        if (debugExecution) Debug.Log("No leaf is currently running");
    }

    public bool GetDebugState() {
        return debugExecution;
    }


}

[Serializable]
public class LeafMethod {
    [SerializeField] [ReadOnly] private string leafName = "";
    [SerializeField] private UnityEvent leafMethod = null;

    public LeafMethod(string name) {
        leafName = name;
    }

    public void Execute() => leafMethod.Invoke();                                                       //Invoke the connected event. An event is connected through the inspector

    public string GetLeafName() {
        return leafName;
    }
}

public class ReadOnlyAttribute : PropertyAttribute {}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight(property, label, true);                                  //Get the height of the object and its children (ture parameter adds children)
    }

    //This method stops the GUI from drawing, then draw the property fields using the attribute, and then it allows the GUI to draw again making the rest as it should be
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        GUI.enabled = false;                                                                        //Stop drawing the GUI
        EditorGUI.PropertyField(position, property, label, true);                                   //Write the property field including children
        GUI.enabled = true;                                                                         //Start drawing the GUI again
    }
}