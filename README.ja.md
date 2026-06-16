# LockCat

![LockCat pixel logo](src/LockPig/Assets/Pixel/target-logo.png)

こんにちは、LockCat です。Windows のデスクトップに小さく座って、必要なときにキーボードと画面を一時的にロックするピクセル猫です。普段はのんびり、見張り中はまじめです。

![LockCat desktop cat](src/LockPig/Assets/Cat/Sprites/cat-idle.png)

[English](README.md) | [中文](README.zh-CN.md) | [Issue を送る](https://github.com/Throb7777/LockCat/issues/new/choose)

## できること

- ホットキーでキーボードを一時ロックします。
- 設定すると、ロック時に画面もオフにします。
- 別のホットキーで復帰します。
- システムトレイからすぐ開けます。
- デスクトップに小さなピクセル猫を表示します。
- ホットキー、言語、猫の設定を保存します。
- インストールとアンインストールは自分の `LockCat` フォルダー内だけを扱います。

## 必要環境

- Windows 10 以降。
- x64 Windows。
- 通常利用にアカウントやネットワーク接続は不要です。

## ダウンロードとインストール

1. [GitHub Releases](https://github.com/Throb7777/LockCat/releases) から `LockCatInstaller.exe` をダウンロードします。
2. インストーラーを実行します。
3. 必要ならインストール先を変更します。
4. Windows 起動時に LockCat も起動するか選びます。
5. 完了すると、LockCat はトレイに入り、設定に応じてデスクトップ猫も表示されます。

フォルダーを選ぶと、その中に `LockCat` フォルダーを作ってインストールします。

```text
選んだ場所: D:\Apps
実際の場所: D:\Apps\LockCat
```

これでファイルがひとつの小さな家にまとまります。

## 既定のホットキー

- ロック: `Ctrl + Alt + K`
- 復帰: `Ctrl + Alt + P`

どちらも設定画面で変更できます。

## デスクトップ猫

デスクトップ猫は飾りだけではなく、LockCat への小さな入口です。

- クリックすると反応します。
- 続けて 3 回クリックするとロックします。最初の 2 回は、ロックする前に確認します。
- 右クリックでクイックメニューを開きます。
- ドラッグして好きな場所に移動できます。
- 非表示にした場合は、トレイメニューまたは設定から再表示できます。
- 常に手前に表示したい場合は、設定で「最前面」を有効にできます。

## 設定

設定はシステムトレイ、または猫の右クリックメニューから開けます。

変更できるもの:

- ロック用ホットキー。
- 復帰用ホットキー。
- ロック時に画面をオフにするか。
- デスクトップ猫を表示するか。
- 猫を常に手前に表示するか。
- Windows 起動時に自動起動するか。
- 表示言語。

## アンインストール

スタートメニューのアンインストール項目、またはインストール先の `LockCatUninstaller.exe` を使います。

小さな猫からの約束です。アンインストーラーは LockCat がインストールしたファイルだけを削除します。セットアップ時に選んだフォルダー全体を消すことはありません。設定を残すと、次回インストール時に同じホットキーや好みを使えます。

## プライバシー

LockCat は入力内容そのものを読み取りません。設定されたホットキーと、猫のアニメーションに必要な簡単な状態だけを扱います。設定はあなたの PC に保存されます。

## ソースからビルド

.NET 8 SDK をインストールしてから実行します。

```powershell
dotnet build src\LockPig\LockPig.csproj -c Release
dotnet build src\LockCat.Installer\LockCat.Installer.csproj -c Release
dotnet build src\LockCat.Uninstaller\LockCat.Uninstaller.csproj -c Release
dotnet run --project qa-uninstall-safety\LockCatUninstallSafetyProbe.csproj -c Release
```

メインアプリは `src/LockPig`、インストーラーとアンインストーラーは `src/LockCat.Installer` と `src/LockCat.Uninstaller` にあります。

Release 用の単一ファイルインストーラーを作る場合は、この順番で publish します。

```powershell
dotnet publish src\LockPig\LockPig.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist\LockCat
dotnet publish src\LockCat.Uninstaller\LockCat.Uninstaller.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist\LockCat-uninstaller-build
$payload = (Resolve-Path dist\LockCat).Path
$uninstaller = (Resolve-Path dist\LockCat-uninstaller-build\LockCatUninstaller.exe).Path
dotnet publish src\LockCat.Installer\LockCat.Installer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true "-p:PayloadSourceDir=$payload" "-p:UninstallerBundlePath=$uninstaller" -o dist\LockCat-installer
```

## フィードバック

気になるところや提案があれば、こちらから送ってください。

https://github.com/Throb7777/LockCat/issues/new/choose

小さなノートを持って確認します。

## ライセンス

LockCat は MIT License で公開されています。第三者のフォントや素材については [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) を参照してください。
