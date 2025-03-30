using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CubeDropper : MonoBehaviour
{

    public int numObjectsPerDrop = 5;
    public float secondsBetweenDrops = 5f;
    public int maxObjects = 200;
    public bool stopDrop = false;
    public float buffer = 45;
    private bool dropping = false;
    private int numObjects = 0;

    private List<Vector3> drops = new List<Vector3>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (buffer > transform.localScale.x/2 || buffer > transform.localScale.z/2) {
            Debug.LogError("Buffer too big, make it smaller than half the extents of the ground");
            stopDrop = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!dropping) StartCoroutine(Drop());
    }

    IEnumerator Drop() {
        dropping = true;
        if (stopDrop || numObjects >= maxObjects) {
            dropping = false;
            yield break;
        }
        for (int i = 0; i < numObjectsPerDrop; i++) {
            Vector3 dropPos = new Vector3(Random.Range(buffer-(transform.localScale.x/2),(transform.localScale.x/2)-buffer), Random.Range(45, 90), Random.Range(buffer-(transform.localScale.z/2), (transform.localScale.z/2)+buffer)) + transform.position;
            while (drops.Any(otherPos => Vector3.Distance(dropPos, otherPos) < 2*buffer)) {
                dropPos = new Vector3(Random.Range(buffer-(transform.localScale.x/2),(transform.localScale.x/2)-buffer), Random.Range(45, 90), Random.Range(buffer-(transform.localScale.z/2), (transform.localScale.z/2)+buffer)) + transform.position;
            }
            float scale = Random.Range(30f, 50f);
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.localScale = Vector3.one * scale;
            g.transform.position = dropPos;
            g.GetComponent<Renderer>().material.color = Random.ColorHSV(0, 1, .9f, 1, .9f, 1);
            g.AddComponent<Rigidbody>().useGravity = true;
            numObjects++;
            drops.Add(dropPos);
            yield return new WaitForSeconds(secondsBetweenDrops/numObjectsPerDrop);
        }
        dropping = false;
        yield break;
    }
}
