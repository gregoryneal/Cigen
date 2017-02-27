using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.Factories;

public class Building : MonoBehaviour {

    public Plot plot { get; private set; }
    public GameObject obj { get; private set; }

    public void Init(Plot p) {
        this.plot = p;
        if (p == null) { 
            print("p null");
            Destroy(gameObject);
            return;
        }
        if (this.plot == null)
            print("this.plot null");
        if (this.plot.city == null)
            print("this.plot.city null");
        this.obj = MakeABuilding(p.city.settings.minBuildingSize, p.city.settings.maxBuildingSize);        
        obj.transform.rotation = p.transform.rotation;

        if (!p.PlaceBuilding(this)) {
            Destroy(obj);
            Destroy(gameObject);
            return;
        }

        transform.position = obj.transform.position;
        transform.rotation = obj.transform.rotation;
        obj.transform.SetParent(transform, true);
    }

    public GameObject MakeABuilding(Vector3 minSize, Vector3 maxSize) {
        GameObject ret = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ret.transform.localScale = new Vector3(Random.Range(minSize.x, maxSize.x), Random.Range(minSize.y, maxSize.y), Random.Range(minSize.z, maxSize.z));
        return ret;
    }
}
