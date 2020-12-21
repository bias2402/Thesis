using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour {
    [Header("Map & Agent")]
    [SerializeField] MapGenerator mapGenerator = null;
    [SerializeField] private float moveCooldown = 1;
    private BlockData currentPositionBlockData = null;
    private bool didAgentMove = false;
    private bool isReadyToMove = true;
    [HideInInspector] public bool didReachGoal { get; private set; } = false;
    [HideInInspector] public bool isAIControlled = false;
    [HideInInspector] public bool isAlive = true;
    private float moveCooldownCounter = 1;

    [Header("UI")]
    [SerializeField] private Text explanationText = null;
    [SerializeField] private GameObject explanationTextBackground = null;

    #region
    public bool GetReadyToMoveState() { return isReadyToMove; }
    #endregion

    void Start() {
        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        mapGenerator.mapCompletedEvent += Reset;
        gameObject.SetActive(false);
    }

    void Reset() {
        gameObject.SetActive(true);
        isReadyToMove = false;
        didReachGoal = false;
        isAlive = true;
        transform.position = mapGenerator.GetSpawnPoint() + Vector3.up;
        transform.rotation = Quaternion.Euler(0, 90, 0);
        moveCooldownCounter = moveCooldown * 3;
        AccessBlockDataFromBlockBelowAgent();
    }



    void Update() {
        if (!isAlive || didReachGoal) return;
        if (!isAIControlled) {
            if (Input.GetKey(KeyCode.W) && isReadyToMove) WalkForward();
            if (Input.GetKey(KeyCode.S) && isReadyToMove) WalkBackward();
            if (Input.GetKey(KeyCode.A) && isReadyToMove) TurnLeft();
            if (Input.GetKey(KeyCode.D) && isReadyToMove) TurnRight();
            if (Input.GetKey(KeyCode.Space) && isReadyToMove) {
                if (Interact(out BlockData blockData)) {
                    switch (blockData.blockType) {
                        case BlockData.BlockType.Boulder:
                            blockData.gameObject.SetActive(false);
                            break;
                    }
                }
            }
        }

        if (didAgentMove) {
            didAgentMove = false;
            AccessBlockDataFromBlockBelowAgent();
            AssessDataFromBlockBelowAgent();
        }

        if (!isReadyToMove) {
            if (moveCooldownCounter > 0) moveCooldownCounter -= Time.deltaTime;
            else {
                moveCooldownCounter = moveCooldown;
                isReadyToMove = true;
            }
        }
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
    public bool Interact(out BlockData go) {
        bool hitSomething = Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, transform.forward), out RaycastHit hit, 1f, ~LayerMask.NameToLayer("Obstacle"));
        go = hit.collider != null ? hit.collider.GetComponent<BlockData>() : null;
        return hitSomething;
    }

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
                StartCoroutine(ResetPlayer("Would you look at that. You actually made it.\nGood job!"));
                break;
        }
    }

    IEnumerator ResetPlayer(string reason) {
        explanationText.text = reason;
        explanationTextBackground.SetActive(true);
        yield return new WaitForSeconds(3);
        explanationTextBackground.SetActive(false);
        Reset();
    }
}