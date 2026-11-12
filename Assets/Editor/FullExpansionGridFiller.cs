using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FullExpansionGridFiller : EditorWindow
{
    private float roadClearance = 1.5f;
    private float hallPadding = .3f;
    private bool blockExisting = true;
    private struct Segment { public Vector2 a, b; public Segment(Vector2 a, Vector2 b) { this.a=a; this.b=b; } }

    [MenuItem("GraveGarden/Map/Fill Entire Expanded Districts")]
    private static void Open() => GetWindow<FullExpansionGridFiller>("Phủ ô hai khu");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Phủ ô còn thiếu trên hai nền tím/vàng trong cùng BuildGrid. Giữ vị trí, ô bị chặn hiện có và khu đầu. Không tự nhận diện đá, hồ hoặc đường bằng màu ảnh.", MessageType.Info);
        roadClearance = EditorGUILayout.FloatField("Cách tim đường (world)", roadClearance);
        hallPadding = EditorGUILayout.FloatField("Chừa thêm quanh hình nhà", hallPadding);
        blockExisting = EditorGUILayout.Toggle("Chặn thêm ô cũ sát đường/nhà", blockExisting);
        EditorGUILayout.HelpBox("Có thể thêm vài nghìn ô. Sao lưu scene trước; không chạy trong Play. Những ô ĐÃ XÓA ở hai khu mới có thể được tạo lại; dùng Buildable=false để giữ vùng cấm.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("Phủ phần đất còn thiếu của hai khu")) Fill();
    }

    private static T Single<T>(Scene scene) where T:Component
    {
        T result=null;
        foreach(T item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if(item.gameObject.scene==scene)
            { if(result!=null)throw new InvalidOperationException("Có nhiều hơn một "+typeof(T).Name); result=item; }
        if(result==null)throw new InvalidOperationException("Thiếu "+typeof(T).Name);
        return result;
    }
    private static GameObject Named(Scene scene,string name)
    {
        GameObject result=null;
        foreach(var root in scene.GetRootGameObjects())
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
            if(t.name==name)
            {if(result!=null)throw new InvalidOperationException("Tên bị trùng: "+name);result=t.gameObject;}
        if(result==null)throw new InvalidOperationException("Thiếu "+name);
        return result;
    }
    private static Bounds SpriteBounds(SpriteRenderer renderer)
    {
        if(renderer==null || renderer.sprite==null || Quaternion.Angle(renderer.transform.rotation,Quaternion.identity)>.01f)
            throw new InvalidOperationException("Nền/hình nhà phải có SpriteRenderer, sprite hợp lệ và không xoay.");
        Bounds local=renderer.sprite.bounds;Vector3 scale=renderer.transform.lossyScale;
        return new Bounds(renderer.transform.TransformPoint(local.center),
            new Vector3(Mathf.Abs(local.size.x*scale.x),Mathf.Abs(local.size.y*scale.y),0));
    }
    private static bool ContainsCell(Bounds b,Vector3 center,float half)
        => center.x-half>=b.min.x-.0001f && center.x+half<=b.max.x+.0001f
        && center.y-half>=b.min.y-.0001f && center.y+half<=b.max.y+.0001f;
    private static bool TouchesCell(Bounds b,Vector3 p,float half)
        => p.x+half>b.min.x+.0001f && p.x-half<b.max.x-.0001f
        && p.y+half>b.min.y+.0001f && p.y-half<b.max.y-.0001f;
    private static float DistanceSquared(Vector2 p,Segment segment)
    {
        Vector2 d=segment.b-segment.a;
        float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(p-segment.a,d)/d.sqrMagnitude):0;
        return (p-segment.a-t*d).sqrMagnitude;
    }
    private bool Blocked(Vector3 p,float cellSize,List<Segment> segments,List<Bounds> houses)
    {
        float road=roadClearance+cellSize*.707107f;
        foreach(var segment in segments) if(DistanceSquared(p,segment)<road*road)return true;
        foreach(var b in houses) if(TouchesCell(b,p,cellSize*.5f))return true;
        return false;
    }
    private static void Record(BuildSlot slot)
    {
        Undo.RecordObject(slot.gameObject,"Cập nhật tên ô");
        foreach(var c in slot.GetComponentsInChildren<Component>(true))if(c!=null)Undo.RecordObject(c,"Giữ ô hiện có");
    }
    private static void Persist(BuildSlot slot)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(slot.gameObject);EditorUtility.SetDirty(slot.gameObject);
        foreach(var c in slot.GetComponentsInChildren<Component>(true))if(c!=null)
        {PrefabUtility.RecordPrefabInstancePropertyModifications(c);EditorUtility.SetDirty(c);}
    }

    private void Fill()
    {
        int undo=-1;
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Thoát Play trước.");
            Scene scene=SceneManager.GetActiveScene();
            if(scene.name!="Garden")throw new InvalidOperationException("Mở và chọn scene Garden.");
            if(float.IsNaN(roadClearance)||float.IsInfinity(roadClearance)||roadClearance<0
                ||float.IsNaN(hallPadding)||float.IsInfinity(hallPadding)||hallPadding<0)
                throw new InvalidOperationException("Khoảng cách phải hữu hạn và không âm.");
            BuildGrid grid=Single<BuildGrid>(scene);
            if(!grid.ExpansionZonesAuthored || !grid.HasValidTransform || grid.SlotPrefab==null || grid.Manager==null
                ||!PrefabUtility.IsPartOfPrefabAsset(grid.SlotPrefab))
                throw new InvalidOperationException("Chạy Prepare Chapter 1 Expansion trước; kiểm tra Slot Prefab, Manager và Transform lưới.");
            float size=grid.CellSize;
            if(size<=0 || float.IsNaN(size)||float.IsInfinity(size))throw new InvalidOperationException("Cell Size không hợp lệ.");
            Bounds[] grounds={SpriteBounds(Named(scene,"VioletGround").GetComponent<SpriteRenderer>()),SpriteBounds(Named(scene,"GoldenGround").GetComponent<SpriteRenderer>())};
            GardenCameraController camera=Single<GardenCameraController>(scene);
            var cameraData=new SerializedObject(camera);
            var original=cameraData.FindProperty("ground").objectReferenceValue as SpriteRenderer;
            Bounds originalBounds=SpriteBounds(original);
            var registry=Single<DistrictDefenseRoutes>(scene);var bindings=registry.ConfiguredBindings;
            if(bindings.Length!=6)throw new InvalidOperationException("Cần đủ sáu dòng phân tuyến.");
            var segments=new List<Segment>();var houses=new List<Bounds>();var seenHalls=new HashSet<TownHallHealth>();
            foreach(var binding in bindings)
            {
                if(binding.route==null||!binding.route.IsValid||binding.hall==null)
                    throw new InvalidOperationException("Bảng tuyến còn thiếu tham chiếu/điểm.");
                for(int i=0;i+1<binding.route.PointCount;i++)segments.Add(new Segment(binding.route.GetPoint(i),binding.route.GetPoint(i+1)));
                if(seenHalls.Add(binding.hall))
                {
                    bool hasBounds=false;Bounds b=default;
                    foreach(var renderer in binding.hall.GetComponentsInChildren<SpriteRenderer>(true))
                        if(renderer.sprite!=null)
                        {Bounds value=SpriteBounds(renderer);if(!hasBounds){b=value;hasBounds=true;}else b.Encapsulate(value);}
                    if(!hasBounds)b=new Bounds(binding.hall.transform.position,new Vector3(3.2f,3.2f,0));
                    b.Expand(new Vector3(hallPadding*2,hallPadding*2,0));houses.Add(b);
                }
            }
            var slots=grid.GetComponentsInChildren<BuildSlot>(true);
            if(slots.Length==0)throw new InvalidOperationException("Lưới chưa có ô.");
            var existing=new HashSet<Vector2Int>();
            foreach(var slot in slots)
                if(slot.Grid!=grid||!existing.Add(slot.Cell)||slot.Cell.x<0||slot.Cell.y<0||slot.Cell.x>=grid.Columns||slot.Cell.y>=grid.Rows)
                    throw new InvalidOperationException("Ô hiện có bị trùng tọa độ, sai Grid hoặc ngoài kích thước.");
            Vector3 origin=grid.transform.position;
            var additions=new Dictionary<Vector2Int,int>();
            int minX=0,minY=0,maxX=grid.Columns,maxY=grid.Rows;
            for(int district=0;district<2;district++)
            {
                Bounds ground=grounds[district];
                int left=Mathf.CeilToInt((ground.min.x-origin.x)/size-.0001f);
                int bottom=Mathf.CeilToInt((ground.min.y-origin.y)/size-.0001f);
                int right=Mathf.FloorToInt((ground.max.x-origin.x)/size+.0001f);
                int top=Mathf.FloorToInt((ground.max.y-origin.y)/size+.0001f);
                if(right-left>256||top-bottom>256)throw new InvalidOperationException("Nền quá lớn so với Cell Size; giới hạn256 mỗi chiều.");
                for(int y=bottom;y<top;y++)
                {
                    if(EditorUtility.DisplayCancelableProgressBar("Tính vùng ô",district==0?"Khu tím":"Khu vàng",(district+(y-bottom)/(float)Mathf.Max(1,top-bottom))/2))
                        throw new OperationCanceledException("Đã hủy trước khi sửa scene.");
                    for(int x=left;x<right;x++)
                    {
                        var coordinate=new Vector2Int(x,y);
                        if(existing.Contains(coordinate)||additions.ContainsKey(coordinate))continue;
                        Vector3 point=origin+new Vector3((x+.5f)*size,(y+.5f)*size,0);
                        if(!ContainsCell(ground,point,size*.5f)||TouchesCell(originalBounds,point,size*.5f)||Blocked(point,size,segments,houses))continue;
                        additions.Add(coordinate,district);minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);
                        maxX=Mathf.Max(maxX,x+1);maxY=Mathf.Max(maxY,y+1);
                    }
                }
            }
            var toBlock=new HashSet<BuildSlot>();
            if(blockExisting)foreach(var slot in slots)
                if(slot.RequiresExpansion && slot.IsBuildable && Blocked(slot.transform.position,size,segments,houses))toBlock.Add(slot);
            if(additions.Count==0&&toBlock.Count==0){Debug.Log("Hai khu đã phủ đủ ô hợp lệ. Không tạo trùng.",grid);return;}
            if(maxX-minX>256||maxY-minY>256)throw new InvalidOperationException("Toàn lưới vượt256 ô mỗi chiều. Chưa thay đổi scene.");
            Vector2Int shift=new Vector2Int(-minX,-minY);
            Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Phủ ô hai khu mở rộng");
            Undo.RecordObject(grid,"Mở kích thước lưới");Undo.RecordObject(grid.transform,"Giữ vị trí ô khi dời gốc");
            grid.transform.position=origin-new Vector3(shift.x*size,shift.y*size,0);
            var data=new SerializedObject(grid);data.FindProperty("columns").intValue=maxX-minX;data.FindProperty("rows").intValue=maxY-minY;data.ApplyModifiedProperties();
            int processed=0;
            foreach(var slot in slots)
            {
                if(processed++%100==0&&EditorUtility.DisplayCancelableProgressBar("Giữ ô cũ","Cập nhật tọa độ, giữ nguyên vị trí",processed/(float)slots.Length))
                    throw new OperationCanceledException("Đã hủy và hoàn tác thay đổi.");
                Record(slot);Vector2Int coordinate=slot.Cell+shift;
                slot.ConfigureAuthoring(grid,coordinate,grid.Manager,size);
                if(toBlock.Contains(slot))
                {var s=new SerializedObject(slot);s.FindProperty("buildable").boolValue=false;s.ApplyModifiedPropertiesWithoutUndo();slot.ClearPreview();}
                Persist(slot);
            }
            int created=0;
            foreach(var pair in additions)
            {
                if(created%100==0&&EditorUtility.DisplayCancelableProgressBar("Phủ ô mới",created+" / "+additions.Count,created/(float)additions.Count))
                    throw new OperationCanceledException("Đã hủy và hoàn tác thay đổi.");
                Vector2Int coordinate=pair.Key+shift;
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(grid.SlotPrefab.gameObject,grid.transform);
                Undo.RegisterCreatedObjectUndo(obj,"Thêm ô còn thiếu");obj.SetActive(true);
                var slot=obj.GetComponent<BuildSlot>();slot.ConfigureAuthoring(grid,coordinate,grid.Manager,size);
                obj.name=(pair.Value==0?"VioletCell_":"GoldenCell_")+coordinate.x.ToString("D3")+"_"+coordinate.y.ToString("D3");
                var s=new SerializedObject(slot);s.FindProperty("requiresExpansion").boolValue=true;s.FindProperty("buildable").boolValue=true;s.ApplyModifiedPropertiesWithoutUndo();
                slot.ClearPreview();Persist(slot);created++;
            }
            EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject=grid.gameObject;
            Debug.Log("Đã thêm "+created+" ô, chặn thêm "+toBlock.Count+" ô cũ gần đường/nhà. Khu đầu và vị trí ô cũ được giữ. Ctrl+S để lưu.",grid);
        }
        catch(Exception e)
        {
            if(undo>=0)Undo.RevertAllDownToGroup(undo);
            if(e is OperationCanceledException)Debug.Log(e.Message);else Debug.LogError("Phủ ô thất bại: "+e.Message);
        }
        finally{EditorUtility.ClearProgressBar();}
    }
}
