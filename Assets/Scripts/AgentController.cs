using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour {
    [SerializeField] MapGenerator mapGenerator = null;
    private Rigidbody body = null;
    private BlockData currentPositionBlockData = null;
    private bool didAgentMove = false;
    private bool isReadyToMove = true;
    [SerializeField] private float moveCooldown = 1;
    private float moveCooldownCounter = 1;
    [SerializeField] private Transform gunTransform = null;

    #region
    public bool GetReadyToMoveState() { return isReadyToMove; }
    #endregion

    void Start() {
        body = GetComponent<Rigidbody>();
        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        mapGenerator.mapCompletedEvent += Reset;
        gameObject.SetActive(false);
    }

    void Reset() {
        gameObject.SetActive(true);
        isReadyToMove = false;
        transform.position = mapGenerator.GetSpawnPoint() + Vector3.up;
        transform.rotation = Quaternion.Euler(0, 90, 0);
        moveCooldownCounter = moveCooldown;
        AccessBlockDataFromBlockBelowAgent();
    }



    void Update() {
        Debug.DrawRay(gunTransform.position, transform.forward * 2.5f);

        if (Input.GetKey(KeyCode.W) && isReadyToMove) WalkForward();
        if (Input.GetKey(KeyCode.S) && isReadyToMove) WalkBackward();
        if (Input.GetKey(KeyCode.A) && isReadyToMove) TurnLeft();
        if (Input.GetKey(KeyCode.D) && isReadyToMove) TurnRight();
        if (Input.GetKey(KeyCode.Space) && isReadyToMove) {
            if (Shoot(out GameObject objectHit)) {
                objectHit.SetActive(false);
            }
        }

        if (didAgentMove) {
            didAgentMove = false;
            AccessBlockDataFromBlockBelowAgent();
            if (currentPositionBlockData.blockType == BlockData.BlockType.LavaBlock) Reset();
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
    public bool Shoot(out GameObject go) {
        bool hitSomething = Physics.Raycast(new Ray(gunTransform.position, transform.forward), out RaycastHit hit, 2, ~LayerMask.NameToLayer("Obstacle"));
        go = hit.collider != null ? hit.collider.gameObject : null;
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
}