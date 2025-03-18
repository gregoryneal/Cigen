namespace Cigen {
    /// <summary>
    /// Type of masks that are used to sample points around a candidate point when constructing a road path.
    /// </summary>
    public enum PathMask {
        /// <summary>
        /// Evaluates discrete points in a circle around the position.
        /// </summary>
        SurfaceMask = 0,
        TunnelMask = 1,
        BridgeMask = 2,
    }
}