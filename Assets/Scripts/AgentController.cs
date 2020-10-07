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
        GetBlockDataFromBlockBelowAgent();
    }

    void Update() {
        if (Input.GetKey(KeyCode.W) && isReadyToMove) {
            if (CanIMoveThisDirection(Vector3.forward)) {
                transform.position += Vector3.forward;
                didAgentMove = true;
                isReadyToMove = false;
            }
        }

        if (Input.GetKey(KeyCode.A) && isReadyToMove) {
            if (CanIMoveThisDirection(Vector3.left)) {
                transform.position += Vector3.left;
                didAgentMove = true;
                isReadyToMove = false;
            }
        }

        if (Input.GetKey(KeyCode.S) && isReadyToMove) {
            if (CanIMoveThisDirection(Vector3.back)) {
                transform.position += Vector3.back;
                didAgentMove = true;
                isReadyToMove = false;
            }
        }

        if (Input.GetKey(KeyCode.D) && isReadyToMove) {
            if (CanIMoveThisDirection(Vector3.right)) {
                transform.position += Vector3.right;
                didAgentMove = true;
                isReadyToMove = false;
            }
        }

        if (didAgentMove) {
            didAgentMove = false;
            GetBlockDataFromBlockBelowAgent();
            if (currentPositionBlockData.blockType == BlockData.BlockType.LavaBlock) Reset();
        }

        if (!isReadyToMove) {
            if (moveCooldownCounter > 0) {
                moveCooldownCounter -= Time.deltaTime;
            } else {
                moveCooldownCounter = moveCooldown;
                isReadyToMove = true;
            }
        }
    }

    bool CanIMoveThisDirection(Vector3 direction) {
        bool raycastResult = Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, direction), out RaycastHit hit, 1, ~LayerMask.NameToLayer("Border Block"));
        return !raycastResult;
    }

    void GetBlockDataFromBlockBelowAgent() {
        Physics.Raycast(new Ray(transform.position + Vector3.up * 0.25f, Vector3.down), out RaycastHit hit, 3, ~LayerMask.NameToLayer("Block"));
        currentPositionBlockData = hit.collider.GetComponent<BlockData>();
    }
}