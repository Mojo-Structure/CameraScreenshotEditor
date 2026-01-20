# Camera Screenshot Tool (Unity EditorWindow)

一個 Unity 編輯器工具視窗（EditorWindow），可從指定 Camera 以自訂解析度輸出 PNG 截圖，支援透明背景（Alpha）、檔名前綴、自動/手動開啟輸出資料夾。

> 注意：此工具是 Editor-only 腳本（使用 `UnityEditor`），不可在 Player/Build 執行。

---

## 功能
- 指定 **目標 Camera** 截圖（不依賴 GameView 視窗尺寸）
- 自訂輸出解析度（Width / Height）
- **透明背景**輸出（PNG Alpha）
- 自訂**檔名前綴**
- 「截圖並儲存」
- 「選擇資料夾後立刻截圖」
- 截圖後可選擇**自動開啟資料夾**
- 手動「開啟輸出資料夾」

---

## 安裝
1. 將 `CameraScreenshotEditor.cs` 放到專案內任一 `Editor` 資料夾，例如：
   - `Assets/Editor/CameraScreenshotEditor.cs`
2. 回到 Unity，等待編譯完成。

---

## 開啟工具
在 Unity 上方選單：
- `Tools > Camera Screenshot Tool`

---

## 使用方式
1. 在工具視窗選擇 **目標攝影機（Camera）**
2. 設定輸出解析度 **寬度 / 高度**
3. （可選）勾選 **透明背景（PNG Alpha）**
4. （可選）輸入 **檔名前綴**
5. 選擇任一操作：
   - **截圖並儲存**：使用目前「儲存路徑」輸出
   - **選擇資料夾截圖**：選一個資料夾後立刻輸出
6. （可選）勾選 **截圖後自動開啟資料夾**
7. 或按 **開啟輸出資料夾** 手動打開

---

## 輸出檔名格式
輸出檔名格式如下：

`{prefix}_yyyyMMdd_HHmmss.png`

例如：
- `KV7_front_20260120_163000.png`

---

## 透明背景（Alpha）說明
透明背景的做法是：
- 截圖當下暫時把 Camera 設為 `SolidColor`
- 並將 `backgroundColor.a = 0`
- 截圖完成後會還原 Camera 原本的 `clearFlags` 與 `backgroundColor`

若你的渲染流程會覆寫 Alpha（例如某些後處理或自訂 shader），輸出是否保留透明通道需要依專案設定確認。

---

## 路徑與刷新說明
- 若儲存到 `Assets/...` 之下，腳本會呼叫 `AssetDatabase.Refresh()` 讓 Unity 重新掃描檔案。
- 若儲存到專案外的任意路徑，Unity 的 Project 視窗不會顯示該資料夾內容；請用「開啟輸出資料夾」在檔案總管/Finder 查看。

---

## License
MIT
