using UnityEngine;

public class GUIDraggableObject {
	public Vector2 pos = Vector2.zero;
	public Vector2 dragStart = Vector2.zero;
	public bool isDragging { get; internal set; } = false;
	private bool isDraggingAll = false;
	private Vector2 draggingAllStart = Vector2.zero;

	public GUIDraggableObject() {}																						//Default constructor for executable nodes

	public GUIDraggableObject(Vector2 position) => pos = position;                                                      //Constructor for creating editor window nodes

	public void Drag(Rect draggingRect) {
		if (Event.current.type == EventType.MouseUp) {                                                                      //If mouse (left) is up
			isDragging = false;                                                                                                 //Not dragging
		} else if (Event.current.type == EventType.MouseDown && draggingRect.Contains(Event.current.mousePosition)) {       //If mouse (left) is down and inside object
			isDragging = true;                                                                                                  //Is dragging
			dragStart = Event.current.mousePosition - pos;                                                                      //Set dragStart to current mouse position minus object position
			Event.current.Use();                                                                                                //Trigger the event
		}

		if (isDragging && !isDraggingAll) {                                                                                 //If only this object is being dragged
			Vector2 newPos = Event.current.mousePosition - dragStart;                                                           //Update position to current mouse position minus start position
			pos = newPos;
		}
	}

	public void SetDraggingAll(bool dragAll) {
		isDraggingAll = dragAll;																							//Set to the value of dragAll
		if (dragAll) draggingAllStart = pos;																				//If dragAll is true, set current position as start position
	}

	public void DraggingAllSetPosition(Vector2 newPos) {
		pos = draggingAllStart - newPos;																					//Set position to start position minus the recieved delta movement for mouse
	}
}