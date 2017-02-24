using System.Collections;
using UnityEngine;
using Cigen.Factories;

namespace Cigen
{
    public class Cigen : MonoBehaviour {

    #region Variables

    #region Public
    public CiSettings settings;
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
            city.AddRandomIntersectionToRoadNetwork();            

            if (settings.numberOfIntersections > 0 && it >= settings.numberOfIntersections) {
                break;
            }
            yield return new WaitForEndOfFrame();
            it++;
        }
        print("Broke loop after " + it + " iterations.");
        yield break;
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
