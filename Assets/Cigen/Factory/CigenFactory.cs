using UnityEngine;
using Cigen.MetricConstraint;

namespace Cigen.Factories { 
    public class CigenFactory {
        public static Road CreateRoad(Intersection head, Intersection tail, string name = "Road") {
            Road road = new GameObject(name).AddComponent<Road>();
            road.Init(head, tail, head.city);
            head.AddRoad(road);
            tail.AddRoad(road);
            head.ConnectToIntersection(tail);
            return road;
        }

	    public static Intersection CreateIntersection(Vector3 position, City city, string name = "Intersection") {
            Intersection temp = new GameObject(name).AddComponent<Intersection>();
            temp.Init(position, city);
            return temp;
        }

        public static City CreateCity(Vector3 position, CiSettings settings, string name = "City") {
            City temp = new GameObject(name).AddComponent<City>();
            temp.Init(position, settings);
            return temp;
        }
    }

    public class MetricFactory {
        public static MetricConstraint.MetricConstraint Process(MetricSpace metric) {
            MetricConstraint.MetricConstraint m = null;

            switch (metric) {
                case MetricSpace.EUCLIDEAN:
                    m = new EuclideanConstraint();
                    break;
                case MetricSpace.MANHATTAN:
                    m = new ManhattanConstraint();
                    break;
            }
            return m;
        }
    }
}
