# CLAUDE.md

Precision Readout。共通ルールは HQ の `../CLAUDE.md`、仕様は `SPEC.md`。

- ビルド確認: `dotnet build PrecisionReadout.sln -p:ModInstallPath=`(Debug は mods フォルダへコピーするので、確認だけならコピーを止める)。
- 書式の核は `Core/ExactFormat.cs`(double の厳密な10進展開)。`Scope`(`Core/Scope.cs`)が有効なときだけ `GameUtil` の書式関数を置き換える。
- PLib(4.25.0)を使う: ホットキーを `ModActions` で登録(キー設定画面で変更可)、文字列は `PLocalization`(`strings/<言語>.po`)。ILRepack で PLib を DLL に取り込むので、配布は DLL 1 つ。
- ハーネステスト(`HarnessTests.cs`、タグ `precisionreadout`)8 件。サムネイルの撮影用に `precisionreadout-shot` もある(通常のタグでは動かない)。
