using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ANNData", order = 1)]
public class SOANNData : ScriptableObject {
    public double currentAgentDirection = 0;                           
    public List<double> visibleBlocks = new List<double>();         
    public List<double> visibleBlocksDistance = new List<double>();
    public List<double> forward = new List<double>();
    public List<double> backwards = new List<double>();
    public List<double> left = new List<double>();
    public List<double> right = new List<double>();
    public List<double> interact = new List<double>();

    public void AddData(double currentAgentDirection, ref List<double> visibleBlocks, ref List<double> visibleBlocksDistance, ref List<double> forward,
                        ref List<double> backwards, ref List<double> left, ref List<double> right, ref List<double> interact) {
        this.currentAgentDirection = currentAgentDirection;
        this.visibleBlocks = visibleBlocks;
        this.visibleBlocksDistance = visibleBlocksDistance;
        this.forward = forward;
        this.backwards = backwards;
        this.left = left;
        this.right = right;
        this.interact = interact;
    }

    public void ClearData() {
        currentAgentDirection = 0;
        visibleBlocks.Clear();
        visibleBlocksDistance.Clear();
    }

    public List<List<double>> GetInputs() {
        List<List<double>> inputs = new List<List<double>>();

        List<double> directions = new List<double>();
        for (int i = 0; i < visibleBlocks.Count; i++) {
            directions.Add(currentAgentDirection);
        }

        inputs.Add(directions);
        inputs.Add(visibleBlocks);
        inputs.Add(visibleBlocksDistance);

        return inputs;
    }

    public List<List<double>> GetDesiredOutputs() {
        List<List<double>> desiredOutputs = new List<List<double>>();

        desiredOutputs.Add(forward);
        desiredOutputs.Add(backwards);
        desiredOutputs.Add(left);
        desiredOutputs.Add(right);
        desiredOutputs.Add(interact);

        return desiredOutputs;
    }

    //1 = front, -1 = back, -0.5 = left, 0.5 = right
    public double GetAgentsCurrentRotation(Transform agent) {
        switch (agent.eulerAngles.y) {
            case 0: return -0.5;
            case 90: return 1;
            case 180: return 0.5;
            case 270: return -1;
            default: return 0;
        }
    }

    //0 = spawn, 1 = ground, 2 = lava, 3 = goal
    public double GetBlockTypeValue(BlockData blockData) {
        if (blockData == null) return -1;
        switch (blockData.blockType) {
            case BlockData.BlockType.Goal: return 3;
            case BlockData.BlockType.LavaBlock: return 2;
            case BlockData.BlockType.Platform: return 1;
            case BlockData.BlockType.Spawn: return 0;
            default: return -1;
        }
    }
}