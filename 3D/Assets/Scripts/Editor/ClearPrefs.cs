using UnityEngine;
using UnityEditor;

public class ClearPrefs
{
    [MenuItem("Tools/Xóa Dữ Liệu Lưu (Clear PlayerPrefs)")]
    public static void ClearAllPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("Đã xóa toàn bộ dữ liệu lưu (PlayerPrefs)! Bạn có thể chơi lại từ đầu.");
    }
}
