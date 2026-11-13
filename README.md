# 📬 World Mail Alerts 1.8.1

**Never lose a mail with items again.** A BepInEx plugin for SPT 4.1.x that warns you with a sound and the game's own notification when a mail with items (insurance, trader rewards…) is about to expire. Menu and notifications in 16 languages.

![version](https://img.shields.io/badge/version-1.8.1-blue) ![SPT](https://img.shields.io/badge/SPT-4.1.x-green) ![license](https://img.shields.io/badge/license-MIT-lightgrey)

## ✨ What you get
- **Alerts from the moment you log in**: no need to open the messenger, the mod loads the trader mails with items by itself.
- **One notification per trader**, the most urgent first: `Prapor : mail to collect before 3d 4h (5 mails)`.
- **Insurance returns are named as such**: `Prapor : insurance return to collect before 3d 4h`.
- **4 alert levels** (by default 1 day, 6 h, 1 h, 10 min), each with its own delay, sound, color and repeat (hours / minutes / seconds).
- **The game's own notification**, placed **anywhere on screen** with two sliders or by dragging it with the mouse, with a live preview while you set it up.
- **19 message formats**, 17 text styles, text size, letter spacing, 15 icons, short or long display.
- **226 built-in sounds** + your own `.wav` files (`BepInEx/config/WorldMailAlerts_sounds`).
- **Silent in raid**: the missed alert is replayed when you are back in the menu.
- Optional: startup summary, blinking, rainbow text, mute the game's own notification sound.

## 🌍 Languages
English, Français, Русский, Deutsch, Español, 中文, 日本語, 한국어, Polski, Português, Čeština, Türkçe, Italiano, Magyar, Română, Slovenčina.

Setting `00. Language`: **Auto** uses your system language (English is the fallback). Restart the game after changing it. Settings are reset when the language changes.

> Translations are machine-style and may contain mistakes. Spotted one? Open an issue, corrections are welcome.

## 📦 Compatibility
- SPT 4.1.x (BepInEx 5)
- **Requires [World Mail Countdown](https://forge.sp-tarkov.com) 1.2.0 or newer.** World Mail Alerts does not load without it.

## 🔧 Installation
1. Install **World Mail Countdown** first.
2. Copy the `BepInEx` folder into your SPT folder:

```
BepInEx/plugins/WorldMailCountdown.dll   (World Mail Countdown)
BepInEx/plugins/WorldMailAlerts.dll      (this mod)
```

Settings: press **F12** in game → **World Mail Alerts**.

## 📝 Changelog
### 1.8.1
- New setting: notification duration (Default / Long).

### 1.8.0
- Menu and notifications in 16 languages.

### 1.6.x
- Alerts right after login, without opening the messenger.
- One notification per trader, with the number of mails.
- Insurance returns named in the notification.
- Notifications stay together and fade out in place.
- Volume applied to every alert.

### 1.5.x
- Live preview of the real notification, message formats, text styles, icons.

### 1.4.x
- Free notification position, text size, custom `.wav` folder.

## 🐞 Bugs
Open an issue, or send me a private message on Discord: **PraED_tv**. Please attach your `BepInEx/LogOutput.log`.
