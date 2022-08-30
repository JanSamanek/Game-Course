using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomNodeGraph", menuName = "Scriptable Object/Dungeon/Room Node Graph")]
public class RoomNodeGraphSO : ScriptableObject
{
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
    [HideInInspector] public List<RoomNodeSO> roomNodeList = new List<RoomNodeSO>();
    [HideInInspector] public Dictionary<string, RoomNodeSO> roomNodeDictionary = new Dictionary<string, RoomNodeSO>();


    private void Awake()
    {   
        LoadRoomNodeDictionairy();
    }

    void LoadRoomNodeDictionairy()
    {
        roomNodeDictionary.Clear();

        foreach(RoomNodeSO roomNode in roomNodeList)
        {
            roomNodeDictionary[roomNode.id] = roomNode;
        }
    }

    public RoomNodeSO GetRoomNode(string childID)
    {
        if (roomNodeDictionary.TryGetValue(childID, out RoomNodeSO roomNode))
        {
            return roomNode;
        }
        return null;
    }

    #region Editor Code
#if UNITY_EDITOR
    [HideInInspector] public RoomNodeSO roomNodeToDrawLineFrom = null;
    [HideInInspector] public Vector2 linePosition;

    public void OnValidate()
    {
        LoadRoomNodeDictionairy();
    }

    public void SetNodeToDrawLineConnectionFrom(RoomNodeSO node, Vector2 position)
    {
        roomNodeToDrawLineFrom = node;
        linePosition = position;
    }

#endif
    #endregion
}
