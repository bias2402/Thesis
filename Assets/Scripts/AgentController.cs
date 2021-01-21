using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public enum AgentType { Human, ANN, Playback }
public enum PlayStyles { Speedrunner, Explorer, Treasurehunter, Done }

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour {
    [Header("Agent Setting")]
    [SerializeField] private AgentType AIType = AgentType.Human;
    private PlayStyles playstyle = PlayStyles.Speedrunner;

    [Header("AI Settings")]
    [Header("ANN")]
    [SerializeField] private BasicANNInitializer ANNInitializer = null;

    [Header("Recording & Playback")]
    [SerializeField] private DataCollector dataCollector = new DataCollector();
    [SerializeField] private bool isRecordingData = false;
    [SerializeField] private TextAsset playerData = null;
    private int completions = 0;
    private string playerName = "";
    private bool isNameSet = false;
    private float explorationPercentage = 0;
    private int treasuresFound = 0;
    private int maxTreasuresInMap = 0;
    private Queue<string> playbackActions;

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
    private bool isReadyToMove = true;
    private float moveCooldownCounter = 1;
    private int move = 0;

    [Header("UI")]
    [SerializeField] private Text explanationText = null;
    [SerializeField] private GameObject explanationTextBackground = null;
    [SerializeField] private Text explorationText = null;
    [SerializeField] private GameObject nameField = null;
    [SerializeField] private InputField nameInput = null;

    //Public get/Set methods
    #region
    public void SetPlayerName() {
        if (nameInput.text == null || nameInput.text == "") return;
        playerName = nameInput.text;
        isNameSet = true;
    }

    public bool GetReadyToMoveState() { return isReadyToMove; }

    public int SetMaxTreasures(int max) => maxTreasuresInMap = max;
    #endregion

    void Start() {
        audioSource = GetComponent<AudioSource>();
        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        mapGenerator.mapCompletedEvent += ResetAgent;

        if (AIType == AgentType.Human) {
            mapGenerator.StartGeneration();
            StartCoroutine(InformPlayer());
        }
        if (AIType == AgentType.Playback) {
            StreamReader sr = new StreamReader("Assets/" + playerData.name + ".txt");
            string data = sr.ReadToEnd();
            CollectedData collectedData = JsonUtility.FromJson<CollectedData>(data);
            mapGenerator.RecreateMap(collectedData);
            playbackActions = new Queue<string>(collectedData.recordedActions);
        }

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        enabled = false;
    }

    IEnumerator InformPlayer() {
        if (!isNameSet) {
            nameField.SetActive(true);
            yield return new WaitUntil(() => isNameSet);
            nameField.SetActive(false);
            explanationTextBackground.SetActive(true);
            explanationText.text = "Thank you for helping me with my Master's\nThesis. Controls and necessary information\n" +
                "is shown on the right, and objectives are\nshown on the left.Remember to upload\nor send the generated text files" +
                "to me\nafter your runs!";
            yield return new WaitForSeconds(8);
        }

        switch (playstyle) {
            case PlayStyles.Speedrunner:
                explanationText.text = "For this run, play as a Speedrunner!\nReach the goal in as few steps as possible!";
                break;
            case PlayStyles.Explorer:
                explanationText.text = "For this run, play as an Explorer!\nExplore the map (at least 95% of it) before you reach the goal!";
                break;
            case PlayStyles.Treasurehunter:
                explanationText.text = "For this run, play as a Treasurehunter\nFind and step on all the treasures before you reach the goal!";
                break;
            case PlayStyles.Done:
                explanationText.text = "No more playstyles. Thanks for helping!\nRemember to send or upload the files for me!";
                break;
        }
        explanationTextBackground.SetActive(true);
        yield return new WaitForSeconds(3);
        explanationTextBackground.SetActive(false);
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

    void ResetAgent() {
        if (isRecordingData) mapGenerator.RecordMap();
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
        if (!isAlive || didReachGoal || explanationTextBackground.activeSelf) return;

        if (isReadyToMove) AgentMovementHandling();
        else {
            transform.Translate(Vector3.forward * move * Time.deltaTime * (1 / moveCooldown));
        }

        if (!isReadyToMove) {
            if (moveCooldownCounter > 0) moveCooldownCounter -= Time.deltaTime;
            else {
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

    void AgentMovementHandling() {
        switch (AIType) {
            case AgentType.Human:
                if (Input.GetKey(KeyCode.W)) {
                    WalkForward();
                    if (isRecordingData) dataCollector.AddMoveToRecords("w");
                }
                if (Input.GetKey(KeyCode.S)) {
                    WalkBackward();
                    if (isRecordingData) dataCollector.AddMoveToRecords("s");
                }
                if (Input.GetKey(KeyCode.A)) {
                    TurnLeft();
                    if (isRecordingData) dataCollector.AddMoveToRecords("a");
                }
                if (Input.GetKey(KeyCode.D)) {
                    TurnRight();
                    if (isRecordingData) dataCollector.AddMoveToRecords("d");
                }
                break;
            case AgentType.Playback:
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
                }
                break;
            case AgentType.ANN:

                break;
            default:
                throw new System.NullReferenceException("AIType not properly set!");
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
            audioSource.clip = clips[0];
            audioSource.Play();
        }
    }

    public void WalkBackward() {
        if (!IsTheBorderInTheWay(-transform.forward)) {
            move = -1;
            isReadyToMove = false;
            audioSource.clip = clips[0];
            audioSource.Play();
        }
    }

    public void TurnLeft() {
        transform.Rotate(new Vector3(0, -90, 0));
        isReadyToMove = false;
        audioSource.clip = clips[1];
        audioSource.Play();
    }

    public void TurnRight() {
        transform.Rotate(new Vector3(0, 90, 0));
        isReadyToMove = false;
        audioSource.clip = clips[1];
        audioSource.Play();
    }

    void AgentReachedGoal() {
        switch (AIType) {
            case AgentType.Human:
                switch (playstyle) {
                    case PlayStyles.Speedrunner:
                        dataCollector.CreateJSONFile("Speed_Runner_" + playerName);
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true, true));
                        playstyle = PlayStyles.Explorer;
                        break;
                    case PlayStyles.Explorer:
                        dataCollector.CreateJSONFile("Explorer_" + playerName, explorationPercentage);
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true, true));
                        playstyle = PlayStyles.Treasurehunter;
                        break;
                    case PlayStyles.Treasurehunter:
                        dataCollector.CreateJSONFile("Treasurehunter_" + playerName, explorationPercentage, treasuresFound);
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true, true));
                        playstyle = PlayStyles.Done;
                        break;
                    case PlayStyles.Done:
                        StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true));
                        break;
                }
                break;
            case AgentType.Playback:
                //TO DO: Allow the agent to iterate through all the different data in the form of a list of text files
                AIType = AgentType.Human;
                StartCoroutine(Feedback("Would you look at that. You actually made it.\nGood job!", true));
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
                audioSource.clip = clips[2];
                audioSource.Play();
                StartCoroutine(Feedback("LAVA! HOT, HOT, HOT! OUCH!", true));
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
                }
                break;
        }
    }
}