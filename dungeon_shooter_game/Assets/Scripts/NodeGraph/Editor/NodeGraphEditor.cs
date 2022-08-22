using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class NodeGraphEditor : EditorWindow
{
    RoomNodeTypeListSO roomNodeTypeList;
    static RoomNodeGraphSO currentRoomNodeGraph;
    GUIStyle nodeStyle;

    const float nodeWidth = 160f;
    const float nodeHeight = 75f;
    const int nodePadding = 25;
    const int nodeBorder = 12;

    [MenuItem("Node Graph Editor", menuItem="Window/Dungeon Editor/Node Graph Editor")]
    public static void WindowInit()
    {
        GetWindow(typeof(NodeGraphEditor), false, "Node Graph Editor");
    }

    void OnEnable()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        nodeStyle.normal.textColor = Color.white;
        nodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        nodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
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
            ProcessEvents(Event.current);

            DrawRoomNodes();
        }

        if (GUI.changed)
        {
            Repaint();
        }

    }

    void ProcessEvents(Event currentevent)
    {
        switch (currentevent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentevent);
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
    }

    void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.ShowAsContext();
    }

    void CreateRoomNode(object mousePositionObject)
    {
        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }
    
    void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        roomNode.Initialise(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

        // add room nooe to room node graph scriptable object  asset database
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);
        AssetDatabase.SaveAssets();
    }

    void DrawRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.Draw(nodeStyle);
        }

        GUI.changed = true;
    }

}
