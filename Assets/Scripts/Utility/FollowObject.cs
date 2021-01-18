using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour {
    [SerializeField] private Transform target;
    [SerializeField] bool lockX = false;
    [SerializeField] bool lockY = false;
    [SerializeField] bool lockZ = false;

    private void Awake() => enabled = false;

    void Update() {
        if (target == null) throw new System.NullReferenceException("Target not set in " + gameObject.name);
        transform.position = new Vector3(lockX ? transform.position.x : target.position.x, 
                                         lockY ? transform.position.y : target.position.y,
                                         lockZ ? transform.position.z : target.position.z);
    }

    private void OnEnable() => transform.position = Vector3.up * 10;
}