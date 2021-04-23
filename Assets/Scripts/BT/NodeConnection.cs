using UnityEngine;
using System;
using UnityEditor;

[Serializable]
public class NodeConnection {
	public Rect connectionRect { get; internal set; }
	[NonSerialized] private TreeMaker treeMaker = null;
	private Node childNode = null;
	private Node parentNode = null;
	public int childIndex = 0;
	public int parentIndex = 0;
	[SerializeReference] private Vector3 childPoint = Vector3.zero;
	[SerializeReference] private Vector3 parentPoint = Vector3.zero;
	private bool isDeleting = false;

	public NodeConnection(Node child, Node parent, TreeMaker treeMaker, bool isLoadingConnection = false, bool isSavingConnection = false) {
		if (child == null && parent == null) {
			Debug.LogError("No start nor end node was given! Connection not created!");
			return;
		}
		if (child != null) {
			childNode = child;
			childPoint = child.parentConnectionPoint;
		}
		if (parent != null) {
			parentNode = parent;
			parentPoint = parent.childConnectionPoint;
		}
		if (!isLoadingConnection) treeMaker.SetCurrentConnection(this);
		if (!isSavingConnection) treeMaker.AddNodeConnection(this);
		this.treeMaker = treeMaker;
	}

	public void SetChildPoint(Vector3 point) => childPoint = point;

	public void SetParentPoint(Vector3 point) => parentPoint = point;

	public void OnGUI() {
		if (isDeleting) return;
		if (childNode == null && parentNode == null) {
			Debug.LogError("No start nor end node!");
			return;
		}
		childPoint = childNode != null ? childNode.parentConnectionPoint : treeMaker.mousePos;				//Get the position for this end of the connection based on childNode
		parentPoint = parentNode != null ? parentNode.childConnectionPoint : treeMaker.mousePos;			//Get the position for this end of the connection based on parentNode
		Handles.DrawLine(childPoint, parentPoint);															//Draw the actual line

		if (parentNode != null && childNode != null) {														//If the connection is complete, draw a button with the delete texture
			Vector2 size = new Vector2(15, 15);
			Vector2 pos = ((parentPoint + childPoint) / 2) - new Vector3(size.x / 2, size.y / 2, 0);
			Rect drawRect = new Rect(pos, size);
			connectionRect = drawRect;
			GUILayout.BeginArea(drawRect);
			Rect btnRect = new Rect(0, 0, drawRect.width, drawRect.height);
			if (GUI.Button(btnRect, treeMaker.delete)) {													//If the button is clicked, delete the connection
				DeleteConnection(false);
			}
			GUILayout.EndArea();
		}
	}

	public bool IsFullyConnected() {
		return childNode == null || parentNode == null ? false : true;
	}

	public void FinishConnection(Node n) {
		if (parentNode == null) {
			parentNode = n;
			childNode.SetParent(parentNode);
			parentNode.AddChild(childNode);
		} else {
			childNode = n;
			parentNode.AddChild(childNode);
			childNode.SetParent(parentNode);
		}
		treeMaker.SetCurrentConnection(null);
	}

	public bool GotChild() {
		return childNode == null ? false : true;
	}

	public bool GotParent() {
		return parentNode == null ? false : true;
	}

	public Node GetChild() {
		return childNode;
	}

	public Node GetParent() {
		return parentNode;
	}

	public void SetChildIndex(int index) => childIndex = index;

	public void SetParentIndex(int index) => parentIndex = index;

	public void DeleteConnection(bool isPhantom) {
		isDeleting = true;
		if (childNode != null) childNode.SetParent(null);
		if (parentNode != null) parentNode.RemoveChild(childNode);
		childNode = null;
		parentNode = null;
		if (!isPhantom) treeMaker.RemoveConnection(treeMaker.GetConnectionIndex(this));
	}
}