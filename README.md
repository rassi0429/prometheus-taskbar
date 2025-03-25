# Prometheus Taskbar

Prometheus Taskbarは、PrometheusメトリクスをWindowsのタスクバーに表示するシンプルなアプリケーションです。システムの状態を常に監視しながら、作業を中断することなく重要なメトリクスを確認できます。

## 機能

- Prometheusサーバーからメトリクスをリアルタイムで取得
- タスクバーのシステムトレイにメトリクス値を表示
- メトリクスのカスタム表示形式（値のみ、名前と値、カスタム形式）
- しきい値を超えた場合のアラート表示（色変更）
- Basic認証のサポート
- PromQLクエリのサポート

## 必要条件

- Windows 10/11
- .NET 8.0以上
- Prometheusサーバー（ローカルまたはリモート）

## インストール方法

1. 最新のリリースから`PrometheusTaskbar.zip`をダウンロード
2. ZIPファイルを任意の場所に解凍
3. `PrometheusTaskbar.exe`を実行

## 使い方

### 初期設定

1. アプリケーションを初めて起動すると、システムトレイにアイコンが表示されます
2. アイコンを右クリックして「設定を開く」を選択
3. 「接続」タブでPrometheusサーバーのURLを設定（デフォルト: http://localhost:9090）
4. 必要に応じて認証情報を設定
5. 「メトリクス」タブで表示したいメトリクスを追加
6. 「表示」タブで表示形式やアラート設定をカスタマイズ
7. 「保存」をクリックして設定を適用

### メトリクスの追加

1. 設定画面の「メトリクス」タブで「追加」ボタンをクリック
2. メトリクス名を入力（例: `node_cpu_seconds_total`）
3. 必要に応じてラベルを追加（例: `mode="idle"`）
4. または、直接PromQLクエリを入力（例: `sum(rate(node_cpu_seconds_total{mode="idle"}[1m]))`）
5. 表示名と単位を設定
6. 「保存」をクリックしてメトリクスを追加

### 表示設定

1. 設定画面の「表示」タブで表示形式を選択
   - 値のみ: メトリクス値だけを表示（例: `85.5%`）
   - 名前と値: メトリクス名と値を表示（例: `CPU: 85.5%`）
   - カスタム: 独自の形式を定義（例: `{name}: {value}{unit}`）
2. 小数点以下の桁数を設定
3. 単位の表示/非表示を設定
4. アラート設定を構成（しきい値と色）

### 日常的な使用

- システムトレイのアイコンにメトリクス値が表示されます
- アイコンにカーソルを合わせると、ツールチップに詳細情報が表示されます
- アイコンを右クリックしてコンテキストメニューを表示:
  - 「更新」: メトリクスを手動で更新
  - 「設定を開く」: 設定画面を表示
  - 「終了」: アプリケーションを終了
- アイコンをダブルクリックすると設定画面が開きます

## ビルド方法

```
git clone https://github.com/yourusername/prometheus-taskbar.git
cd prometheus-taskbar
dotnet build
```

## 開発環境のセットアップ

1. リポジトリをクローン: `git clone https://github.com/yourusername/prometheus-taskbar.git`
2. 依存関係をインストール: `dotnet restore`
3. プロジェクトをビルド: `dotnet build`
4. アプリケーションを実行: `dotnet run --project PrometheusTaskbar/PrometheusTaskbar.csproj`

## ライセンス

[MIT License](LICENSE)
