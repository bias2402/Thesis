using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockData : MonoBehaviour {
    [HideInInspector] public int lavaSpreadChance;

    public List<BlockData> neighboorBlocks = new List<BlockData>();
    public List<Vector3> neighboorDirection = new List<Vector3>();
    public enum BlockType { LavaBlock, BladeTrap, PitTrap, SpikeTrap, MegaDash, Shield, Spawn, Goal, Platform };
    public BlockType blockType;
    public readonly Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };

    //Raycast neighboors, get their BlockData component and save information about them for easy access by players/agents
    public void FindNeighboors() {
        for (int i = 0; i < directions.Length; i++) {
            Ray ray = new Ray(transform.position, directions[i]);
            Physics.Raycast(ray, out RaycastHit hit, 1.5f);
            if (hit.collider != null) {
                neighboorBlocks.Add(hit.collider.GetComponent<BlockData>());
                neighboorDirection.Add(directions[i]);
            }
        }
    }
}