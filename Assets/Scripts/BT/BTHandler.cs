using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BTHandler : TreeHandler {
    [SerializeField] private AgentController agentController = null;
    [SerializeField] private int turnToGoalCounterLimit = 3;
    private int walkingCounter = 0;
    private int requiredRotation = 0;

    public void CheckForLava() {
        int rotation = agentController.GetRotationInt();
        BlockData currentPositionBlock = agentController.GetCurrentBlockData();

        BlockType forwardBlock;
        if (currentPositionBlock.neighboorBlocks.Count == 4) forwardBlock = currentPositionBlock.neighboorBlocks[rotation].blockType;
        else {
            if (currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[rotation])) {
                int index = currentPositionBlock.neighboorDirection.FindIndex(x => x == currentPositionBlock.directions[rotation]);
                try { forwardBlock = currentPositionBlock.neighboorBlocks[index].blockType; } catch (System.ArgumentOutOfRangeException) {
                    forwardBlock = BlockType.None;
                    Debug.Log("Bad index. Baaaad!: " + index);
                }
            } else forwardBlock = BlockType.None;
        }

        if (forwardBlock == BlockType.LavaBlock || forwardBlock == BlockType.None) Callback(true);
        else Callback(false);
    }

    public void ResetCounter() {
        walkingCounter = 0;
        Callback(true);
    }

    public void WalkBackwards() {
        agentController.SetNextMove("s");
        Callback(true);
    }

    public void FindNecessaryRotationToPointToGoal() {
        BlockData currentBlock = agentController.GetCurrentBlockData();
        int rotation = agentController.GetRotationInt();
        if (currentBlock.neighboorDirection.Contains(currentBlock.directions[1])) {
            if (rotation != 1) {
                if (rotation == 0) requiredRotation = 1;
                else requiredRotation = 1 - rotation;
            }
        } else {
            if (rotation != 2) {
                if (rotation == 3) requiredRotation = -1;
                else requiredRotation = 1 + rotation;
            }
        }
        Callback(true);
    }

    public void IsBlockTowardsGoalBlocked() {
        int rotation = agentController.GetRotationInt();
        BlockData currentPositionBlock = agentController.GetCurrentBlockData();

        BlockType goalDirectionBlock;
        if (currentPositionBlock.neighboorBlocks.Count == 4) goalDirectionBlock = currentPositionBlock.neighboorBlocks[1].blockType;
        else {
            if (currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[1])) {
                int index = currentPositionBlock.neighboorDirection.FindIndex(x => x == currentPositionBlock.directions[1]);
                try { goalDirectionBlock = currentPositionBlock.neighboorBlocks[index].blockType; } catch (System.ArgumentOutOfRangeException) {
                    goalDirectionBlock = BlockType.None;
                    Debug.Log("Bad index. Baaaad!: " + index);
                }
            } else {
                int index = currentPositionBlock.neighboorDirection.FindIndex(x => x == currentPositionBlock.directions[2]);
                try { goalDirectionBlock = currentPositionBlock.neighboorBlocks[index].blockType; } catch (System.ArgumentOutOfRangeException) {
                    goalDirectionBlock = BlockType.None;
                    Debug.Log("Bad index. Baaaad!: " + index);
                }
            }
        }

        if (goalDirectionBlock == BlockType.LavaBlock || goalDirectionBlock == BlockType.None) Callback(false);
        else Callback(true);
    }

    public void Turn() {
        if (requiredRotation < 0) {
            requiredRotation++;
            agentController.SetNextMove("d");
        } else {
            requiredRotation--;
            agentController.SetNextMove("a");
        }

        if (requiredRotation == 0) Callback(true);
    }

    public void CheckForAnotherRandomDirection() {
        int rotation = agentController.GetRotationInt();
        BlockData currentPositionBlock = agentController.GetCurrentBlockData();

        BlockType otherDirectionBlock = BlockType.None;
        for (int i = 0; i < currentPositionBlock.directions.Length; i++) {
            if (currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[1]) && i == 1) continue;
            else if (i == 2) continue;

            if (currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[i])) {
                int index = currentPositionBlock.neighboorDirection.FindIndex(x => x == currentPositionBlock.directions[i]);
                if (currentPositionBlock.neighboorBlocks[index].blockType == BlockType.LavaBlock) continue;
                else {
                    otherDirectionBlock = currentPositionBlock.neighboorBlocks[index].blockType;
                    if (currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[1])) {
                        if (i == 0) requiredRotation = 1;
                        else requiredRotation = 1 - i;
                    }
                    break;
                }
            }
        }

        if (otherDirectionBlock == BlockType.LavaBlock || otherDirectionBlock == BlockType.None) Callback(false);
        else Callback(true);
    }

    public void WalkForward() {
        agentController.SetNextMove("w");
        Callback(true);
    }

    public void IncrementWalkCounter() {
        walkingCounter++;
        Callback(true);
    }

    public void CheckCounter() {
        if (walkingCounter == turnToGoalCounterLimit) {
            if (Random.Range(0, 7) == 0) {
                Callback(true);
            } else Callback(false);
        } else Callback(false);
    }

    public void IsNotAlignedWithGoal() {
        if (agentController.GetRotationInt() == 1) Callback(false);
        else Callback(true);
    }
}