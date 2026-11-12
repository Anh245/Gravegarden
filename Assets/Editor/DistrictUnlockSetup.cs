using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DistrictUnlockSetup : EditorWindow
{
    private int patchWidth = 24;
    private int patchHeight = 24;
    private float roadClearance = 1.25f;
    private float hallClearance = 1.6f;

    [MenuItem("GraveGarden/Map/Prepare Chapter 1 Expansion")]
    private static void Open() => GetWindow<DistrictUnlockSetup>("Mở hai khu");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Thêm hai vùng xây vào MỘT BuildGrid hiện có, giữ vị trí ô cũ và gán hệ thống mở khu. Chạy ngoài Play và sao lưu scene trước.", MessageType.Info);
        patchWidth = EditorGUILayout.IntSlider("Số cột mỗi vùng", patchWidth, 12, 40);
        patchHeight = EditorGUILayout.IntSlider("Số hàng mỗi vùng", patchHeight, 12, 40);
        roadClearance = EditorGUILayout.FloatField("Cách tim đường (world)", roadClearance);
        hallClearance = EditorGUILayout.FloatField("Cách nhà chính (world)", hallClearance);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("Chuẩn bị ô và gán mở khu")) Prepare();
    }

    private static T Single<T>(Scene scene) where T : Component
    {
        T result = null;
        foreach (T item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if (item.gameObject.scene == scene)
            {
                if (result != null) throw new InvalidOperationException("Cần đúng một " + typeof(T).Name + " trong Garden.");
                result = item;
            }
        if (result == null) throw new InvalidOperationException("Thiếu " + typeof(T).Name);
        return result;
    }

    private static GameObject Named(Scene scene, string name)
    {
        GameObject result = null;
        foreach (var root in scene.GetRootGameObjects())
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == name)
            {
                if (result != null) throw new InvalidOperationException("Tên bị trùng: " + name);
                result = item.gameObject;
            }
        if (result == null) throw new InvalidOperationException("Không tìm thấy " + name);
        return result;
    }

    private void Prepare()
    {
        Scene scene = SceneManager.GetActiveScene();
        int undo = -1;
        try
        {
            if (scene.name != "Garden") throw new InvalidOperationException("Mở và chọn scene Garden trước.");
            BuildGrid grid = Single<BuildGrid>(scene);
            if (grid.ExpansionZonesAuthored) { Debug.LogWarning("Đã chuẩn bị vùng mở rộng. Không tạo trùng.", grid); return; }
            WaveDirector waves = Single<WaveDirector>(scene);
            DistrictDefenseRoutes routes = Single<DistrictDefenseRoutes>(scene);
            PlayerWallet wallet = Single<PlayerWallet>(scene);
            GameObject regions = Named(scene, "ExpandedRegions_Preview");
            TownHallHealth primary = Named(scene, "HeartCrypt").GetComponent<TownHallHealth>();
            TownHallHealth violet = Named(scene, "VioletCrypt").GetComponent<TownHallHealth>();
            TownHallHealth golden = Named(scene, "GoldenCrypt").GetComponent<TownHallHealth>();
            var violetBar = Named(scene, "VioletHallHealthBar");
            var goldenBar = Named(scene, "GoldenHallHealthBar");
            var violetGround = Named(scene, "VioletGround").GetComponent<SpriteRenderer>();
            var goldenGround = Named(scene, "GoldenGround").GetComponent<SpriteRenderer>();
            if (primary == null || violet == null || golden == null || violetGround == null || goldenGround == null)
                throw new InvalidOperationException("Kiểm tra TownHallHealth và SpriteRenderer của hai nền.");
            if (!grid.HasValidTransform || grid.SlotPrefab == null || grid.Manager == null
                || !PrefabUtility.IsPartOfPrefabAsset(grid.SlotPrefab))
                throw new InvalidOperationException("Grid phải Scale=1, Rotation=0, có Slot Prefab từ Project và BuildManager.");
            if (roadClearance < 0 || hallClearance < 0 || float.IsNaN(roadClearance) || float.IsNaN(hallClearance)
                || float.IsInfinity(roadClearance) || float.IsInfinity(hallClearance))
                throw new InvalidOperationException("Khoảng cách phải hữu hạn và không âm.");
            if (!violet.transform.IsChildOf(regions.transform) || !golden.transform.IsChildOf(regions.transform)
                || grid.transform.IsChildOf(regions.transform) || primary.transform.IsChildOf(regions.transform))
                throw new InvalidOperationException("Hai nhà phụ phải ở trong root mở rộng; Grid và HeartCrypt ở ngoài.");
            var bindings = routes.ConfiguredBindings;
            if (bindings.Length != 6) throw new InvalidOperationException("Gán đủ sáu dòng DistrictDefenseRoutes trước.");
            var seenRoutes = new HashSet<EnemyRoute>();
            int nPrimary=0, nViolet=0, nGolden=0;
            foreach (var binding in bindings)
            {
                if (binding.route == null || binding.hall == null || !binding.route.IsValid || !seenRoutes.Add(binding.route))
                    throw new InvalidOperationException("Bảng phân tuyến thiếu tham chiếu, thiếu điểm hoặc trùng tuyến.");
                if (binding.hall==primary) nPrimary++;
                else if(binding.hall==violet) nViolet++;
                else if(binding.hall==golden) nGolden++;
                else throw new InvalidOperationException("Bảng phân tuyến có nhà không đúng.");
            }
            if(nPrimary!=2 || nViolet!=2 || nGolden!=2) throw new InvalidOperationException("Mỗi nhà cần đúng hai tuyến.");
            var oldSlots = grid.GetComponentsInChildren<BuildSlot>(true);
            if (oldSlots.Length == 0) throw new InvalidOperationException("Lưới hiện tại chưa có ô.");
            var existing = new HashSet<Vector2Int>();
            foreach (var slot in oldSlots)
                if (slot.Grid != grid || !existing.Add(slot.Cell) || slot.Cell.x<0 || slot.Cell.y<0
                    || slot.Cell.x>=grid.Columns || slot.Cell.y>=grid.Rows)
                    throw new InvalidOperationException("Ô cũ có tọa độ trùng hoặc sai Grid/kích thước.");
            float size = grid.CellSize;
            Vector3 origin = grid.transform.position;
            Bounds[] grounds = { SpriteBounds(violetGround), SpriteBounds(goldenGround) };
            Vector3[] halls = { violet.transform.position, golden.transform.position };
            Vector2Int[] starts = new Vector2Int[2];
            int minX=0, minY=0, maxX=grid.Columns, maxY=grid.Rows;
            for(int i=0;i<2;i++)
            {
                float halfW=patchWidth*size*.5f, halfH=patchHeight*size*.5f;
                if (grounds[i].size.x < 2*halfW+2*size || grounds[i].size.y < 2*halfH+2*size)
                    throw new InvalidOperationException("Vùng ô lớn hơn nền. Giảm số hàng/cột.");
                float x=Mathf.Clamp(halls[i].x+(i==0?-4f:4f),grounds[i].min.x+halfW+size,grounds[i].max.x-halfW-size);
                float y=Mathf.Clamp(halls[i].y,grounds[i].min.y+halfH+size,grounds[i].max.y-halfH-size);
                starts[i]=new Vector2Int(Mathf.FloorToInt((x-halfW-origin.x)/size),Mathf.FloorToInt((y-halfH-origin.y)/size));
                minX=Mathf.Min(minX,starts[i].x);minY=Mathf.Min(minY,starts[i].y);
                maxX=Mathf.Max(maxX,starts[i].x+patchWidth);maxY=Mathf.Max(maxY,starts[i].y+patchHeight);
            }
            Vector2Int shift=new Vector2Int(-minX,-minY);
            int columns=maxX-minX, rows=maxY-minY;
            if(columns>256 || rows>256) throw new InvalidOperationException("Lưới vượt256 ô mỗi chiều. Kiểm tra Cell Size và tọa độ bản đồ.");
            Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Chuẩn bị mở rộng chương1");
            Undo.RecordObject(grid.transform,"Dời gốc lưới, giữ tọa độ thế giới của ô");
            Undo.RecordObject(grid,"Mở kích thước lưới");
            grid.transform.position=origin-new Vector3(shift.x*size,shift.y*size,0);
            var gridData=new SerializedObject(grid);
            gridData.FindProperty("columns").intValue=columns;gridData.FindProperty("rows").intValue=rows;
            gridData.ApplyModifiedProperties();
            existing.Clear();
            foreach(var slot in oldSlots)
            {
                Vector2Int coordinate=slot.Cell+shift;
                Undo.RecordObject(slot.gameObject,"Đổi tên ô theo tọa độ mới");
                foreach(var component in slot.GetComponentsInChildren<Component>(true)) if(component!=null) Undo.RecordObject(component,"Giữ vị trí ô cũ");
                slot.ConfigureAuthoring(grid,coordinate,grid.Manager,size);
                slot.name="Cell_"+coordinate.x.ToString("D3")+"_"+coordinate.y.ToString("D3");
                MarkPrefab(slot);existing.Add(coordinate);
            }
            int created=0;
            for(int district=0;district<2;district++)
            for(int y=0;y<patchHeight;y++)
            for(int x=0;x<patchWidth;x++)
            {
                Vector2Int coordinate=starts[district]+shift+new Vector2Int(x,y);
                if(!existing.Add(coordinate)) continue;
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(grid.SlotPrefab.gameObject,grid.transform);
                Undo.RegisterCreatedObjectUndo(obj,"Tạo ô mở rộng");
                obj.SetActive(true);
                var slot=obj.GetComponent<BuildSlot>();slot.ConfigureAuthoring(grid,coordinate,grid.Manager,size);
                obj.name=(district==0?"VioletCell_":"GoldenCell_")+coordinate.x.ToString("D3")+"_"+coordinate.y.ToString("D3");
                Vector3 point=slot.transform.position;
                bool allowed=true;
                float pad=size*.707107f;
                foreach(var hall in halls) if(Vector2.Distance(point,hall)<hallClearance+pad) allowed=false;
                foreach(var binding in bindings)
                    for(int k=0;k+1<binding.route.PointCount;k++)
                        if(DistanceToSegment(point,binding.route.GetPoint(k),binding.route.GetPoint(k+1))<roadClearance+pad) allowed=false;
                var slotData=new SerializedObject(slot);slotData.FindProperty("requiresExpansion").boolValue=true;
                slotData.FindProperty("buildable").boolValue=allowed;slotData.ApplyModifiedPropertiesWithoutUndo();
                slot.ClearPreview();MarkPrefab(slot);created++;
            }
            DistrictExpansionController expansion=null;
            foreach(var item in UnityEngine.Object.FindObjectsByType<DistrictExpansionController>(FindObjectsInactive.Include))
                if(item.gameObject.scene==scene) { if(expansion!=null)throw new InvalidOperationException("Controller mở khu bị trùng."); expansion=item; }
            if(expansion==null)
            {
                var obj=new GameObject("DistrictExpansionController");SceneManager.MoveGameObjectToScene(obj,scene);
                Undo.RegisterCreatedObjectUndo(obj,"Tạo quản lý mở khu");obj.transform.SetParent(waves.transform.parent,false);
                expansion=Undo.AddComponent<DistrictExpansionController>(obj);
            }
            Undo.RecordObject(expansion,"Gán tham chiếu mở khu");
            var setup=new SerializedObject(expansion);
            Assign(setup,"waves",waves);Assign(setup,"routes",routes);Assign(setup,"wallet",wallet);Assign(setup,"grid",grid);
            Assign(setup,"primaryHall",primary);Assign(setup,"violetHall",violet);Assign(setup,"goldenHall",golden);
            Assign(setup,"regionsRoot",regions);Assign(setup,"violetHealthBar",violetBar);Assign(setup,"goldenHealthBar",goldenBar);
            setup.ApplyModifiedProperties();
            Undo.RecordObject(waves,"Nối mở khu vào WaveDirector");var waveData=new SerializedObject(waves);
            Assign(waveData,"expansion",expansion);waveData.ApplyModifiedProperties();
            gridData.Update();gridData.FindProperty("expansionZonesAuthored").boolValue=true;gridData.ApplyModifiedProperties();
            Undo.RecordObject(regions,"Ẩn khu mới");Undo.RecordObject(violetBar,"Ẩn thanh phụ");Undo.RecordObject(goldenBar,"Ẩn thanh phụ");
            regions.SetActive(false);violetBar.SetActive(false);goldenBar.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject=expansion.gameObject;
            Debug.Log("Đã thêm "+created+" ô (gồm ô bị chặn gần đường/nhà), giữ vị trí ô cũ. Kiểm tra Inspector và Ctrl+S.",expansion);
        }
        catch(Exception error)
        {
            if(undo>=0) Undo.RevertAllDownToGroup(undo);
            Debug.LogError("Chuẩn bị mở rộng thất bại: "+error.Message);
        }
    }

    // Renderer.bounds của đối tượng tắt có thể không có dữ liệu cập nhật; tính từ sprite và transform.
    private static Bounds SpriteBounds(SpriteRenderer renderer)
    {
        if(renderer.sprite==null || Quaternion.Angle(renderer.transform.rotation,Quaternion.identity)>.01f)
            throw new InvalidOperationException("Nền thiếu sprite hoặc bị xoay.");
        Bounds local=renderer.sprite.bounds;
        Vector3 scale=renderer.transform.lossyScale;
        return new Bounds(renderer.transform.TransformPoint(local.center),new Vector3(Mathf.Abs(local.size.x*scale.x),Mathf.Abs(local.size.y*scale.y),0));
    }
    private static float DistanceToSegment(Vector2 point,Vector2 a,Vector2 b)
    {
        Vector2 d=b-a;float length=d.sqrMagnitude;
        return Vector2.Distance(point,a+(length>0?Mathf.Clamp01(Vector2.Dot(point-a,d)/length):0)*d);
    }
    private static void Assign(SerializedObject data,string name,UnityEngine.Object value)=>data.FindProperty(name).objectReferenceValue=value;
    private static void MarkPrefab(BuildSlot slot)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(slot.gameObject);
        EditorUtility.SetDirty(slot.gameObject);
        foreach(var component in slot.GetComponentsInChildren<Component>(true)) if(component!=null)
        {PrefabUtility.RecordPrefabInstancePropertyModifications(component);EditorUtility.SetDirty(component);}
    }
}
