using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ClickRaycastDebugger : MonoBehaviour
{
    private readonly List<RaycastResult> results = new();
    private int clickNumber;

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        clickNumber++;

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            Debug.LogWarning("Không tìm thấy EventSystem đang hoạt động.");
            return;
        }

        PointerEventData pointer = new PointerEventData(eventSystem)
        {
            position = mouse.position.ReadValue(),
            button = PointerEventData.InputButton.Left
        };

        results.Clear();
        eventSystem.RaycastAll(pointer, results);

        StringBuilder message = new StringBuilder();

        message.AppendLine($"KIỂM TRA CLICK #{clickNumber}");
        message.AppendLine($"Vị trí chuột: {pointer.position}");
        message.AppendLine($"Số kết quả: {results.Count}");

        if (results.Count == 0)
        {
            message.AppendLine(
                "Không có UI hoặc Collider nào được Raycaster nhận tại đây.");
        }

        for (int i = 0; i < results.Count; i++)
        {
            RaycastResult hit = results[i];

            string raycaster = hit.module != null
                ? hit.module.GetType().Name
                : "Không rõ";

            GameObject handler =
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(
                    hit.gameObject);

            string handlerPath = handler != null
                ? GetPath(handler.transform)
                : "Không có";

            message.AppendLine(
                $"{i + 1}. {GetPath(hit.gameObject.transform)}");

            message.AppendLine($"   Raycaster: {raycaster}");
            message.AppendLine($"   Nơi xử lý click: {handlerPath}");
        }

        Object context = results.Count > 0
            ? results[0].gameObject
            : gameObject;

        Debug.Log(message.ToString(), context);
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;

        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}