# KRPackToolGUI

查詢 `.rho5` 檔案內部檔案結構的 WPF 小工具，重用 `KartriderLibrary` 中
`RhoPacker.decode` 所使用的同一套 rho5 解析器
（`PackFolderManager.OpenSingleFile` → `Rho5`）。

此資料夾**不納入版控**（見上層 `.gitignore` 的 `/KRPackToolGUI/`），
且已在 `KRPackTool.csproj` 以 `<Compile Remove="KRPackToolGUI\**" />` 排除，
不影響主專案建置。

## 建置 / 執行

```
dotnet run --project KRPackToolGUI/KRPackToolGUI.csproj
```

或用 Visual Studio 開啟 `KRPackToolGUI/KRPackToolGUI.slnx`。

需求：.NET 10 SDK、Windows。`KartriderLibrary` 沒有自己的專案檔，
其原始碼由本專案的 `.csproj` 以 glob 直接編譯（排除依賴 `KRPackTool.Program`
的 `Data/RhoPacker.cs`）。

## 用法

1. **新增 rho5…**（或直接把 `.rho5` 檔拖進視窗）選擇一個或多個檔案。
2. **地區**：預設「自動」＝讀取 rho5 檔案旁的 `CountryCode.ini`，
   找不到時退回 `TW`；也可手動指定 `KR` / `CN` / `TW`
   （rho5 檔名表的解密金鑰依地區不同）。
3. 按 **解析**。

### 樹狀結構

以每個 rho5 檔案為頂層節點，往下展開資料夾與檔案。

### 平面清單

每列一個檔案，格式為 `rho5檔名\資料夾\...\檔案`，例如：

```
DataPack2_00000.rho5\aaa\bbb.xml
DataPack2_00001.rho5\aaa\ccc.xml
```

可用上方欄位即時篩選，並以 **複製** / **匯出 .txt** 匯出目前顯示的內容
（有選取列時只作用於選取列）。

## 已知限制

沿用原解析器行為：rho5 根目錄下（不在任何子資料夾內）的檔案不會被列出，
因為 `PackFolderManager` 在建樹時就未保留這類項目。實務上 datapack
的內容都在子資料夾底下，一般不受影響。
