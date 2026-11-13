using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MailCountdown
{
    // Plugin compagnon : alertes sonores + notification native du jeu. Ne touche pas au compteur (WorldMailCountdown 1.2.0).
    [BepInPlugin("praedlab.mailalerts", "World Mail Alerts", "1.8.1")]
    [BepInDependency("praedlab.mailcountdown", "1.2.0")]
    public class MailAlerts : BaseUnityPlugin
    {
        static ManualLogSource Log;
        const int AN = 4;
        static string[] WhenNames = { "1 min", "2 min", "5 min", "10 min", "15 min", "20 min", "30 min", "45 min", "1 h", "90 min", "2 h", "3 h", "4 h", "6 h", "8 h", "12 h", "18 h", "1 jour", "36 h", "2 jours", "3 jours", "4 jours", "5 jours", "7 jours" };
        static readonly float[] WhenMin = { 1, 2, 5, 10, 15, 20, 30, 45, 60, 90, 120, 180, 240, 360, 480, 720, 1080, 1440, 2160, 2880, 4320, 5760, 7200, 10080 };
        static readonly string[] DefWhen = { "1 jour", "6 h", "1 h", "10 min" };
        static readonly string[] DefSound = { "Soft ping", "Chime", "Marimba", "Arpeggio" };
        static readonly Color[] Cols = { new Color(0.3f, 0.9f, 1f), new Color(1f, 0.9f, 0.2f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.25f, 0.25f) };

        static ConfigEntry<bool> cfgOn, cfgTest;
        static ConfigEntry<float> cfgVol, cfgPosX, cfgPosY; static ConfigEntry<bool> cfgMove, cfgPreview, cfgRainbow, cfgRaidSilent, cfgRaidReplay, cfgBlink, cfgSummary, cfgHistory, cfgHistOpen, cfgHistClear;
        static ConfigEntry<int> cfgBlinkCount, cfgSummaryHours, cfgFontSize;
        static ConfigEntry<string> cfgFontStyle, cfgSoundDir;
        static ConfigEntry<bool> cfgOpenDir;
        static ConfigEntry<KeyboardShortcut> cfgHistKey;
        static MailAlerts inst; static ConfigEntry<float> cfgRbSpeed; static ConfigEntry<Color>[] cfgColor = new ConfigEntry<Color>[4]; static float nextPreview;
        static ConfigEntry<string> cfgListen, cfgIcon;
        static ConfigEntry<string>[] cfgWhen = new ConfigEntry<string>[AN], cfgSound = new ConfigEntry<string>[AN];
        static ConfigEntry<bool>[] cfgAlertOn = new ConfigEntry<bool>[AN];
        static ConfigEntry<int>[] cfgRH = new ConfigEntry<int>[AN], cfgRM = new ConfigEntry<int>[AN], cfgRS = new ConfigEntry<int>[AN];

        static Type msgType, snType;
        static FieldInfo fAll, fId, fMax, fUtc;
        static PropertyInfo pReward;
        static bool ready, failed;
        static float nextScan; static int slowLogged, hitchLogged;
        static readonly HashSet<string> alerted = new HashSet<string>();
        static readonly Dictionary<string, float> lastFire = new Dictionary<string, float>();
        static readonly List<object> buf = new List<object>();

        const string CustomPrefix = "Perso: ";
        static string[] SoundNames;
        static readonly Dictionary<string, string> customFiles = new Dictionary<string, string>();

        static void ScanCustomSounds()
        {
            SoundNames = Synth.Names;
            try
            {
                string dir = Path.Combine(BepInEx.Paths.ConfigPath, "WorldMailAlerts_sounds");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "LISEZ-MOI.txt"),
                        "Depose tes fichiers .wav ici (PCM 8/16/24/32 bits ou float, mono ou stereo, 30 s max, 20 Mo max).\r\n" +
                        "Relance le jeu : ils apparaissent dans les listes de sons du menu F12 sous 'Perso: <nom du fichier>'.\r\n" +
                        "MP3/OGG : convertis d'abord en WAV (par exemple avec Audacity).\r\n");
                }
                var names = new List<string>(Synth.Names);
                var files = Directory.GetFiles(dir, "*.wav");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string f in files)
                {
                    string n = CustomPrefix + Path.GetFileNameWithoutExtension(f);
                    if (customFiles.ContainsKey(n)) continue;
                    customFiles[n] = f; names.Add(n);
                }
                SoundNames = names.ToArray();
                if (files.Length > 0) Log.LogInfo("Sons perso trouves : " + files.Length);
            }
            catch (Exception e) { Log.LogWarning("Dossier de sons perso : " + e.Message); SoundNames = Synth.Names; }
        }

        static AudioClip LoadCustom(string name)
        {
            string path;
            if (!customFiles.TryGetValue(name, out path)) return null;
            float[] data; int ch, rate; string err;
            if (!WavLoader.TryLoad(path, out data, out ch, out rate, out err))
            { Log.LogWarning("Son perso '" + Path.GetFileName(path) + "' ignore : " + err); return null; }
            var clip = AudioClip.Create("mailalert_" + name, data.Length / ch, ch, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly HashSet<string> rendering = new HashSet<string>();
        static readonly Queue<KeyValuePair<string, float[]>> readyQ = new Queue<KeyValuePair<string, float[]>>();
        static readonly Dictionary<string, float> wantPlay = new Dictionary<string, float>();
        static readonly object renderLock = new object();
        static string pendingListen = "";

        static MethodInfo mNotif; static Type nullableColor; static bool notifTried;
        static bool fr;

        static ConfigEntry<string> cfgLang, cfgDuration;
        static string[] StyleNames, FormatNames;
        static readonly int[] StyleFlags = { 0, 1, 2, 3, 16, 17, 8, 32, 4, 5, 64, 6, 20, 18, 19, 33, 65 };
        static readonly int[] DefWhenIdx = { 17, 13, 8, 3 };
        static readonly string[] Templates = {
            "{V} - {MAIL} : {EXP}", "{V} : {CB}", "{V} : {LEFT}", "{LEFT} ({V})", "{MAIL} ({V}) : {X}", "{V} | {X}", "{X} | {MAIL} | {V}",
            "{ATTN} - {V} - {X}", "{URGENT} {V} - {X}", "{REMIND} : {X} ({V})", "{DEADLINE} : {X} ({V})", "{SOON} : {V} - {X}", "{WAITS} {X}",
            "[{MAIL}] {V} - {X}", "{V} >> {X}", "! {V} - {X} !", "{MAIL} : {EXP}", "{X}", "{FROM} : {EXP}" };

        static string PickLang(string v)
        {
            int k = Array.IndexOf(Loc.Names, v);
            if (k >= 0) return Loc.Codes[k];
            switch (Application.systemLanguage)
            {
                case SystemLanguage.French: return "fr"; case SystemLanguage.Russian: return "ru"; case SystemLanguage.German: return "de";
                case SystemLanguage.Spanish: return "es"; case SystemLanguage.Chinese: case SystemLanguage.ChineseSimplified: case SystemLanguage.ChineseTraditional: return "zh";
                case SystemLanguage.Japanese: return "ja"; case SystemLanguage.Korean: return "ko"; case SystemLanguage.Polish: return "pl";
                case SystemLanguage.Portuguese: return "pt"; case SystemLanguage.Czech: return "cs"; case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.Italian: return "it"; case SystemLanguage.Hungarian: return "hu"; case SystemLanguage.Romanian: return "ro"; case SystemLanguage.Slovak: return "sk";
            }
            return "en";
        }
        static string Safe(string x) { return x.Replace("'", "\u2019").Replace("=", "-").Replace("\\", "/").Replace("\"", "").Replace("[", "(").Replace("]", ")").Replace("\n", " ").Replace("\t", " ").Trim(); }
        static string K(int n, string id) { return n.ToString("00") + ". " + Safe(Loc.T(id)); }
        static string Sec(int n, string id) { return n.ToString("00") + ". " + Safe(Loc.T(id)); }
        static string D(string id) { return Loc.T("d_" + id); }

        static void BuildLists()
        {
            var wn = new string[WhenMin.Length];
            for (int i = 0; i < WhenMin.Length; i++)
            {
                int m = (int)WhenMin[i];
                if (m >= 1440 && m % 1440 == 0) { int d = m / 1440; wn[i] = d + " " + Loc.T(d == 1 ? "w_day" : "w_days"); }
                else if (m >= 60 && m % 60 == 0) wn[i] = (m / 60) + " " + Loc.T("w_h");
                else wn[i] = m + " " + Loc.T("w_min");
            }
            WhenNames = wn;
            string[] words = { "s_bold", "s_italic", "s_underline", "s_lower", "s_upper", "s_smallcaps", "s_strike" };
            int[] bits = { 1, 2, 4, 8, 16, 32, 64 };
            StyleNames = new string[StyleFlags.Length];
            for (int i = 0; i < StyleFlags.Length; i++)
            {
                if (StyleFlags[i] == 0) { StyleNames[i] = Loc.T("s_normal"); continue; }
                var parts = new List<string>();
                for (int b = 0; b < bits.Length; b++) if ((StyleFlags[i] & bits[b]) != 0) parts.Add(Loc.T(words[b]));
                StyleNames[i] = string.Join(" + ", parts.ToArray());
            }
            FormatNames = new string[Templates.Length];
            var used = new HashSet<string>();
            for (int i = 0; i < Templates.Length; i++)
            {
                string n = Render(Templates[i], "Prapor", Remaining(3 * 86400 + 4 * 3600), false);
                while (used.Contains(n)) n += " ";
                used.Add(n); FormatNames[i] = n;
            }
        }

        static string Render(string tpl, string v, string x, bool ins)
        {
            string t = tpl.Replace("{MAIL}", Loc.T(ins ? "p_ins" : "p_mail")).Replace("{EXP}", Loc.T("p_exp")).Replace("{LEFT}", Loc.T("p_left"))
                .Replace("{CB}", Loc.T(ins ? "p_cb_ins" : "p_cb")).Replace("{URGENT}", Loc.T("p_urgent")).Replace("{ATTN}", Loc.T("p_attn"))
                .Replace("{REMIND}", Loc.T("p_remind")).Replace("{DEADLINE}", Loc.T("p_deadline")).Replace("{SOON}", Loc.T("p_soon"))
                .Replace("{WAITS}", Loc.T("p_waits")).Replace("{FROM}", Loc.T(ins ? "p_from_ins" : "p_from"));
            return t.Replace("{V}", v).Replace("{X}", x);
        }

        static float WhenOf(int i) { int k = Array.IndexOf(WhenNames, cfgWhen[i].Value); return k >= 0 ? WhenMin[k] : 60f; }
        static float RepeatOf(int i) { return cfgRH[i].Value * 3600f + cfgRM[i].Value * 60f + cfgRS[i].Value; }

        void Awake()
        {
            Log = Logger; inst = this;
            ScanCustomSounds();
            // ---- langue (lue en premier : le menu est ecrit dans cette langue) ----
            var langList = new List<string> { "Auto" }; langList.AddRange(Loc.Names);
            cfgLang = Config.Bind("00. Language", "Language", "Auto", new ConfigDescription("Language of the menu and notifications. Restart the game after changing it (settings are reset when the language changes). / Langue du menu et des notifications. Relance le jeu apres un changement.", new AcceptableValueList<string>(langList.ToArray())));
            Loc.Lang = PickLang(cfgLang.Value);
            fr = Loc.Lang == "fr";
            BuildLists();
            string A = Sec(1, "sec_alerts");
            cfgOn = Config.Bind(A, K(1, "on"), true, D("on"));
            cfgVol = Config.Bind(A, K(2, "vol"), 0.5f, new ConfigDescription(D("vol"), new AcceptableValueRange<float>(0f, 1f)));
            cfgIcon = Config.Bind(A, K(3, "icon"), "Mail", new ConfigDescription(D("icon"), new AcceptableValueList<string>("Mail", "Alert", "Default", "Note", "Quest", "Friend", "Achievement", "EntryPoint", "RagFair", "Hideout", "WishlistQuest", "WishlistHideout", "WishlistTrading", "WishlistEquipment", "WishlistOther")));
            cfgMove = Config.Bind(A, K(4, "move"), false, D("move"));
            cfgPosX = Config.Bind(A, K(5, "posx"), 0f, new ConfigDescription(D("posx"), new AcceptableValueRange<float>(-50f, 50f)));
            cfgPosY = Config.Bind(A, K(6, "posy"), -45f, new ConfigDescription(D("posy"), new AcceptableValueRange<float>(-50f, 50f)));
            for (int i = 0; i < AN; i++)
            {
                string S = (2 + i).ToString("00") + ". " + Safe(Loc.T("sec_alert")) + " " + (i + 1);
                cfgAlertOn[i] = Config.Bind(S, K(1, "active"), true, D("active"));
                cfgWhen[i] = Config.Bind(S, K(2, "when"), WhenNames[DefWhenIdx[i]], new ConfigDescription(D("when"), new AcceptableValueList<string>(WhenNames)));
                cfgSound[i] = Config.Bind(S, K(3, "sound"), DefSound[i], new ConfigDescription(D("sound"), new AcceptableValueList<string>(SoundNames)));
                cfgRH[i] = Config.Bind(S, K(4, "rh"), 0, new ConfigDescription(D("rh"), new AcceptableValueRange<int>(0, 23)));
                cfgRM[i] = Config.Bind(S, K(5, "rm"), 1, new ConfigDescription(D("rm"), new AcceptableValueRange<int>(0, 59)));
                cfgRS[i] = Config.Bind(S, K(6, "rs"), 0, new ConfigDescription(D("rs"), new AcceptableValueRange<int>(0, 59)));
                cfgColor[i] = Config.Bind(S, K(7, "color"), Cols[i], D("color"));
            }
            cfgPreview = Config.Bind(A, K(7, "preview"), false, D("preview"));
            cfgRainbow = Config.Bind(A, K(8, "rainbow"), false, D("rainbow"));
            cfgRbSpeed = Config.Bind(A, K(9, "rbspeed"), 0.5f, new ConfigDescription(D("rbspeed"), new AcceptableValueRange<float>(0.1f, 3f)));
            cfgRaidSilent = Config.Bind(A, K(10, "raidsilent"), true, D("raidsilent"));
            cfgRaidReplay = Config.Bind(A, K(11, "raidreplay"), true, D("raidreplay"));
            cfgBlink = Config.Bind(A, K(12, "blink"), false, D("blink"));
            cfgBlinkCount = Config.Bind(A, K(13, "blinkcount"), 3, new ConfigDescription(D("blinkcount"), new AcceptableValueRange<int>(1, 6)));
            cfgSummary = Config.Bind(A, K(14, "summary"), true, D("summary"));
            cfgSummaryHours = Config.Bind(A, K(15, "summaryhours"), 24, new ConfigDescription(D("summaryhours"), new AcceptableValueRange<int>(1, 168)));
            cfgFontSize = Config.Bind(A, K(16, "fontsize"), 0, new ConfigDescription(D("fontsize"), new AcceptableValueRange<int>(0, 60)));
            cfgFontStyle = Config.Bind(A, K(17, "fontstyle"), StyleNames[0], new ConfigDescription(D("fontstyle"), new AcceptableValueList<string>(StyleNames)));
            cfgMuteGame = Config.Bind(A, K(18, "mutegame"), true, D("mutegame"));
            cfgTextFormat = Config.Bind(A, K(19, "format"), FormatNames[1], new ConfigDescription(D("format"), new AcceptableValueList<string>(FormatNames)));
            cfgSpacing = Config.Bind(A, K(20, "spacing"), 0f, new ConfigDescription(D("spacing"), new AcceptableValueRange<float>(-5f, 30f)));
            cfgPreviewSound = Config.Bind(A, K(21, "previewsound"), false, D("previewsound"));
            cfgDuration = Config.Bind(A, K(22, "duration"), "Long", new ConfigDescription(D("duration"), new AcceptableValueList<string>("Default", "Long")));
            string W = Sec(9, "sec_sounds");
            string sdir = Path.Combine(BepInEx.Paths.ConfigPath, "WorldMailAlerts_sounds");
            cfgSoundDir = Config.Bind(W, K(1, "dir"), sdir, D("dir"));
            cfgOpenDir = Config.Bind(W, K(2, "opendir"), false, D("opendir"));
            try
            {
                var snT = AccessTools.TypeByName("EFT.SocialNetwork");
                if (snT != null)
                {
                    var hp = new HarmonyMethod(typeof(MailAlerts).GetMethod("CaptureSocial", BindingFlags.Static | BindingFlags.NonPublic));
                    var h2 = new Harmony("praedlab.mailalerts.social");
                    foreach (string mn in new[] { "UpdateDialogueList", "method_1", "NotificationReceived", "UpdateReadMessages" })
                    { var mm = AccessTools.Method(snT, mn); if (mm != null) try { h2.Patch(mm, postfix: hp); } catch { } }
                }
                var nvT = AccessTools.TypeByName("EFT.UI.NotifierView");
                var mPlay = nvT != null ? AccessTools.Method(nvT, "PlaySound") : null;
                if (mPlay != null) new Harmony("praedlab.mailalerts").Patch(mPlay, prefix: new HarmonyMethod(typeof(MailAlerts).GetMethod("MutePrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                else Log.LogWarning("PlaySound du jeu introuvable : le son du jeu ne sera pas coupe.");
            }
            catch (Exception e) { Log.LogWarning("Coupure du son du jeu impossible : " + e.Message); }
            BindTraders();
            string T = Sec(6, "sec_test");
            cfgTest = Config.Bind(T, K(1, "test"), false, D("test"));
            cfgListen = Config.Bind(T, K(2, "listen"), "None", new ConfigDescription(D("listen"), new AcceptableValueList<string>(SoundNames)));
            cfgListen.SettingChanged += delegate { if (cfgListen.Value != "None") pendingListen = cfgListen.Value; };
            Log.LogInfo("World Mail Alerts 1.8.1 charge (" + Loc.Lang + ")");
        }

        // ---------- sons (rendu sur un thread, lecture sur le thread principal) ----------
        static int playLogged;
        static void EnsureSrc()
        {
            if (src != null || inst == null) return;
            src = inst.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f; src.volume = 1f; src.priority = 0;
            src.ignoreListenerPause = true; src.bypassListenerEffects = true; src.bypassEffects = true; src.bypassReverbZones = true;
        }

        static void Play(string name, float vol)
        {
            if (name == "None") return;
            EnsureSrc();
            if (src == null) { Log.LogWarning("Son : pas de source audio"); return; }
            AudioClip c;
            if (!clips.TryGetValue(name, out c) && name.StartsWith(CustomPrefix)) { c = LoadCustom(name); clips[name] = c; }
            if (c != null) { src.PlayOneShot(c, Mathf.Clamp01(vol)); if (playLogged++ < 5) Log.LogInfo("Son joue : " + name + " (" + c.length.ToString("0.0") + " s)"); return; }
            if (name.StartsWith(CustomPrefix))
            {
                // fichier illisible : on joue un son du mod a la place plutot que le silence
                Log.LogWarning("Son perso illisible ou introuvable : " + name + " -> Soft ping");
                name = "Soft ping";
                if (!clips.TryGetValue(name, out c)) c = null;
                if (c != null) { src.PlayOneShot(c, Mathf.Clamp01(vol)); return; }
            }
            wantPlay[name] = Time.realtimeSinceStartup;
            if (rendering.Contains(name)) return;
            rendering.Add(name);
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                float[] d = null;
                try { lock (renderLock) { d = Synth.Render(name); } } catch { }
                lock (readyQ) readyQ.Enqueue(new KeyValuePair<string, float[]>(name, d));
            });
        }

        static void Drain()
        {
            while (true)
            {
                KeyValuePair<string, float[]> kv;
                lock (readyQ) { if (readyQ.Count == 0) return; kv = readyQ.Dequeue(); }
                rendering.Remove(kv.Key);
                try
                {
                    if (kv.Value != null)
                    {
                        var clip = AudioClip.Create("mailalert_" + kv.Key, kv.Value.Length, 1, Synth.SR, false);
                        clip.SetData(kv.Value, 0);
                        clips[kv.Key] = clip;
                    }
                    float t;
                    if (wantPlay.TryGetValue(kv.Key, out t))
                    {
                        wantPlay.Remove(kv.Key);
                        AudioClip c;
                        if (Time.realtimeSinceStartup - t < 10f && clips.TryGetValue(kv.Key, out c) && c != null && src != null)
                            src.PlayOneShot(c, Mathf.Clamp01(cfgVol.Value));
                    }
                }
                catch { }
            }
        }

        // ---------- notification native du jeu ----------
        static object ParseEnum(Type t, string name, string fb)
        {
            try { return Enum.Parse(t, name, true); } catch { try { return Enum.Parse(t, fb, true); } catch { return Activator.CreateInstance(t); } }
        }

        static bool MutePrefix(object __0)
        {
            try
            {
                if (cfgMuteGame == null || !cfgMuteGame.Value || __0 == null) return true;
                if (pDesc == null) pDesc = __0.GetType().GetProperty("Description", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                string d = pDesc != null ? pDesc.GetValue(__0, null) as string : null;
                if (d != null && ourTexts.Contains(d)) return false;
            }
            catch { }
            return true;
        }

        static readonly HashSet<string> badIcons = new HashSet<string>();
        static void Notify(string text, Color col)
        {
            try
            {
                ourTexts.Add(text);
                if (ourTexts.Count > 60) ourTexts.Clear();
                if (!notifTried)
                {
                    notifTried = true;
                    var nm = AccessTools.TypeByName("EFT.Communications.NotificationManager");
                    mNotif = nm != null ? AccessTools.Method(nm, "DisplayMessageNotification") : null;
                    if (mNotif != null && mNotif.GetParameters().Length != 4) mNotif = null;
                    if (mNotif != null) nullableColor = mNotif.GetParameters()[3].ParameterType;
                    else Log.LogWarning("Notification du jeu indisponible.");
                }
                if (mNotif == null) return;
                var ps = mNotif.GetParameters();
                object dur = ParseEnum(ps[1].ParameterType, cfgDuration != null ? cfgDuration.Value : "Long", "Default");
                object c = Activator.CreateInstance(nullableColor, new object[] { col });
                string iconName = badIcons.Contains(cfgIcon.Value) ? "Mail" : cfgIcon.Value;
                try
                {
                    mNotif.Invoke(null, new object[] { text, dur, ParseEnum(ps[2].ParameterType, iconName, "Default"), c });
                }
                catch (Exception ie)
                {
                    // icone inconnue du jeu : on la note comme mauvaise et on reessaie avec Mail, sans couper les notifications
                    var inner = ie.InnerException ?? ie;
                    if (iconName != "Mail")
                    {
                        badIcons.Add(iconName);
                        Log.LogWarning("Icone '" + iconName + "' non geree par le jeu (" + inner.Message + ") -> Mail");
                        mNotif.Invoke(null, new object[] { text, dur, ParseEnum(ps[2].ParameterType, "Mail", "Default"), c });
                    }
                    else throw;
                }
            }
            catch (Exception e) { Log.LogWarning("Notification : " + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
        }

        static string Remaining(double secs)
        {
            int m = (int)Math.Ceiling(secs / 60.0);
            string d = Loc.T("u_d"), h = Loc.T("u_h"), mm = Loc.T("u_m");
            if (m >= 1440) return (m / 1440) + d + " " + ((m % 1440) / 60) + h;
            if (m >= 60) return (m / 60) + h + " " + (m % 60) + mm;
            return m + mm;
        }

        // ---------- vendeurs ----------
        static readonly string[][] Traders = {
            new[] { "Prapor", "54cb50c76803fa8b248b4571" }, new[] { "Therapist", "54cb57776803fa99248b456e" }, new[] { "Fence", "579dc571d53a0658a154fbec" },
            new[] { "Skier", "58330581ace78e27b8b10cee" }, new[] { "Peacekeeper", "5935c25fb3acc3127c3d8cd9" }, new[] { "Mechanic", "5a7c2eca46aef81a7ca2145d" },
            new[] { "Ragman", "5ac3b934156ae10c4430e83c" }, new[] { "Jaeger", "5c0647fdd443bc2504c2d371" }, new[] { "Ref", "6617beeaa9cfa777ca915b7c" },
            new[] { "Autre", "" } };
        static ConfigEntry<bool>[] cfgTraderOn = new ConfigEntry<bool>[10];
        static ConfigEntry<string>[] cfgTraderSound = new ConfigEntry<string>[10];
        static FieldInfo fMsgMember; static PropertyInfo pMemId, pMemName;

        static ConfigEntry<string>[] cfgTraderWhen = new ConfigEntry<string>[10];
        static float TraderWhenOf(int tr) { if (tr < 0 || tr >= cfgTraderWhen.Length || cfgTraderWhen[tr] == null) return float.MaxValue; int k = Array.IndexOf(WhenNames, cfgTraderWhen[tr].Value); return k >= 0 ? WhenMin[k] : float.MaxValue; }

        // plus de menu par vendeur : le nom du vendeur reste seulement affiche dans le message
        static void BindTraders() { }

        static string StrOf(object o, string n)
        {
            if (o == null) return null;
            var all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            try
            {
                var p = o.GetType().GetProperty(n, all);
                if (p != null && p.GetIndexParameters().Length == 0) { var v = p.GetValue(o, null); if (v != null) return v.ToString(); }
                var f = o.GetType().GetField(n, all);
                if (f != null) { var v = f.GetValue(o); if (v != null) return v.ToString(); }
            }
            catch { }
            return null;
        }

        static readonly Dictionary<string, KeyValuePair<int, string>> traderCache = new Dictionary<string, KeyValuePair<int, string>>();
        static int TraderCached(string id, object msg, out string name)
        {
            KeyValuePair<int, string> kv;
            if (traderCache.TryGetValue(id, out kv)) { name = kv.Value; return kv.Key; }
            int tr = TraderOf(msg, out name);
            traderCache[id] = new KeyValuePair<int, string>(tr, name);
            if (traderCache.Count > 2000) traderCache.Clear();
            return tr;
        }

        static int traderLogged;
        static int TraderOf(object msg, out string name)
        {
            name = ""; int other = Traders.Length - 1;
            try
            {
                var all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (fMsgMember == null) fMsgMember = msg.GetType().GetField("Member", all);
                object mem = fMsgMember != null ? fMsgMember.GetValue(msg) : null;
                if (mem == null) { if (traderLogged++ < 3) Log.LogInfo("Vendeur : Member vide"); return other; }
                object info = null;
                try { var pi = mem.GetType().GetProperty("Info", all); if (pi != null) info = pi.GetValue(mem, null); } catch { }
                string id = StrOf(mem, "Id") ?? StrOf(mem, "_id") ?? StrOf(mem, "AccountId");
                string nick = StrOf(mem, "LocalizedNickname") ?? StrOf(info, "Nickname") ?? StrOf(info, "LocalizedNickname") ?? "";
                if (traderLogged++ < 3) Log.LogInfo("Vendeur : id=" + id + " nom=" + nick + " accountId=" + StrOf(mem, "AccountId"));
                string idAlt = StrOf(mem, "AccountId");
                for (int i = 0; i < other; i++)
                    if ((id != null && id == Traders[i][1]) || (idAlt != null && idAlt == Traders[i][1])) { name = Traders[i][0]; return i; }
                string low = nick.ToLowerInvariant();
                for (int i = 0; i < other; i++) if (low.Length > 0 && low.Contains(Traders[i][0].ToLowerInvariant())) { name = Traders[i][0]; return i; }
                name = nick;
            }
            catch { }
            return other;
        }

        static string PickSound(int tier, int tr)
        {
            return cfgSound[tier].Value;
        }

        // ---------- historique ----------
        class Seen { public bool ins; public DateTime expiry; public string type = ""; public int items = -1; public int miss; public DateTime missAt; }
        static readonly Dictionary<string, Seen> seen = new Dictionary<string, Seen>();
        class Hist { public DateTime when; public string status, id, type; public DateTime expiry; public int items; }
        static readonly List<Hist> history = new List<Hist>();
        static bool historyLoaded, showHistory; static Vector2 histScroll;
        static string HistPath { get { return Path.Combine(BepInEx.Paths.ConfigPath, "WorldMailAlerts_history.txt"); } }

        static void LoadHistory()
        {
            historyLoaded = true;
            try
            {
                if (!File.Exists(HistPath)) return;
                foreach (string line in File.ReadAllLines(HistPath))
                {
                    string[] p = line.Split('|');
                    if (p.Length < 6) continue;
                    long w, e; int it;
                    if (!long.TryParse(p[0], out w) || !long.TryParse(p[4], out e)) continue;
                    int.TryParse(p[5], out it);
                    history.Add(new Hist { when = new DateTime(w, DateTimeKind.Utc), status = p[1], id = p[2], type = p[3], expiry = new DateTime(e, DateTimeKind.Utc), items = it });
                }
            }
            catch (Exception e) { Log.LogWarning("Historique : lecture impossible (" + e.Message + ")"); }
        }

        static void SaveHistory()
        {
            try
            {
                while (history.Count > 200) history.RemoveAt(0);
                var lines = new List<string>();
                foreach (var h in history)
                    lines.Add(h.when.Ticks + "|" + h.status + "|" + h.id + "|" + (h.type ?? "").Replace('|', '/') + "|" + h.expiry.Ticks + "|" + h.items);
                File.WriteAllLines(HistPath, lines.ToArray());
            }
            catch (Exception e) { Log.LogWarning("Historique : ecriture impossible (" + e.Message + ")"); }
        }

        static string ShortId(string id) { return string.IsNullOrEmpty(id) ? "?" : (id.Length > 6 ? id.Substring(id.Length - 6) : id); }

        static string ReadMsgType(object msg)
        {
            try
            {
                var all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (string n in new[] { "Type", "MessageType", "type" })
                {
                    var f = msg.GetType().GetField(n, all);
                    if (f != null) { object v = f.GetValue(msg); if (v != null) return v.ToString(); }
                    var p = msg.GetType().GetProperty(n, all);
                    if (p != null && p.GetIndexParameters().Length == 0) { object v = p.GetValue(msg, null); if (v != null) return v.ToString(); }
                }
            }
            catch { }
            return "";
        }

        static int ReadMsgItems(object msg)
        {
            try
            {
                var all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var f = msg.GetType().GetField("Items", all);
                object it = f != null ? f.GetValue(msg) : null;
                if (it == null) return -1;
                var dataF = it.GetType().GetField("data", all) ?? it.GetType().GetField("Data", all);
                object d = dataF != null ? dataF.GetValue(it) : null;
                var arr = d as Array; if (arr != null) return arr.Length;
                var col = d as System.Collections.ICollection; if (col != null) return col.Count;
                var cnt = it.GetType().GetProperty("Count", all);
                if (cnt != null) return (int)cnt.GetValue(it, null);
            }
            catch { }
            return -1;
        }

        static void TrackSeen(string id, object msg, DateTime expiry, HashSet<string> present)
        {
            present.Add(id);
            Seen s;
            if (!seen.TryGetValue(id, out s)) { s = new Seen(); seen[id] = s; s.type = ReadMsgType(msg); s.ins = (s.type ?? "").IndexOf("Insurance", StringComparison.OrdinalIgnoreCase) >= 0; s.items = ReadMsgItems(msg); }
            s.expiry = expiry; s.miss = 0;
        }

        static void JudgeMissing(HashSet<string> present, DateTime now)
        {
            List<string> done = null;
            foreach (var kv in seen)
            {
                if (present.Contains(kv.Key)) continue;
                var s = kv.Value;
                if (s.miss == 0) s.missAt = now;
                s.miss++;
                if (s.miss < 3) continue;
                if (done == null) done = new List<string>();
                done.Add(kv.Key);
                continue; // historique retire
                bool lost = s.missAt >= s.expiry.AddSeconds(-4);
                history.Add(new Hist { when = now, status = lost ? "lost" : "collected", id = ShortId(kv.Key), type = s.type, expiry = s.expiry, items = s.items });
            }
            if (done != null)
            {
                foreach (string k in done) { seen.Remove(k); lastFire.Remove(k); }
            }
        }

        // ---------- fenetre d'historique : couleurs reprises de l'ecran Marchands ----------
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        static Texture2D Solid(Color c)
        {
            string k = ColorUtility.ToHtmlStringRGBA(c);
            Texture2D t;
            if (texCache.TryGetValue(k, out t) && t != null) return t;
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false); t.SetPixel(0, 0, c); t.Apply(); t.hideFlags = HideFlags.HideAndDontSave;
            texCache[k] = t; return t;
        }
        static Color Hex(string h, float a = 1f) { Color c; ColorUtility.TryParseHtmlString(h, out c); c.a = a; return c; }
        static int histFilter; // 0 tous, 1 recuperes, 2 perdus
        static GUIStyle stBox, stLabel, stTitle, stTab, stTabOn, stBtn, stRowA, stRowB;
        static void BuildStyles()
        {
            if (stBox != null) return;
            stBox = new GUIStyle(); stBox.normal.background = Solid(Hex("#0b0d0d", 0.97f));
            stLabel = new GUIStyle(); stLabel.normal.textColor = Hex("#f5f4d4"); stLabel.fontSize = 14; stLabel.alignment = TextAnchor.MiddleLeft; stLabel.padding = new RectOffset(10, 10, 0, 0);
            stTitle = new GUIStyle(stLabel); stTitle.fontSize = 18; stTitle.fontStyle = FontStyle.Bold; stTitle.normal.textColor = Hex("#a7b0b4");
            stTab = new GUIStyle(); stTab.normal.background = Solid(Hex("#232628")); stTab.hover.background = Solid(Hex("#2e3235")); stTab.normal.textColor = Hex("#929ca0"); stTab.hover.textColor = Hex("#f5f4d4"); stTab.alignment = TextAnchor.MiddleCenter; stTab.fontSize = 13; stTab.fontStyle = FontStyle.Bold;
            stTabOn = new GUIStyle(stTab); stTabOn.normal.background = Solid(Hex("#a7b0b4")); stTabOn.hover.background = Solid(Hex("#a7b0b4")); stTabOn.normal.textColor = Hex("#0b0d0d"); stTabOn.hover.textColor = Hex("#0b0d0d");
            stBtn = new GUIStyle(stTab); stBtn.fontSize = 13;
            stRowA = new GUIStyle(stLabel); stRowA.normal.background = Solid(Hex("#191a1a")); stRowA.fixedHeight = 26;
            stRowB = new GUIStyle(stLabel); stRowB.normal.background = Solid(Hex("#0f1109")); stRowB.fixedHeight = 26;
        }

        internal void DrawHistoryWindow()
        {
            if (!showHistory) return;
            BuildStyles();
            bool f = fr;
            if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { showHistory = false; return; }
            float w = Math.Min(Screen.width - 40, 680f), h = Math.Min(Screen.height - 80, 500f);
            Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), GUIContent.none, new GUIStyle { normal = { background = Solid(Hex("#3a4448")) } });
            GUI.Box(r, GUIContent.none, stBox);
            int lost = 0, got = 0;
            foreach (var x in history) { if (x.status == "lost") lost++; else got++; }
            // bandeau du haut
            GUI.Box(new Rect(r.x, r.y, r.width, 40), GUIContent.none, new GUIStyle { normal = { background = Solid(Hex("#192d35")) } });
            GUI.Label(new Rect(r.x, r.y, r.width - 100, 40), f ? "HISTORIQUE DU COURRIER" : "MAIL HISTORY", stTitle);
            if (GUI.Button(new Rect(r.xMax - 90, r.y + 6, 80, 28), f ? "FERMER" : "CLOSE", stBtn)) showHistory = false;
            // onglets
            string[] tabs = { (f ? "TOUS " : "ALL ") + history.Count, (f ? "RECUPERES " : "COLLECTED ") + got, (f ? "PERDUS " : "LOST ") + lost };
            for (int i = 0; i < 3; i++)
                if (GUI.Button(new Rect(r.x + 10 + i * 150, r.y + 48, 146, 28), tabs[i], histFilter == i ? stTabOn : stTab)) histFilter = i;
            // liste
            Rect list = new Rect(r.x + 10, r.y + 84, r.width - 20, r.height - 84 - 52);
            GUI.Box(list, GUIContent.none, new GUIStyle { normal = { background = Solid(Hex("#060605")) } });
            var shown = new List<Hist>();
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var e = history[i];
                if (histFilter == 1 && e.status == "lost") continue;
                if (histFilter == 2 && e.status != "lost") continue;
                shown.Add(e);
            }
            histScroll = GUI.BeginScrollView(list, histScroll, new Rect(0, 0, list.width - 18, Math.Max(list.height, shown.Count * 26f)));
            if (shown.Count == 0) GUI.Label(new Rect(0, 0, list.width - 18, 30), f ? "Rien pour l'instant. Les courriers sont notes quand ils disparaissent." : "Nothing yet. Mails are logged when they disappear.", stLabel);
            for (int i = 0; i < shown.Count; i++)
            {
                var e = shown[i];
                var st = (i % 2 == 0) ? stRowA : stRowB;
                bool isLost = e.status == "lost";
                st.normal.textColor = isLost ? Hex("#ff6a5a") : Hex("#9ee37d");
                string items = e.items >= 0 ? "   " + e.items + (f ? " objet(s)" : " item(s)") : "";
                string type = string.IsNullOrEmpty(e.type) ? "" : "   " + e.type;
                string lab = e.when.ToLocalTime().ToString("dd/MM HH:mm", CultureInfo.InvariantCulture) + "   " + (isLost ? (f ? "PERDU" : "LOST") : (f ? "RECUPERE" : "COLLECTED")) + type + items;
                GUI.Label(new Rect(0, i * 26f, list.width - 18, 26), lab, st);
            }
            GUI.EndScrollView();
            // bas
            if (GUI.Button(new Rect(r.x + 10, r.yMax - 40, 140, 30), f ? "EFFACER" : "CLEAR", stBtn)) { history.Clear(); SaveHistory(); }
            GUI.Label(new Rect(r.x + 160, r.yMax - 40, r.width - 170, 30), f ? "Echap ou la touche de l'historique pour fermer" : "Esc or the history key to close", stLabel);
        }

        // ---------- clignotement ----------
        static IEnumerator BlinkRoutine(string text, Color col)
        {
            int n = cfgBlinkCount.Value;
            for (int k = 1; k < n; k++)
            {
                yield return new WaitForSecondsRealtime(1.3f);
                Notify(text, k % 2 == 1 ? Color.white : col);
            }
        }

        // ---------- chargement du courrier au lancement (sans ouvrir la messagerie) ----------
        static object social; static MethodInfo mUpdMsgs; static PropertyInfo pDialogues;
        static readonly HashSet<string> preloaded = new HashSet<string>();
        static readonly Queue<object> preloadQ = new Queue<object>();
        static float nextPreload, preloadUntil = -1f; static int preloadLogged;
        static void CaptureSocial(object __instance) { if (__instance != null && social == null) { social = __instance; Log.LogInfo("Messagerie du jeu reperee : chargement du courrier possible."); } }

        static void PreloadMail()
        {
            if (social == null || Time.realtimeSinceStartup < nextPreload) return;
            if (preloadUntil < 0f) preloadUntil = Time.realtimeSinceStartup + 120f;
            if (Time.realtimeSinceStartup > preloadUntil && preloadQ.Count == 0) return;
            try
            {
                var all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (mUpdMsgs == null) { mUpdMsgs = social.GetType().GetMethod("UpdateDialogMessages", all); pDialogues = social.GetType().GetProperty("Dialogues", all); }
                if (mUpdMsgs == null || pDialogues == null) { nextPreload = float.MaxValue; return; }
                if (preloadQ.Count > 0)
                {
                    // un dialogue par seconde, pour ne pas surcharger le jeu
                    nextPreload = Time.realtimeSinceStartup + 1f;
                    object d = preloadQ.Dequeue();
                    mUpdMsgs.Invoke(social, new object[] { d, 0.0 });
                    return;
                }
                nextPreload = Time.realtimeSinceStartup + 3f;
                var list = pDialogues.GetValue(social, null) as IEnumerable;
                if (list == null) return;
                foreach (object d in list)
                {
                    if (d == null) continue;
                    var dt = d.GetType();
                    string id = StrOf(d, "_id") ?? d.GetHashCode().ToString();
                    if (preloaded.Contains(id)) continue;
                    bool loaded = false, rewards = false; int att = 0;
                    try { loaded = (bool)dt.GetProperty("MessagesLoaded", all).GetValue(d, null); } catch { }
                    try { rewards = (bool)dt.GetProperty("HasMessagesWithRewards", all).GetValue(d, null); } catch { }
                    try { att = (int)dt.GetField("AttachmentsNew", all).GetValue(d); } catch { }
                    if (loaded) { preloaded.Add(id); continue; }
                    if (!rewards && att <= 0) continue;
                    preloaded.Add(id);
                    preloadQ.Enqueue(d);
                    if (preloadLogged++ < 20) Log.LogInfo("Chargement du courrier : " + (StrOf(d, "Name") ?? id));
                }
            }
            catch (Exception e) { nextPreload = Time.realtimeSinceStartup + 10f; Log.LogWarning("Chargement du courrier : " + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
        }

        // ---------- detection du raid / raid detection ----------
        static bool gwTried; static PropertyInfo pGw;
        static bool InRaid()
        {
            try
            {
                if (!gwTried)
                {
                    gwTried = true;
                    var gt = AccessTools.TypeByName("EFT.GameWorld");
                    var sg = AccessTools.TypeByName("Comfort.Common.Singleton`1");
                    if (gt != null && sg != null)
                        pGw = sg.MakeGenericType(gt).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                    if (pGw == null) Log.LogInfo("Detection du raid indisponible : le silence en raid est ignore.");
                }
                if (pGw == null) return false;
                object inst = pGw.GetValue(null, null);
                if (inst == null) return false;
                return inst.GetType().Name.IndexOf("Hideout", StringComparison.OrdinalIgnoreCase) < 0;
            }
            catch { return false; }
        }

        static int heldTier = -1; static double heldLeft; static float heldAt;
        static float nextRaidCheck;
        static void Hold(int tier, double left)
        {
            if (heldTier < 0) heldAt = Time.realtimeSinceStartup;
            if (heldTier < 0 || WhenOf(tier) < WhenOf(heldTier) || (tier == heldTier && left < heldLeft))
            { heldTier = tier; heldLeft = left + (Time.realtimeSinceStartup - heldAt); heldAt = Time.realtimeSinceStartup; }
        }
        static void ReleaseHeld()
        {
            if (heldTier < 0 || Time.realtimeSinceStartup < nextRaidCheck) return;
            nextRaidCheck = Time.realtimeSinceStartup + 2f;
            if (cfgRaidSilent.Value && InRaid()) return;
            int t = heldTier; double left = heldLeft - (Time.realtimeSinceStartup - heldAt); heldTier = -1;
            if (left <= 5.0 || !cfgRaidReplay.Value) return;
            Fire(t, left, true, heldTr, heldName);
        }

        // renvoie true si l'alerte a vraiment ete jouee
        static string BuildText(double secsLeft, string trName, int count = 1, bool ins = false)
        {
            string v = string.IsNullOrEmpty(trName) ? "?" : trName;
            int k = Array.IndexOf(FormatNames, cfgTextFormat.Value);
            if (k < 0) k = 1;
            string text = Render(Templates[k], v, Remaining(secsLeft), ins);
            if (count > 1) text += "  " + Loc.T(ins ? "p_count_ins" : "p_count").Replace("{N}", count.ToString());
            return text;
        }

        static int heldTr = -1; static string heldName = "";
        static PropertyInfo pIsHiding;
        static bool AnyHiding()
        {
            try
            {
                if (notifView == null || fVisible == null) return false;
                var lst = fVisible.GetValue(notifView) as IEnumerable;
                if (lst == null) return false;
                foreach (object o in lst)
                {
                    if (o == null) continue;
                    if (pIsHiding == null) { var t = o.GetType(); pIsHiding = t.GetProperty("IsHiding", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? (t.BaseType != null ? t.BaseType.GetProperty("IsHiding", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null); }
                    if (pIsHiding != null && (bool)pIsHiding.GetValue(o, null)) return true;
                }
            }
            catch { }
            return false;
        }

        static int VisibleCount()
        {
            try
            {
                if (notifView == null) return 0;
                if (fVisible == null) fVisible = notifView.GetType().GetField("_visibleNotifications", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var c = fVisible != null ? fVisible.GetValue(notifView) as System.Collections.ICollection : null;
                return c != null ? c.Count : 0;
            }
            catch { return 0; }
        }

        static IEnumerator FireMany(List<object[]> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                Fire((int)m[0], (double)m[1], true, (int)m[3], (string)m[4], i == 0, (int)m[5], (bool)m[6]);
                if (i < list.Count - 1) yield return new WaitForSecondsRealtime(0.6f);
            }
        }

        static bool Fire(int tier, double secsLeft, bool bypass = false, int tr = -1, string trName = "", bool sound = true, int count = 1, bool ins = false)
        {
            if (!bypass && cfgRaidSilent.Value && InRaid()) { Hold(tier, secsLeft); heldTr = tr; heldName = trName; return false; }
            if (sound) Play(PickSound(tier, tr), cfgVol.Value);
            string text = BuildText(secsLeft, trName, count, ins);
            Color col = cfgColor[tier].Value;
            Notify(text, col);
            if (cfgBlink.Value && inst != null) inst.StartCoroutine(BlinkRoutine(text, col));
            return true;
        }

        // ---------- lecture du courrier ----------
        static void Init()
        {
            ready = true;
            try
            {
                msgType = AccessTools.TypeByName("ChatShared.DialogueChatMessage");
                snType = AccessTools.TypeByName("EFT.SocialNetwork");
                var all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                if (msgType == null || snType == null) { failed = true; return; }
                fAll = snType.GetField("AllMessages", all);
                fId = msgType.GetField("_id", all);
                fMax = msgType.GetField("MaxStorageTime", all);
                fUtc = msgType.GetField("UtcDateTime", all);
                pReward = msgType.GetProperty("DisplayRewardStatus", all);
                if (fAll == null || fId == null || fMax == null || fUtc == null || pReward == null) { failed = true; Log.LogWarning("Liste des courriers introuvable : alertes desactivees."); }
            }
            catch (Exception e) { failed = true; Log.LogWarning("Init : " + e.Message); }
        }

        static readonly HashSet<string> knownIds = new HashSet<string>(); static bool baselineDone; static float firstTrackedAt = -1f; static bool summaryDone;
        static void Scan()
        {
            var dict = fAll.GetValue(null) as IDictionary;
            if (dict == null) return;
            DateTime now = DateTime.UtcNow;
            float rt = Time.realtimeSinceStartup;
            buf.Clear();
            try { foreach (DictionaryEntry de in dict) buf.Add(de.Value); } catch { return; }
            int fireTier = -1, fireTr = -1; double fireLeft = 0; string fireId = null, fireName = "";
            var many = new List<object[]>();
            int tracked = 0, soon = 0; double soonSecs = cfgSummaryHours.Value * 3600.0;
            var present = new HashSet<string>();
            foreach (object msg in buf)
            {
                if (msg == null) continue;
                tracked++;
                try
                {
                    if (!(bool)pReward.GetValue(msg, null)) continue;
                    long max = (long)fMax.GetValue(msg);
                    if (max <= 0 || max >= 2147483647L) continue;
                    DateTime expiry = ((DateTime)fUtc.GetValue(msg)).AddSeconds(max);
                    double left = (expiry - now).TotalSeconds;
                    string id = (string)fId.GetValue(msg) ?? msg.GetHashCode().ToString();
                    TrackSeen(id, msg, expiry, present);
                    if (left <= 0) continue;
                    if (left <= soonSecs) soon++;
                    string trName; int tr = TraderCached(id, msg, out trName);
                    Seen sn; if (trName.Length > 0 && seen.TryGetValue(id, out sn)) sn.type = trName;
                    if (cfgTraderOn[tr] != null && !cfgTraderOn[tr].Value) continue;
                    double mins = left / 60.0;
                    if (mins > TraderWhenOf(tr)) continue;
                    int best = -1, current = -1;
                    for (int i = 0; i < AN; i++)
                    {
                        if (mins > WhenOf(i)) continue;
                        if (cfgAlertOn[i].Value && (current < 0 || WhenOf(i) < WhenOf(current))) current = i;
                        string key = id + "|" + i;
                        if (alerted.Contains(key)) continue;
                        alerted.Add(key);
                        if (!cfgAlertOn[i].Value) continue;
                        if (best < 0 || WhenOf(i) < WhenOf(best)) best = i;
                    }
                    if (best < 0 && current >= 0 && RepeatOf(current) > 0f)
                    {
                        float last;
                        if (lastFire.TryGetValue(id, out last) && rt - last >= RepeatOf(current)) best = current;
                    }
                    if (best >= 0) { Seen sq; many.Add(new object[] { best, left, id, tr, trName, seen.TryGetValue(id, out sq) && sq.ins }); }
                    if (best >= 0 && (fireTier < 0 || WhenOf(best) < WhenOf(fireTier) || (best == fireTier && left < fireLeft)))
                    { fireTier = best; fireLeft = left; fireId = id; fireTr = tr; fireName = trName; }
                }
                catch { }
            }
            if (tracked > 0) JudgeMissing(present, now);
            if (fireTier >= 0)
            {
                if (cfgRaidSilent.Value && InRaid())
                {
                    // en raid : on retient seulement la plus urgente, rejouee apres le raid
                    Fire(fireTier, fireLeft, false, fireTr, fireName);
                }
                else if (inst != null)
                {
                    // hors raid : une notification par courrier, l'une apres l'autre
                    foreach (var m in many) lastFire[(string)m[2]] = rt;
                    // regroupement : une notification par vendeur (la plus urgente, avec le nombre de courriers)
                    var groups = new Dictionary<string, object[]>();
                    var order = new List<string>();
                    foreach (var m in many)
                    {
                        string gk = ((int)m[3] < Traders.Length - 1) ? "t" + (int)m[3] : "n" + (string)m[4];
                        object[] g;
                        if (!groups.TryGetValue(gk, out g)) { g = new object[] { m[0], m[1], m[2], m[3], m[4], 1, (bool)m[5] }; groups[gk] = g; order.Add(gk); }
                        else
                        {
                            g[5] = (int)g[5] + 1; g[6] = (bool)g[6] && (bool)m[5];
                            if ((double)m[1] < (double)g[1]) { g[0] = m[0]; g[1] = m[1]; }
                        }
                    }
                    var list = new List<object[]>();
                    foreach (string gk in order) list.Add(groups[gk]);
                    list.Sort((x, y) => ((double)x[1]).CompareTo((double)y[1]));
                    if (list.Count > 12) list.RemoveRange(12, list.Count - 12);
                    inst.StartCoroutine(FireMany(list));
                }
            }
            // resume au lancement : une seule fois, quelques secondes apres le chargement du courrier
            if (!summaryDone && tracked > 0)
            {
                if (firstTrackedAt < 0f) firstTrackedAt = rt;
                if (rt - firstTrackedAt >= 8f && !(cfgRaidSilent.Value && InRaid()))
                {
                    summaryDone = true;
                    if (cfgSummary.Value && cfgOn.Value && soon > 0)
                    {
                        string txt = Loc.T("p_summary").Replace("{N}", soon.ToString()).Replace("{H}", cfgSummaryHours.Value.ToString());
                        Notify(txt, cfgColor[1].Value);
                    }
                }
            }
        }

        IEnumerator TestRoutine()
        {
            for (int i = 0; i < AN; i++)
            {
                Fire(i, WhenOf(i) * 60.0 - 5.0, true, i % 9, Traders[i % 9][0]);
                yield return new WaitForSecondsRealtime(2.2f);
            }
        }

        // ---------- deplacement de la notification du jeu ----------
        static ConfigEntry<bool> cfgPreviewSound; static ConfigEntry<float> cfgSpacing; static ConfigEntry<string> cfgTextFormat; static ConfigEntry<bool> cfgMuteGame; static readonly HashSet<string> ourTexts = new HashSet<string>(); static PropertyInfo pDesc; static string lastSig = ""; static float sigChangedAt; static bool sigPending; static string offsetSig = ""; static Vector2 lastMin; static bool reparented; static FieldInfo fBack; static Vector2 stableMin; static float movingSince = -1f; static bool moveLogged; static GuiHost guiHost; static bool dragging; static Vector2 grab; static FieldInfo fVisible; static int findTries; static readonly Vector3[] cs = new Vector3[4]; static readonly List<RectTransform> views = new List<RectTransform>(); static float nextViews; static Component notifView; static RectTransform notifRt; static Vector2 notifOrig; static float nextFind; static bool posLogged, foundLogged;
        static FieldInfo fText; static PropertyInfo pTextColor; static float nextRb;
        static void ApplyRainbow()
        {
            if (Time.realtimeSinceStartup < nextRb || notifView == null) return;
            nextRb = Time.realtimeSinceStartup + 0.05f;
            try
            {
                if (fVisible == null)
                    fVisible = notifView.GetType().GetField("_visibleNotifications", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var lst = fVisible != null ? fVisible.GetValue(notifView) as IEnumerable : null;
                if (lst == null) return;
                Color c = Color.HSVToRGB((Time.realtimeSinceStartup * cfgRbSpeed.Value) % 1f, 0.85f, 1f);
                foreach (object o in lst)
                {
                    if (o == null) continue;
                    if (fText == null) fText = o.GetType().GetField("_text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy) ?? o.GetType().BaseType.GetField("_text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    object tx = fText != null ? fText.GetValue(o) : null;
                    if (tx == null) continue;
                    if (pTextColor == null) pTextColor = tx.GetType().GetProperty("color");
                    if (pTextColor != null) pTextColor.SetValue(tx, c, null);
                }
            }
            catch { }
        }

        static float nextMask; static int maskLogged;
        // le jeu decoupe la notification a la zone d'origine (masque) : on enleve ces masques pour pouvoir la mettre partout
        static void FreeMasks()
        {
            if (Time.realtimeSinceStartup < nextMask || notifRt == null) return;
            nextMask = Time.realtimeSinceStartup + 3f;
            try
            {
                var mts = new List<Type>();
                foreach (string tn in new[] { "UnityEngine.UI.RectMask2D", "UnityEngine.UI.Mask" }) { var mt = AccessTools.TypeByName(tn); if (mt != null) mts.Add(mt); }
                // on remonte seulement les petits parents de la notification, jamais les gros ecrans (sinon tout le menu casse)
                Transform tr = notifRt;
                for (int up = 0; up < 3 && tr != null; up++, tr = tr.parent)
                {
                    if (tr.GetComponent<Canvas>() != null) break;
                    var rt = tr as RectTransform;
                    if (rt != null && (rt.rect.width * rt.lossyScale.x > Screen.width * 0.6f && rt.rect.height * rt.lossyScale.y > Screen.height * 0.6f)) continue;
                    foreach (var mt in mts)
                    {
                        var c = tr.GetComponent(mt) as Behaviour;
                        if (c != null && c.enabled) { c.enabled = false; if (maskLogged++ < 6) Log.LogInfo("Masque desactive : " + mt.Name + " sur " + tr.name); }
                    }
                }
            }
            catch { }
        }

        static readonly Dictionary<object, float> origH = new Dictionary<object, float>(); static FieldInfo fLayoutF, fContF, fHeightF; static PropertyInfo pPrefH, pHiding; static MethodInfo mForce; static PropertyInfo pSpacing; static float nextFont; static FieldInfo fTextF; static PropertyInfo pFontSize, pFontStyle;
        static void ApplyFont()
        {
            if (Time.realtimeSinceStartup < nextFont || notifView == null) return;
            nextFont = Time.realtimeSinceStartup + 0.2f;
            try
            {
                var all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (fVisible == null) fVisible = notifView.GetType().GetField("_visibleNotifications", all);
                var lst = fVisible != null ? fVisible.GetValue(notifView) as IEnumerable : null;
                if (lst == null) return;
                int style = 0;
                { int si = Array.IndexOf(StyleNames, cfgFontStyle.Value); style = si >= 0 ? StyleFlags[si] : 0; }
                foreach (object v in lst)
                {
                    if (v == null) continue;
                    if (fTextF == null) fTextF = v.GetType().GetField("_text", all) ?? v.GetType().BaseType.GetField("_text", all);
                    object tx = fTextF != null ? fTextF.GetValue(v) : null;
                    if (tx == null) continue;
                    if (pFontSize == null) { pFontSize = tx.GetType().GetProperty("fontSize", all); pFontStyle = tx.GetType().GetProperty("fontStyle", all); }
                    if (pFontSize != null && cfgFontSize.Value > 0 && Math.Abs((float)pFontSize.GetValue(tx, null) - cfgFontSize.Value) > 0.1f) pFontSize.SetValue(tx, (float)cfgFontSize.Value, null);
                    if (pSpacing == null) pSpacing = tx.GetType().GetProperty("characterSpacing", all);
                    if (pSpacing != null && Math.Abs((float)pSpacing.GetValue(tx, null) - cfgSpacing.Value) > 0.01f) pSpacing.SetValue(tx, cfgSpacing.Value, null);
                    if (pFontStyle != null)
                    {
                        object want = Enum.ToObject(pFontStyle.PropertyType, style);
                        if (!pFontStyle.GetValue(tx, null).Equals(want)) pFontStyle.SetValue(tx, want, null);
                    }
                    if (cfgFontSize.Value > 0 || cfgSpacing.Value != 0f) FitFrame(v);
                }
            }
            catch { }
        }

        // le jeu fixe la hauteur du cadre quand la notification apparait : on la recalcule quand la taille du texte change
        static void FitFrame(object v)
        {
            try
            {
                var all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var vt = v.GetType(); var bt = vt.BaseType ?? vt;
                if (fLayoutF == null) { fLayoutF = vt.GetField("_layout", all) ?? bt.GetField("_layout", all); fContF = vt.GetField("_container", all) ?? bt.GetField("_container", all); fHeightF = vt.GetField("_height", all) ?? bt.GetField("_height", all); pHiding = vt.GetProperty("IsHiding", all) ?? bt.GetProperty("IsHiding", all); }
                if (fLayoutF == null || fContF == null) return;
                if (pHiding != null && (bool)pHiding.GetValue(v, null)) return;
                var cont = fContF.GetValue(v) as RectTransform;
                object lay = fLayoutF.GetValue(v);
                if (cont == null || lay == null) return;
                if (pPrefH == null) pPrefH = lay.GetType().GetProperty("preferredHeight", all);
                if (pPrefH == null) return;
                // hauteur voulue = hauteur du texte (et pas celle du conteneur, qui peut etre enorme et creer un trou)
                if (fTextF == null) fTextF = vt.GetField("_text", all) ?? bt.GetField("_text", all);
                object tx = fTextF != null ? fTextF.GetValue(v) : null;
                if (tx == null) return;
                var pTP = tx.GetType().GetProperty("preferredHeight", all);
                if (pTP == null) return;
                float textH = (float)pTP.GetValue(tx, null);
                float cur = (float)pPrefH.GetValue(lay, null);
                float orig;
                if (!origH.TryGetValue(v, out orig)) { orig = cur; origH[v] = orig; if (origH.Count > 50) origH.Clear(); }
                if (orig < 10f) return; // encore en animation d'apparition
                float want = Mathf.Clamp(textH + 8f, orig, orig * 3f);
                if (Math.Abs(want - cur) > 1f) { pPrefH.SetValue(lay, want, null); if (fHeightF != null) fHeightF.SetValue(v, want); }
            }
            catch { }
        }

        static void MoveNotif()
        {
            if (!cfgMove.Value && !cfgRainbow.Value && cfgFontSize.Value == 0 && cfgFontStyle.Value == StyleNames[0] && cfgSpacing.Value == 0f) { findTries = 0; return; }
            if (notifRt == null)
            {
                if (Time.realtimeSinceStartup < nextFind) return;
                nextFind = Time.realtimeSinceStartup + (findTries < 12 ? 3f : 10f);
                findTries++;
                var t = AccessTools.TypeByName("EFT.UI.NotifierView");
                if (t == null) return;
                var objs = Resources.FindObjectsOfTypeAll(t);
                Component nv = null;
                if (objs != null)
                    foreach (var o in objs)
                    {
                        var cmp = o as Component;
                        if (cmp != null && cmp.gameObject.scene.IsValid()) { nv = cmp; break; }
                    }
                if (nv != null)
                {
                    notifView = nv;
                    var fc = t.GetField("_container", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    notifRt = (fc != null ? fc.GetValue(nv) as RectTransform : null) ?? (nv.transform as RectTransform);
                }
                if (notifRt == null)
                {
                    // plan B : une notification affichee -> on deplace le conteneur qui la porte
                    var bt = AccessTools.TypeByName("EFT.UI.BaseNotificationView");
                    var vs = bt != null ? Resources.FindObjectsOfTypeAll(bt) : null;
                    if (vs != null)
                        foreach (var o in vs)
                        {
                            var cmp = o as Component;
                            if (cmp != null && cmp.gameObject.scene.IsValid() && cmp.gameObject.activeInHierarchy && cmp.transform.parent as RectTransform != null)
                            { notifView = cmp; notifRt = cmp.transform.parent as RectTransform; break; }
                        }
                    if (!posLogged) { posLogged = true; Log.LogWarning("NotifierView : " + (nv == null ? "introuvable" : "trouve") + " (" + (objs == null ? 0 : objs.Length) + " objets), plan B : " + (notifRt != null ? notifRt.name : "rien")); }
                }
                if (notifRt == null) return;
                var cv = notifRt.GetComponentInParent<Canvas>();
                if (!foundLogged) { foundLogged = true; Log.LogInfo("Notification du jeu trouvee : " + notifView.name + " conteneur=" + notifRt.name + " taille=" + notifRt.rect.size + " canvas=" + (cv != null ? cv.renderMode.ToString() + " x" + cv.scaleFactor : "aucun")); }
            }
            if (cfgRainbow.Value) ApplyRainbow();
            ApplyFont();
            if (cfgMove.Value) FreeMasks();
            if (cfgMove.Value && !reparented && notifRt != null)
            {
                // on sort le conteneur de la notification des zones qui la decoupent : il passe sous le canvas principal
                try
                {
                    var cvs = notifRt.GetComponentInParent<Canvas>();
                    Transform rootT = cvs != null ? cvs.rootCanvas.transform : null;
                    if (rootT != null && notifRt.parent != rootT)
                    {
                        Log.LogInfo("Conteneur deplace hors des masques : ancien parent=" + (notifRt.parent != null ? notifRt.parent.name : "aucun"));
                        notifRt.SetParent(rootT, true);
                        notifRt.SetAsLastSibling();
                    }
                    reparented = true;
                }
                catch (Exception e) { reparented = true; Log.LogWarning("Deplacement du conteneur impossible : " + e.Message); }
            }
            if (!cfgMove.Value) return;
            var canvas = notifRt.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Canvas root = canvas.rootCanvas;
            Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            // rectangle reel de la notification affichee (sinon celui du conteneur)
            if (Time.realtimeSinceStartup >= nextViews)
            {
                nextViews = Time.realtimeSinceStartup + 0.2f;
                views.Clear();
                try
                {
                    if (fVisible == null && notifView != null)
                        fVisible = notifView.GetType().GetField("_visibleNotifications", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var lst = fVisible != null && notifView != null ? fVisible.GetValue(notifView) as IEnumerable : null;
                    if (lst != null)
                        foreach (object o in lst)
                        {
                            var cmp = o as Component;
                            if (cmp != null && cmp.gameObject.activeInHierarchy && cmp.transform is RectTransform)
                            {
                                // on mesure la barre visible (fond), pas le rectangle large de la vue
                                RectTransform rr = cmp.transform as RectTransform;
                                if (fBack == null) fBack = cmp.GetType().GetField("_background", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? cmp.GetType().BaseType.GetField("_background", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                var bg = fBack != null ? fBack.GetValue(cmp) as Component : null;
                                if (bg != null && bg.gameObject.activeInHierarchy && bg.transform is RectTransform) rr = bg.transform as RectTransform;
                                views.Add(rr);
                            }
                        }
                }
                catch { }
            }
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var r in views)
            {
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                r.GetWorldCorners(cs);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, cs[i]);
                    if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                    if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
                }
            }
            // pas de notification visible : on ne touche a rien (evite de pousser le conteneur hors ecran)
            if (minX > maxX) { stableMin = new Vector2(-9999f, -9999f); return; }
            float w = maxX - minX, h = maxY - minY;
            if (cfgPreview.Value)
            {
                Vector3 mp = Input.mousePosition;
                if (Input.GetMouseButtonDown(0) && mp.x >= minX - 6f && mp.x <= maxX + 6f && mp.y >= minY - 6f && mp.y <= maxY + 6f && w > 10f)
                { dragging = true; grab = new Vector2(mp.x - minX, mp.y - minY); }
                if (!Input.GetMouseButton(0)) dragging = false;
                if (dragging)
                {
                    float fw = Mathf.Max(1f, Screen.width - w), fh = Mathf.Max(1f, Screen.height - h);
                    cfgPosX.Value = Mathf.Clamp((mp.x - grab.x) / fw * 100f - 50f, -50f, 50f);
                    cfgPosY.Value = Mathf.Clamp((mp.y - grab.y) / fh * 100f - 50f, -50f, 50f);
                }
            }
            float tx = (cfgPosX.Value + 50f) / 100f, ty = (cfgPosY.Value + 50f) / 100f;
            Vector2 wantMin = new Vector2(tx * Mathf.Max(0f, Screen.width - w), ty * Mathf.Max(0f, Screen.height - h));
            if (AnyHiding()) return; // une notification s'efface : on ne bouge rien, elle disparait sur place
            Vector2 curMin = new Vector2(minX, minY);
            // decalage fige : on ne recalcule que sur UNE notification immobile, et seulement si les reglages ont change
            string psig = cfgPosX.Value.ToString("0.0") + "|" + cfgPosY.Value.ToString("0.0") + "|" + Screen.width + "x" + Screen.height;
            bool stillNow = (curMin - lastMin).sqrMagnitude < 1f;
            lastMin = curMin;
            if (!dragging)
            {
                if (offsetSig == psig) return;
                if (views.Count != 1 || !stillNow) return;
            }
            offsetSig = psig;
            Vector2 d = wantMin - curMin;
            if (!moveLogged) { moveLogged = true; Log.LogInfo("Placement : actuel=" + curMin + " voulu=" + wantMin + " taille=" + w + "x" + h + " vues=" + views.Count); }
            if (d.sqrMagnitude < 4f) return;
            float sf = root.scaleFactor <= 0.0001f ? 1f : root.scaleFactor;
            notifRt.anchoredPosition += d / sf;
        }

        void Update()
        {
            try
            {
                MoveNotif();
                if (cfgOpenDir != null && cfgOpenDir.Value)
                {
                    cfgOpenDir.Value = false;
                    try { string d = Path.Combine(BepInEx.Paths.ConfigPath, "WorldMailAlerts_sounds"); Directory.CreateDirectory(d); Application.OpenURL("file:///" + d.Replace('\\', '/')); } catch { }
                }
                if (cfgHistory != null)
                {
                    if (!historyLoaded) LoadHistory();
                    if (cfgHistOpen.Value) { cfgHistOpen.Value = false; showHistory = true; }
                    if (cfgHistClear.Value) { cfgHistClear.Value = false; history.Clear(); SaveHistory(); }
                    try { if (cfgHistKey.Value.IsDown()) showHistory = !showHistory; } catch { }
                    if (guiHost == null) { guiHost = gameObject.AddComponent<GuiHost>(); guiHost.p = this; guiHost.enabled = false; }
                    if (guiHost.enabled != showHistory) guiHost.enabled = showHistory;
                }
                if (src == null)
                {
                    src = gameObject.AddComponent<AudioSource>();
                    src.playOnAwake = false; src.spatialBlend = 0f;
                }
                Drain();
                if (cfgTest.Value) { cfgTest.Value = false; StartCoroutine(TestRoutine()); }
                if (pendingListen.Length > 0) { string p = pendingListen; pendingListen = ""; Play(p, cfgVol.Value); }
                if (cfgPreview.Value)
                {
                    string sig = cfgTextFormat.Value + "|" + cfgFontStyle.Value + "|" + cfgFontSize.Value + "|" + cfgSpacing.Value + "|" + cfgIcon.Value + "|" + cfgColor[1].Value;
                    if (sig != lastSig) { lastSig = sig; sigChangedAt = Time.realtimeSinceStartup; sigPending = true; }
                    if (sigPending && Time.realtimeSinceStartup - sigChangedAt > 0.5f) { sigPending = false; nextPreview = 0f; }
                }
                if (cfgPreview.Value && Time.realtimeSinceStartup >= nextPreview)
                {
                    nextPreview = Time.realtimeSinceStartup + 5f;
                    if (cfgPreviewSound.Value) Play(PickSound(0, 0), cfgVol.Value);
                    Notify(BuildText(3600.0, "Prapor"), cfgColor[1].Value);
                }
                if (cfgOn.Value && !InRaid()) PreloadMail();
                if (heldTier >= 0) ReleaseHeld();
                if (!cfgOn.Value) return;
                if (Time.realtimeSinceStartup < nextScan) return;
                nextScan = Time.realtimeSinceStartup + 5f;
                if (!ready) Init();
                if (!failed && fAll != null)
Scan();
            }
            catch (Exception e) { Log.LogWarning("Alertes : " + e.Message); nextScan = Time.realtimeSinceStartup + 30f; }
        }
    }
    public class GuiHost : MonoBehaviour
    {
        internal MailAlerts p;
        void OnGUI() { if (p != null) p.DrawHistoryWindow(); }
    }
}
