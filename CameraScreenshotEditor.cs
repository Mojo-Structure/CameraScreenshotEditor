using UnityEngine;
using UnityEditor;
using System.IO;

// CameraScreenshotEditor
// Unity 編輯器視窗工具，用於擷取相機螢幕截圖（可選透明 PNG 格式）
// 帶有檔案名稱前綴、自動開啟資料夾和安全性清理功能。

// 這是一個 EditorWindow：會在 Unity 編輯器裡開一個獨立工具視窗（不是掛在 Inspector 的 CustomEditor）
public class CameraScreenshotEditor : EditorWindow
{
    // 目標攝影機：你要從哪台 Camera 截圖
    private Camera targetCamera;

    // 輸出解析度（不是 GameView 畫面大小，而是你想要的截圖尺寸）
    private int width = 1920;
    private int height = 1080;

    // 預設輸出資料夾（在專案內：Assets/Screenshots）
    private string customFolderPath = "Assets/Screenshots";

    // 透明背景開關：true 時，會暫時把 Camera 改成 SolidColor 且背景 alpha=0，輸出 PNG 透明通道
    private bool transparentBackground = true;

    // 檔名前綴：例如 "KV7_front" -> KV7_front_20260120_163500.png
    private string filePrefix = "screenshot";

    // 截圖後自動開啟資料夾（用檔案總管/Finder 打開）
    private bool autoOpenFolderAfterCapture = false;

    // 在 Unity 菜單新增入口：Tools/Camera Screenshot Tool
    [MenuItem("Tools/Camera Screenshot Tool")]
    public static void ShowWindow()
    {
        // 開啟/取得視窗，標題為 "Camera Screenshot"
        GetWindow<CameraScreenshotEditor>("Camera Screenshot");
    }

    // EditorWindow 的 UI 繪製入口
    void OnGUI()
    {
        GUILayout.Label("截圖設定", EditorStyles.boldLabel);

        // 選擇要截圖的 Camera（允許拖曳場景中的 Camera 進來）
        targetCamera = (Camera)EditorGUILayout.ObjectField("目標攝影機", targetCamera, typeof(Camera), true);

        // 輸入輸出解析度
        width = EditorGUILayout.IntField("寬度", width);
        height = EditorGUILayout.IntField("高度", height);

        EditorGUILayout.Space();

        // 透明背景開關（輸出 PNG Alpha）
        transparentBackground = EditorGUILayout.ToggleLeft("透明背景（PNG Alpha）", transparentBackground);

        // 檔名前綴輸入欄位
        filePrefix = EditorGUILayout.TextField("檔名前綴", filePrefix);

        // 截圖後是否自動開啟資料夾
        autoOpenFolderAfterCapture = EditorGUILayout.ToggleLeft("截圖後自動開啟資料夾", autoOpenFolderAfterCapture);

        EditorGUILayout.Space();

        // 第一排：顯示目前儲存路徑 + 「選擇資料夾截圖」按鈕
        EditorGUILayout.BeginHorizontal();

        // 顯示目前輸出路徑（可能是 Assets/... 或任意外部資料夾）
        GUILayout.Label($"儲存路徑：\n{customFolderPath}", EditorStyles.wordWrappedLabel);

        // 這個按鈕會：
        // 1) 讓你選資料夾
        // 2) 選完後立刻截一張，存到你選的新資料夾
        if (GUILayout.Button("選擇資料夾截圖", GUILayout.Height(30)))
        {
            // OpenFolderPanel 回傳的是「絕對路徑」
            string selectedPath = EditorUtility.OpenFolderPanel("選擇儲存截圖的資料夾", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 更新輸出路徑
                customFolderPath = selectedPath;

                // 有指定攝影機才可以截圖
                if (targetCamera != null)
                {
                    // 執行截圖，回傳存檔路徑
                    string savedPath = CaptureScreenshot(customFolderPath, transparentBackground, filePrefix);
                    // 若勾選自動開啟：就打開「該檔案所在資料夾」
                    if (autoOpenFolderAfterCapture && !string.IsNullOrEmpty(savedPath))
                    {
                        OpenFolder(Path.GetDirectoryName(savedPath));
                    }
                }
                else
                {
                    Debug.LogWarning("請先指定一個攝影機！");
                }
            }
        }
        
        EditorGUILayout.EndHorizontal();

        // 把下一排按鈕推到視窗底部（UI 排版用）
        GUILayout.FlexibleSpace();

        // 第二排：截圖與開啟資料夾
        EditorGUILayout.BeginHorizontal();

        // 直接用目前路徑截一張圖
        if (GUILayout.Button("截圖並儲存", GUILayout.Height(30)))
        {
            if (targetCamera != null)
            {
                string savedPath = CaptureScreenshot(customFolderPath, transparentBackground, filePrefix);

                // 若勾選自動開啟：就打開「該檔案所在資料夾」
                if (autoOpenFolderAfterCapture && !string.IsNullOrEmpty(savedPath))
                {
                    OpenFolder(Path.GetDirectoryName(savedPath));
                }
            }
            else
            {
                Debug.LogWarning("請先指定一個攝影機！");
            }
        }

        // 手動開啟目前設定的輸出資料夾
        if (GUILayout.Button("開啟輸出資料夾", GUILayout.Height(30)))
        {
            OpenFolder(customFolderPath);
        }

        EditorGUILayout.EndHorizontal();
    }

    // 截圖主流程：
    // - 建立 RenderTexture (含 alpha)
    // - Camera.Render() 到 RenderTexture
    // - ReadPixels 把 RenderTexture 讀回 Texture2D
    // - EncodeToPNG 存檔
    // - 回傳存檔路徑（讓外面可以決定要不要自動開啟資料夾）
    string CaptureScreenshot(string folderPath, bool useTransparentBackground, string prefix)
    {
        // 防呆：路徑空白就不做
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Debug.LogWarning("儲存路徑是空的，無法截圖。");
            return null;
        }

        // 穩定性修補 2：解析度必須 > 0（否則 RenderTexture/Texture2D 會出錯）
        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning("寬度/高度必須大於 0。");
            return null;
        }

        // 保存攝影機原本設定（避免截完後 Camera 被永久改掉）
        var oldTargetTexture = targetCamera.targetTexture;
        var oldClearFlags = targetCamera.clearFlags;
        var oldBgColor = targetCamera.backgroundColor;

        // 穩定性修補 1：保存目前 active RenderTexture
        // 因為 RenderTexture.active 是全域狀態，若你直接設成 null 可能影響別的 Editor 畫面/工具
        var oldActiveRT = RenderTexture.active;

        // 如果需要透明背景：暫時強制改成純色背景、alpha = 0
        // (若使用 Skybox 通常背景會是完全不透明)
        if (useTransparentBackground)
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            var bg = targetCamera.backgroundColor;
            bg.a = 0f;
            targetCamera.backgroundColor = bg;
        }

        // 建立一個含 alpha 的 RenderTexture（ARGB32）
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        
        // 讓攝影機把畫面渲染到這個 RenderTexture
        targetCamera.targetTexture = rt;

        // 建立一張含 alpha 的 Texture2D（RGBA32），用來接收 ReadPixels 結果
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // 讓攝影機渲染一次（離線渲染，不依賴 GameView）
        targetCamera.Render();

        // 指定 active RT，ReadPixels 才知道要從哪裡讀
        RenderTexture.active = rt;

        // 把 RenderTexture 的像素讀回到 Texture2D
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 清理/還原 RenderTexture 相關狀態
        targetCamera.targetTexture = oldTargetTexture;
        RenderTexture.active = oldActiveRT;                 // 還原全域 active RT
        DestroyImmediate(rt);                               // 立即釋放 RT（Editor 裡避免累積）

        // 若有改透明背景設定：截完後還原攝影機原本設定
        if (useTransparentBackground)
        {
            targetCamera.clearFlags = oldClearFlags;
            targetCamera.backgroundColor = oldBgColor;
        }

        // 若資料夾不存在就建立
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // 把檔名前綴做安全處理：避免檔名包含系統不允許的字元
        string safePrefix = MakeSafeFileName(string.IsNullOrWhiteSpace(prefix) ? "screenshot" : prefix.Trim());

        // 組合檔名：前綴 + 時間戳
        string fileName = $"{safePrefix}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(folderPath, fileName);

        // 寫入 PNG
        File.WriteAllBytes(fullPath, screenShot.EncodeToPNG());
        Debug.Log($"截圖已儲存到：{fullPath}");

        // 釋放 Texture2D，避免 Editor 長時間使用累積記憶體
        DestroyImmediate(screenShot);

        // 只有「存到 Assets/ 底下」Unity Project 視窗才會顯示該檔案
        // Refresh 讓 Unity 重新掃描 Assets 的檔案變動
        if (fullPath.Replace("\\", "/").Contains("/Assets/"))
            AssetDatabase.Refresh();

        return fullPath;
    }

    // 開啟輸出資料夾（在 Windows 會開檔案總管；macOS 會開 Finder）
    void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Debug.LogWarning("資料夾路徑是空的，無法開啟。");
            return;
        }

        // 如果是 Assets/... 這種相對路徑，轉成絕對路徑
        // 因為 RevealInFinder 需要絕對路徑
        string pathToOpen = folderPath;
        if (!Path.IsPathRooted(pathToOpen))
        {
            // Application.dataPath = .../ProjectName/Assets
            // 取 parent 得到 .../ProjectName
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            // 把 "Assets/xxx" 拼成 ".../ProjectName/Assets/xxx"
            pathToOpen = Path.Combine(projectRoot, folderPath);
        }

        // 資料夾不存在就提示
        if (!Directory.Exists(pathToOpen))
        {
            Debug.LogWarning($"資料夾不存在：{pathToOpen}");
            return;
        }

        // 在 OS 的檔案管理器中顯示該資料夾
        EditorUtility.RevealInFinder(pathToOpen);
    }

    // 把檔名中的非法字元換成 "_"，避免存檔失敗
    string MakeSafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c.ToString(), "_");
        }
        return name;
    }
}
