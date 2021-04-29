using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BTHandler : TreeHandler {
    [SerializeField] private AgentController agentController = null;
    private int requiredRotation = 0;
    private int randomDirectionRotation = 0;
    private List<BlockData> seenBlocks = new List<BlockData>();
    private KeyValuePair<BlockData, string> lastTurn = new KeyValuePair<BlockData, string>();
    private string memory = "";
    private bool wasForced = false;

    public void WalkBackwards() {
        agentController.SetNextMove("s");
        MemoryHandling("s");
        Callback(true);
    }

    public void FindGoalDirection() {
        BlockData currentBlock = agentController.GetCurrentBlockData();
        int rotation = agentController.GetRotationInt();

        if (currentBlock.neighboorDirection.Contains(currentBlock.directions[1])) {
            switch (rotation) {
                case 0:
                    requiredRotation = 1;
                    break;
                case 1:
                    requiredRotation = 0;
                    break;
                case 2:
                    requiredRotation = -1;
                    break;
                case 3:
                    requiredRotation = Random.Range(0, 2) == 0 ? -1 : 1;
                    break;
            }
        } else {
            switch (rotation) {
                case 0:
                    requiredRotation = Random.Range(0, 2) == 0 ? -1 : 1;
                    break;
                case 1:
                    requiredRotation = 1;
                    break;
                case 2:
                    requiredRotation = 0;
                    break;
                case 3:
                    requiredRotation = -1;
                    break;
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
        int nextPosIndex, depth, prevSteps = 0, steps = 0, optimalRotation = 1;

        for (int i = 0; i < currentPositionBlock.directions.Length; i++) {
            if ((i + rotation) % 2 == 0) continue;
            if (!currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[i])) continue;

            nextPosIndex = currentPositionBlock.neighboorDirection.FindIndex(x => x == currentPositionBlock.directions[i]);
            moves.Clear();
            try { moves.Push(currentPositionBlock.neighboorBlocks[nextPosIndex]); } catch { continue; }
            depth = 0;
            steps = 0;
            currentPos = currentPositionBlock;
            visisted.Clear();

            while (moves.Count > 0 && depth <= 5) {
                visisted.Add(currentPos);
                currentPos = moves.Pop();
                for (int j = 0; j < currentPos.neighboorBlocks.Count; j++) {
                    if (visisted.Contains(currentPos.neighboorBlocks[j])) continue;
                    if (currentPos.neighboorBlocks[j].blockType == BlockType.LavaBlock) continue;
                    moves.Push(currentPos.neighboorBlocks[j]);
                    steps++;
                }
                depth++;
            }

            if (steps > prevSteps) {
                optimalRotation = i;
                prevSteps = steps;
            }
        }

        requiredRotation = optimalRotation - rotation;
        requiredRotation = requiredRotation > 1 ? 1 : requiredRotation < -1 ? -1 : requiredRotation;
        randomDirectionRotation = optimalRotation;

        if (currentPositionBlock == lastTurn.Key) {
            if (lastTurn.Value.Equals("a")) requiredRotation = 1;
            else requiredRotation = -1;
            randomDirectionRotation = rotation + requiredRotation;
        }

        Callback(true);
    }

    public void CheckFrontBlock() {
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

        if (forwardBlock == BlockType.LavaBlock || forwardBlock == BlockType.None) Callback(false);
        else {
            if (wasForced) {
                Callback(false);
                wasForced = false;
            }
            else Callback(true);
        }
    }

    public void CanTurnTowardsGoal() {
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
        else {
            if (wasForced) {
                Callback(false);
                wasForced = false;
            } else Callback(true);
        }
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

    public void IsFacingGoalDirection() {
        int rotation = agentController.GetRotationInt();
        BlockData currentPositionBlock = agentController.GetCurrentBlockData();

        if (currentPositionBlock.neighboorDirection.Contains(currentPositionBlock.directions[1])) {
            if (rotation == 1) {
                Callback(true);
                return;
            }
        } else {
            if (rotation == 2) {
                Callback(true);
                return;
            }
        }
        Callback(false);
    }

    public void Turn() {
        if (requiredRotation < 0) {
            requiredRotation++;
            agentController.SetNextMove("a");
            MemoryHandling("a");
            lastTurn = new KeyValuePair<BlockData, string>(agentController.GetCurrentBlockData(), "a");
        } else if (requiredRotation > 0) {
            requiredRotation--;
            agentController.SetNextMove("d");
            MemoryHandling("d");
            lastTurn = new KeyValuePair<BlockData, string>(agentController.GetCurrentBlockData(), "d");
        }

        if (requiredRotation == 0) Callback(true);
    }

    public void WalkForward() {
        if (memory.Equals("wsws") || memory.Equals("swsw")) {
            agentController.SetNextMove("s");
            MemoryHandling("s");
            wasForced = true;
        } else {
            agentController.SetNextMove("w");
            MemoryHandling("w");
        }

        List<BlockData> visibleMap = agentController.GetCurrentBlockMap();
        for (int i = 0; i < visibleMap.Count; i++) {
            if (!seenBlocks.Contains(visibleMap[i])) seenBlocks.Add(visibleMap[i]);
        }

        Callback(true);
    }

    private void MemoryHandling(string action) {
        if (memory.Length == 4) memory = memory.Substring(1, 3);
        memory += action;
    }
}