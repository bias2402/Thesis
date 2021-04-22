using UnityEngine;

public static class FSM {
    private static string memory = "";
    private static string[] patterns =  { "wsw", "sws", "ada", "dad", "wdw", "waw" };

    public static string GetNextAction(BlockData blockData, int rotation) {
        BlockType forwardBlock;
        if (blockData.neighboorBlocks.Count == 4) forwardBlock = blockData.neighboorBlocks[rotation].blockType;
        else {
            if (blockData.neighboorDirection.Contains(blockData.directions[rotation])) {
                int index = blockData.neighboorDirection.FindIndex(x => x == blockData.directions[rotation]);
                forwardBlock = blockData.neighboorBlocks[index].blockType;
            }
            else forwardBlock = BlockType.None;
        }

        if (forwardBlock == BlockType.LavaBlock || forwardBlock == BlockType.None) {
            if (IsATurnPossible(blockData, rotation, out int direction)) {
                if (direction == 1) {
                    AddToMemory("d");
                    if (IsRepeatingPattern()) return "a";
                    return "d";
                }
                else if (direction == -1) {
                    AddToMemory("a");
                    if (IsRepeatingPattern()) return "d";
                    return "a";
                }
                else {
                    AddToMemory("s");
                    if (IsRepeatingPattern()) {
                        if (Random.Range(0, 2) == 0) return "a";
                        else return "d";
                    }
                    return "s";
                }
            } else {
                AddToMemory("s");
                if (IsRepeatingPattern()) {
                    if (Random.Range(0, 2) == 0) return "a";
                    else return "d";
                }
                return "s";
            }
        } else {
            AddToMemory("w");
            if (IsRepeatingPattern()) {
                if (Random.Range(0, 2) == 0) return "a";
                else return "d";
            }
            return "w";
        }
    }

    static bool IsATurnPossible(BlockData blockData, int rotation, out int direction) {
        direction = 0;
        int index = GetLeftBlockIndex(rotation);
        BlockType leftBlock, rightBlock;
        if (index < blockData.neighboorBlocks.Count) leftBlock = blockData.neighboorBlocks[index].blockType;
        else leftBlock = BlockType.None;

        index = GetRightBlockIndex(rotation);
        if (index < blockData.neighboorBlocks.Count) rightBlock = blockData.neighboorBlocks[index].blockType;
        else rightBlock = BlockType.None;

        if ((leftBlock == BlockType.LavaBlock || leftBlock == BlockType.None) &&
            (rightBlock == BlockType.LavaBlock || rightBlock == BlockType.None)) return false;
        else if (leftBlock == BlockType.LavaBlock) {
            direction = 1;
            return true;
        } else {
            direction = -1;
            return true;
        }
    }

    static int GetLeftBlockIndex(int rotation) {
        int output;
        if (rotation == 0) output = 3;
        else output = rotation - 1;
        return output;
    }

    static int GetRightBlockIndex(int rotation) {
        int output;
        if (rotation == 3) output = 0;
        else output = rotation + 1;
        return output;
    }

    static void AddToMemory(string action) {
        if (memory.Length == 5) {
            memory = memory.Substring(1, 4);
        }
        memory += action;
    }

    static bool IsRepeatingPattern() {
        for (int i = 0; i < patterns.Length; i++) {
            if (memory.Contains(patterns[i])) {
                memory = "";
                Debug.Log("Repeating");
                return true;
            }
        }
        return false;
    }
}