/// <summary>
/// Defines how distances are calculated between two points on the road network.
/// Effects how the shortest paths between two points are drawn by the algorithm.
/// Euclidean: Straight line from A to B
/// Manhattan: Roads travel along either vertical or horizontal axes. Some bullshit is happening where it isn't absolute though, this is a bug.
/// </summary>
namespace Cigen.MetricConstraint {
    [System.Serializable]
    public enum MetricSpace {
        EUCLIDEAN,
        MANHATTAN,
        GRID,
    }
}