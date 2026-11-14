# CLAUDE.md

Precision Readout。共通ルールは HQ の `../CLAUDE.md`、仕様は `SPEC.md`。

- ビルド確認: `dotnet build PrecisionReadout.sln -p:ModInstallPath=`(Debug は mods フォルダへコピーするので、確認だけならコピーを止める)。
- 書式の核は `Core/ExactFormat.cs`(double の厳密な10進展開)。`Scope`(`Core/Scope.cs`)が有効なときだけ `GameUtil` の書式関数を置き換える。
- 実機未確認。初回登録は HQ の「初回登録」の順序を守る。ハーネステスト(`HarnessTests.cs`)は未作成。
