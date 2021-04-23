using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Node : GUIDraggableObject {
	[Header("Base Node Settings")]
	[SerializeReference] private Node parent = null;
	[SerializeReference] private List<Node> children = new List<Node>();
	public string name = "";
	[SerializeField] private NodeTypes nodeType = NodeTypes.Leaf;
	private bool isRunning = false;
	private int currentChildRunning = 0;
	private LeafMethod leafMethod = null;

	[SerializeReference] private bool isDebugging = false;

	[Header("Tree Maker Settings")]
	private TreeMaker treeMaker = null;
	public bool isRoot = false;
	public float[] size = new float[2] { 0, 0 };
	public Vector3 childConnectionPoint = Vector3.zero;
	public Vector3 parentConnectionPoint = Vector3.zero;
	public bool isSelected = false;
	public int listIndex = 0;

	//Methods for the creation of the node through the TreeMaker
	#region
	public Node() { }																					//Construction for loading nodes in the tree maker

	public Node(string name, float[] size, Vector2 position, TreeMaker treeMaker) : base(position) {
		this.name = name;
		this.size = size;
		this.treeMaker = treeMaker;
	}					//TreeMaker nodes' constructor

	public void OnGUI() {																				//Handles the drawing of the node in the editor window
		Rect drawRect = new Rect(pos.x, pos.y, size[0], size[1]);                                           //Draw a rect

		GUILayout.BeginArea(drawRect, GUI.skin.GetStyle("Box"));                                            //Begin GUILayout block
		GUILayout.Label(name, GUI.skin.GetStyle("Box"), GUILayout.ExpandWidth(true));                       //Make a label with the node's name that can expand horizontally
		GUILayout.EndArea();                                                                                //End GUILayout block

		Rect setParentButton = new Rect(drawRect.width / 2, 0, 10, 10);                                     //Make the rect for the parentButton
		Rect addChildButton = new Rect(drawRect.width / 2, drawRect.height, 10, 10);                        //Make the rect for the childButton

		childConnectionPoint = drawRect.position + addChildButton.position;                                 //Calculate the position for the children to connect to
		parentConnectionPoint = drawRect.position + setParentButton.position;                               //Calculate the position for the parent to connect to

		//This region controls the drawing of the node
		#region
		GUILayout.BeginArea(new Rect(pos.x - 5, pos.y - 5, size[0] + 10, size[1] + 10));                    //Create a new box around the node that is a bit larger
		if (!isRoot) {                                                                                      //Check if node is the root node
			//This button is for setting the parent node of the current connection aka setting this node as a child! It's confusing, I know.
			if (GUI.Button(setParentButton, "")) {                                                              //Add a new button for connecting a parent to the node
				if (treeMaker.currentConnection == null) {                                                          //Check if a connection is currently acive
					if (parent == null) {                                                                               //Check that node doesn't have a parent yet
						new NodeConnection(this, null, treeMaker);                                                          //Create a new connection, setting this node as the child
					} else {
						Debug.LogWarning("A node can only ever have one parent!");
					}
				} else {
					if (!treeMaker.currentConnection.GotChild()) {                                                      //Check if the current connection have a child node
						if (parent == null) {                                                                               //Check if the node's parent is null
							if (treeMaker.currentConnection.GetParent() != this) {                                              //Check that the current connection's parent node isn't this node
								treeMaker.currentConnection.FinishConnection(this);                                                 //Finish the connection
							} else {
								Debug.LogWarning("A node can't connect to itself!");
							}
						} else {
							Debug.LogWarning("A node can only ever have one parent!");
						}
					} else {
						Debug.LogWarning("The connection already got a parent node set!");
					}
				}
			}
		}
		if (nodeType != NodeTypes.Leaf) {
			//This button is for setting the child node of the current connection aka setting this node as a parent! It's confusing, I know.
			if (GUI.Button(addChildButton, "")) {                                                               //Add a new button for connecting a child to the node
				if ((nodeType == NodeTypes.Inverter && children.Count >= 1) || (nodeType == NodeTypes.Succeeder && children.Count >= 1)) {
					Debug.LogWarning("A decorater can only have one child!");
				} else {
					if (treeMaker.currentConnection == null) {															 //Check if a connection is currently acive
						new NodeConnection(null, this, treeMaker);                                                          //Create a new connection, setting this node as the parent
					} else {
						if (!treeMaker.currentConnection.GotParent()) {                                                 //Check if the current connections have a parent node
							if (treeMaker.currentConnection.GetChild() != this) {                                           //Check that the current connection's child node isn't this node
								treeMaker.currentConnection.FinishConnection(this);                                             //Finish the connection
							} else {
								Debug.LogWarning("A node can't connect to itself!");
							}
						} else {
							Debug.LogWarning("The connection already got a child node set! " + treeMaker.currentConnection.GetChild());
							Debug.LogWarning(children.Count);
						}
					}
				}
			}
		}

		//Based on the node type, draw a texture while the node is big to indicate the type. The rect sets the position, width, and height of the texture
		if (!isRoot) {																								//Don't draw if the node is the root, since root is also a selector
			if (size[0] > 100) {
				switch (nodeType) {
					case NodeTypes.Inverter:
						GUI.DrawTexture(new Rect(drawRect.width / 2 - 20, 25, 50, 50), treeMaker.inverter);
						break;
					case NodeTypes.Leaf:
						GUI.DrawTexture(new Rect(drawRect.width / 2 - 20, 25, 50, 50), treeMaker.leaf);
						break;
					case NodeTypes.Selector:
						GUI.DrawTexture(new Rect(drawRect.width / 2 - 20, 25, 50, 50), treeMaker.prioritySelector);
						break;
					case NodeTypes.Sequence:
						GUI.DrawTexture(new Rect(drawRect.width / 2 - 20, 25, 50, 50), treeMaker.sequence);
						break;
					case NodeTypes.Succeeder:
						GUI.DrawTexture(new Rect(drawRect.width / 2 - 20, 25, 50, 50), treeMaker.succeeder);
						break;
					default:
						GUI.DrawTexture(new Rect(drawRect.width / 2 - 20, 25, 50, 50), treeMaker.delete);
						break;
				}
			}
		} else {																									//Draw if the node is the root
			if (size[0] > 100) {
				GUI.DrawTexture(new Rect(drawRect.width / 2 - 20, 25, 50, 50), treeMaker.root);
			}
		}
		GUILayout.EndArea();
		#endregion

		if (nodeType == NodeTypes.Inverter || nodeType == NodeTypes.Succeeder) {
			if (children.Count > 1) {
				int iterations = children.Count;
				for (int i = 1; i < iterations; i++) {
					treeMaker.FindImproperConnections(this);
					if (children.Count > 1) children.RemoveAt(1);
				}
			}
		}
		if (nodeType == NodeTypes.Leaf && children.Count != 0) {
			children.Clear();
			treeMaker.FindImproperConnections(this);
		}
		Drag(drawRect);                                                                                             //Drag the rect
	}

	public bool IsConnectedToRoot() {
		if (isRoot) return true;
		else if (GetParent() != null) return GetParent().IsConnectedToRoot();
		else return false;
	}

	public void MakeRoot() => isRoot = true;

	public void SetTypeToRoot() => nodeType = NodeTypes.Selector;

	public void ChangeSize(float[] newSize) => size = newSize;

	public void RepositionForZoom(Node root) {
		float x = 25;
		float y = 55;
		Vector2 offset = Vector2.zero;
		if (!isRoot) {
			if (size[0] >= 100) {
				if (pos.x < root.pos.x) {
					offset.x -= x;
				} else {
					offset.x += x;
				}
				offset.y += y;
			} else {
				if (pos.x < root.pos.x) {
					offset.x += x;
				} else {
					offset.x -= x;
				}
				offset.y -= y;
			}
		} else offset.y = 0;
		pos += offset;
	}

	public void SetName(string name) {
		this.name = name;
		OnGUI();
	}

	public string GetNodeName() {
		return name;
	}

	public void SetParent(Node parent) => this.parent = parent;

	public void AddChild(Node child) => children.Add(child);

	public void RemoveChild(Node child) {
		int index = -1;
		for (int i = 0; i < children.Count; i++) {
			if (children[i] == child) {
				index = i;
				break;
			}
		}
		if (index == -1) return;
		children.RemoveAt(index);
	}

	public List<Node> GetChildren() {
		return children;
	}

	public Node GetParent() {
		return parent;
	}

	public NodeTypes GetNodeType() {
		return nodeType;
	}

	public void SetNodeType(NodeTypes type) => nodeType = type;

	public void ParseNodeInfoToEditPanel() => treeMaker.PassNodeInformation(name, nodeType);

	public void ParseLoadedInformation(string name, NodeTypes nodeType, float[] size, bool isRoot, TreeMaker treeMaker) {
		this.name = name;
		this.nodeType = nodeType;
		this.size = size;
		this.isRoot = isRoot;
		this.treeMaker = treeMaker;
	}
	#endregion

	//Methods for the execution of the node through the TreeHandler
	#region
	public Node(string name, NodeTypes nodeType) {														//This is the constructor for the runtime nodes for tree execution
		this.name = name;
		this.nodeType = nodeType;
	}

	public void SetDebugMode(bool isDebugging) => this.isDebugging = isDebugging;

	public void SetLeafMethod(LeafMethod leafMethod) => this.leafMethod = leafMethod;

	public bool IsNodeRunning() {
		return isRunning;
	}

	public void Execute() {
		if (isDebugging) {
			DebugMeSenpai(GetNodeName() + " is executing");
		}

		isRunning = true;
		switch (nodeType) {																					//Based on the node type, execute the step in the tree
			case NodeTypes.Leaf:
				leafMethod.Execute();                                                                               //Call the Execute method for the connected event
				break;
			case NodeTypes.Selector:
			case NodeTypes.Sequence:
				CheckIfAChildIsRunning();																			//Check for running children
				children[currentChildRunning].Execute();															//Execute child
				break;
			case NodeTypes.Inverter:
			case NodeTypes.Succeeder:
				children[currentChildRunning].Execute();															//Execute child
				break;
		}
	}

	public void Callback(bool result) {
		if (isDebugging) {
			if (GetNodeType() == NodeTypes.Leaf) {
				DebugMeSenpai(GetNodeName() + " has finished its execution with a result of " + result);
			} else {
				DebugMeSenpai(children[currentChildRunning].GetNodeName() + " has called back to its parent " + GetNodeName() + " with a result of " + result);
			}
		}
							
		isRunning = false;
		switch (nodeType) {																					//Based on type, perform a callback to the node's parent
			case NodeTypes.Leaf:
				parent.Callback(result);																			//Callback directly to parent with result
				break;
			case NodeTypes.Selector:
				currentChildRunning++;                                                                              //Increment child that is running
				CheckIfAChildIsRunning();																			//Check if a child is running
				if (result || currentChildRunning >= children.Count) {												//If the result is true or current child exceeds list count
					currentChildRunning = 0;																			
					if (parent != null) parent.Callback(result);														//If parent isn't equal null, call its callback
					else DebugMeSenpai(GetNodeName() + " is the top of the tree. Execution done for this frame.");		
				} else Execute();																					//Otherwise, call execute to execute the next child
				break;
			case NodeTypes.Sequence:		
				currentChildRunning++;                                                                              //Increment child that is running
				CheckIfAChildIsRunning();                                                                           //Check if a child is running
				if (result && currentChildRunning < children.Count) Execute();										//If result is true and the child count hasn't been reached, call execute
				else {
					currentChildRunning = 0;
					if (parent != null) parent.Callback(result);                                                    //If parent isn't equal null, call its callback
					else DebugMeSenpai(GetNodeName() + " is the top of the tree. Execution done for this frame.");
				}
				break;
			case NodeTypes.Inverter:
				parent.Callback(!result);																			//Callback with the inverted result
				break;
			case NodeTypes.Succeeder:
				parent.Callback(true);																				//Callback with true
				break;
		}
	}

	public void CheckIfAChildIsRunning() {
		if (children == null || children.Count == 0) {
			Debug.LogError(GetNodeName() + " does not have any children and it is not a leaf!");
			return;
		}

		for (int i = 0; i < children.Count; i++) {
			if (children[i].IsNodeRunning()) {
				currentChildRunning = i;
				return;
			}
		}
	}

	void DebugMeSenpai(string debugLine) {
		Debug.Log(debugLine);
	}
	#endregion
}