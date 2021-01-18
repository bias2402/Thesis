using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public enum AgentAIType { None, ANN, Playback }

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour {
    [Header("AI Settings")]
    [SerializeField] private AgentAIType AIType = AgentAIType.None;
    [Space]
    [SerializeField] private TextAsset playerData = null;
    //[SerializeField] private BasicANNInitializer ANNInitializer = null;
    //[Space]
    [SerializeField] private DataCollector dataCollector = new DataCollector();
    [SerializeField] private bool recordData = false;
    private int completions = 0;
    [Space]
    private string playerName = "";
    private bool isNameSet = false;
    private float explorationPercentage = 0;
    [SerializeField] private GameObject nameField = null;
    [SerializeField] private InputField nameInput = null;

    [Header("Map & Agent")]
    private Rigidbody rb = null;
    [SerializeField] private FollowObject cameraFollower = null;
    [SerializeField] private MapGenerator mapGenerator = null;
    [SerializeField] private float moveCooldown = 1;
    private BlockData currentPositionBlockData = null;
    private bool didAgentMove = false;
    private bool isReadyToMove = true;
    [HideInInspector] public bool didReachGoal { get; private set; } = false;
    [HideInInspector] public bool isAlive = true;
    private float moveCooldownCounter = 1;

    [Header("UI")]
    [SerializeField] private Text explanationText = null;
    [SerializeField] private GameObject explanationTextBackground = null;
    [SerializeField] private Text explorationText = null;

    #region
    public void SetPlayerName() {
        playerName = nameInput.text;
        isNameSet = true;
    }

    public bool GetReadyToMoveState() { return isReadyToMove; }
    #endregion

    void Start() {
        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        mapGenerator.mapCompletedEvent += ResetAgent;

        if (AIType == AgentAIType.None) StartCoroutine(InformPlayer());
        if (AIType == AgentAIType.Playback) {
            StreamReader sr = new StreamReader("Assets/" + playerData.name + ".txt");
            string data = sr.ReadToEnd();
            mapGenerator.RecreateMap(JsonUtility.FromJson<CollectedData>(data));
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
        }
        mapGenerator.StartGeneration();

        switch (completions) {
            case 0:
                explanationText.text = "Play as a: Speedrunner\nGet to the goal as fast as you can!";
                break;
            case 1:
                explanationText.text = "Play as an: Explorer\nExplore the map before you reach the goal!";
                break;
            default:
                explanationText.text = "No more playstyles. Thanks for helping!\nRemember to send the files back to me!";
                break;
        }
        explanationTextBackground.SetActive(true);
        yield return new WaitForSeconds(3);
        explanationTextBackground.SetActive(false);
    }

    void ResetAgent() {
        if (recordData) mapGenerator.RecordMap();
        enabled = true;
        isReadyToMove = false;
        didReachGoal = false;
        isAlive = true;
        transform.position = mapGenerator.GetSpawnPoint() + Vector3.up;
        transform.rotation = Quaternion.Euler(0, 90, 0);
        moveCooldownCounter = moveCooldown * 3;
        rb.isKinematic = false;
        AccessBlockDataFromBlockBelowAgent();
        cameraFollower.enabled = true;
    }

    void Update() {
        if (!isAlive || didReachGoal) return;
        if (AIType == AgentAIType.None) {
            if (Input.GetKey(KeyCode.W) && isReadyToMove) {
                WalkForward();
                if (recordData) dataCollector.AddMoveToRecords("w");
            }

            if (Input.GetKey(KeyCode.S) && isReadyToMove) {
                WalkBackward();
                if (recordData) dataCollector.AddMoveToRecords("s");
            }

            if (Input.GetKey(KeyCode.A) && isReadyToMove) {
                TurnLeft();
                if (recordData) dataCollector.AddMoveToRecords("a");
            }

            if (Input.GetKey(KeyCode.D) && isReadyToMove) {
                TurnRight();
                if (recordData) dataCollector.AddMoveToRecords("d");
            }

            //if (Input.GetKey(KeyCode.Space) && isReadyToMove) {
            //    if (Interact(out BlockData blockData)) {
            //        switch (blockData.blockType) {
            //            case BlockData.BlockType.Boulder:
            //                blockData.gameObject.SetActive(false);
            //                break;
            //        }
            //    }
            //    if (Input.GetKeyDown(KeyCode.Return)) {
            //        ANNInitializer.CreateANN();
            //    }
            //}
        } else if (AIType == AgentAIType.Playback) {

        }

        if (didAgentMove) {
            didAgentMove = false;
            AccessBlockDataFromBlockBelowAgent();
            AssessDataFromBlockBelowAgent();
            Exploration();
        }

        if (!isReadyToMove) {
            if (moveCooldownCounter > 0) moveCooldownCounter -= Time.deltaTime;
            else {
                moveCooldownCounter = moveCooldown;
                isReadyToMove = true;
            }
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

    /// <summary>
    /// Returns information about the success of the movement
    /// </summary>
    /// <returns></returns>
    public bool WalkForward() {
        if (!IsTheBorderInTheWay(transform.forward)) {
            transform.Translate(Vector3.forward);
            didAgentMove = true;
            isReadyToMove = false;
            return true;
        } else return false;
    }

    /// <summary>
    /// Returns information about the success of the movement
    /// </summary>
    /// <returns></returns>
    public bool WalkBackward() {
        if (!IsTheBorderInTheWay(-transform.forward)) {
            transform.Translate(Vector3.back);
            didAgentMove = true;
            isReadyToMove = false;
            return true;
        } else return false;
    }

    public void TurnLeft() {
        transform.Rotate(new Vector3(0, -90, 0));
        didAgentMove = true;
        isReadyToMove = false;
    }

    public void TurnRight() {
        transform.Rotate(new Vector3(0, 90, 0));
        didAgentMove = true;
        isReadyToMove = false;
    }

    /// <summary>
    /// Returns information whether it hit something or not. Also outs the GameObject hit
    /// </summary>
    /// <returns></returns>
    //public bool Interact(out BlockData go) {
    //    bool hitSomething = Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, transform.forward), out RaycastHit hit, 1f, ~LayerMask.NameToLayer("Obstacle"));
    //    go = hit.collider != null ? hit.collider.GetComponent<BlockData>() : null;
    //    return hitSomething;
    //}

    bool IsTheBorderInTheWay(Vector3 direction) {
        bool raycastResult = Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, direction), 1, ~9);
        return raycastResult;
    }

    void AccessBlockDataFromBlockBelowAgent() {
        Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, Vector3.down), out RaycastHit hit, 3, ~LayerMask.NameToLayer("Block"));
        currentPositionBlockData = hit.collider.GetComponent<BlockData>();
    }

    public void AssessDataFromBlockBelowAgent() {
        switch (currentPositionBlockData.blockType) {
            case BlockData.BlockType.LavaBlock:
                isAlive = false;
                StartCoroutine(ResetPlayer("LAVA! HOT, HOT, HOT! OUCH!"));
                break;
            case BlockData.BlockType.Goal:
                didReachGoal = true;
                switch (completions) {
                    case 0:
                        dataCollector.CreateJSONFile("Speed_Runner_" + playerName, explorationPercentage);
                        break;
                    case 1:
                        dataCollector.CreateJSONFile("Explorer_" + playerName, explorationPercentage);
                        break;
                }
                completions++;
                StartCoroutine(ResetPlayer("Would you look at that. You actually made it.\nGood job!", true));
                break;
        }
    }

    IEnumerator ResetPlayer(string reason, bool newMap = false) {
        explanationText.text = reason;
        explanationTextBackground.SetActive(true);
        yield return new WaitForSeconds(3);
        explanationTextBackground.SetActive(false);
        if (newMap) StartCoroutine(InformPlayer());
        ResetAgent();
    }
}