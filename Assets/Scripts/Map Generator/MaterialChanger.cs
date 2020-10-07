using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialChanger : MonoBehaviour {
    [SerializeField] private Texture[] textures = new Texture[0];
    [SerializeField] private float speed = 1; 
    
    private int index = 0;
    private Renderer render;
    private MapGenerator generator = null;

    void Start() {
        render = GetComponent<Renderer>();
        generator = FindObjectOfType<MapGenerator>();
        if (generator != null) {
            generator.materialUpdater += StartUpdater; //Subscribe to the delegate in MapGenerator
        }
    }

    public void StartUpdater() {
        StartCoroutine(UpdateMaterial());
    }
    
    //Iterate through the textures, one texture at a time, with a delay based on the variable called speed. At the end, start over by recalling
    IEnumerator UpdateMaterial() {
        render.material.mainTexture = textures[index];
        yield return new WaitForSecondsRealtime(speed);
        index++;
        if (index == textures.Length) {
            index = 0;
        }
        StartCoroutine(UpdateMaterial());
    }

    //When the script is disabled (or object is disabled/destroyed), unsubscribe from the delegate in MapGenerator
    void OnDisable() {
        if (generator != null) {
            generator.materialUpdater -= StartUpdater;
        }
    }
}