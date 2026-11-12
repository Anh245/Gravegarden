using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GraveGardenExpandedMapBuilder
{
    private const string Output = "Assets/Art/Backgrounds/ExpansionUnity";

    [MenuItem("GraveGarden/Map/Create Expansion Layout (Preview)")]
    private static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogError("Hãy thoát Play trước khi tạo bản đồ."); return; }
        GardenCameraController controller = UnityEngine.Object.FindAnyObjectByType<GardenCameraController>();
        if (controller == null) { Debug.LogError("Mở scene Garden trước."); return; }
        var cameraData = new SerializedObject(controller);
        SpriteRenderer ground = cameraData.FindProperty("ground").objectReferenceValue as SpriteRenderer;
        if (ground == null || ground.sprite == null) { Debug.LogError("Camera chưa có Ground."); return; }
        if (controller.gameObject.scene.GetRootGameObjects().Length == 0) return;
        foreach (GameObject item in controller.gameObject.scene.GetRootGameObjects())
            if (item.name == "ExpandedRegions_Preview")
            { Selection.activeGameObject = item; Debug.LogWarning("Đã có ExpandedRegions_Preview; không tạo trùng."); return; }
        if (Mathf.Abs(ground.transform.eulerAngles.z) > .01f)
        { Debug.LogError("Ground phải không xoay để chuyển tọa độ từ Godot."); return; }

        Sprite violet, golden;
        try
        {
            violet = ImportSprite("Assets/Art/Backgrounds/violet_ground.png", "violet_ground.png");
            golden = ImportSprite("Assets/Art/Backgrounds/golden_ground.png", "golden_ground.png");
        }
        catch (Exception e) { Debug.LogError(e.Message); return; }
        Bounds basis = ground.bounds;
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tạo bố cục mở rộng GraveGarden");
        var root = new GameObject("ExpandedRegions_Preview");
        Undo.RegisterCreatedObjectUndo(root, "Tạo bản đồ");
        var violetRoot = Child(root.transform, "VioletDistrict");
        var goldenRoot = Child(root.transform, "GoldenDistrict");
        SpriteRenderer north = Ground(violetRoot, "VioletGround", violet, basis, -1408, ground);
        SpriteRenderer south = Ground(goldenRoot, "GoldenGround", golden, basis, 4480, ground);
        Marker(violetRoot, "VioletHallAnchor", basis, 4730, -1408);
        Marker(goldenRoot, "GoldenHallAnchor", basis, 1700, 4480);
        Route(violetRoot, "VioletUpper", GraveGardenExpansionGeometry.VioletUpper, basis);
        Route(violetRoot, "VioletLower", GraveGardenExpansionGeometry.VioletLower, basis);
        Route(goldenRoot, "GoldenUpper", GraveGardenExpansionGeometry.GoldenUpper, basis);
        Route(goldenRoot, "GoldenLower", GraveGardenExpansionGeometry.GoldenLower, basis);
        root.SetActive(false);
        WorldMapBounds map = controller.GetComponent<WorldMapBounds>();
        if (map == null) map = Undo.AddComponent<WorldMapBounds>(controller.gameObject);
        Undo.RecordObject(map, "Gán các nền");
        var mapData = new SerializedObject(map);
        var grounds = mapData.FindProperty("grounds");
        grounds.arraySize = 3;
        grounds.GetArrayElementAtIndex(0).objectReferenceValue = ground;
        grounds.GetArrayElementAtIndex(1).objectReferenceValue = north;
        grounds.GetArrayElementAtIndex(2).objectReferenceValue = south;
        mapData.ApplyModifiedProperties();
        Undo.RecordObject(controller, "Gán giới hạn camera");
        cameraData.Update();
        cameraData.FindProperty("worldBounds").objectReferenceValue = map;
        cameraData.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Undo.CollapseUndoOperations(undo);
        Selection.activeGameObject = root;
        Debug.Log("Đã tạo hai nền, bốn tuyến và hai mốc nhà chính. Bật ExpandedRegions_Preview để xem; chưa gán vào WaveDirector. Ctrl+S để lưu scene.", root);
    }

    private static Transform Child(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private static Vector3 Convert(Bounds b, Vector2 p)
    {
        return new Vector3(b.min.x + p.x * b.size.x / 5504f,
                           b.max.y - p.y * b.size.y / 3072f, b.center.z);
    }

    private static SpriteRenderer Ground(Transform parent, string name, Sprite sprite, Bounds b, float y, SpriteRenderer original)
    {
        Transform obj = Child(parent, name);
        obj.position = Convert(b, new Vector2(2752, y));
        obj.localScale = new Vector3(b.size.x / sprite.bounds.size.x, b.size.y / sprite.bounds.size.y, 1);
        var renderer = obj.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = original.sharedMaterial;
        renderer.sortingLayerID = original.sortingLayerID;
        renderer.sortingOrder = original.sortingOrder + 1;
        return renderer;
    }

    private static void Marker(Transform parent, string name, Bounds b, float x, float y)
    {
        Transform marker = Child(parent, name);
        marker.position = Convert(b, new Vector2(x, y));
        marker.gameObject.AddComponent<ExpansionHallMarker>();
    }

    private static void Route(Transform parent, string name, Vector2[] coordinates, Bounds b)
    {
        Transform obj = Child(parent, name);
        var route = obj.gameObject.AddComponent<EnemyRoute>();
        var data = new SerializedObject(route);
        var points = data.FindProperty("points");
        points.arraySize = coordinates.Length;
        for (int i = 0; i < coordinates.Length; i++)
        {
            Transform point = Child(obj, "Point_" + i.ToString("D3"));
            point.position = Convert(b, coordinates[i]);
            points.GetArrayElementAtIndex(i).objectReferenceValue = point;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite ImportSprite(string source, string file)
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(source) == null)
            throw new InvalidOperationException("Thiếu ảnh: " + source);
        if (!AssetDatabase.IsValidFolder(Output))
            AssetDatabase.CreateFolder("Assets/Art/Backgrounds", "ExpansionUnity");
        string target = Output + "/" + file;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(target) == null && !AssetDatabase.CopyAsset(source, target))
            throw new InvalidOperationException("Không sao chép được " + source);
        var importer = AssetImporter.GetAtPath(target) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePivot = new Vector2(.5f, .5f);
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = Vector4.zero;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 8192;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(target);
        if (sprite == null) throw new InvalidOperationException("Không đọc được sprite " + target);
        return sprite;
    }
}

