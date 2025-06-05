# Cigen
A procedural city generator built for Unity.

Goal: To provide **realistic** and **configurable** procedural placement of roads for use in Unity games.

The "main" scene will allow you to see the new anisotropic least cost path finder, which is working within the project by running it and selecting two points on the generated map with left and right click. You will watch the pathfinder generate a new path between the two points respecting the customizable terrain cost that you can set on the Road Pathfinder Settings object that should be located on one of the scene objects.

This videos showcases an earlier version of the anisotropic least cost path finder. It shows the bidirectional search pathing through the terrain. Notice that it stops at the red zone which indicates water. The algorithm will determine the least cost path that includes bridges and tunnels should the user enable them, each with configurable relative costs. Cost weights can be changed dynamically during the search to favor different areas, though as of present this feature isn't easily accessible. Once the path is solved you can save the selected nodes, which will be flagged with information such as if its a bridge or tunnel, or other important info. The goal is to incorporate a clothoid curve generator to use the control points to generate smooth realistic road paths. Then ultimately the path finder can be used as a road network generator for entire cityscapes and terrains. 

Notice in this video: the final generated path really shows the value in selecting a good distance heuristic for the A* pathfinder, the generated path, while quickly computed, is clearly not optimal.
<!---![](http://i.imgur.com/dAkyvcl.gif)--->


https://github.com/user-attachments/assets/17ca2183-7d68-4681-91e5-c813df2bbd78

