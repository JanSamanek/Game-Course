using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector] public string id;
    [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
    [HideInInspector] public List<string> childrenRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

    #region Unity Editor Code
#if UNITY_EDITOR

    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeType = roomNodeType;
        this.roomNodeGraph = nodeGraph;

        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    public void Draw(GUIStyle nodeStyle)
    {
        GUILayout.BeginArea(rect, nodeStyle);
        EditorGUI.BeginChangeCheck();

        if(parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
        {
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
            int selection = EditorGUILayout.Popup("", selected, GetRoomNodesTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];

            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isCorridor
                && roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isBossRoom
                && roomNodeTypeList.list[selection].isBossRoom)
            {

                if (childrenRoomNodeIDList.Count > 0)
                {
                    for (int i = childrenRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childrenRoomNodeIDList[i]);

                        if (childRoomNode != null)
                        {
                            RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);

                            childRoomNode.RemoveParentRoomNodeIDFromRoomNode(id);
                        }
                    }
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(this);

        GUILayout.EndArea();
    }

    string[] GetRoomNodesTypesToDisplay()
    {
        string[] types = new string[roomNodeTypeList.list.Count];

        for(int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if(roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                types[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }

        return types;
    }

    public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcesMouseDownEvent(currentEvent);
                break;

            case EventType.MouseUp:
                ProcesMouseUpEvent(currentEvent);
                break;

            case EventType.MouseDrag:
                ProcesMouseDragEvent(currentEvent);
                break;
        }
    }

    void ProcesMouseDownEvent(Event currentEvent)
    {
        if(currentEvent.button == 0)
        {
            ProcessLeftClickDownEvent(currentEvent);
        }
        else if(currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);
        }
    }

    void ProcessLeftClickDownEvent(Event currentEvent)
    {
        Selection.activeObject = this;

        isSelected = !isSelected;
    }

    void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawLineConnectionFrom(this, currentEvent.mousePosition);
    }
    void ProcesMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent(currentEvent);
        }
    }

    void ProcessLeftClickUpEvent(Event currentEvent)
    {
        if (isLeftClickDragging)
            isLeftClickDragging = false;
    }

    void ProcesMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickDragEvent(currentEvent);
        }
    }

    void ProcessLeftClickDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;

        DragNode(currentEvent.delta);
        GUI.changed = true;
    }

    void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    public bool AddChildNodeIDToRoomNode(string childID)
    {
        if (IsChildRoomValid(childID))
        {
            childrenRoomNodeIDList.Add(childID);
            return true;
        }

        return false;
    }

    public bool IsChildRoomValid(string childID)
    {
        bool isConnectedBossNode = false;

        foreach(RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
                isConnectedBossNode = true;
        }

        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNode)
            return false;

        if(roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
            return false;

        if (childrenRoomNodeIDList.Contains(childID))
            return false;

        if (id == childID)
            return false;

        if (parentRoomNodeIDList.Contains(childID))
            return false;

        if(roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
            return false;

        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
            return false;

        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
            return false;

        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childrenRoomNodeIDList.Count > Settings.maxChildCorridors)
            return false;

        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEntrance)
            return false;

        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childrenRoomNodeIDList.Count > 0)
            return false;

        return true;
    }

    public bool AddParentNodeIDToRoomNode(string ID)
    {
        parentRoomNodeIDList.Add(ID);
        return true;
    }

    public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
    {
        if (childrenRoomNodeIDList.Contains(childID))
        {
            childrenRoomNodeIDList.Remove(childID);
            return true;
        }

        return false;
    }

    public bool RemoveParentRoomNodeIDFromRoomNode(string childID)
    {
        if (parentRoomNodeIDList.Contains(childID))
        {
            parentRoomNodeIDList.Remove(childID);
            return true;
        }

        return false;
    }

#endif
    #endregion
}

