using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DependencyAssistantMenu {
    [MenuItem("GameObject/Find All References to this gameobject", false, 0)]
    private static void FindAllReferences(MenuCommand menuCommand) {
        GameObject selected = Selection.activeObject as GameObject;
        var refs = DependencyAssistant.GetAllReferencesToGameObject(selected);
        foreach (var r in refs) {
            Debug.Log(r);
        }
        if (refs.Count == 0) Debug.Log("No references found.");
    }

    [MenuItem("GameObject/Find All References to this gameobject (Including children)", false, 0)]
    private static void FindAllReferencesRecursively(MenuCommand menuCommand) {
        GameObject selected = Selection.activeObject as GameObject;
        var children = selected.transform.GetTransformAndAllChildren();

        bool found = false;
        foreach (var c in children) {
            var refs = DependencyAssistant.GetAllReferencesToGameObject(c.gameObject);
            foreach (var r in refs) {
                found = true;
                Debug.Log(r);
            }
        }

        if (!found) {
            Debug.Log("No references found.");
        }
    }

    [MenuItem("GameObject/Find all gameobjects that this gameobject references.", false, 0)]
    private static void GetAllReferencesInHolding(MenuCommand menuCommand) {
        GameObject selected = Selection.activeObject as GameObject;
        var refs = DependencyAssistant.GetAllReferencesInHolding(selected);
        foreach (var r in refs) {
            Debug.Log(r);
        }
        if (refs.Count == 0) Debug.Log("No references found.");
    }

    [MenuItem("GameObject/Find all gameobjects that this gameobject references. (Including children)", false, 0)]
    private static void GetAllReferencesInHoldingRecursively(MenuCommand menuCommand) {
        GameObject selected = Selection.activeObject as GameObject;
        var children = selected.transform.GetTransformAndAllChildren();

        bool found = false;
        foreach (var c in children) {
            var refs = DependencyAssistant.GetAllReferencesInHolding(c.gameObject);
            foreach (var r in refs) {
                found = true;
                Debug.Log(r);
            }
        }

        if (!found) {
            Debug.Log("No references found.");
        }
    }
}

public static class TransformHelper {
    public static List<Transform> GetTransformAndAllChildren(this Transform aTransform, List<Transform> aList = null) {
        if (aList == null)
            aList = new List<Transform>();
        aList.Add(aTransform);
        for (int n = 0; n < aTransform.childCount; n++)
            aTransform.GetChild(n).GetTransformAndAllChildren(aList);
        return aList;
    }
}