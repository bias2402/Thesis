﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType { LavaBlock, Spawn, Goal, Platform, Treasure };
public class BlockData : MonoBehaviour {
    [HideInInspector] public int lavaSpreadChance;

    public List<BlockData> neighboorBlocks = new List<BlockData>();
    public List<Vector3> neighboorDirection = new List<Vector3>();
    public BlockType blockType;
    public readonly Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
    public bool isCasting { get; private set; } = true;

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

    public void Cast() {
        Physics.Raycast(transform.position + Vector3.up * 5, Vector3.down, out RaycastHit hit, 8);
        if (hit.collider.CompareTag("ExploPlate")) isCasting = false;
    }
}