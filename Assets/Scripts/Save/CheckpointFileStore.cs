using System;
using System.IO;
using System.Text;

public static class CheckpointFileStore
{
    public const int MaxFileBytes = 2 * 1024 * 1024;

    public static bool TryRead(string path, out string json, out string error)
    {
        json = null;
        error = "";
        try
        {
            if (!File.Exists(path)) { error = "Chưa có bản lưu."; return false; }
            long length = new FileInfo(path).Length;
            if (length <= 0 || length > MaxFileBytes) { error = "Kích thước bản lưu không hợp lệ."; return false; }
            json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) { error = "Bản lưu trống."; return false; }
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
            || exception is ArgumentException || exception is NotSupportedException)
        {
            error = "Không đọc được bản lưu: " + exception.Message;
            return false;
        }
    }

    public static bool TryWrite(string path, string json, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(json)) { error = "Không ghi bản lưu trống."; return false; }
        byte[] bytes = new UTF8Encoding(false).GetBytes(json);
        if (bytes.Length > MaxFileBytes) { error = "Bản lưu quá lớn."; return false; }
        string temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak", true);
            else File.Move(temporary, path);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
            || exception is ArgumentException || exception is NotSupportedException)
        {
            error = "Không ghi được bản lưu: " + exception.Message;
            return false;
        }
        finally
        {
            // Chỉ xóa file tạm của chính thao tác này, giữ bản chính và .bak.
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
