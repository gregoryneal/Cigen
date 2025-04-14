# Cigen
A procedural city generator built for Unity.

NOTE: ~~This project is pretty dead due to lost interest, I may pick it up in the future but as of now not likely.~~ **I am actively working on this project again.**

Goal: To provide procedural placement of roads and buildings for use in Unity games.

~~To use: Download this repository and open the unity project. Make sure there is at least one empty GameObject with a Cigen component attached. You need to supply a CitySettings asset to the Cigen component. To create one right click in your project window and click Create > Cigen > CitySettings. Then drag it into the Settings inspector variable on the Cigen component. Mess with your settings then press play.~~
The project is in a developmental state at present, it does not function as in the GIF. To explain this, the new direction consists of multiple high level projects transcribed from a few different academic papers. I am working on these one at a time and the current state of the project will reflect this as I move along.

If you must play with it, you can load either the main scene to see the new anisotropic least cost path finder, which is working within the project by running it and selecting two points on the generated map. You will watch the pathfinder generate a new path between the two points respecting the customizable terrain cost that you can set on the Road Pathfinder Settings object that should be located on one of the scene objects.

The other working scene is within the SceneClothoidExplorer folder and when you hit play you must click and drag along in the game window to generate a clothoid curve given the input points that are sampled. This curve will
be used to generate detailed control points for the road mesh once the path is solved in the pathfinder. I am however, making it a seperate project and will eventually move it into its own repo. I think many people would enjoy the clothoid curve generator with multiple generation methods depending on your use case.

Be patient the gif takes a few seconds to start.
![](http://i.imgur.com/dAkyvcl.gif)
