using System;
using System.Reflection;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

[AttributeUsage(AttributeTargets.Field)]
public class Reference : Attribute {
    public string refPath;
    public Type componentType;

    public Reference(string refPath) => this.refPath = refPath;
    public Reference(string refPath, Type component) {
        this.refPath = refPath;
        this.componentType = component;
    }
}

[DefaultExecutionOrder(-100)]
[ExecuteInEditMode]
public class DependencyAssistant : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnLoad() {  // Set all of the in script fields with Reference attribute to point to the correct GO/Component.
        // Find all running MonoBehavior in Editor hierarchy (the game is already running so they're instantiated but Start() didn't run maybe.).
        UnityEngine.Object[] allGameObjects = UnityEngine.Object.FindObjectsOfType(typeof(MonoBehaviour));
        foreach (var obj in allGameObjects) {
            // Get fields annotated with our custom Reference attribute.
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
                .Where(prop => Attribute.IsDefined(prop, typeof(Reference)));

            // Set the fields with Reference attribute.
            foreach (var field in fields) {
                var attributes = (Reference[])
                    field.GetCustomAttributes(typeof(Reference), false);
                foreach (var attr in attributes) {
                    GameObject loadedGameObj = GameObject.Find(attr.refPath);
                    Debug.Assert(loadedGameObj != null);

                    if (attr.componentType != null) {
                        var c = loadedGameObj.GetComponent(attr.componentType);
                        Debug.Assert(c != null);
                        field.SetValue(obj, c);
                    } else {
                        field.SetValue(obj, loadedGameObj);
                    }
                }
            }
        }
    }

    // Returns the list of references the selected GameObject has to other GameObjects.
    public static List<string> GetAllReferencesInHolding(GameObject go) {
        var goComponents = go.GetComponents<Component>().ToList();
        var componentIDs = new List<int>();
        foreach (var c in goComponents) {
            if (c == null) continue; // Happens if there's an empty script as in "Missing (Mono Script)"
            componentIDs.Add(c.GetInstanceID());
        }

        string goPath = GetGameObjectPath(go.transform);
        var ret = new List<string>();

        // Find the selected MonoBehavior(s) in Editor hierarchy.
        MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
        foreach (var b in behaviours) {
            // Get all drag-drop fields (Filter all non-public and non-UnityEngine.Object since they cannot be dragged and dropped.)
            var fields = b.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | /*BindingFlags.NonPublic |*/ BindingFlags.GetProperty)
                            .Where(prop => !Attribute.IsDefined(prop, typeof(Reference)) && typeof(UnityEngine.Object).IsAssignableFrom(prop.FieldType)); ;
            // Get all annotated fields.
            var refAnnotatedFields = b.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
                .Where(prop => Attribute.IsDefined(prop, typeof(Reference)));

            // Print out the fields from both drag-and-drop and annotated fields.
            foreach (var field in fields) {
                if (field.GetValue(b) == null) continue;

                var val = field.GetValue(b);
                Transform refTransform = null;
                if (val is Component) {
                    refTransform = (val as Component).transform;
                } else if (val is GameObject) { 
                    refTransform = (val as GameObject).transform; 
                }
                
                if (refTransform != null) {
                    ret.Add($"[DP] GameObject '{goPath}' Class: '{b.GetType()}' Field: '{field.Name}': has a reference to '{GetGameObjectPath(refTransform)}'.");
                } else {
                    // If we can't find a transform for it, maybe it's a reference to an unstantiated object in the project files.
                    ret.Add($"[DP] GameObject '{goPath}' Class: '{b.GetType()}' Field: '{field.Name}': has a reference to '{val} {AssetDatabase.GetAssetPath(val as Object)}'.");
                }
                
            }
            foreach (var field in refAnnotatedFields) {
                Reference refAttr = ((Reference[])field.GetCustomAttributes(typeof(Reference), false))[0];
                ret.Add($"[DP] GameObject '{goPath}' Class: '{b.GetType()}' Field: '{field.Name}': has a reference to '{refAttr.refPath}'.");
            }
        }

        return ret;
    }

    // Returns the list of references other GameObjects have to the selected GameObject.
    public static List<string> GetAllReferencesToGameObject(GameObject go) {
        var goComponents = go.GetComponents<Component>().ToList();
        var componentIDs = new List<int>();
        foreach (var c in goComponents) {
            if (c == null) continue; // Happens if there's an empty script as in "Missing (Mono Script)"
            componentIDs.Add(c.GetInstanceID());
        }

        string goPath = GetGameObjectPath(go.transform);
        var ret = new List<string>();

        // Find all running MonoBehavior in Editor hierarchy.
        UnityEngine.Object[] editorMonoBehaviours = UnityEngine.Object.FindObjectsOfType(typeof(MonoBehaviour));
        foreach (var mb in editorMonoBehaviours) {
            string objPath = GetGameObjectPath((mb as MonoBehaviour).transform);
            // Get all drag-drop fields (Filter all non-UnityEngine.Object since they cannot be dragged and dropped.)
            var fields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | /*BindingFlags.NonPublic |*/ BindingFlags.GetProperty)
                            .Where(prop => !Attribute.IsDefined(prop, typeof(Reference)) && typeof(UnityEngine.Object).IsAssignableFrom(prop.FieldType)); ;
            // Get all annotated fields.
            var refAnnotatedFields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty)
                .Where(prop => Attribute.IsDefined(prop, typeof(Reference)));

            // If an editor drag and drop references our selected GO or any of its components, then add it to list of references.
            foreach (var field in fields) {
                if (field.GetValue(mb) == null) continue;

                var refHashCode = field.GetValue(mb).GetHashCode();
                if (refHashCode == go.GetHashCode() || componentIDs.Contains(refHashCode)) {
                    ret.Add($"[DP] GameObject '{objPath}' Class: '{mb.GetType()}' Field: '{field.Name}': has a reference to '{goPath}'.");
                }
            }
            // If a field references our selected GO, then add it to list of references.
            foreach (var field in refAnnotatedFields) {
                Reference refAttr = ((Reference[])field.GetCustomAttributes(typeof(Reference), false))[0];
                if (goPath == refAttr.refPath) {
                    ret.Add($"[DP] GameObject '{objPath}' Class: '{mb.GetType()}' Field: '{field.Name}': has a reference to '{goPath}'.");
                }
            }
        }

        return ret;
    }

    private static string GetGameObjectPath(Transform transform) {
        string path = transform.name;
        while (transform.parent != null) {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}