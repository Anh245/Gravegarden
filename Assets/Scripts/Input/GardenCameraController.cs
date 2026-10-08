using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Camera))]

public class GardenCameraController : MonoBehaviour
{

    [SerializeField] private SpriteRenderer ground;
    [SerializeField] private float minSize = 3f;
    [SerializeField] private float maxSize = 14f;
    [SerializeField] private float zoomStep = 0.75f;

    private Camera viewCamera;
    private bool dragging;
    private Vector2 previousPointer;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();

        if (ground == null)
        {
            Debug.LogError(
                "Chưa gán Ground cho GardenCameraController.",
                this
            );

            enabled = false;
            return;
        }

        if (!viewCamera.orthographic)
        {
            Debug.LogError("Camera phải dùng Orthographic.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        // Giới hạn góc nhìn cả khi không có chuột.
        ClampToGround();

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 pointer = mouse.position.ReadValue();

        bool overUI =
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();

        if (mouse.rightButton.wasPressedThisFrame && !overUI)
        {
            dragging = true;
            previousPointer = pointer;
        }

        if (!mouse.rightButton.isPressed)
            dragging = false;

        if (dragging)
        {
            Vector3 previousWorld = ScreenToWorld(previousPointer);
            Vector3 currentWorld = ScreenToWorld(pointer);

            // Dịch camera để cảnh đi theo hướng kéo chuột.
            transform.position += previousWorld - currentWorld;
            previousPointer = pointer;
        }

        float scroll = mouse.scroll.ReadValue().y;

        if (!overUI && Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 beforeZoom = ScreenToWorld(pointer);

            GetSizeLimits(out float smallest, out float largest);

            viewCamera.orthographicSize = Mathf.Clamp(
                viewCamera.orthographicSize
                    - Mathf.Sign(scroll) * zoomStep,
                smallest,
                largest
            );

            Vector3 afterZoom = ScreenToWorld(pointer);

            // Giữ vùng dưới con trỏ ổn định khi zoom.
            transform.position += beforeZoom - afterZoom;
        }

        ClampToGround();
    }

    private Vector3 ScreenToWorld(Vector2 pointer)
    {
        float distance = ground.bounds.center.z - transform.position.z;

        return viewCamera.ScreenToWorldPoint(
            new Vector3(pointer.x, pointer.y, distance)
        );
    }

    private void GetSizeLimits(out float smallest, out float largest)
    {
        Bounds bounds = ground.bounds;

        float fitSize = Mathf.Min(
            bounds.extents.y,
            bounds.extents.x / viewCamera.aspect
        );

        largest = Mathf.Min(maxSize, fitSize);
        smallest = Mathf.Min(minSize, largest);
    }

    private void ClampToGround()
    {
        GetSizeLimits(out float smallest, out float largest);

        viewCamera.orthographicSize = Mathf.Clamp(
            viewCamera.orthographicSize,
            smallest,
            largest
        );

        Bounds bounds = ground.bounds;
        float halfHeight = viewCamera.orthographicSize;
        float halfWidth = halfHeight * viewCamera.aspect;

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(
            position.x,
            bounds.min.x + halfWidth,
            bounds.max.x - halfWidth
        );

        position.y = Mathf.Clamp(
            position.y,
            bounds.min.y + halfHeight,
            bounds.max.y - halfHeight
        );

        transform.position = position;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            dragging = false;
    }

    private void OnDisable()
    {
        dragging = false;
    }
}
