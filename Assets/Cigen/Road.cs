using System.Collections;
using UnityEngine;

public class Road : MonoBehaviour {
	public City city { get; private set; }
	public Intersection parentNode;
	public Intersection childNode;
    public bool built { get; private set; }

	public void Init(Intersection parent, Intersection child, City city) {
		this.parentNode = parent;
		this.childNode = child;
		this.city = city;
        this.built = false;
        transform.parent = parent.transform;
        StartCoroutine(Build());
	}

    public IEnumerator Build() {
        if (built) {
            yield break;
        }

        Vector3 start = parentNode.Position;
        Vector3 end = childNode.Position;
          
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Vector3 btwn = end - start;
        float dist = btwn.magnitude;
        Vector3 targetLocalScale = new Vector3(city.settings.roadDimensions.x, city.settings.roadDimensions.y, dist);
        Vector3 targetPosition = start + (btwn / 2f);    
        cube.transform.LookAt(end);
        System.Action setEndState = () =>
        {
            cube.transform.localScale = targetLocalScale;
            cube.transform.position = targetPosition;
            cube.transform.LookAt(end);
        };

        if (city.settings.animateRoadBuilding) {
            float zScale = 0.1f;
            Vector3 initScale = city.settings.roadDimensions;
            initScale.z = zScale;
            cube.transform.localScale = initScale;
            float timeToBuild = dist / city.settings.roadBuildSpeed;
            float currTime = 0f;
            while (currTime < timeToBuild) {
                cube.transform.position = Vector3.Lerp(start, targetPosition, currTime/timeToBuild);
                zScale = Mathf.Lerp(0.1f, dist, currTime/timeToBuild);
                {
                    Vector3 currScale = cube.transform.localScale;
                    currScale.z = zScale;
                    cube.transform.localScale = currScale;
                }
                cube.transform.LookAt(end);
                yield return new WaitForEndOfFrame();
                currTime += Time.deltaTime;
            }
        }

        setEndState();
        cube.transform.parent = transform;
        built = true;
        yield break;
    }
}
