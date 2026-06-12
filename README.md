# 開発中止

.net mauiコレクションのスワイプ関連が非常にアレで、色々とCodexにやってもらいましたが全く上手くいかないので中止。

Javaのエコシステムに関わりたくなかったのですが、次はKotlinでHotentryReaderをやります。

# HotentryReader

HotentryReader は、はてなブックマークのホッテントリを快適に閲覧するための Android アプリです。

ログイン機能やブックマーク機能は持たず、人気記事を読む体験に絞った個人開発の閲覧専用アプリです。カテゴリごとのホッテントリを RSS から取得し、ダークテーマの縦リストで表示します。

## スクリーンショット

| ホーム | ミュートワード設定 |
| --- | --- |
| <img src="docs/images/home.png" alt="ホーム" width="320"> | <img src="docs/images/settings.png" alt="ミュートワード設定" width="320"> |

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

### GitHub Releases から APK をダウンロードする

通常利用する場合は、GitHub Releases から APK をダウンロードしてください。

1. このリポジトリの `Releases` を開く
2. 最新のリリースを選択する
3. `Assets` から `HotentryReader-v0.1.0.apk` のような APK ファイルをダウンロードする
4. Android 端末に APK を転送する
5. APK を開いてインストールする

Android 側で「提供元不明のアプリ」のインストール許可が必要になる場合があります。

### 開発環境からビルドする

```powershell
dotnet build HotentryReader.csproj -f net10.0-android
```

APK を作成して実機へインストールする場合:

```powershell
dotnet build HotentryReader.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true
adb install -r bin\Debug\net10.0-android\com.companyname.hotentryreader-Signed.apk
```

## リリース運用

HotentryReader は Git tag ベースで GitHub Releases を作成し、APK は Release の Assets として配布します。

APK や AAB などのビルド成果物はリポジトリには含めません。

### 初回リリース v0.1.0 の作成手順

1. リリース用の変更を main ブランチに反映する
2. Android APK をビルドする

```powershell
dotnet build HotentryReader.csproj -f net10.0-android -c Release -p:EmbedAssembliesIntoApk=true
```

3. 生成された APK を分かりやすい名前にリネームする

```powershell
Copy-Item bin\Release\net10.0-android\com.companyname.hotentryreader-Signed.apk HotentryReader-v0.1.0.apk
```

4. Git tag を作成して push する

```powershell
git tag -a v0.1.0 -m "HotentryReader v0.1.0"
git push origin v0.1.0
```

5. GitHub の `Releases` から `v0.1.0` の Release を作成する
6. Release Notes には [docs/releases/v0.1.0.md](docs/releases/v0.1.0.md) の内容を使用する
7. `HotentryReader-v0.1.0.apk` を Release Assets に添付する

## ライセンス

ライセンスは未定です。
