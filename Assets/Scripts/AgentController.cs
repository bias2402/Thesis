using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using MachineLearning;
using Utilities;
using System.Globalization;

public enum AgentType { Human, ANN, BT, CNN, FSM, Playback }
public enum PlayStyles { Speedrunner, Explorer, Treasurehunter, Done }
public enum TestCombination { Test1, Test2, Test3 }

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour {
    [Header("Agent Setting")]
    [SerializeField] private AgentType agentType = AgentType.Human;
    private PlayStyles playstyle = PlayStyles.Speedrunner;
    public static int gridVisibility = 11;

    [Header("Recording Data")]
    [SerializeField] private DataCollector dataCollector = new DataCollector();
    [SerializeField] private bool isRecordingData = false;
    private int steps = 0;
    private string playerName = "";
    private bool isNameSet = false;
    private float explorationPercentage = 0;
    private int treasuresFound = 0;
    private int maxTreasuresInMap = 0;
    private float agentDelay = 0;
    private Queue<string> playbackActions;

    [Header("AI Settings & Playback")]
    [SerializeField] private TextAsset playerData = null;
    [SerializeField] private TrainingDataIterator dataIterator = null;
    [SerializeField] private bool debugAI = false;
    [SerializeField] private bool isTraining = false;
    [SerializeField] private bool isEmulatingTraining = false;
    private string nextMove = "";

    [Header("ANN Settings")]
    [SerializeField] [Range(1, 3)] private int annDebugDepth = 1;
    [SerializeField] private ANNSaver annSaver = null;
    [SerializeField] private ANNSaver annOutput = null;
    [SerializeField] private TextAsset annConfigFile = null;
    private Configuration.ANNConfig annConfig = null;
    private ANN ann = null;

    [Header("CNN Settings")]
    [SerializeField] [Range(1, 3)] private int cnnDebugDepth = 1;
    [SerializeField] private CNNSaver cnnSaver = null;
    [SerializeField] private CNNSaver cnnOutput = null;
    [SerializeField] private TextAsset cnnConfigFile = null;
    private Configuration.CNNConfig cnnConfig = null;
    private CNN cnn = null;

    [Header("Map & Agent")]
    [SerializeField] private MapGenerator mapGenerator = null;
    [SerializeField] private FollowObject cameraFollower = null;
    [SerializeField] private float moveCooldown = 1;
    [SerializeField] private AudioClip[] clips = null;
    private AudioSource audioSource = null;
    private BlockData currentPositionBlockData = null;
    private Rigidbody rb = null;
    public bool didReachGoal { get; private set; } = false;
    public bool isAlive { get; private set; } = true;
    private bool isReadyToMove = false;
    private float moveCooldownCounter = 1.5f;
    private int move = 0;
    private bool isPreparing = false;
    private BTHandler btHandler = null;

    [Header("UI")]
    [SerializeField] private Text moveSuggestion = null;
    [SerializeField] private Text explanationText = null;
    [SerializeField] private GameObject explanationTextBackground = null;
    [SerializeField] private GameObject nameField = null;
    [SerializeField] private InputField nameInput = null;
    [SerializeField] private Text actionsText = null;
    [SerializeField] private Text explorationText = null;
    [SerializeField] private Text treasureText = null;

    void Start() {
        audioSource = GetComponent<AudioSource>();
        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        mapGenerator.mapCompletedEvent += ResetAgent;

        switch (agentType) {
            case AgentType.Human:
                isTraining = false;
                mapGenerator.StartGeneration();
                StartCoroutine(InformPlayer());
                break;
            case AgentType.FSM:
                isTraining = false;
                mapGenerator.StartGeneration();
                break;
            case AgentType.BT:
                isTraining = false;
                btHandler = GetComponent<BTHandler>();
                btHandler.enabled = true;
                btHandler.InitTree();
                mapGenerator.StartGeneration();
                break;
            case AgentType.Playback:
                //actionsText.gameObject.SetActive(true);
                //explorationText.gameObject.SetActive(true);
                //treasureText.gameObject.SetActive(true);

                isRecordingData = false;
                isTraining = false;
                RecreateRun();
                break;
            case AgentType.ANN:
                //actionsText.gameObject.SetActive(true);
                //explorationText.gameObject.SetActive(true);
                //treasureText.gameObject.SetActive(true);

                isRecordingData = false;

                if (annSaver == null || string.IsNullOrEmpty(annSaver.serializedANN)) {
                    if (annConfigFile != null) {
                        annConfig = Configuration.DeserializeANN("Assets/" + annConfigFile.name + ".txt");
                        ann = new ANN(annConfig, 11 * 11);
                    } else {
                        ann = new ANN(4, 2, 4, 4, ActivationFunctionHandler.ActivationFunction.Sigmoid,
                            ActivationFunctionHandler.ActivationFunction.Sigmoid, 1);
                    }
                } else ann = MLSerializer.DeserializeANN(annSaver.serializedANN);

                if (debugAI) MLDebugger.EnableDebugging(ann, annDebugDepth);

                if (isTraining && !isEmulatingTraining) RecreateRun();
                else mapGenerator.StartGeneration();
                break;
            case AgentType.CNN:
                //actionsText.gameObject.SetActive(true);
                //explorationText.gameObject.SetActive(true);
                //treasureText.gameObject.SetActive(true);

                isRecordingData = false;

                if (cnnSaver == null || string.IsNullOrEmpty(cnnSaver.serializedCNN)) {
                    cnn = new CNN();
                    if (cnnConfig != null) cnnConfig = Configuration.DeserializeCNN("Assets/" + cnnConfigFile.name + ".txt");
                    AddCNNFilters(cnn);
                } else cnn = MLSerializer.DeserializeCNN(cnnSaver.serializedCNN);
                if (debugAI) MLDebugger.EnableDebugging(cnn, cnnDebugDepth);

                if (isTraining && !isEmulatingTraining) RecreateRun();
                else mapGenerator.StartGeneration();
                break;
        }

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        enabled = false;
    }

    void RecreateRun() {
        if (dataIterator != null) {
            if (!dataIterator.GetNextFile(out playerData)) {
                Debug.Log("No more files in iterator");
                return;
            }
        }
        if (playerData == null) throw new System.NullReferenceException("PlayerData file wasn't set! It is needed for training!");
        StreamReader sr = dataIterator == null ? new StreamReader("Assets/Player Data/" + playerData.name + ".txt") :
                                                 new StreamReader(dataIterator.GetPath() + playerData.name + ".txt");
        CollectedData collectedData = JsonUtility.FromJson<CollectedData>(sr.ReadToEnd());
        mapGenerator.RecreateMap(collectedData);
        playbackActions = new Queue<string>(collectedData.recordedActions);
    }

    void ResetAgent() {
        if (didReachGoal) {
            steps = 0;
            explorationPercentage = 0;
            treasuresFound = 0;
            maxTreasuresInMap = 5;
            actionsText.text = "Actions: " + steps;
            explorationText.text = "Exploration: " + explorationPercentage.ToString() + "%";
            treasureText.text = "Treasures found: " + treasuresFound + "/" + maxTreasuresInMap;
            foreach (BlockData bd in mapGenerator.blocksCreated) {
                bd.isCasting = true;
                bd.isFound = false;
            }
        }

        enabled = true;
        isReadyToMove = false;
        didReachGoal = false;
        isAlive = true;
        transform.position = mapGenerator.GetSpawnPoint() + Vector3.up;
        transform.rotation = Quaternion.Euler(0, 90, 0);
        move = 0;
        moveCooldownCounter = moveCooldown * 3;
        rb.isKinematic = false;
        cameraFollower.enabled = true;
        GetBlockDataFromBlockBelowAgent();
    }

    void Update() {
        if (isTraining && isEmulatingTraining) {
            if (Input.GetKeyDown(KeyCode.T)) {
                StartCoroutine(EmulationHandler());
            }
        } else {
            if (Input.GetKeyDown(KeyCode.KeypadPlus)) Time.timeScale = Time.timeScale < 5 ? Time.timeScale += 1 : 5;
            if (Input.GetKeyDown(KeyCode.KeypadMinus)) Time.timeScale = Time.timeScale > 1 ? Time.timeScale -= 1 : 1;

            if (!isAlive || didReachGoal || isPreparing) return;

            if (isReadyToMove) {
                AgentMovementHandling();
                if (agentType == AgentType.Human && isRecordingData) agentDelay += Time.deltaTime;
            } else {
                transform.Translate(Vector3.forward * move * Time.deltaTime * (1 / moveCooldown));
                if (moveCooldownCounter > 0) moveCooldownCounter -= Time.deltaTime;
                else {
                    if (isRecordingData) {
                        dataCollector.AddDelayToRecords(agentDelay);
                        agentDelay = 0;
                    }
                    moveCooldownCounter = moveCooldown;
                    isReadyToMove = true;
                    move = 0;
                    GetBlockDataFromBlockBelowAgent();
                    AssessDataFromBlockBelowAgent();
                    Exploration();
                    Align();
                }
            }
        }
    }

    IEnumerator EmulationHandler() {
        if (agentType == AgentType.ANN || agentType == AgentType.CNN) {
            StreamReader sr;
            if (dataIterator != null) {
                while (dataIterator.GetNextFile(out playerData)) {
                    sr = new StreamReader(dataIterator.GetPath() + playerData.name + ".txt");
                    CollectedData collectedData = JsonUtility.FromJson<CollectedData>(sr.ReadToEnd());
                    sr.Close();
                    mapGenerator.RecreateMap(collectedData);
                    yield return new WaitForSeconds(3);

                    if (agentType == AgentType.CNN) {
                        cnn.EmulateTraining(collectedData, cnnConfig);
                        cnnSaver.serializedCNN = MLSerializer.SerializeCNN(cnn);
                    } else {
                        ann.EmulateTraining(collectedData);
                        annSaver.serializedANN = MLSerializer.SerializeANN(ann);
                    }
                }
            } else {
                sr = new StreamReader("Assets/Player Data/" + playerData.name + ".txt");
                CollectedData collectedData = JsonUtility.FromJson<CollectedData>(sr.ReadToEnd());
                sr.Close();
                mapGenerator.RecreateMap(collectedData);
                yield return new WaitForSeconds(3);

                if (agentType == AgentType.CNN) {
                    cnn.EmulateTraining(collectedData, cnnConfig);
                    cnnSaver.serializedCNN = MLSerializer.SerializeCNN(cnn);
                } else {
                    ann.EmulateTraining(collectedData);
                    annSaver.serializedANN = MLSerializer.SerializeANN(ann);
                }
            }
            Debug.Log("Training done");
        }
    }

    //Public Get/Set methods
    #region
    public void SetPlayerName() {
        if (nameInput.text == null || nameInput.text == "") return;
        playerName = nameInput.text;
        isNameSet = true;
    }

    public bool GetReadyToMoveState() { return isReadyToMove; }

    public int SetMaxTreasures(int max) => maxTreasuresInMap = max;
    #endregion

    //Movement and environment handling
    #region
    void AgentMovementHandling() {
        switch (agentType) {
            case AgentType.Human:
                if (Input.GetKey(KeyCode.W)) {
                    WalkForward();
                    steps++;
                    actionsText.text = "Actions: " + steps;
                    if (isRecordingData) dataCollector.AddMoveToRecords("w");
                }
                if (Input.GetKey(KeyCode.S)) {
                    WalkBackward();
                    steps++;
                    actionsText.text = "Actions: " + steps;
                    if (isRecordingData) dataCollector.AddMoveToRecords("s");
                }
                if (Input.GetKey(KeyCode.A)) {
                    TurnLeft();
                    steps++;
                    actionsText.text = "Actions: " + steps;
                    if (isRecordingData) dataCollector.AddMoveToRecords("a");
                }
                if (Input.GetKey(KeyCode.D)) {
                    TurnRight();
                    steps++;
                    actionsText.text = "Actions: " + steps;
                    if (isRecordingData) dataCollector.AddMoveToRecords("d");
                }
                break;
            case AgentType.ANN:
            case AgentType.CNN:
                if (isTraining) Playback();
                else PerformNextMove();
                break;
            case AgentType.BT:
                btHandler.Execute();
                PerformNextMove();
                isReadyToMove = false;
                break;
            case AgentType.FSM:
                FSMExecution();
                PerformNextMove();
                isReadyToMove = false;
                break;
            case AgentType.Playback:
                Playback();
                break;
            default:
                throw new System.NullReferenceException("AIType not properly set!");
        }
    }

    void Playback() {
        if (agentDelay > 0) {
            agentDelay -= Time.deltaTime;
            return;
        }

        string nextMove = playbackActions.Dequeue();
        switch (nextMove) {
            case "w":
                WalkForward();
                break;
            case "a":
                TurnLeft();
                break;
            case "s":
                WalkBackward();
                break;
            case "d":
                TurnRight();
                break;
            default:
                agentDelay = float.Parse(nextMove, CultureInfo.InvariantCulture);
                break;
        }
    }

    void PerformNextMove() {
        switch (nextMove) {
            case "w":
                WalkForward();
                break;
            case "a":
                TurnLeft();
                break;
            case "s":
                WalkBackward();
                break;
            case "d":
                TurnRight();
                break;
            default:
                if (agentType == AgentType.ANN || agentType == AgentType.CNN) FeedDataToNetwork(new List<double>() { 0, 0, 0, 0 });
                break;
        }
    }

    void Align() {
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x);
        pos.z = Mathf.Round(pos.z);
        transform.position = pos;
    }

    public void WalkForward() {
        if (!IsTheBorderInTheWay(transform.forward)) {
            move = 1;
            isReadyToMove = false;
            if (audioSource != null && audioSource.enabled) {
                audioSource.clip = clips[0];
                audioSource.Play();
            }
            if (agentType == AgentType.ANN || agentType == AgentType.CNN) FeedDataToNetwork(new List<double>() { 1, 0, 0, 0 });
        }
    }

    public void WalkBackward() {
        if (!IsTheBorderInTheWay(-transform.forward)) {
            move = -1;
            isReadyToMove = false;
            if (audioSource != null && audioSource.enabled) {
                audioSource.clip = clips[0];
                audioSource.Play();
            }
            if (agentType == AgentType.ANN || agentType == AgentType.CNN) FeedDataToNetwork(new List<double>() { 0, 0, 1, 0 });
        }
    }

    public void TurnLeft() {
        transform.Rotate(new Vector3(0, -90, 0));
        isReadyToMove = false;
        if (audioSource != null && audioSource.enabled) {
            audioSource.clip = clips[1];
            audioSource.Play();
        }
        if (agentType == AgentType.ANN || agentType == AgentType.CNN) FeedDataToNetwork(new List<double>() { 0, 1, 0, 0 });
    }

    public void TurnRight() {
        transform.Rotate(new Vector3(0, 90, 0));
        isReadyToMove = false;
        if (audioSource != null && audioSource.enabled) {
            audioSource.clip = clips[1];
            audioSource.Play();
        }
        if (agentType == AgentType.ANN || agentType == AgentType.CNN) FeedDataToNetwork(new List<double>() { 0, 0, 0, 1 });
    }

    void AgentReachedGoal() {
        switch (agentType) {
            case AgentType.Human:
                switch (playstyle) {
                    case PlayStyles.Speedrunner:
                        if (isRecordingData) dataCollector.CreateJSONFile("Speed_Runner_" + playerName);
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true, true));
                        playstyle = PlayStyles.Explorer;
                        break;
                    case PlayStyles.Explorer:
                        if (isRecordingData) dataCollector.CreateJSONFile("Explorer_" + playerName, explorationPercentage);
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true, true));
                        playstyle = PlayStyles.Treasurehunter;
                        break;
                    case PlayStyles.Treasurehunter:
                        if (treasuresFound < 5) {
                            didReachGoal = false;
                            StartCoroutine(Feedback("There are still treasures left to find!", false));
                            break;
                        }
                        if (isRecordingData) dataCollector.CreateJSONFile("Treasurehunter_" + playerName, explorationPercentage, treasuresFound);
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true, true));
                        playstyle = PlayStyles.Done;
                        break;
                    case PlayStyles.Done:
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true));
                        break;
                }
                break;
            case AgentType.Playback:
                agentType = AgentType.Human;
                StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true));
                break;
            case AgentType.ANN:
                annSaver.serializedANN = MLSerializer.SerializeANN(ann);
                RecreateRun();
                break;
            case AgentType.CNN:
                cnnSaver.serializedCNN = MLSerializer.SerializeCNN(cnn);
                RecreateRun();
                break;
        }
    }

    void Exploration() {
        float explored = 0;
        for (int i = 0; i < mapGenerator.blocksCreated.Count; i++) {
            if (mapGenerator.blocksCreated[i].isCasting) mapGenerator.blocksCreated[i].Cast();
            else explored++;
        }
        explorationPercentage = Mathf.Round(explored / (mapGenerator.blocksCreated.Count - 1) * 100);
        explorationText.text = "Exploration: " + explorationPercentage.ToString() + "%";
    }

    bool IsTheBorderInTheWay(Vector3 direction) {
        bool raycastResult = Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, direction), 1, ~9);
        return raycastResult;
    }

    void GetBlockDataFromBlockBelowAgent() {
        Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, Vector3.down), out RaycastHit hit, 3, ~LayerMask.NameToLayer("Block"));
        currentPositionBlockData = hit.collider.GetComponent<BlockData>();
    }

    public void AssessDataFromBlockBelowAgent() {
        switch (currentPositionBlockData.blockType) {
            case BlockType.LavaBlock:
                isAlive = false;
                if (audioSource != null && audioSource.enabled) {
                    audioSource.clip = clips[2];
                    audioSource.Play();
                }
                StartCoroutine(Feedback("LAVA! HOT, HOT, HOT!", true));
                break;
            case BlockType.Goal:
                didReachGoal = true;
                AgentReachedGoal();
                break;
            case BlockType.Treasure:
                if (!currentPositionBlockData.isFound) {
                    treasuresFound++;
                    currentPositionBlockData.isFound = true;
                    StartCoroutine(Feedback("You've found a treasure!", false));
                    treasureText.text = "Treasures found: " + treasuresFound + "/" + maxTreasuresInMap;
                }
                break;
        }
    }
    #endregion

    //Methods for Human setting
    #region
    IEnumerator InformPlayer() {
        isPreparing = true;
        if (!isNameSet) {
            nameField.SetActive(true);
            yield return new WaitUntil(() => isNameSet);
            nameField.SetActive(false);
            explanationTextBackground.SetActive(true);
            explanationText.text = "Thank you for helping me with my Master's\nThesis. Controls and necessary information\n" +
                "is shown on the right, and objectives are\nshown on the left. Remember to upload\nor send the generated text files " +
                "to me\nafter your runs!";
            yield return new WaitForSeconds(8);
        }

        switch (playstyle) {
            case PlayStyles.Speedrunner:
                //actionsText.gameObject.SetActive(true);
                explanationText.text = "For this run, play as a Speedrunner!\nReach the goal in as few actions as possible!";
                if (isRecordingData) mapGenerator.RecordMap();
                break;
            case PlayStyles.Explorer:
                explorationText.gameObject.SetActive(true);
                explanationText.text = "For this run, play as an Explorer!\nExplore the map as much as possible\nbefore you reach the goal!";
                if (isRecordingData) mapGenerator.RecordMap();
                break;
            case PlayStyles.Treasurehunter:
                treasureText.gameObject.SetActive(true);
                explanationText.text = "For this run, play as a Treasurehunter\nFind and step on all the treasures\nbefore you reach the goal!";
                if (isRecordingData) mapGenerator.RecordMap();
                break;
            case PlayStyles.Done:
                explanationText.text = "No more playstyles. Thanks for helping!\nRemember to send or upload the files for me!";
                break;
        }
        explanationTextBackground.SetActive(true);
        yield return new WaitForSeconds(3);
        explanationTextBackground.SetActive(false);
        isPreparing = false;
    }

    IEnumerator Feedback(string reason, bool reset, bool continuedRecording = false) {
        explanationText.text = reason;
        explanationTextBackground.SetActive(true);
        yield return new WaitForSeconds(3);
        explanationTextBackground.SetActive(false);
        if (reset) {
            ResetAgent();
            if (continuedRecording) StartCoroutine(InformPlayer());
        }
    }
    #endregion

    //Methods for AI
    #region
    public float[,] GetCurrentFloatMap() {
        float[,] visibleMap = new float[11, 11];
        LayerMask blockMask = LayerMask.GetMask("Block");
        int posX = (int)transform.position.x, posZ = (int)transform.position.z;
        for (int x = posX - 5, vmx = 0; x <= posX + 5; x++, vmx++) {                            //Go from left to right
            for (int z = posZ - 5, vmz = 0; z <= posZ + 5; z++, vmz++) {                            //Go from bottom to top
                Ray ray = new Ray(new Vector3(x, 1, z), Vector3.down * 4);
                Physics.Raycast(ray, out RaycastHit hit, 4, blockMask);                                 //Raycast downwards
                if (hit.collider != null) {
                    visibleMap[vmx, vmz] = GetBlockValue(hit.collider.GetComponent<BlockData>().blockType);
                } else {
                    visibleMap[vmx, vmz] = GetBlockValue(BlockType.None);
                }
            }
        }
        return visibleMap;
    }

    public List<BlockData> GetCurrentBlockMap() {
        List<BlockData> visibleMap = new List<BlockData>();
        LayerMask blockMask = LayerMask.GetMask("Block");
        int posX = (int)transform.position.x, posZ = (int)transform.position.z;
        for (int x = posX - 5, vmx = 0; x <= posX + 5; x++, vmx++) {                            //Go from left to right
            for (int z = posZ - 5, vmz = 0; z <= posZ + 5; z++, vmz++) {                            //Go from bottom to top
                Ray ray = new Ray(new Vector3(x, 1, z), Vector3.down * 4);
                Physics.Raycast(ray, out RaycastHit hit, 4, blockMask);                                 //Raycast downwards
                if (hit.collider != null) {
                    visibleMap.Add(hit.collider.GetComponent<BlockData>());
                }
            }
        }
        return visibleMap;
    }

    void FSMExecution() => nextMove = FSM.GetNextAction(currentPositionBlockData, GetRotationInt()).ToLower();

    public int GetRotationInt() {
        switch (Mathf.RoundToInt(transform.eulerAngles.y) % 360) {
            case 0:
                return 0;
            case 90:
                return 1;
            case 180:
                return 2;
            case 270:
                return 3;
            default:
                throw new System.ArgumentOutOfRangeException("The rotation is odd");
        }
    }

    public BlockData GetCurrentBlockData() { return currentPositionBlockData; }

    public void SetNextMove(string move) => nextMove = move;

    //Methods for networks (ANN/CNN)
    #region
    void FeedDataToNetwork(List<double> givenInput) {
        if (agentType == AgentType.Human || agentType == AgentType.Playback) return;
        float[,] visibleMap = GetCurrentFloatMap();

        List<double> outputs = null;
        switch (agentType) {
            case AgentType.ANN:
                if (isTraining) outputs = ann.Train(ann.GenerateANNInputs(new List<float[,]> { visibleMap }, "Visisble Area"), givenInput);
                else outputs = ann.Run(ann.GenerateANNInputs(new List<float[,]> { visibleMap }, "Visisble Area"));
                annOutput.serializedANN = MLDebugger.GetOutputAndReset();
                break;
            case AgentType.CNN:
                if (isTraining) {
                    if (cnnConfig == null) outputs = cnn.Train(visibleMap, givenInput);
                    else outputs = cnn.Train(visibleMap, givenInput, cnnConfig);
                } else {
                    if (cnnConfig == null) outputs = cnn.Run(visibleMap);
                    else outputs = cnn.Run(visibleMap, cnnConfig);
                }
                cnnOutput.serializedCNN = MLDebugger.GetOutputAndReset();
                break;
        }
        if (outputs != null) moveSuggestion.text = "Suggested move: " + GetMoveFromInt(GetIndexOfMaxOutput(outputs));
        nextMove = GetMoveFromInt(GetIndexOfMaxOutput(outputs)).ToLower();
        string s = "";
        foreach (double d in outputs) {
            s += d + "   ";
        }
        s += "\n" + GetIndexOfMaxOutput(outputs);
        Debug.Log(s);
    }

    int GetIndexOfMaxOutput(List<double> outputs) {
        if (outputs == null) return -1;
        double? max = null;
        int? index = null;
        for (int i = 0; i < outputs.Count; i++) {
            if (index == null) {
                index = i;
                max = outputs[i];
            } else if (outputs[i] > max) {
                max = outputs[i];
                index = i;
            }
        }
        return (int)index;
    }

    string GetMoveFromInt(int i) {
        switch (i) {
            case 0:
                return "w";
            case 1:
                return "a";
            case 2:
                return "s";
            case 3:
                return "d";
            default:
                return "Something went wrong!";
        }
    }

    static float GetBlockValue(BlockType type) {
        switch (type) {
            case BlockType.Goal:
                return 1f;
            case BlockType.LavaBlock:
                return -1;
            case BlockType.Platform:
                return 0f;
            case BlockType.Spawn:
                return 0f;
            case BlockType.Treasure:
                return 1f;
            default:
                return -1;
        }
    }

    public static float GetBlockValue(string type) {
        switch (type) {
            case "Goal":
                return GetBlockValue(BlockType.Goal);
            case "LavaBlock":
                return GetBlockValue(BlockType.LavaBlock);
            case "Platform":
                return GetBlockValue(BlockType.Platform);
            case "Spawn":
                return GetBlockValue(BlockType.Spawn);
            case "Treasure":
                return GetBlockValue(BlockType.Treasure);
            default:
                return GetBlockValue(BlockType.None);
        }
    }

    void AddCNNFilters(CNN cnn) {
        //Path filters
        #region
        float[,] pathHorizontal = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(pathHorizontal, "Path Horizontal");

        float[,] pathVertical = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(pathVertical, "Path Vertical");

        float[,] pathUpperRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(pathUpperRightCorner, "Path Upper Right Corner");

        float[,] pathLowerRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(pathLowerRightCorner, "Path Lower Right Corner");

        float[,] pathUpperLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(pathUpperLeftCorner, "Path Upper Left Corner");

        float[,] pathLowerLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(pathLowerLeftCorner, "Path Lower Left Corner");
        #endregion

        //Treasure filters
        #region
        float[,] treasureHorizontal = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Treasure), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(treasureHorizontal, "Treasure Horizontal");

        float[,] treasureVertical = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Treasure), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(treasureVertical, "Treasure Vertical");

        float[,] treasureUpperRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Treasure), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(treasureUpperRightCorner, "Treasure Upper Right Corner");

        float[,] treasureLowerRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Treasure), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(treasureLowerRightCorner, "Treasure Lower Right Corner");

        float[,] treasureUpperLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Treasure), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(treasureUpperLeftCorner, "Treasure Upper Left Corner");

        float[,] treasureLowerLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Treasure), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(treasureLowerLeftCorner, "Treasure Lower Left Corner");
        #endregion

        //Spawn filters
        #region
        float[,] spawnHorizontal = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Spawn), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(spawnHorizontal, "Spawn Horizontal");

        float[,] spawnVertical = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Spawn), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(spawnVertical, "Spawn Vertical");

        float[,] spawnUpperRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Spawn), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(spawnUpperRightCorner, "Spawn Upper Right Corner");

        float[,] spawnLowerRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Spawn), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(spawnLowerRightCorner, "Spawn Lower Right Corner");

        float[,] spawnUpperLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Spawn), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(spawnUpperLeftCorner, "Spawn Upper Left Corner");

        float[,] spawnLowerLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Spawn), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(spawnLowerLeftCorner, "Spawn Lower Left Corner");
        #endregion

        //Goal filters
        #region
        float[,] goalHorizontal = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Goal), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(goalHorizontal, "Goal Horizontal");

        float[,] goalVertical = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Goal), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(goalVertical, "Goal Vertical");

        float[,] goalUpperRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Goal), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(goalUpperRightCorner, "Goal Upper Right Corner");

        float[,] goalLowerRightCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Goal), GetBlockValue(BlockType.Platform) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(goalLowerRightCorner, "Goal Lower Right Corner");

        float[,] goalUpperLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Goal), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(goalUpperLeftCorner, "Goal Upper Left Corner");

        float[,] goalLowerLeftCorner = new float[3, 3] {
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.None), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.Goal), GetBlockValue(BlockType.None) },
            { GetBlockValue(BlockType.None), GetBlockValue(BlockType.Platform), GetBlockValue(BlockType.None) }
        };
        cnn.AddNewFilter(goalLowerLeftCorner, "Goal Lower Left Corner");
        #endregion
    }
    #endregion
    #endregion
}

public static class TrainingEmulator {
    private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    public static CNN EmulateTraining(this CNN cnn, CollectedData collectedData, Configuration.CNNConfig cnnConfig = null) {
        stopwatch.Start();
        Queue<string> actions = new Queue<string>(collectedData.recordedActions);
        int[] spawnPosition = new int[2];

        for (int i = 0; i < collectedData.recordedMap.Count; i++) {
            if (collectedData.recordedMap[i].Equals("Spawn")) {
                spawnPosition[0] = 0;
                spawnPosition[1] = i;
            }
        }

        int[] currentPosition = new int[2];
        currentPosition.CloneFrom(spawnPosition);
        int[] nextPosition = new int[2];
        nextPosition.CloneFrom(currentPosition);

        string action;
        string nextPositionInfo;
        int heading = 1;
        while (actions.Count > 0) {
            action = actions.Dequeue();
            if (action.IsFloat()) continue;

            heading = SetHeading(heading, action);
            if (action.Equals("w") || action.Equals("s")) nextPosition.GetNextPosition(heading, collectedData.mapSizeX, collectedData.mapSizeZ);
            else nextPosition.CloneFrom(currentPosition);
            nextPositionInfo = GetPositionInfo(new int[2].CloneFrom(currentPosition), collectedData.recordedMap, collectedData.mapSizeZ + 1);
            if (nextPositionInfo.Equals("LavaBlock")) currentPosition.CloneFrom(spawnPosition);
            else currentPosition.CloneFrom(nextPosition);

            float[,] visibleMap = GetVisibleMap(collectedData.recordedMap, collectedData.mapSizeZ + 1, nextPosition);
            if (cnnConfig == null)
                cnn.Train(visibleMap, GenerateDesiredOutputs(action));
            else
                cnn.Train(visibleMap, GenerateDesiredOutputs(action), cnnConfig);
        }

        stopwatch.Stop();
        Debug.Log("Emulation CNN training complete after: " + stopwatch.Elapsed.Seconds + "s");
        return cnn;
    }

    public static ANN EmulateTraining(this ANN ann, CollectedData collectedData) {
        stopwatch.Start();
        Queue<string> actions = new Queue<string>(collectedData.recordedActions);
        int[] spawnPosition = new int[2];

        for (int i = 0; i < collectedData.recordedMap.Count; i++) {
            if (collectedData.recordedMap[i].Equals("Spawn")) {
                spawnPosition[0] = 0;
                spawnPosition[1] = i;
            }
        }

        int[] currentPosition = new int[2];
        currentPosition.CloneFrom(spawnPosition);
        int[] nextPosition = new int[2];
        nextPosition.CloneFrom(currentPosition);

        string action;
        string nextPositionInfo;
        int heading = 1;
        while (actions.Count > 0) {
            action = actions.Dequeue();
            if (action.IsFloat()) continue;

            heading = SetHeading(heading, action);
            if (action.Equals("w") || action.Equals("s")) nextPosition.GetNextPosition(heading, collectedData.mapSizeX, collectedData.mapSizeZ);
            else nextPosition.CloneFrom(currentPosition);
            nextPositionInfo = GetPositionInfo(new int[2].CloneFrom(currentPosition), collectedData.recordedMap, collectedData.mapSizeZ + 1);
            if (nextPositionInfo.Equals("LavaBlock")) currentPosition.CloneFrom(spawnPosition);
            else currentPosition.CloneFrom(nextPosition);

            float[,] visibleMap = GetVisibleMap(collectedData.recordedMap, collectedData.mapSizeZ + 1, nextPosition);
            ann.Train(ann.GenerateANNInputs(new List<float[,]> { visibleMap }, "Current Position"), GenerateDesiredOutputs(action));
        }

        stopwatch.Stop();
        Debug.Log("Emulation ANN training complete after: " + stopwatch.Elapsed.Milliseconds + "ms");
        stopwatch.Reset();
        return ann;
    }

    private static int[] CloneFrom(this int[] to, int[] from) {
        if (to.Length != from.Length) {
            Debug.LogError("Arrays weren't the same size!");
            return to;
        }
        for (int i = 0; i < from.Length; i++) {
            to[i] = from[i];
        }
        return to;
    }

    private static int SetHeading(this int currentHeading, string input) {
        if (input.Equals("a")) {
            if (currentHeading == 0) currentHeading = 3;
            else currentHeading--;
        } else if (input.Equals("d")) {
            if (currentHeading == 3) currentHeading = 0;
            else currentHeading++;
        }
        return currentHeading;
    }

    private static int[] GetNextPosition(this int[] currentPosition, int currentHeading, int sizeX, int sizeZ) {
        switch (currentHeading) {
            case 0:
                currentPosition[1]++;
                if (currentPosition[1] > sizeZ) currentPosition[1] = sizeZ;
                return currentPosition;
            case 1:
                currentPosition[0]++;
                if (currentPosition[0] > sizeX) currentPosition[0] = sizeX;
                return currentPosition;
            case 2:
                currentPosition[1]--;
                if (currentPosition[1] < 0) currentPosition[1] = 0;
                return currentPosition;
            case 3:
                currentPosition[0]--;
                if (currentPosition[0] < 0) currentPosition[0] = 0;
                return currentPosition;
            default:
                throw new System.ArgumentException("Heading was outside of range!");
        }
    }

    private static string GetPositionInfo(int[] position, List<string> map, int mapSizeZ) {
        return map[mapSizeZ * position[0] + position[1]];
    }

    private static List<double> GenerateDesiredOutputs(string action) {
        switch (action) {
            case "w":
                return new List<double>() { 1, 0, 0, 0 };
            case "a":
                return new List<double>() { 0, 1, 0, 0 };
            case "s":
                return new List<double>() { 0, 0, 1, 0 };
            case "d":
                return new List<double>() { 0, 0, 0, 1 };
            default:
                throw new System.ArgumentException("Given actions was outside the allowed argument types!");
        }
    }

    private static float[,] GetVisibleMap(List<string> map, int mapSizeZ, int[] position) {
        float[,] visibleMap = new float[AgentController.gridVisibility, AgentController.gridVisibility];
        int offset = (int)Mathf.Floor(AgentController.gridVisibility / 2);
        int xCoord, zCoord, index;
        for (int x = -offset, vmx = 0; x < offset; x++, vmx++) {
            for (int z = -offset, vmz = 0; z < offset; z++, vmz++) {
                xCoord = position[0] + x * mapSizeZ;
                zCoord = position[1] + z;
                index = xCoord + zCoord;
                if (index > 0) visibleMap[vmx, vmz] = AgentController.GetBlockValue(map[index]);
                else visibleMap[vmx, vmz] = AgentController.GetBlockValue("");
            }
        }
        return visibleMap;
    }
}