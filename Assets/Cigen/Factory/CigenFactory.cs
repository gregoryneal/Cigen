using UnityEngine;
using Cigen.MetricConstraint;

namespace Cigen.Factories { 
    public class CigenFactory {
        public static Road CreateRoad(Intersection head, Intersection tail, string name = "Road") {
            Road road = new GameObject(name).AddComponent<Road>();
            road.Init(head, tail, head.city);
            return road;
        }

        public static RoadPath CreatePath(Intersection head, Intersection tail) {
            RoadPath rp = new RoadPath(head, tail);
            rp.BuildPath();
            return rp;
        }

	    public static Intersection CreateOrMergeIntersection(Vector3 position, City city, string name = "Intersection") {
            Intersection c = city.CreateOrMergeNear(position);
            c.name = name;
            return c;
        }

        public static City CreateCity(Vector3 position, CitySettings settings, string name = "City") {
            City temp = new GameObject(name).AddComponent<City>();
            temp.Init(position, settings);
            return temp;
        }

        public static Plot[] CreatePlots(Road road, string name = "Plot") {
            BoxCollider bc;

            Plot[] plots = new Plot[2];
            Plot plot = new GameObject(name, typeof(BoxCollider)).gameObject.AddComponent<Plot>();
            plot.Init(road, PlotRoadSide.PLOTLEFT);
            plots[0] = plot;
            bc = plot.GetComponent<BoxCollider>();
            bc.isTrigger = true;

            plot = new GameObject(name, typeof(BoxCollider)).AddComponent<Plot>();
            plot.Init(road, PlotRoadSide.PLOTRIGHT);
            plots[1] = plot;
            bc = plot.GetComponent<BoxCollider>();
            bc.isTrigger = true;            

            return plots;
        }

        public static Building CreateBuilding(Plot plot, string name = "Building") {
            Building b = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Building>();
            b.Init(plot);
            return b;
        }
    }

    public class MetricFactory {
        public static MetricConstraint.MetricConstraint Process(MetricSpace metric, CitySettings settings) {
            MetricConstraint.MetricConstraint m = null;

            switch (metric) {
                case MetricSpace.EUCLIDEAN:
                    m = new EuclideanConstraint(settings);
                    break;
                case MetricSpace.MANHATTAN:
                    m = new ManhattanConstraint(settings);
                    break;
                case MetricSpace.GRID:
                    m = new GridConstraint(settings);
                    break;
            }
            return m;
        }
    }
}
