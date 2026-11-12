// Phép kiểm tra điểm nằm trong vùng quanh một đoạn thẳng hữu hạn.
// Dùng số float thuần để có thể kiểm thử hình học độc lập với Unity.
public static class PierceGeometry
{
    public static bool ContainsPoint(
        float pointX, float pointY,
        float startX, float startY,
        float endX, float endY,
        float halfWidth)
    {
        if (halfWidth < 0f || float.IsNaN(halfWidth))
            return false;
        float dx = endX - startX;
        float dy = endY - startY;
        float lengthSquared = dx * dx + dy * dy;
        float t = lengthSquared > 0f
            ? ((pointX - startX) * dx + (pointY - startY) * dy) / lengthSquared
            : 0f;
        if (t < 0f) t = 0f;
        else if (t > 1f) t = 1f;
        float offsetX = pointX - (startX + t * dx);
        float offsetY = pointY - (startY + t * dy);
        return offsetX * offsetX + offsetY * offsetY <= halfWidth * halfWidth;
    }
}
