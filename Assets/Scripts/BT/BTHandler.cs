using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BTHandler : TreeHandler {
    [SerializeField] private AgentController agentController = null;
    [SerializeField] private int turnToGoalCounterLimit = 3;
    private int walkingCounter = 0;
    private int requiredRotation = 0;
    private int randomDirectionRotation = 0;
    private readonly bool[] checkedOtherRotations = new bool[4] { false, true, false, false };

    public void ResetCounter() {
        walkingCounter = 0;
        Callback(true);
    }

    public void WalkBackwards() {
        for (int i = 0; i < checkedOtherRotations.Length; i++) {
            checkedOtherRotations[i] = false;
        }
        checkedOtherRotations[1] = true;

        agentController.SetNextMove("s");
        Callback(true);
    }

    public void FindGoalDirection() {
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
                else requiredRotation = 2 - rotation;
            }
        }
        Callback(true);
    }

    public void FindAnotherRandomDirection() {
        int rotation = agentController.GetRotationInt();
        BlockData currentPositionBlock = agentController.GetCurrentBlockData();

        Stack<BlockData> moves = new Stack<BlockData>();
        List<BlockData> visisted = new List<BlockData>();
        BlockData currentPos;
        int nextPosIndex, depth, bias = 0;
        int[] steps = new int[4];

        for (int i = 0; i < currentPositionBlock.directions.Length; i++) {
            if (i == rotation) continue;
            if (!currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[i])) continue;
            nextPosIndex = currentPositionBlock.neighboorDirection.FindIndex(x => x == currentPositionBlock.directions[i]);
            moves.Clear();
            try { moves.Push(currentPositionBlock.neighboorBlocks[nextPosIndex]); }
            catch { continue; }
            depth = 0;
            bias = 0;
            currentPos = currentPositionBlock;
            visisted.Clear();

            while (moves.Count > 0 && depth <= 3) {
                visisted.Add(currentPos);
                currentPos = moves.Pop();
                for (int j = 0; j < currentPos.neighboorBlocks.Count; j++) {
                    if (visisted.Contains(currentPos.neighboorBlocks[j])) continue;
                    if (currentPos.neighboorBlocks[j].blockType == BlockType.LavaBlock) continue;
                    moves.Push(currentPos.neighboorBlocks[j]);
                    if (j == i) bias++;
                }
                depth++;
            }

            steps[i] = depth + bias;
        }

        steps[0] -= 1;
        steps[2] -= 1;
        if (steps[0] > 0 || steps[1] > 0 || steps[2] > 0) steps[3] = 0;
        int max = 0, optimalRotation = 0;

        for (int i = 0; i < steps.Length; i++) {
            if (steps[i] > max) {
                max = steps[i];
                optimalRotation = i;
            }
        }

        requiredRotation = optimalRotation - rotation;
        randomDirectionRotation = optimalRotation;
        Callback(true);
        Debug.Log("Req: " + requiredRotation + ", RDir: " + randomDirectionRotation);

        /*
        checkedOtherRotations[1] = true;
        if (!currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[1])) {
            checkedOtherRotations[2] = true;
        }

        List<int> possibleRotations = new List<int>();
        for (int i = 0; i < checkedOtherRotations.Length; i++) {
            if (!checkedOtherRotations[i]) possibleRotations.Add(i);
        }
        if (possibleRotations.Count == 0) {
            Callback(false);
            return;
        }

        int index = possibleRotations[Random.Range(0, possibleRotations.Count)];
        possibleRotations.RemoveAt(possibleRotations.FindIndex(x => x == index));
        checkedOtherRotations[index] = true;
        if (possibleRotations.Count > 0 && (index - rotation) % 2 == 0) {
            index = possibleRotations[Random.Range(0, possibleRotations.Count)];
        }

        try {
            if (currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[index])) {
                int dirIndex = currentPositionBlock.neighboorDirection.FindIndex(x => x == currentPositionBlock.directions[index]);
                if (currentPositionBlock.neighboorBlocks[dirIndex].blockType == BlockType.LavaBlock) return;
                requiredRotation = index - rotation;
                randomDirectionRotation = index;
                Callback(true);
            }
        } catch (System.ArgumentOutOfRangeException) {
            Debug.LogError("Well, fuck");
        }*/
    }

    public void IsTheNextBlockLava() {
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

    public void IsTheBlockTowardsGoalLava() {
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

    public void IsTheRandomDirectionBlockLava() {
        int rotation = randomDirectionRotation;
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

        if (forwardBlock == BlockType.LavaBlock || forwardBlock == BlockType.None) Callback(false);
        else Callback(true);
    }

    public void IsNotAlignedWithGoal() {
        if (agentController.GetRotationInt() == 1) Callback(false);
        else Callback(true);
    }

    public void Turn() {
        if (requiredRotation < 0) {
            requiredRotation++;
            agentController.SetNextMove("a");
        } else if (requiredRotation > 0) {
            requiredRotation--;
            agentController.SetNextMove("d");
        }

        if (requiredRotation == 0) {
            for (int i = 0; i < checkedOtherRotations.Length; i++) {
                checkedOtherRotations[i] = false;
            }
            checkedOtherRotations[1] = true;
            Callback(true);
        }
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
                return;
            }
            Callback(false);
            return;
        }
        Callback(false);
    }
}