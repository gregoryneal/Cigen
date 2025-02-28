namespace Cigen {
    
    //HighwayTypes are how we can configure global highway generation goals. It is controlled via a Texture2D
    //set in the city settings object. The 3 color channels can be converted to a HighwayType as follows:
    //R -> Ring Roads
    //G -> 
    [System.Serializable]
    public enum HighwayType {
        //this type of highway will encircle the population center. 
        //it can be further varied by a parameter setting that controls
        //how much the ring road should follow the population center contour
        //vs how much the ring road should just be an ellipse whose dimensions
        //are the same as the bounding box of the rect. Corresponds to the red 
        //channel.
        RING=0,
        //a throughpass is the same as bypass but there can be a highway
        //right in the middle of the population center. Corresponds to the 
        //green channel.
        THROUGHPASS=1,
        //a bypass will have some highways that pass near the centroid of the
        //population center, but never through the middle of it. It will start
        //outside of the PC, closest to the edge of the texture, along the axis
        //that is longer. It will shoot rays through the PC, following local 
        //constraints (max grade, marking bridges and tunnels as needed).
        //Corresponds to the blue channel.
        BYPASS=2,
    }

    //Segment types differentiate the type of generated road segments. Add any new ones here
    [System.Serializable]
    public enum SegmentType {
        Highway = 0,
        Street = 1, 
        Path = 2,
    }
}