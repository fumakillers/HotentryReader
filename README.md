# HotentryViewer

HotentryViewer は、はてなブックマークのホッテントリを快適に読むための Android アプリです。

ログインやブックマーク機能は持たず、人気記事を眺める体験に絞ったシンプルな閲覧専用アプリとして開発しています。カテゴリごとのホッテントリを RSS から取得し、ダークテーマの縦リストで表示します。

## スクリーンショット

画像は後で追加予定です。

| ホーム | 設定 |
| --- | --- |
| Coming soon | Coming soon |

## 主な機能

- ホッテントリ一覧表示
- 記事詳細表示
- はてなブックマークのコメント閲覧
- 左右スワイプによるカテゴリ切り替え
- プルリフレッシュ
- ミュートワード
- 正規表現ミュート

## 使用技術

- .NET MAUI
- C#
- XAML
- Android
- HttpClient
- System.Xml.Linq
- Microsoft.Extensions.Caching.Memory
- JSON ファイルによる設定保存

## 開発環境

- .NET 10
- .NET MAUI
- Visual Studio / dotnet CLI
- Android SDK
- Android 実機またはエミュレータ

現在の主なターゲットは Android です。

## インストール方法

開発環境でビルドして実機へインストールする場合:

```powershell
dotnet build HotentryReader.csproj -f net10.0-android
```

APK を作成してインストールする場合:

```powershell
dotnet build HotentryReader.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true
adb install -r bin\Debug\net10.0-android\com.companyname.hotentryreader-Signed.apk
```

## GitHub Releases から APK をダウンロードする

配布版 APK は GitHub Releases に掲載予定です。

1. GitHub のリポジトリページを開く
2. `Releases` を開く
3. 最新リリースを選択する
4. Assets から `.apk` ファイルをダウンロードする
5. Android 端末に APK を転送してインストールする

Android 側で提供元不明のアプリのインストール許可が必要になる場合があります。

## ライセンス

ライセンスは未定です。

