# Cigen
A procedural city generator built for Unity.

NOTE: ~~This project is pretty dead due to lost interest, I may pick it up in the future but as of now not likely.~~ **I am actively working on this project again.**

Goal: To provide procedural placement of roads and buildings for use in Unity games.

~~To use: Download this repository and open the unity project. Make sure there is at least one empty GameObject with a Cigen component attached. You need to supply a CitySettings asset to the Cigen component. To create one right click in your project window and click Create > Cigen > CitySettings. Then drag it into the Settings inspector variable on the Cigen component. Mess with your settings then press play.~~
The project is in a developmental state at present, it does not function as in the GIF.

If you must play with it, you can load one of two scenes. The "main" scene will allow you to see the new anisotropic least cost path finder, which is working within the project by running it and selecting two points on the generated map with left and right click. You will watch the pathfinder generate a new path between the two points respecting the customizable terrain cost that you can set on the Road Pathfinder Settings object that should be located on one of the scene objects.

The other working scene is within the SceneClothoidExplorer folder and when you hit play you must click and drag along in the game window to generate a clothoid curve given the input points that are sampled. This curve will
be used to generate detailed control points for the road mesh once the path is solved in the pathfinder. I am however, making it a seperate project and will eventually move it into its own repo. I think many people would enjoy the clothoid curve generator with multiple generation methods depending on your use case.

This videos showcases an earlier version of the anisotropic least cost path finder. It shows the bidirectional search pathing through the terrain. Notice that it stops at the red zone which indicates water. The algorithm will determine the least cost path that includes bridges and tunnels should the user enable them, each with configurable relative costs. Cost weights can be changed dynamically during the search to favor different areas, though as of present this feature isn't easily accessible. Once the path is solved you can save the selected nodes, which will be flagged with information such as if its a bridge or tunnel, or other important info. The goal is to incorporate the clothoid curve generator to use the control points to generate smooth realistic road paths. Then ultimately the path finder can be used as a road network generator for entire cityscapes and terrains. 
<!---![](http://i.imgur.com/dAkyvcl.gif)--->


https://github.com/user-attachments/assets/17ca2183-7d68-4681-91e5-c813df2bbd78

