using UnityEditor;
using UnityEngine;

public static class PlatformCreator
{
    [MenuItem("GameObject/3D Object/Green Platform", false, 0)]
    public static void CreateGreenPlatform()
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "GreenPlatform";
        platform.transform.position = Vector3.zero;
        platform.transform.localScale = new Vector3(10f, 0.5f, 10f);

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (material.shader.name == "Hidden/InternalErrorShader")
            material = new Material(Shader.Find("Standard"));

        material.color = new Color(0.18f, 0.65f, 0.18f);
        platform.GetComponent<Renderer>().material = material;

        Undo.RegisterCreatedObjectUndo(platform, "Create Green Platform");
        Selection.activeGameObject = platform;

        Debug.Log("Green Platform created at (0, 0, 0) with scale (10, 0.5, 10).");
    }
}
