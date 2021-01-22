using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class MapGenerator : MonoBehaviour {
    public delegate void MapCompleted();
    public MapCompleted mapCompletedEvent;
    [SerializeField] private DataCollector dataCollector = null;

    [Header("Map Settings")]
    [SerializeField] private bool showMapSettings = false;
    [SerializeField] private bool visualizeMapCreation = false;
    private Transform map;
    [Range(10, 50)] [SerializeField] private int xSize = 20;
    [Range(10, 50)] [SerializeField] private int zSize = 20;
    [Range(0, 30)] [SerializeField] private int procentChanceToSpawnLava = 10;
    [Range(0, 100)] [SerializeField] private int lavaSpreadChance = 50;
    [Range(0, 25)] [SerializeField] private int stoneSpawnChance = 3;
    private Vector3 spawnPoint = Vector3.zero;
    private Vector3 goalPoint = Vector3.zero;

    [Header("Canvas' and MapChecker")]
    [SerializeField] private GameObject mapCreatorPanel = null;
    [SerializeField] private GameObject createNewMapButton = null;
    [SerializeField] private Transform mapChecker = null;

    [Header("Size inputs")]
    [SerializeField] private InputField xInput = null;
    [SerializeField] private InputField zInput = null;
    [SerializeField] private InputField chanceInput = null;
    [SerializeField] private InputField spreadInput = null;

    [Header("Prefabs")]
    [SerializeField] private GameObject platformBlockPrefab = null;
    [SerializeField] private GameObject lavaBlockPrefab = null;
    [SerializeField] private GameObject goalBlockPrefab = null;
    [SerializeField] private GameObject spawnBlockPrefab = null;
    [SerializeField] private GameObject borderBlockPrefab = null;
    [SerializeField] private GameObject treasurePrefab = null;

    [Header("Object pools")]
    [SerializeField] private Transform border = null;
    [SerializeField] private Transform path = null;
    [SerializeField] private Transform fill = null;
    [SerializeField] private Transform treasureBlocks = null;

    [Header("Debugging")]
    [Tooltip("This option will spam the console!")]
    [SerializeField] private bool debugRunning = false;
    [SerializeField] private GameObject debuggerBlockPrefab = null;
    [SerializeField] private GameObject debuggerPathBlockPrefab = null;
    [SerializeField] private Transform debuggers = null;

    private Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
    private List<Vector3> securePath = new List<Vector3>();
    public List<BlockData> blocksCreated { get; private set; } = new List<BlockData>();

    private Camera mainCam = null;

    public delegate void MaterialUpdater();
    public MaterialUpdater materialUpdater;

    public Vector3 GetSpawnPoint() { return spawnPoint; }

    public void RecreateMap(CollectedData data) {
        StartCoroutine(CreateMapBorder(true));

        int index = 0;
        Vector3 nextPostion;
        BlockData block = null;
        for (int x = -xSize / 2; x < xSize / 2 + 1; x++) { //Go from left to right
            for (int z = -zSize / 2; z < zSize / 2 + 1; z++) { //Go from bottom to top
                nextPostion = new Vector3(x, 0, z);
                switch (data.recordedMap[index]) {
                    case "Platform":
                        block = Instantiate(platformBlockPrefab, nextPostion, Quaternion.identity, fill).GetComponent<BlockData>();
                        break;
                    case "LavaBlock":
                        block = Instantiate(lavaBlockPrefab, nextPostion, Quaternion.identity, fill).GetComponent<BlockData>();
                        break;
                    case "Spawn":
                        block = Instantiate(spawnBlockPrefab, nextPostion, Quaternion.identity, fill).GetComponent<BlockData>();
                        spawnPoint = block.transform.position;
                        break;
                    case "Goal":
                        block = Instantiate(goalBlockPrefab, nextPostion, Quaternion.identity, fill).GetComponent<BlockData>();
                        goalPoint = block.transform.position;
                        break;
                }
                blocksCreated.Add(block);
                index++;
            }
        }
        StartCoroutine(RaycastNeighboors());
    }

    public void StartGeneration() {
        map = transform;
        mainCam = Camera.main;
        if (!showMapSettings) {
            mapCreatorPanel.SetActive(false);
            createNewMapButton.SetActive(false);
            CreateMap();
        } else {
            mapCreatorPanel.SetActive(true);
            createNewMapButton.SetActive(true);
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.N)) { //Create a new map with same settings (shortcut)
            StopAllCoroutines();
            CreateMap();
        }
    }

    //Called from the x inputfield when the value is edited. This will clamp the value between 10 and 50

    public void UpdateSizeX() {
        xSize = int.Parse(xInput.text);
        if (xSize < 10) {
            xSize = 10;
            xInput.text = "10";
        } else if (xSize > 50) {
            xSize = 50;
            xInput.text = "50";
        }
    }

    //Called from the z inputfield when the value is edited. This will clamp the value between 10 and 50
    public void UpdateSizeY() {
        zSize = int.Parse(zInput.text);
        if (zSize < 10) {
            zSize = 10;
            zInput.text = "10";
        } else if (zSize > 50) {
            zSize = 50;
            zInput.text = "50";
        }
    }

    //Called from the lava spawn chance inputfield when the value is edited. This will clamp the value between 0 and 30
    public void UpdateChance() {
        procentChanceToSpawnLava = int.Parse(chanceInput.text);
        if (procentChanceToSpawnLava < 0) {
            procentChanceToSpawnLava = 0;
            chanceInput.text = "0";
        } else if (procentChanceToSpawnLava > 30) {
            procentChanceToSpawnLava = 30;
            chanceInput.text = "30";
        }
    }

    //Called from the lava spread chance inputfield when the value is edited. This will clamp the value between 0 and 100
    public void UpdateSpreadChance() {
        lavaSpreadChance = int.Parse(spreadInput.text);
        if (lavaSpreadChance < 0) {
            lavaSpreadChance = 0;
            chanceInput.text = "0";
        } else if (lavaSpreadChance > 100) {
            lavaSpreadChance = 100;
            spreadInput.text = "100";
        }
    }

    //Used by a button to show the mapCreatorPanel
    public void ShowCreateNewMapMenu() {
        mapCreatorPanel.SetActive(true);
        createNewMapButton.SetActive(false);
    }

    //This is called from a button, which will start the creation of a new map
    public void CreateMap() {
        Debug.ClearDeveloperConsole(); //Clear the console
        foreach (Transform child in map) { //Destroy all objects in the pools
            foreach (Transform grandchild in child) {
                Destroy(grandchild.gameObject);
            }
        }
        spawnPoint = new Vector3(-xSize / 2, 0, Random.Range(-zSize / 2, zSize / 2)); //Choose a random spawn point
        securePath.Add(spawnPoint);
        goalPoint = new Vector3(xSize / 2, 0, Random.Range(-zSize / 2, zSize / 2)); //Choose a random goal point
        securePath.Add(goalPoint);
        mapCreatorPanel.SetActive(false); //Hide the mapCreatorPanel
        StartCoroutine(CreateMapBorder()); //Start the creation of the map, starting with the border
    }

    //Draw a border around the map.
    IEnumerator CreateMapBorder(bool isRecreating = false) {
        if (border.childCount > 0) {
            foreach (Transform child in border) {
                Destroy(child.gameObject);
            }
        }
        //Iterate from negative to positive, so the map is centered.
        for (int x = (-xSize / 2) - 1; x < (xSize / 2) + 2; x++) { //Start at the left of the map's left side and go to the right of the map's right side
            for (int z = (-zSize / 2) - 1; z < (zSize / 2) + 2; z++) { //Do the same with the top and bottom of the map
                if (visualizeMapCreation) {
                    yield return null;
                }
                Instantiate(borderBlockPrefab, new Vector3(x, 0.5f, z), Quaternion.identity, border);
                if (x != (-xSize / 2) - 1 && x != (xSize / 2) + 1) {
                    Instantiate(borderBlockPrefab, new Vector3(x, 0.5f, (zSize / 2) + 1), Quaternion.identity, border);
                    break;
                }
            }
        }
        if (!visualizeMapCreation) {
            yield return null;
        }
        if (!isRecreating) StartCoroutine(CreateSecurePath());
    }

    //Create a random path from the start to the goal. If it fails, it will delete the path and start over.
    IEnumerator CreateSecurePath() {
        blocksCreated.Clear();

        GameObject s = Instantiate(spawnBlockPrefab, spawnPoint, Quaternion.identity, path); //Create a spawn block
        blocksCreated.Add(s.GetComponent<BlockData>());

        GameObject g = Instantiate(goalBlockPrefab, goalPoint, Quaternion.identity, path); //Create a goal block
        blocksCreated.Add(g.GetComponent<BlockData>());

        bool succes = true;
        Vector3 current = spawnPoint; //Start at the spawn point
        securePath.Clear(); //Clear the securePath list
        securePath.Add(spawnPoint); //Add the spawn
        securePath.Add(goalPoint); //Add the goal
        int timesContinued = 0;
        while (current != goalPoint) { //While not at the goal
            if (visualizeMapCreation) {
                yield return null;
            }
            Vector3 nextDir = Vector3.zero;
            int rnd = Random.Range(0, 13); //Get a random number (inclusive, exclusive)
            if (rnd <= 3) {
                nextDir = directions[0]; //Forward
            } else if (rnd > 3 && rnd <= 7) {
                nextDir = directions[1]; //Back
            } else if (rnd > 7 && rnd <= 11) {
                nextDir = directions[2]; //Left
            } else {
                nextDir = directions[3]; //Right
            }

            if (timesContinued > 50) { //If it is stuck for more than 50 iterations, break out of the loop
                succes = false;
                break;
            }

            Vector3 check = current + nextDir; //Create a check variable
            //If it hits outside the map size, continue to next iteration
            if (securePath.Contains(check) || check.x < -xSize / 2 || check.x > xSize / 2 || check.z < -zSize / 2 || check.z > zSize / 2) {
                timesContinued++;
                continue;
            }
            int usedPositions = 0;
            bool reachedGoal = false;
            List<Vector3> checkList = securePath; //Make a copy of the secure path, which can be used to check for overlaps
            checkList.Add(check); //Add the check point to the check list
            for (int i = 0; i < directions.Length; i++) { //For each of the directions, try and move that way.
                if (check == goalPoint) { //If the check point is the same as the goal point, break
                    usedPositions = 0;
                    break;
                }

                if (check + directions[i] == goalPoint) { //If check point plus the direction hits the goal point, set reachedGoal to true and break
                    if (debugRunning) Debug.Log("Reached the goal; breaking");
                    usedPositions = 0;
                    reachedGoal = true;
                    break;
                }

                if (checkList.Contains(check + directions[i])) { //If the check point plus direction hits a point in the check list, increment usedPositions and break
                    usedPositions++;
                }
            }

            if (usedPositions >= 2) { //If usedPositions is equal to or greater than two, increment timesContinued and continue to next iteration
                timesContinued++;
                continue;
            }

            current += nextDir;

            if (!securePath.Contains(current)) { //If the securePath doesn't contain the current point, add it...
                securePath.Add(current);
            }
            GameObject block = Instantiate(platformBlockPrefab, current, Quaternion.identity, path); //... and instantiate the platform block
            blocksCreated.Add(block.GetComponent<BlockData>());
            if (debugRunning) { //If debugging, instantiate a debug block at the point too.
                Instantiate(debuggerPathBlockPrefab, current, Quaternion.identity, debuggers);
            }
            timesContinued = 0; //Reset timesContinued, so it doesn't start from some random value for the next check nor continue instantly

            if (reachedGoal) { //If the goal is reached, break the loop
                break;
            }
        }
        if (!visualizeMapCreation) {
            yield return null;
        }
        if (succes) { //If it manages to reach the goal...
            //... check if the securePath list only contains the blocks of the path
            for (int i = 0; i < securePath.Count; i++) { //Iterate through the securePath list
                bool found = false;
                foreach (Transform child in path) { //Check each position of child in path to see if one of them matches the lists item...
                    if (securePath[i] == child.position) {
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    securePath.RemoveAt(i); //... if it doesn't find it, remove the item from the list 
                }
            }
            StartCoroutine(FillMap());
        } else { //If it didn't manage to reach the goal, clear the path and debuggers (if they are active)
            foreach (Transform child in path) {
                Destroy(child.gameObject);
            }
            if (debugRunning) {
                foreach (Transform child in debuggers) {
                    Destroy(child.gameObject);
                }
            }
            if (debugRunning) Debug.Log("Didn't succeed. Starting over");
            StartCoroutine(CreateSecurePath());
        }

    }

    IEnumerator FillMap() {
        LayerMask blockMask = LayerMask.GetMask("Block");
        //Iterate from negative to positive, so the map is centered.
        for (int x = -xSize / 2; x < xSize / 2 + 1; x++) { //Go from left to right
            for (int z = -zSize / 2; z < zSize / 2 + 1; z++) { //Go from bottom to top
                if (visualizeMapCreation) {
                    yield return null;
                }
                Vector3 nextPostion = new Vector3(x, 0, z); //Save the position
                if (securePath.Contains(nextPostion)) {
                    if (debugRunning) {
                        Instantiate(debuggerBlockPrefab, nextPostion, Quaternion.identity, debuggers);
                    }
                    //Debug.Log("This position is part of the path: " + nextPostion);
                    continue;
                }
                BlockCreater(nextPostion, blockMask); //Spawn a block using BlockCreater()
            }
        }
        if (!visualizeMapCreation) {
            yield return null;
        }
        StartCoroutine(HoleCheck());
    }

    //Use randomness to check values for lava to either spawn or spread. Depending on these values, this will spawn either a lava block or platform block
    void BlockCreater(Vector3 nextPosition, LayerMask blockMask) {
        bool hitLava = false;
        BlockData lavaBlockFound = null;
        for (int i = 0; i < directions.Length; i++) { //Check the blocks in each direction of the current position
            Ray ray = new Ray(nextPosition, directions[i]);
            Physics.Raycast(ray, out RaycastHit hit, 1.5f, blockMask); //Raycast and save information about the object hit
            if (hit.collider != null && hit.collider.GetComponent<BlockData>().blockType.ToString() == "LavaBlock") { //Make sure it actually hit something, and if it is a lava block...
                hitLava = true; //... set hitLava to true...
                lavaBlockFound = hit.collider.GetComponent<BlockData>(); //... and save a reference to its data before breaking
                break;
            }
        }
        if (hitLava) { //If it hit lava in the loop above...
            if (Random.Range(1, 101) <= lavaBlockFound.lavaSpreadChance) { //... check if a random number is less than the block hit's lavaSpreadChance
                BlockData block = Instantiate(lavaBlockPrefab, nextPosition, Quaternion.identity, fill).GetComponent<BlockData>(); //If it is less than or equal to, spawn a lava block...
                block.lavaSpreadChance = Mathf.CeilToInt(lavaBlockFound.lavaSpreadChance / 2); //... set the new block's lavaSpreadChance to half that of the original block
                blocksCreated.Add(block);
            } else { //If the numbers is greater than the chance...
                BlockData block = Instantiate(platformBlockPrefab, nextPosition, Quaternion.identity, fill).GetComponent<BlockData>(); //... spawn a platform block
                blocksCreated.Add(block);
            }
        } else { //If it didn't hit lava in the loop above...
            if (Random.Range(1, 101) <= procentChanceToSpawnLava) { //... check if a random number is less than or equal to the chance of spawning a lava block
                BlockData block = Instantiate(lavaBlockPrefab, nextPosition, Quaternion.identity, fill).GetComponent<BlockData>(); //If it is, spawn a lava block...
                block.lavaSpreadChance = lavaSpreadChance; //... and set the lavaSpreadChance to the start value
                blocksCreated.Add(block);
            } else {
                BlockData block = Instantiate(platformBlockPrefab, nextPosition, Quaternion.identity, fill).GetComponent<BlockData>(); //Spawn a platform block
                blocksCreated.Add(block);
            }
        }
    }

    //Check each position in the map to make sure there are no missing blocks
    IEnumerator HoleCheck() {
        LayerMask blockMask = LayerMask.GetMask("Block");
        for (int x = -xSize / 2; x < xSize / 2 + 1; x++) { //Go from left to right
            if (visualizeMapCreation) yield return null;
            for (int z = -zSize / 2; z < zSize / 2 + 1; z++) { //Go from bottom to top
                mapChecker.position = new Vector3(x, 2, z); //Place the checker
                Ray ray = new Ray(mapChecker.position, Vector3.down);
                Physics.Raycast(ray, out RaycastHit hit, 4, blockMask); //Raycast downwards
                if (hit.collider != null) { //If it hit someting, continue
                    if (debugRunning) Debug.Log("Block existed");
                    continue;
                } else {
                    if (debugRunning) Debug.Log("Block didn't exist; created it at position: " + (mapChecker.position + (Vector3.down * 2)));
                    BlockCreater(mapChecker.position + (Vector3.down * 2), blockMask); //If it didn't hit anything, call BlockCreater
                }
            }
        }
        if (!visualizeMapCreation) {
            yield return null;
        }
        StartCoroutine(TreasureSpawner());
        //StartCoroutine(RaycastNeighboors());
    }

    IEnumerator TreasureSpawner() {
        int maxNumberOfTreasureBlocksToSpawn = 5;
        int currentNumberOfTreasureBlocks = 0;
        float spawnChance = 0.05f;
        LayerMask blockMask = LayerMask.GetMask("Block");
        for (int x = (-xSize / 2) + 3; x < (xSize / 2) - 3; x++) { //Go from left to right
            if (visualizeMapCreation) yield return null;
            for (int z = -zSize / 2; z < zSize / 2 + 1; z++) { //Go from bottom to top
                if (currentNumberOfTreasureBlocks == maxNumberOfTreasureBlocksToSpawn) break;
                mapChecker.position = new Vector3(x, 2, z); //Place the checker
                Ray ray = new Ray(mapChecker.position, Vector3.down);
                Physics.Raycast(ray, out RaycastHit hit, 4, blockMask); //Raycast downwards

                bool skip = false;
                BlockData block = hit.collider.GetComponent<BlockData>();
                block.FindNeighboors();
                foreach (BlockData bd in block.neighboorBlocks) {
                    if (bd.blockType == BlockType.Treasure) skip = true;
                }
                if (skip) continue;

                if (block.blockType == BlockType.Platform) {
                    int rng = Random.Range(0, 101);
                    if ((float)rng <= spawnChance) {
                        spawnChance = 0.05f;
                        currentNumberOfTreasureBlocks++;

                        GameObject go = Instantiate(treasurePrefab, treasureBlocks);
                        go.transform.position = hit.collider.transform.position;
                        blocksCreated.Add(go.GetComponent<BlockData>());
                        blocksCreated.Remove(blocksCreated.Find(i => i.gameObject == hit.collider.gameObject));
                        Destroy(hit.collider.gameObject);

                        if (debugRunning) Debug.Log("Treasure block spawned at " + (mapChecker.position + Vector3.down));
                    } else {
                        spawnChance *= 2;
                    }
                }
            }
            if (currentNumberOfTreasureBlocks == maxNumberOfTreasureBlocksToSpawn) break;
        }

        StartCoroutine(EnsureAPathToObjectives(treasureBlocks));
    }

    IEnumerator EnsureAPathToObjectives(Transform pool) {
        List<BlockData> explored = new List<BlockData>();
        Stack<BlockData> unexplored = new Stack<BlockData>();
        int whileBreaker = 0;
        bool foundPath = false;
        Vector3 closestPointToPath = new Vector3(100, 100, 100);

        foreach (Transform obj in pool) {
            if (visualizeMapCreation) yield return null;
            explored.Clear();
            unexplored.Clear();
            whileBreaker = 0;
            foundPath = false;

            BlockData bd = blocksCreated.Find(x => x.gameObject == obj.gameObject);
            unexplored.Push(bd);
            while (unexplored.Count > 0) {
                whileBreaker++;
                if (whileBreaker > 10000) break;

                bd = unexplored.Pop();
                explored.Add(bd);
                foreach (Vector3 v3 in securePath) {
                    if (Vector3.Distance(bd.transform.position, v3) <
                        Vector3.Distance(closestPointToPath, v3)) {
                        closestPointToPath = bd.transform.position;
                    }
                }

                if (securePath.Contains(bd.transform.position)) {
                    foundPath = true;
                    break;
                }

                bd.FindNeighboors();
                foreach (BlockData neighboor in bd.neighboorBlocks) {
                    if (neighboor.blockType != BlockType.LavaBlock) {
                        if (!explored.Contains(neighboor)) {
                            unexplored.Push(neighboor);
                        }
                    }
                }
            }

            if (!foundPath) {
                Debug.Log("Path not found. Creating a path");
                Vector3 closestPointOnPath = new Vector3(100, 100, 100);
                float shortestDistance = 1000;
                foreach (Vector3 v3 in securePath) {
                    if (Vector3.Distance(closestPointToPath, v3) < shortestDistance) {
                        shortestDistance = Vector3.Distance(closestPointToPath, v3);
                        closestPointOnPath = v3;
                    }
                }

                RaycastHit[] blocksHit = Physics.RaycastAll(new Ray(closestPointToPath, closestPointToPath - closestPointOnPath), 
                    shortestDistance);
                foreach (RaycastHit hit in blocksHit) {
                    if (hit.transform.position == closestPointToPath || hit.transform.position == closestPointOnPath) continue;

                    Debug.Log(hit.collider.name + ", " + hit.collider.transform.position + ", " + closestPointToPath + ", " + closestPointOnPath);
                    bd = hit.collider.GetComponent<BlockData>();
                    GameObject go = Instantiate(platformBlockPrefab, path);
                    go.transform.position = bd.transform.position;
                    securePath.Add(go.transform.position);
                    blocksCreated.Add(go.GetComponent<BlockData>());
                    blocksCreated.Remove(bd);
                }
            }
        }

        StartCoroutine(RaycastNeighboors());
    }

    //Used to make each block save information about their neighboors and start their animation
    IEnumerator RaycastNeighboors() {
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < blocksCreated.Count; i++) { //Go through all blocks created
            blocksCreated[i].FindNeighboors(); //Call their FindNeighboors method. This will use GetComponent for each block in the map, so it MIGHT be hard on the computer!
        }
        if (showMapSettings) createNewMapButton.SetActive(true);
        TriggerMaterialUpdater();
    }

    //Trigger material updater in the blocks. This is used for lava animation and uses the delegate at the top
    public void TriggerMaterialUpdater() {
        materialUpdater();
        mapCompletedEvent();
    }

    public void RecordMap() {
        dataCollector.SetMapSize(xSize, zSize);
        LayerMask blockMask = LayerMask.GetMask("Block");
        for (int x = -xSize / 2; x < xSize / 2 + 1; x++) { //Go from left to right
            for (int z = -zSize / 2; z < zSize / 2 + 1; z++) { //Go from bottom to top
                mapChecker.position = new Vector3(x, 2, z); //Place the checker
                Ray ray = new Ray(mapChecker.position, Vector3.down);
                Physics.Raycast(ray, out RaycastHit hit, 4, blockMask); //Raycast downwards
                if (hit.collider != null) dataCollector.AddMapData(hit.collider.GetComponent<BlockData>().blockType.ToString());
                else throw new System.NullReferenceException("Didn't find a block at position (" + x + ", " + z + ")!");
            }
        }
    }
}