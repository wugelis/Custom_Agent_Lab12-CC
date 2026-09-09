---
name: create-netcore-class-library
description: 依照 Mermaid classDiagram 中的類別名稱、屬性、型別與方法，建立一個 .NET Core 10 類別庫（Class Library）專案並改寫成 C#。每個 .cs 檔案只能定義一個型別（class、enum、interface 各自獨立成檔）。當使用者說「把這個類別圖轉成 .NET 專案」「幫我建立 C# 類別庫」「產生 .NET Core 10 class library」「convert this mermaid class diagram to C#」時使用。
---

把一份 Mermaid `classDiagram` 轉成可編譯的 .NET Core 10 C# 類別庫。這不是在操作一個正在跑的應用程式，
所以沒有 driver script——這個 skill 的「執行工具」就是 `dotnet` CLI 加上 Write/Edit，照下面步驟做、最後
用 `dotnet build` 驗證即可。以下流程已在本機（`dotnet 10.0.400`，`dotnet new classlib` 預設
`TargetFramework` 為 `net10.0`）實際跑過一次，用專案裡的 `CD_01_車輛租用系統.md`（Account / RentalOrder /
Car 三個類別 + 一個關聯）當測試案例，`dotnet build` 產出 0 warning、0 error。

## Step 1：找到來源的 Mermaid 類別圖

- 使用者指定的檔案（例如 `CD_01_車輛租用系統.md`），或對話中貼的 ```` ```mermaid classDiagram ... ``` ````
  區塊。
- 不確定要轉哪一份圖、或一個檔案裡有多張圖時，直接問使用者，不要用猜的。

## Step 2：解析類別圖

逐一讀出每個 `class` 區塊，記錄：

- **類別名稱**
- **屬性**：`可見度符號 名稱 : 型別`（例如 `+username : String`）
- **方法**：`可見度符號 名稱(參數名 : 型別, ...) 回傳型別`（例如 `+login(username : String, password : String) Boolean`）
- **關聯線**：`-->`（關聯／持有一個參考）、`o--`（聚合）、`*--`（組合）、`--|>`（繼承）、`..|>`（實作介面）
- **`<<enumeration>>`** stereotype 的 class → 轉成 C# `enum`

## Step 3：型別對應表（Mermaid/UML → C#）

| Mermaid/UML 型別 | C# 型別 | 備註 |
|---|---|---|
| `String` | `string` | |
| `Boolean` | `bool` | |
| `Int` / `Integer` | `int` | |
| `Float` / `Double` | `double` | 預設用 `double`；若使用者明確要精確金額計算（例如租金、貨幣），改用 `decimal` 並在回報時說明理由 |
| `Date` | `DateTime` | 只需要日期不需要時間時，可改用 `DateOnly`（.NET 10 支援良好）——兩者都跟使用者確認過一次即可，不用每次都問 |
| `void` | `void` | |
| 圖上未定義的型別（見 Gotchas） | 建立最小 stub `class` | 不要默默改成 `object` 或省略型別 |
| `List<X>` 等集合寫法 | `List<X>` | |

可見度符號對應：`+` → `public`，`-` → `private`，`#` → `protected`，`~` → `internal`。

## Step 4：建立 .NET Core 10 類別庫專案

```bash
dotnet --version                       # 確認是 10.x（本機驗證版本：10.0.400）
dotnet new classlib -n <ProjectName> -o <ProjectName>
```

- 已安裝 .NET 10 SDK 時，`dotnet new classlib` 產生的 `.csproj` 預設 `<TargetFramework>net10.0</TargetFramework>`，
  不需要額外加 `-f net10.0`。
- **刪除範本自動產生的 `Class1.cs`**——這是空殼檔案，不刪會留下沒用到的類別。

## Step 5：每個型別各自一個 .cs 檔案

- 檔名＝型別名稱＋`.cs`（例如 `Account.cs`、`RentalOrder.cs`），一個檔案裡**只能有一個** `class` /
  `enum` / `interface`。就算 enum 只有兩三個值，也要獨立成檔，不要跟用到它的 class 放在同一個檔案。
- 類別名稱、方法名稱一律轉成 C# 慣例的 **PascalCase**（Mermaid 常用 camelCase，例如
  `registerAccount` → `RegisterAccount`）；屬性也用 PascalCase 的 auto-property（`username` →
  `public string Username { get; set; }`）。
- `string` 屬性給預設值 `= string.Empty;` 避免 nullable 警告（專案模板預設開啟 `<Nullable>enable</Nullable>`）。
- 方法主體先用 `throw new NotImplementedException();` 佔位，除非使用者要求要把邏輯也一併實作出來。
- namespace 用專案名稱（例如 `namespace RentalSystem.Domain;`），用檔案範圍宣告（`;` 結尾），不要巢狀大括號。

範例（來自驗證用的 `Car` 類別）：

```csharp
namespace RentalSystem.Domain;

public class Car
{
    public string CarType { get; set; } = string.Empty;

    public double DailyRate { get; set; }

    public Car SelectCarType(string carType)
    {
        throw new NotImplementedException();
    }

    public double GetDailyRate()
    {
        throw new NotImplementedException();
    }
}
```

## Step 6：處理類別間的關聯

- 關聯／聚合／組合（`-->`、`o--`、`*--`）：在來源類別加一個屬性參考目標類別（單一物件用目標類別型別，
  一對多用 `List<目標類別>`），型別給 `?` 或初始化視 nullable 設定而定。例如 `RentalOrder --> Car` 轉成
  `RentalOrder` 裡的 `public Car? SelectedCar { get; set; }`。
- 繼承（`--|>`）：`public class Sub : Base`。
- 實作介面（`..|>`）：`public class C : IInterface`，介面另外開一個 `IInterface.cs`。

## Step 7：建置驗證

```bash
cd <ProjectName>
dotnet build
```

必須是 0 error 才算完成。

## Step 8：完成後回報使用者

- 專案路徑與建立的檔案清單。
- 若因為 Step 3/Gotchas 的原因補了圖上沒有的 stub 型別，逐一列出，並說明是「為了讓程式碼能編譯而補的最小
  版本」，請使用者確認要不要補完欄位或改設計。
- `dotnet build` 的結果（0 warning / 0 error，或列出還沒解決的錯誤）。

## Gotchas

- **Mermaid 方法簽章常引用圖上沒有定義的型別**：例如 `Account.registerAccount(accountInfo : AccountInfo)`，
  但整張圖沒有 `AccountInfo` 這個 class。直接照抄會編譯失敗；正確做法是建立一個最小的 stub class（把能從
  上下文推斷出的欄位放進去即可），並在 Step 8 回報時明確列出，而不是默默把參數型別改成 `object` 或刪掉。
- **`Float` 不要無腦對應 `float`**：C# 的商業邏輯（租金、金額）用 `double` 或 `decimal` 比 `float` 精度
  更符合預期，預設用 `double`，金額精確計算的場景才用 `decimal`。
- **範本產生的 `Class1.cs` 一定要刪**，否則專案裡會留一個沒用到的空類別檔案，跟「每個型別一個檔案」的
  規則沒有直接衝突，但屬於沒用到的雜物，應該清掉。
- **enum 也要獨立成檔**：即使某個 enum 只在單一屬性型別裡用到、只有兩三個值，也要開一個獨立的
  `<EnumName>.cs`，不要圖方便跟它所屬的 class 放在同一個檔案裡。
- **已安裝多個 .NET SDK 時**：`dotnet new classlib` 會用當前生效的最新 SDK 版本當預設
  `TargetFramework`（本機是 `net10.0`）；若專案目錄有 `global.json` 鎖定較舊版本，需要先確認
  `dotnet --version` 的輸出，必要時手動把 `.csproj` 的 `<TargetFramework>` 改成 `net10.0`。
