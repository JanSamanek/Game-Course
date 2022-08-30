using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;

public class NodeGraphEditor : EditorWindow
{
    RoomNodeTypeListSO roomNodeTypeList;
    static RoomNodeGraphSO currentRoomNodeGraph;
    RoomNodeSO currentRoomNode = null;

    void ProcessEvents(Event currentEvent)
    {
        if(currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
        {
        currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }

        if(currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        }
        else
        {
            currentRoomNode.ProcessEvents(currentEvent);
        }
    }

    GUIStyle nodeSelectedStyle;
    GUIStyle nodeStyle;
    
    const float nodeWidth = 160f;
    const float nodeHeight = 75f;
    const int nodePadding = 15;
    const int nodeBorder = 12;

    const float connectingLineArrowSize = 6f;
    const float connectingLineWidth = 3f;

    [MenuItem("Node Graph Editor", menuItem="Window/Dungeon Editor/Node Graph Editor")]
    public static void WindowInit()
    {
        GetWindow(typeof(NodeGraphEditor), false, "Node Graph Editor");
    }

    void OnEnable()
    {
        Selection.selectionChanged += InspectorSelectionChanged;
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        nodeStyle.normal.textColor = Color.white;
        nodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        nodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        nodeSelectedStyle = new GUIStyle();
        nodeSelectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
        nodeSelectedStyle.normal.textColor = Color.white;
        nodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        nodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= InspectorSelectionChanged;
    }

    // Open the room node graph editor window if a room node graph scriptable object is double clicked in inspector
    [OnOpenAsset(0)]    // Needs the UnityEditor.Callbacks namespace
    static bool OnDoubleClickAsset(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

        if(roomNodeGraph != null)
        {
            WindowInit();

            currentRoomNodeGraph = roomNodeGraph;
            return true;
        }
        return false;
    }

    void OnGUI()
    {
        if (currentRoomNodeGraph != null)
        {
            DrawDraggedLine();

            ProcessEvents(Event.current);

            DrawRoomNodeConnections();

            DrawRoomNodes();
        }

        if (GUI.changed)
        {
            Repaint();
        }

    }

    void DrawDraggedLine()
    {
        if(currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition,
                currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, Color.white, null, connectingLineWidth);
        }
    }
    RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
    {
        for( int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
            {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }

        return null;
    }

    void ProcessRoomNodeGraphEvents( Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                    break;

            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;

            default:
                break;
        }
    }

    void ProcessMouseDownEvent(Event currentevent)
    {
        if (currentevent.button == 1)
        {
            ShowContextMenu(currentevent.mousePosition);
        }
        else if(currentevent.button == 0)
        {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }

    void ProcessMouseDragEvent(Event currentEvent)
    {
        if(currentEvent.button == 1)
        {
            ProcessRightMouseDragEvent(currentEvent);
        }
    }

    void ProcessRightMouseDragEvent(Event currentEvent)
    {
        if(currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(currentEvent.delta);
            GUI.changed = true;
        }
    }

    void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePosition += delta;
    }

    void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
        menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);

        menu.ShowAsContext();
    }

    void SelectAllRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.isSelected = true;
        }
        GUI.changed = true;
    }

    void CreateRoomNode(object mousePositionObject)
    {
        if(currentRoomNodeGraph.roomNodeList.Count == 0)
        {
            CreateRoomNode(new Vector2(200f, 200F), roomNodeTypeList.list.Find(x => x.isEntrance));
        }
        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }
    
    void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        roomNode.Initialise(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

        // add room node to room node graph scriptable object  asset database
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);
        AssetDatabase.SaveAssets();

        currentRoomNodeGraph.OnValidate();
    }

    void DeleteSelectedRoomNodeLinks()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if(roomNode.isSelected && roomNode.childrenRoomNodeIDList.Count > 0)
            {
                for(int i = roomNode.childrenRoomNodeIDList.Count - 1; i >= 0; i--)
                {
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.childrenRoomNodeIDList[i]);

                    if(childRoomNode != null && childRoomNode.isSelected)
                    {
                        roomNode.RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);

                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }
    }

    void DeleteSelectedRoomNodes()
    {
        Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();

        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if(roomNode.isSelected && !roomNode.roomNodeType.isEntrance)
            {
                roomNodeDeletionQueue.Enqueue(roomNode);

                foreach(string childRoomNodeID in roomNode.childrenRoomNodeIDList)
                {
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(childRoomNodeID);

                    if (childRoomNode != null)
                    {
                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }

                foreach (string parentRoomNodeID in roomNode.parentRoomNodeIDList)
                {
                    RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNode(parentRoomNodeID);

                    if (parentRoomNode != null)
                    {
                        parentRoomNode.RemoveChildRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }

        while (roomNodeDeletionQueue.Count > 0)
        {
            RoomNodeSO roomNodeToDelete = roomNodeDeletionQueue.Dequeue();

            currentRoomNodeGraph.roomNodeDictionary.Remove(roomNodeToDelete.id);
            currentRoomNodeGraph.roomNodeList.Remove(roomNodeToDelete);
            DestroyImmediate(roomNodeToDelete, true);
            AssetDatabase.SaveAssets();

        }

    }

    void ProcessMouseUpEvent(Event currentEvent)
    {
        if(currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);

            if(roomNode != null)
            {
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildNodeIDToRoomNode(roomNode.id))
                {
                    roomNode.AddParentNodeIDToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }

            ClearLineDrag();
        }
    }

    void ClearLineDrag()
    {
        currentRoomNodeGraph.linePosition = Vector2.zero;
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        GUI.changed = true;
    }

    void ClearAllSelectedRoomNodes()
    {
        foreach(RoomNodeSO node in currentRoomNodeGraph.roomNodeList)
        {
            if (node.isSelected)
            {
                node.isSelected = false;
                GUI.changed = true;
            }
        }
    }

    void DrawRoomNodeConnections()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if(roomNode.childrenRoomNodeIDList.Count > 0)
            {
                foreach(string childID in roomNode.childrenRoomNodeIDList)
                {
                    DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childID]);
                    GUI.changed = true;
                }
            }
        }
    }

    void DrawConnectionLine(RoomNodeSO parentNode, RoomNodeSO childNode)
    {
        Vector2 startPos = parentNode.rect.center;
        Vector2 endPos = childNode.rect.center;

        Vector2 midPos = (startPos + endPos)/2f;

        Vector2 dir = endPos - startPos;

        Vector2 arrowTailPoint1 = midPos - new Vector2(-dir.y, dir.x).normalized * connectingLineArrowSize;
        Vector2 arrowTailPoint2 = midPos + new Vector2(-dir.y, dir.x).normalized * connectingLineArrowSize;

        Vector2 arrowHeadPoint = midPos + dir.normalized * connectingLineArrowSize;

        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.white, null, connectingLineWidth);

        Handles.DrawBezier(startPos, endPos, startPos, endPos, Color.white, null, connectingLineWidth);
        GUI.changed = true;
    }

    void DrawRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.Draw(nodeSelectedStyle);
            }
            else
            {
                roomNode.Draw(nodeStyle);
            }
        }

        GUI.changed = true;
    }

    void InspectorSelectionChanged()
    {
        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

        if(roomNodeGraph != null)
        {
            currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
        }
    }

}
