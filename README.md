# SXG2026 プログラミングバトル<br>～スーパータンクウォーズ～

<div align="center">
  <img width="880" alt="Header" src="Documents/ReadMeImages/Top/000.png" />
</div>

## 概要

本リポジトリは、イベント **SXG2026** の一般参加企画「スーパータンクウォーズ」プログラミングバトル用のUnityプロジェクトです。
**ゲーム内に登場するAI戦車** を作成・提出いただき、イベント当日に集まったAI戦車で対戦を行います。

公開プロジェクトでは、制作した戦車をサンプル戦車と対戦させて動作確認できます。本番のトーナメント運営機能は含まれていません。

<br>

---

## 重要：まずはここから（ダウンロード方法）

### Unityプロジェクトで制作する方

1. SXG2026向けUnityプロジェクトの配布ページを開きます。
2. **Assets** からSXG2026向けのプロジェクトZIPをダウンロードします。
3. ZIPを展開して、Unity Hubでプロジェクトを開きます。

- SXG2026向けUnityプロジェクトの配布ページ：https://XXX/sxg2026-unity-release

<div align="center">
  <img width="880" alt="ReleasesのAssetsからZIPをダウンロードする操作例" src="Documents/ReadMeImages/Top/002.png" />
</div>

※画像はダウンロード操作の例です。ダウンロードするZIPの名前は、SXG2026向け配布ページの案内を確認してください。

> 補足：Unityに不慣れな方は、プロジェクトを開いて動作確認するまでの手順を先に読むと迷いにくいです。
> - [『Unity初心者向け：起動・動作確認ガイド』](Documents/ReadMeFiles/README_UnityBeginnerSetup.md)

> 補足：すぐに作業を始めたい方は、次の2つを見ると迷いにくいです。
> - [『AI作成手順』](Documents/ReadMeFiles/README_HowToCreate.md)
> - [『AI提出手順』](Documents/ReadMeFiles/README_HowToSubmit.md)

### Unityを使わずに制作する方

配布ビルドを使用すると、Unityをインストールせずに戦車を制作し、提出用ZIPを出力できます。

- SXG2026向け配布ビルド：https://XXX/sxg2026-build

### 上級者向け（任意：Gitで取得する）

Gitに慣れている方は、このリポジトリをCloneして使っても構いません。
更新があった場合はPullで最新版にできます。
※初めての方は、配布ページからZIPを取得するほうが簡単です。

<br>

---

## 開発環境

- Unity 6000.3.24f1（Unityプロジェクトを使用する場合）
- 解像度 1920x1080 固定

<br>

---

## 進め方（最短手順）

以下は、Unityプロジェクトで制作する場合の手順です。

- **Step 1.**　[『AI作成手順』](Documents/ReadMeFiles/README_HowToCreate.md) に沿って、挑戦者（戦車）を作成します。挑戦者の作成には、connpassの受付番号が必要です。
- **Step 2.**　[『戦車のカスタマイズ方法』](Documents/ReadMeFiles/README_HowToCustomizeTank.md) を見ながら、砲塔や装甲を追加して試運転します。
- **Step 3.**　[『リファレンス』](Documents/ReadMeFiles/README_Reference.md) を確認しながら、プログラムを調整します。
  - プログラムが苦手な人や、アイデアが欲しい人は [『かんたんAI作成機能』](Documents/ReadMeFiles/README_HowToEasyAI.md) もご利用ください。
- **Step 4.**　[『AI提出手順』](Documents/ReadMeFiles/README_HowToSubmit.md) に沿って、提出用ZIPを作成して提出します。

<br>

---

## ゲーム内容（概要）

ステージ上で戦車を動かし、大砲を撃ち合って戦うゲームです。
**倒した相手の戦車の出撃コストと、試合終了時の残機ボーナス**でスコアが決まり、試合終了時に最もスコアを獲得したAI戦車の勝利です。

- 相手に砲弾を当てて倒したら、倒した敵の出撃コストがポイントとして加算されます。
- ステージ外に落下すると大破します（残りのコストで出撃できれば復帰します）。
- ステージ中央には障害物があり、地形を活かした立ち回りも重要です。
- SXG2026では、上空に留まり続けて戦闘を避ける戦車への対策として、上空にいる戦車をステージ中央方向へ吹き飛ばすルールを追加しています。

<br>

---

## 募集要項（概要）

SXG2026への参加エントリーは**connpassのみ**で受け付けます。戦車を作成するときは、エントリー後に発行される受付番号を使用してください。

**基礎的なC#プログラミングができれば**、年齢・職業・お住まい問わず参加可能です。
Unity初心者の方でも、サンプルコードや「かんたんAI作成機能」を使って調整できます。Unityを使わずに制作する場合は、配布ビルドを使用できます。

- 募集開始予定：2026年10月1日
- イベント開催予定：2026年11月15日
- 提出締切：2026年11月8日（日）23:59
- connpassページ：https://connpass.com/event/408297/
- 提出フォーム：https://forms.gle/DuWm4o32XAxjv1a16

<br>

---

## お問い合わせ

イベントやAI作成に関するお問い合わせは、イベントページからお願いします。

- connpassページ：https://connpass.com/event/408297/

<br>

---

## 🔗 関連リンク（PV・公式記事など）

- YouTube
  - PV（まずはこちら）：https://youtu.be/TrEf7w3QpWI
  - チャンネル：https://www.youtube.com/@xeenjp

- 公式X（最新情報）
  - https://x.com/xeenjp

- 公式ブログ（過去大会の記事）
  - 2026-02-12：TIGS2026 プロバト参加者募集：https://www.xeen.co.jp/staffblog/2026/02/staffblog-20250212.html
  - 2025-10-01：SXG2025 出展紹介：https://www.xeen.co.jp/staffblog/2025/10/blog-20251001.html
  - 2025-11-19：イベントレポート：https://www.xeen.co.jp/staffblog/2025/11/staffblog-20251119.html