using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(BuildGrid))]
public class BuildGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        BuildGrid grid = (BuildGrid)target;
        EditorGUILayout.HelpBox("Tạo ô từ prefab BuildSlot. Ô bị xóa hoặc Buildable tắt sẽ chặn xây. Đặt Cell Size=0.64 để khớp Godot.", MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (grid.ExpansionZonesAuthored)
                EditorGUILayout.HelpBox("Lưới đã có vùng mở rộng. Không tạo phủ kín hình chữ nhật vì sẽ lấp lại đường đi. Chỉnh từng ô hiện có nếu cần.", MessageType.Info);
            else if (GUILayout.Button("Tạo các ô còn thiếu / căn lại ô")) Generate(grid);
        }
    }

    private void Generate(BuildGrid grid)
    {
        if (grid.SlotPrefab == null || grid.Manager == null || !grid.HasValidTransform
            || grid.Columns < 1 || grid.Rows < 1 || grid.CellSize <= 0f
            || !PrefabUtility.IsPartOfPrefabAsset(grid.SlotPrefab))
        {
            Debug.LogError("Gán Slot Prefab từ Project, BuildManager; kiểm tra Rotation=0, Scale thế giới=1 và kích thước lưới.", grid);
            return;
        }
        var existing = new Dictionary<Vector2Int, BuildSlot>();
        foreach (BuildSlot slot in grid.GetComponentsInChildren<BuildSlot>(true))
        {
            if (slot.Grid != grid || existing.ContainsKey(slot.Cell)
                || slot.Cell.x < 0 || slot.Cell.y < 0 || slot.Cell.x >= grid.Columns || slot.Cell.y >= grid.Rows)
            {
                Debug.LogError("Có ô sai Grid, trùng tọa độ hoặc ngoài kích thước mới. Sửa các ô đó trước.", slot);
                return;
            }
            existing.Add(slot.Cell, slot);
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Tạo/căn ô lưới xây");
        for (int y = 0; y < grid.Rows; y++)
        for (int x = 0; x < grid.Columns; x++)
        {
            Vector2Int coordinate = new Vector2Int(x, y);
            if (!existing.TryGetValue(coordinate, out BuildSlot slot))
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(grid.SlotPrefab.gameObject, grid.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Tạo ô xây");
                slot = instance.GetComponent<BuildSlot>();
            }
            foreach (Component component in slot.GetComponentsInChildren<Component>(true))
                if (component != null) Undo.RecordObject(component, "Căn ô xây");
            slot.gameObject.name = $"Cell_{x:00}_{y:00}";
            slot.ConfigureAuthoring(grid, coordinate, grid.Manager, grid.CellSize);
            foreach (Component component in slot.GetComponentsInChildren<Component>(true))
                if (component != null)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    EditorUtility.SetDirty(component);
                }
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
    }
}
