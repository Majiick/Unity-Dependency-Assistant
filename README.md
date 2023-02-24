
# Unity-Dependency-Assistant
Adds functionality to find references to GameObjects and their components within the Unity editor.

This is useful if you're trying to figure out if a GameObject can be safely deleted for example. My use case was to untangle a project with spaghetti dependencies, and this functionality helped a lot to know where dependencies need to be changed.

# Example Output

![alt text](https://i.imgur.com/CQaS4b5.png)

![alt text](https://i.imgur.com/GOcaZd7.png)


# Usage
Drop the `.cs` files in your Unity project. Note that `DependencyAssistantMenu.cs` needs to be within `Assets/Editor`.

Unity Dependency Assistant will automatically find drag-and-drop references for any GameObject and all of their attached components. However, if you are using `GameObject.Find` in code, then you will have to annotate your fields with the `Reference` annotation. 

```
// For referencing individual components.
// The first argument is a path to the GameObject within the hierarchy.
// The second argument is the component which you want to get.
[Reference("Map/Player", typeof(MyPlayerController))]
MyPlayerController playerController;

// For referencing whole GameObjects.
[Reference("Map/Player")]
GameObject player;
```

Unity Dependency Assistant will **automatically set your fields** to the component or GameObject which you are referring to. This is done in an `OnLoad()` with a `[DefaultExecutionOrder(-100)]`.

# TODO
- Make this work at runtime so scripts that dynamically refer to other components can be found.
- Have an option to notify the user if a reference was broken due to them removing or moving a GameObject/component. I have a rudimentary routine that does this but it's not user friendly.
