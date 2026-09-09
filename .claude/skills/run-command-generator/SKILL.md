---
name: run-command-generator
description: 將緊接在此 skill 呼叫之後的提示詞，或使用者在對話中標記／引用的文字，轉換成一個新的 Claude Code 自訂 slash command，寫成 .claude/commands/ 底下的 .md 檔案。當使用者說「把這段變成 command」「存成 slash command」「幫我做一個指令」「turn this into a command」「save this as a slash command」「make a command from the above」時使用。
---

把一段提示詞（prompt）沉澱成可重複呼叫的 Claude Code slash command。這不是在操作一個正在跑的應用程式，
所以沒有 driver script — 這個 skill 的「執行工具」就是 Write/Edit 本身，直接讀懂來源文字、寫出格式正確的
command 檔案即可。

## Step 1：確定來源文字

- 若呼叫時有帶參數（例如 `/run-command-generator <text>`），該參數就是來源文字。
- 若沒有帶參數，來源文字是使用者在對話中標記／引用／指向的那段內容 —— 可能是上一則訊息、一段程式碼
  區塊、或使用者說「把上面這段變成 command」時所指的內容。
- 若不確定要用哪一段，直接問使用者，不要用猜的。

## Step 2：決定 command 名稱與存放位置

- **名稱**：使用者若有明確指定就 slugify（小寫、連字號、僅英數字）；否則從來源文字的核心動作/意圖推斷
  一個簡短的 kebab-case 名稱（例如「幫我審查 diff 找 bug」→ `review-diff`）。
- **位置**：預設寫到專案層級 `<cwd>/.claude/commands/<name>.md`。只有當使用者明確要求「全域」「所有專案
  都能用」或提到 `~/.claude` 時，才改寫到個人層級 `~/.claude/commands/<name>.md`（會套用到所有專案，動手
  前先跟使用者確認）。
- 需要分類/命名空間時可用子資料夾，例如 `.claude/commands/frontend/component.md` 會變成可用
  `/frontend:component` 呼叫。
- `.claude/commands/` 目錄不存在就先建立。
- 若同名檔案已存在：先讀出內容比對，問使用者是否要覆蓋，不要靜默覆蓋既有 command。

## Step 3：寫出 command 檔案內容

Frontmatter 欄位（Claude Code 自訂 command 支援）：

| 欄位 | 用途 |
|---|---|
| `description` | 一行簡短描述。`/help` 清單會顯示，也是模型判斷是否自動觸發此 command 的依據 |
| `argument-hint`（選用） | 提示呼叫時要帶的參數，例如 `[file] [message]` |
| `allowed-tools`（選用） | 若 body 用到 `` !`command` `` 執行 bash，需在此明確列出允許的指令，例如 `Bash(git status:*)` |
| `model`（選用） | 只有來源文字明確要求特定模型才需要加 |

Body（提示詞模板）撰寫原則：

- 把來源文字整理成可重複使用的 prompt，**盡量保留原本用詞與意圖**，只做必要整理（去除口語贅詞、修掉明
  顯格式問題），不要過度改寫或加料。
- 來源文字中「每次呼叫可能不同」的部分（檔名、目標對象、訊息內容…），替換成 `$ARGUMENTS`（整包參數）
  或 `$1`、`$2`…（位置參數），並在 `argument-hint` 註明。
- 若來源文字提到「先讀某個檔案」，用 `@path/to/file` 語法把檔案內容帶進 context。
- 若來源文字提到「先跑某個指令再處理結果」，用 `` !`command` `` 語法在 command 執行時先跑該 bash 指令
  並把輸出注入 prompt；同時要在 frontmatter 補上對應的 `allowed-tools`。

## Step 4：完成後

1. 用 Write（新檔案）或 Edit（改既有檔案）寫入。
2. 回報使用者：檔案路徑、如何呼叫（`/<name> [args]`）、最終的 frontmatter `description`。
3. 不要自動執行剛建立的 command，除非使用者另外要求。

## Gotchas

- command 名稱只能是英數字、連字號、底線，不能有空白或中文；使用者要中文名稱時，取一個對應的英文
  slug，中文說明留在 `description` 裡即可。
- `description` 空泛（例如「小工具」）會讓模型很難判斷何時該自動帶出這個 command —— 要寫進使用者實際
  會打的動詞/情境字詞。
- 個人層級 command（`~/.claude/commands/`）對所有專案生效，寫入前務必確認範圍，不要預設寫到全域。
