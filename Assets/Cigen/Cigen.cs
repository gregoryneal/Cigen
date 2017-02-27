using System.Collections;
using UnityEngine;
using Cigen.Factories;
using System.Collections.Generic;

namespace Cigen
{
    public class Cigen : MonoBehaviour {

    #region Variables

    #region Public
    public CitySettings settings;
    #endregion

    #region Private
    private City city;
    #endregion

    #endregion

    #region Unity Methods

    void Start() {
        StartCoroutine(BuildCity(settings.initialCityPosition));
    }

    void OnDisable() {
        StopAllCoroutines();
    }

    #endregion

    public IEnumerator BuildCity(Vector3 initState) {
        if (settings == null) {
            Debug.LogError("No CigenSettings detected, drag and drop one into the inspector.");
            yield break;
        }

        //print("building city");
        int it = 0;
        transform.position = initState;
        city = CigenFactory.CreateCity(initState, settings);
        while (true) {
            it++;
            if (city.intersections.Count >= settings.maxNumberOfIntersections) {
                break;
            }
                        
            city.AddRandomIntersectionToRoadNetwork();            
            yield return new WaitUntil(()=>Input.GetKeyDown(KeyCode.Space));
        }
        foreach (Road road in city.roads) {
            if (road.length >= settings.minimumRoadLength) {
                road.ZonePlots();
                yield return new WaitForEndOfFrame();
            }
        }

        print("Generated " + city.intersections.Count + " intersections in " + it + " iterations.");
        yield break;
    }

    private Plot RandomPlot() {
        Intersection i = city.intersections[Random.Range(0, city.intersections.Count)];
        Road r = i.roads[Random.Range(0, i.roads.Count)];
        
        if (Random.value < 0.5f) {
            if (r.leftPlot == null)
                print("lplot null");
            return r.leftPlot;
        }
        
        if (r.rightPlot == null)
            print("rplot null");
        return r.rightPlot;
    }

    private void ConnectNodes(Intersection parent, Intersection child) {
        CigenFactory.CreateRoad(parent, child);
        //DrawLineBetweenPoints(parent.Position, child.Position);

    }

    #region Math methods

    #region Metric constraint methods

    #endregion

    #endregion
}
}
