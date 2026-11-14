using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MailCountdown
{
    // Fenetre d'historique : copie de la vraie fenetre du jeu "ProfileEventsRewardWindow" (titre, croix, liste qui defile, 2 boutons)
    public static class HistoryUI
    {
        public static ManualLogSource Log;
        public static BepInEx.Configuration.ConfigEntry<bool> cfgChatBtn;
        public static BepInEx.Configuration.ConfigEntry<float> cfgRowSize, cfgSumSize, cfgRowBg; public static BepInEx.Configuration.ConfigEntry<string> cfgSumStyle; public static BepInEx.Configuration.ConfigEntry<Color> cfgSumColor, cfgBgColor; public static BepInEx.Configuration.ConfigEntry<bool> cfgBgOn, cfgRowColorOn, cfgRowRainbow; public static BepInEx.Configuration.ConfigEntry<string> cfgRowStyle, cfgRowFormat; public static BepInEx.Configuration.ConfigEntry<Color> cfgRowColor, cfgDateColor; public static BepInEx.Configuration.ConfigEntry<float> cfgAlpha;
        class Entry { public DateTime when; public string text; public Color col; public long rub; public string cat = ""; }
        static readonly List<Entry> entries = new List<Entry>();
        static bool loaded;
        const int Max = 50;
        static string FilePath { get { return Path.Combine(BepInEx.Paths.ConfigPath, "praedlab.mailalerts.history.txt"); } }

        static GameObject canvasGo, window;
        static Transform listParent; static GameObject rowTemplate;
        static TextMeshProUGUI caption;
        static readonly List<GameObject> rows = new List<GameObject>();
        static bool buildFailed; static int buildTries;

        // ---------- donnees ----------
        public static void Add(string text, Color col, long rub = 0, string cat = "")
        {
            Load();
            entries.Add(new Entry { when = DateTime.Now, text = text, col = col, rub = rub, cat = cat ?? "" }); rowsDirty = true;
            while (entries.Count > Max) entries.RemoveAt(0);
            Save();
            if (window != null && window.activeSelf) Refresh();
        }

        public static void Clear() { Load(); entries.Clear(); rowsDirty = true; Save(); if (window != null && window.activeSelf) Refresh(); }

        static void Load()
        {
            if (loaded) return; loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    var p = line.Split('\t');
                    if (p.Length < 4) continue;
                    long ticks; if (!long.TryParse(p[0], out ticks)) continue;
                    Color c; if (!ColorUtility.TryParseHtmlString("#" + p[1], out c)) c = Color.white;
                    long rub; long.TryParse(p[2], out rub);
                    entries.Add(new Entry { when = new DateTime(ticks), col = c, rub = rub, text = p[3], cat = p.Length > 4 ? p[4] : "" });
                }
                while (entries.Count > Max) entries.RemoveAt(0);
            }
            catch (Exception e) { Log.LogWarning("[historique] lecture : " + e.Message); }
        }

        static void Save()
        {
            try
            {
                var lines = new List<string>();
                foreach (var e in entries) lines.Add(e.when.Ticks + "\t" + ColorUtility.ToHtmlStringRGB(e.col) + "\t" + e.rub + "\t" + e.text.Replace("\t", " ").Replace("\n", " ") + "\t" + e.cat);
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch (Exception e) { Log.LogWarning("[historique] ecriture : " + e.Message); }
        }

        // ---------- fenetre ----------
        public static bool IsOpen { get { return window != null && window.activeSelf; } }

        public static void Toggle()
        {
            if (IsOpen) { Close(); return; }
            Open();
        }

        public static void Close() { if (window != null) window.SetActive(false); }

        static void Open()
        {
            EndWarm();
            var osw = System.Diagnostics.Stopwatch.StartNew();
            Load(); long a = osw.ElapsedMilliseconds;
            if (window == null && !Build()) return;
            long b = osw.ElapsedMilliseconds;
            window.SetActive(true); long c = osw.ElapsedMilliseconds;
            HistoryBg.Reload(); // l'image choisie est rechargee a chaque ouverture (sinon elle restait vide apres un relancement)
            // les lignes ne sont refaites que si quelque chose a change (nouveau courrier, reglage, format "il y a")
            string fmt = cfgRowFormat != null ? cfgRowFormat.Value : "";
            if (rowsDirty || rows.Count == 0 || fmt.Contains("Ago")) Refresh(); else ApplyLook();
            long d = osw.ElapsedMilliseconds;
            if (openLogs < 10) { openLogs++; openMsg = "[perf] ouverture historique : fichier " + a + " ms, fenetre " + (b - a) + " ms, affichage " + (c - b) + " ms, lignes " + (d - c) + " ms"; openFrame = 2; }
        }
        static bool rowsDirty = true; static readonly List<KeyValuePair<GameObject, TextMeshProUGUI>> wrapQ = new List<KeyValuePair<GameObject, TextMeshProUGUI>>();
        static int openLogs, openFrame, warmFrames; static string openMsg;
        static CanvasGroup warmCg;
        static void EndWarm()
        {
            if (warmFrames <= 0) return;
            warmFrames = 0;
            if (window != null) window.SetActive(false);
            if (warmCg != null) { warmCg.alpha = 1f; warmCg.blocksRaycasts = true; warmCg.interactable = true; }
        }

        static bool Build()
        {
            var bsw = System.Diagnostics.Stopwatch.StartNew();
            try { return BuildInner(); } finally { Log.LogInfo("[perf] creation de la fenetre : " + bsw.ElapsedMilliseconds + " ms"); }
        }
        static bool BuildInner()
        {
            if (buildFailed && buildTries > 3) return false;
            buildTries++;
            try
            {
                // la vraie fenetre du jeu, cachee dans la messagerie
                GameObject src = null;
                foreach (var w in Resources.FindObjectsOfTypeAll<EFT.UI.Chat.ProfileEventsWindow>())
                    if (w != null && w.gameObject.scene.IsValid()) { src = w.gameObject; break; }
                if (src == null) { Log.LogWarning("[historique] fenetre du jeu introuvable (ProfileEventsWindow)"); buildFailed = true; return false; }

                canvasGo = new GameObject("WorldMailAlerts_History");
                UnityEngine.Object.DontDestroyOnLoad(canvasGo);
                var cv = canvasGo.AddComponent<Canvas>();
                cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 30001;
                var sc = canvasGo.AddComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 1f;
                canvasGo.AddComponent<GraphicRaycaster>();

                bool wasActive = src.activeSelf;
                src.SetActive(false); // la copie nait inactive : le script du jeu ne demarre pas
                window = UnityEngine.Object.Instantiate(src, canvasGo.transform, false);
                src.SetActive(wasActive);
                window.name = "History";

                var gw = window.GetComponent<EFT.UI.Chat.ProfileEventsWindow>();
                listParent = gw._eventsList;
                rowTemplate = gw._eventViewTemplate != null ? gw._eventViewTemplate.gameObject : null;
                var clearBtn = gw._applyAllButton; var closeBtn = gw._cancelButton;
                UnityEngine.Object.DestroyImmediate(gw); // on garde le visuel, pas la logique du jeu

                Visible(window);
                var rt = window.transform as RectTransform;
                if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }
                SetupFrame();

                // bandeau colle en haut : on reduit la marge du haut du rangement vertical du cadre (sans sortir le bandeau du rangement)
                try
                {
                    var cpRt = Find(window.transform, "Caption Panel") as RectTransform;
                    var vlg = cpRt != null && cpRt.parent != null ? cpRt.parent.GetComponent<VerticalLayoutGroup>() : null;
                    if (vlg != null)
                    {
                        int before = vlg.padding.top;
                        vlg.padding.top = before;
                        Log.LogInfo("[historique] marge du haut : " + before + " -> " + vlg.padding.top);
                    }
                }
                catch (Exception e) { Log.LogWarning("[historique] marge : " + e.Message); }

                // le bandeau gris est une image dont le dessin laisse ~3 px vides en haut : on la remplace par une couleur pleine
                try
                {
                    var cpI = Find(window.transform, "Caption Panel");
                    var cimg = cpI != null ? cpI.GetComponent<Image>() : null;
                    if (cimg != null)
                    {
                        Log.LogInfo("[historique] bandeau image : " + (cimg.sprite != null ? cimg.sprite.name + " bordures=" + cimg.sprite.border : "aucune") + " type=" + cimg.type + " couleur=" + cimg.color);
                        cimg.sprite = null; cimg.type = Image.Type.Simple;
                    }
                }
                catch (Exception e) { Log.LogWarning("[historique] bandeau image : " + e.Message); }
                // le fond Inner a peut-etre aussi un dessin avec une bande en haut : couleur pleine + bandeau pousse tout en haut
                try
                {
                    var inT = Find(window.transform, "Inner");
                    var inImg = inT != null ? inT.GetComponent<Image>() : null;
                    if (inImg != null) { Log.LogInfo("[historique] fond Inner image : " + (inImg.sprite != null ? inImg.sprite.name + " bordures=" + inImg.sprite.border : "aucune")); inImg.sprite = null; inImg.type = Image.Type.Simple; }
                    var cpR = Find(window.transform, "Caption Panel") as RectTransform;
                    var vg = cpR != null && cpR.parent != null ? cpR.parent.GetComponent<VerticalLayoutGroup>() : null;
                    // UnityExplorer : la bande = Border (le liseré), dessine PAR-DESSUS le bandeau. On le passe derriere tout le contenu.
                    var bdT = Find(window.transform, "Border");
                    if (bdT != null) { var bdR = bdT as RectTransform; var cpH = Find(window.transform, "Caption Panel") as RectTransform; float ch = cpH != null && cpH.rect.height > 1f ? cpH.rect.height : 17f; if (bdR != null) bdR.offsetMax = new Vector2(bdR.offsetMax.x, -ch); Log.LogInfo("[historique] cadre baisse sous le bandeau : " + ch); bdT.SetAsFirstSibling();
                        // la partie noire aussi : un fond neuf qui commence sous le bandeau ; le fond d'origine devient transparent
                        try
                        {
                            var inImg2 = panel != null ? panel.GetComponent<Image>() : null;
                            if (inImg2 != null)
                            {
                                var body = new GameObject("WMA_Body", typeof(RectTransform), typeof(Image));
                                var brt = (RectTransform)body.transform; brt.SetParent(panel, false);
                                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = new Vector2(0f, -ch);
                                var bi = body.GetComponent<Image>(); bi.raycastTarget = false;
                                Color orig = bgSaved ? bgOrig : inImg2.color;
                                bi.color = inImg2.color;
                                body.AddComponent<LayoutElement>().ignoreLayout = true;
                                inImg2.color = new Color(orig.r, orig.g, orig.b, 0f);
                                bgImg = bi; bgOrig = orig; bgSaved = true;
                                // le bandeau etait gris transparent sur fond noir : on lui donne un fond plein de la meme teinte, sur toute la largeur
                                var top = new GameObject("WMA_TopBar", typeof(RectTransform), typeof(Image));
                                var trt = (RectTransform)top.transform; trt.SetParent(panel, false);
                                trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
                                trt.offsetMin = new Vector2(-5f, -ch); trt.offsetMax = new Vector2(5f, 5f); // couvre aussi la bande de 5 px au-dessus, comme le bandeau de la messagerie
                                var ti = top.GetComponent<Image>(); ti.raycastTarget = false; ti.color = new Color(0.10f, 0.105f, 0.11f, 1f);
                                top.AddComponent<LayoutElement>().ignoreLayout = true;
                                Log.LogInfo("[historique] fond noir baisse sous le bandeau");
                            }
                        }
                        catch (Exception e2) { Log.LogWarning("[historique] fond noir : " + e2.Message); } var ble = bdT.GetComponent<LayoutElement>() ?? bdT.gameObject.AddComponent<LayoutElement>(); ble.ignoreLayout = true; Log.LogInfo("[historique] liseré passe derriere"); }
                }
                catch (Exception e) { Log.LogWarning("[historique] haut : " + e.Message); }
                // titre
                var capT = Find(window.transform, "Caption");
                caption = capT != null ? capT.GetComponent<TextMeshProUGUI>() : null;
                if (caption != null) caption.text = Loc.T("h_btn");

                // roue dentee a cote de la croix : un petit bouton tout neuf avec le texte du titre
                try
                {
                    var xb = Find(window.transform, "Close Button") as RectTransform;
                    if (xb != null && caption != null)
                    {
                        var gear = new GameObject("WMA_SettingsButton", typeof(RectTransform), typeof(Image));
                        var grt = (RectTransform)gear.transform; grt.SetParent(xb.parent, false);
                        float sz = Mathf.Max(18f, xb.rect.height);
                        // la roue est rangee DANS la croix, collee a sa gauche : elle la suit partout
                        grt.SetParent(xb, false);
                        var gle = gear.AddComponent<LayoutElement>(); gle.ignoreLayout = true;
                        grt.anchorMin = grt.anchorMax = new Vector2(0f, 0.5f); grt.pivot = new Vector2(1f, 0.5f);
                        grt.sizeDelta = new Vector2(sz, sz);
                        grt.anchoredPosition = new Vector2(-6f, 0f);
                        var gi = gear.GetComponent<Image>(); gi.color = new Color(0f, 0f, 0f, 0.01f); gi.raycastTarget = true; // zone cliquable invisible
                        var lg = UnityEngine.Object.Instantiate(caption.gameObject, gear.transform, false);
                        foreach (Transform ch in lg.transform) UnityEngine.Object.Destroy(ch.gameObject);
                        var lt = lg.GetComponent<TextMeshProUGUI>(); var lr = lg.transform as RectTransform;
                        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero; lr.pivot = new Vector2(0.5f, 0.5f);
                        lt.alignment = TextAlignmentOptions.Center; lt.enableWordWrapping = false; lt.raycastTarget = false; lt.enableAutoSizing = false; lt.fontSize = sz * 0.8f;
                        bool has = false; try { has = lt.font != null && lt.font.HasCharacter('\u2699'); } catch { }
                        lt.text = has ? "\u2699" : "";
                        // pas de roue dans la police : on prend l'icone de reglages du jeu
                        Sprite cog = null;
                        if (!has)
                        {
                            // icone du jeu (options_icon), trouvee dans la liste deja faite des images du jeu (pas de recherche lourde)
                            HistoryBg.ScanGame();
                            cog = HistoryBg.FindSprite("options_icon");
                            if (cog == null) cog = MakeGear();
                            if (cog != null)
                            {
                                var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                                var irt = (RectTransform)ic.transform; irt.SetParent(gear.transform, false);
                                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = new Vector2(-1, -1); irt.offsetMax = new Vector2(1, 1);
                                var iim = ic.GetComponent<Image>(); iim.sprite = cog; iim.preserveAspect = true; iim.raycastTarget = false; iim.color = Color.white;
                                lt.text = "";
                                UnityEngine.Object.Destroy(lg); // plus aucun texte copie du titre sur la roue
                            }
                            else lt.text = "\u2261"; // ≡ si rien d'autre
                            Log.LogInfo("[historique] icone de reglages : " + (cog != null ? cog.name : "aucune, symbole ≡"));
                        }
                        Hook(gear, OpenSettings);
                        gear.transform.SetAsLastSibling();
                        Log.LogInfo("[historique] bouton reglages : " + lt.text + " a " + grt.anchoredPosition + " taille " + sz);
                    }
                }
                catch (Exception e) { Log.LogWarning("[historique] bouton reglages : " + e.Message); }
                // croix du bandeau
                var x = Find(window.transform, "Close Button");
                if (x != null)
                {
                    var db = x.GetComponent<EFT.UI.DefaultUIButton>();
                    if (db != null) { db.OnClick.RemoveAllListeners(); db.OnClick.AddListener(Close); }
                    else Hook(x.gameObject, Close);
                }

                // boutons du bas : Vider / Fermer
                if (clearBtn != null)
                {
                    clearBtn.gameObject.SetActive(true);
                    try { clearBtn.SetHeaderText(Loc.T("h_clear")); } catch { }
                    clearBtn.OnClick.RemoveAllListeners(); clearBtn.OnClick.AddListener(Clear);
                }
                // la rangee de boutons descend un peu : plus d'air sous la liste

                try { if (clearBtn != null) { var barRt = clearBtn.transform.parent as RectTransform; if (barRt != null) { barRt.anchoredPosition += new Vector2(0f, -22f); } } } catch { }
                // bilan de la session : maintenant la premiere ligne de la liste (plus de texte separe)
                if (false)
                try
                {
                    if (clearBtn != null && closeBtn != null && caption != null)
                    {
                        var bar = clearBtn.transform.parent;
                        var go = UnityEngine.Object.Instantiate(caption.gameObject, bar, false);
                        go.name = "SessionSummary";
                        foreach (Transform ch in go.transform) UnityEngine.Object.Destroy(ch.gameObject);
                        go.transform.SetSiblingIndex(closeBtn.transform.GetSiblingIndex());
                        summary = go.GetComponent<TextMeshProUGUI>();
                        summary.alignment = TextAlignmentOptions.Center; summary.enableWordWrapping = true; summary.richText = true;
                        summary.enableAutoSizing = false; sumDefault = Mathf.Max(17f, caption.fontSize * 1.35f); summary.fontSize = sumDefault;
                        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
                        le.ignoreLayout = false; le.flexibleWidth = 1f; le.minWidth = 120f;
                        if (bar.GetComponent<HorizontalOrVerticalLayoutGroup>() == null)
                        {
                            // pas de rangement automatique : on la place entre les deux boutons
                            var a = clearBtn.transform as RectTransform; var b = closeBtn.transform as RectTransform; var r = go.transform as RectTransform;
                            r.anchorMin = r.anchorMax = a.anchorMin; r.pivot = new Vector2(0.5f, 0.5f);
                            r.anchoredPosition = (a.anchoredPosition + b.anchoredPosition) / 2f;
                            r.sizeDelta = new Vector2(Mathf.Max(120f, Mathf.Abs(b.anchoredPosition.x - a.anchoredPosition.x) - (a.rect.width + b.rect.width) / 2f - 10f), a.rect.height);
                        }
                        // sur sa propre ligne, collee au-dessus de la rangee de boutons : rien entre VIDER et FERMER
                        {
                            var sr = go.transform as RectTransform;
                            // en haut : sous le bandeau du titre, au-dessus de la liste
                            go.transform.SetParent(panel != null ? (Transform)panel : bar, false);
                            var le2 = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); le2.ignoreLayout = true;
                            sr.anchorMin = new Vector2(0f, 1f); sr.anchorMax = new Vector2(1f, 1f); sr.pivot = new Vector2(0.5f, 1f);
                            sr.offsetMin = new Vector2(20f, 0f); sr.offsetMax = new Vector2(-20f, 0f);
                            sr.anchoredPosition = new Vector2(0f, -36f);
                            var fit = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
                            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                            summaryRt = sr;
                        }
                        Log.LogInfo("[historique] bilan ajoute entre les boutons (rangement auto=" + (bar.GetComponent<HorizontalOrVerticalLayoutGroup>() != null) + ")");
                    }
                }
                catch (Exception e) { Log.LogWarning("[historique] bilan : " + e.Message); }
                if (closeBtn != null)
                {
                    try { closeBtn.SetHeaderText(Loc.T("h_close")); } catch { }
                    closeBtn.OnClick.RemoveAllListeners(); closeBtn.OnClick.AddListener(Close);
                }

                if (rowTemplate != null) rowTemplate.SetActive(false);
                // graphique de la journee + bouton GRAPHIQUE/LISTE dans le bandeau
                try
                {
                    Transform area = Find(panel, "EventsList") ?? (listParent != null && listParent.parent != null ? listParent.parent : null);
                    MoneyGraph.Build(area, caption);
                    MoneyGraph.SalesToday = () =>
                    {
                        var l = new List<KeyValuePair<DateTime, long>>(); var d0 = DateTime.Now.Date;
                        foreach (var e in entries) if (e.rub > 0 && e.when >= d0) l.Add(new KeyValuePair<DateTime, long>(e.when, e.rub));
                        l.Sort((a, b) => a.Key.CompareTo(b.Key)); return l;
                    };
                    var capPanel = Find(window.transform, "Caption Panel");
                    var xb2 = Find(window.transform, "Close Button") as RectTransform;
                    if (capPanel != null && caption != null && xb2 != null)
                    {
                        var gb = UnityEngine.Object.Instantiate(caption.gameObject, xb2, false);
                        gb.name = "WMA_GraphButton";
                        StripScripts(gb);
                        foreach (Transform ch in gb.transform) UnityEngine.Object.Destroy(ch.gameObject);
                        var gle2 = gb.GetComponent<LayoutElement>() ?? gb.AddComponent<LayoutElement>(); gle2.ignoreLayout = true;
                        var gt = gb.GetComponent<TextMeshProUGUI>(); var gr = (RectTransform)gb.transform;
                        gr.anchorMin = gr.anchorMax = new Vector2(0f, 0.5f); gr.pivot = new Vector2(1f, 0.5f);
                        gt.enableWordWrapping = false; gt.alignment = TextAlignmentOptions.Right; gt.raycastTarget = true; gt.text = Loc.T("h_graph");
                        float gwid = Mathf.Max(gt.GetPreferredValues(Loc.T("h_graph")).x, gt.GetPreferredValues(Loc.T("h_list")).x); gr.sizeDelta = new Vector2(Mathf.Ceil(gwid) + 12f, Mathf.Max(16f, xb2.rect.height)); gt.overflowMode = TextOverflowModes.Overflow;
                        gr.anchoredPosition = new Vector2(-(Mathf.Max(18f, xb2.rect.height) + 14f), 0f);
                        graphLabel = gt;
                        Hook(gb, () => { MoneyGraph.Toggle(); if (graphLabel != null) graphLabel.text = Loc.T(MoneyGraph.Visible ? "h_list" : "h_graph"); });
                    }
                }
                catch (Exception e) { Log.LogWarning("[graphique] " + e.Message); }
                Log.LogInfo("[historique] fenetre creee (liste=" + (listParent != null ? listParent.name : "?") + ", modele=" + (rowTemplate != null ? rowTemplate.name : "?") + ")");
                return listParent != null && rowTemplate != null;
            }
            catch (Exception e)
            {
                buildFailed = true;
                Log.LogWarning("[historique] creation impossible : " + e);
                if (canvasGo != null) UnityEngine.Object.Destroy(canvasGo);
                canvasGo = null; window = null;
                return false;
            }
        }

        static void Refresh()
        {
            try { wrapQ.Clear(); RefreshRows(); WrapAll(); rowsDirty = false; } finally { ApplyLook(); }
        }
        static void RefreshRows()
        {
            if (listParent == null || rowTemplate == null) return;
            foreach (var r in rows) if (r != null) UnityEngine.Object.Destroy(r);
            rows.Clear(); rowLabels.Clear();

            // ligne de total des ventes de la session
            long total = 0; int sold = 0; DateTime start = SessionStart;
            foreach (var e in entries) if (e.rub > 0 && e.when >= start) { total += e.rub; sold++; }
            if (caption != null) caption.text = Loc.T("h_btn");
            AddSummaryRow(Summary(start, sold, total));
            if (summary != null) { summary.text = Summary(start, sold, total); summary.fontSize = cfgSumSize != null && cfgSumSize.Value > 0 ? cfgSumSize.Value : sumDefault;
                if (cfgSumColor != null) summary.color = cfgSumColor.Value;
                FontStyles fs = FontStyles.Normal; string st = cfgSumStyle != null ? cfgSumStyle.Value : "Normal";
                if (st.Contains("Bold")) fs |= FontStyles.Bold; if (st.Contains("Italic")) fs |= FontStyles.Italic; if (st == "Caps") fs |= FontStyles.UpperCase;
                summary.fontStyle = fs; }

            AddFilterRow();
            int shown = 0;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                if (Filter != "all" && e.cat != Filter) continue;
                shown++;
                AddRow(RowText(e), cfgRowColorOn != null && cfgRowColorOn.Value && cfgRowColor != null ? cfgRowColor.Value : e.col);
            }
            if (shown == 0) AddRow(Loc.T("h_empty"), new Color(0.6f, 0.6f, 0.6f));
        }

        // ---------- filtres : une rangee de boutons du jeu (copies du bouton AMIS de la messagerie) ----------
        static readonly string[] FilterCats = { "all", "c_flea", "c_ins", "c_quest", "c_gift", "c_btr" };
        static string filter;
        static string FilterPath { get { return Path.Combine(BepInEx.Paths.ConfigPath, "praedlab.mailalerts.filter.txt"); } }
        static string Filter
        {
            get
            {
                if (filter == null) { filter = "all"; try { if (File.Exists(FilterPath)) { string f = File.ReadAllText(FilterPath).Trim(); if (Array.IndexOf(FilterCats, f) >= 0) filter = f; } } catch { } }
                return filter;
            }
        }
        static void SetFilter(string f)
        {
            if (f == Filter) return;
            filter = f; try { File.WriteAllText(FilterPath, f); } catch { }
            rowsDirty = true; Refresh();
        }
        static int filterLogs;
        static void AddFilterRow()
        {
            try
            {
                if (chat == null) foreach (var c in Resources.FindObjectsOfTypeAll<EFT.UI.Chat.ChatScreen>()) if (c != null && c.gameObject.scene.IsValid()) { chat = c; break; }
                var fb = chat != null ? chat._friendsButton : null;
                if (fb == null) { if (filterLogs++ < 1) Log.LogWarning("[filtres] bouton du jeu introuvable"); return; }
                var row = new GameObject("WMA_Filters", typeof(RectTransform));
                row.transform.SetParent(listParent, false);
                var hl = row.AddComponent<HorizontalLayoutGroup>();
                hl.spacing = 4f; hl.childAlignment = TextAnchor.MiddleCenter; hl.padding = new RectOffset(4, 4, 3, 3);
                hl.childControlWidth = false; hl.childControlHeight = false; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
                float bh = ((RectTransform)fb.transform).rect.height; if (bh < 18f || bh > 60f) bh = 30f;
                var le = row.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = bh + 6f;
                foreach (string cat in FilterCats)
                {
                    var go = UnityEngine.Object.Instantiate(fb.gameObject, row.transform, false);
                    go.name = "WMA_Filter_" + cat;
                    var b = go.GetComponent<Button>();
                    string cc = cat;
                    if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => SetFilter(cc)); }
                    bool on = cat == Filter;
                    foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        t.text = cat == "all" ? Loc.T("h_all") : Loc.T(cat);
                        t.color = on ? new Color(1f, 0.86f, 0.45f) : new Color(0.62f, 0.62f, 0.62f);
                        t.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                    }
                    var icon = Find(go.transform, "Icon"); if (icon != null) icon.gameObject.SetActive(false);
                    var rt = (RectTransform)go.transform; rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bh);
                    go.SetActive(true);
                    FitButton(go, false);
                }
                Visible(row);
                rows.Add(row);
                if (filterLogs++ < 1) Log.LogInfo("[filtres] rangee ajoutee, filtre actif : " + Filter);
            }
            catch (Exception e) { if (filterLogs++ < 3) Log.LogWarning("[filtres] " + e.Message); }
        }

        static float sumDefault = 17f, rowDefault;
        public static readonly string[] RowFormats = { "Date Time | Text", "Short date | Text", "Time | Text", "Time sec | Text", "[Time] Text", "Text | Date Time", "Text | Ago", "Ago | Text", "Text only" };
        static string RowText(Entry e)
        {
            string dc = "<color=#" + ColorUtility.ToHtmlStringRGBA(cfgDateColor != null ? cfgDateColor.Value : new Color(0.54f, 0.54f, 0.54f)) + ">";
            string f = cfgRowFormat != null ? cfgRowFormat.Value : RowFormats[0];
            switch (f)
            {
                case "Short date | Text": return dc + e.when.ToString("dd/MM  HH:mm") + "</color>   " + e.text;
                case "Time | Text": return dc + e.when.ToString("HH:mm") + "</color>   " + e.text;
                case "Time sec | Text": return dc + e.when.ToString("HH:mm:ss") + "</color>   " + e.text;
                case "[Time] Text": return dc + "[" + e.when.ToString("HH:mm") + "]</color> " + e.text;
                case "Text | Date Time": return e.text + "   " + dc + e.when.ToString("dd/MM/yyyy HH:mm") + "</color>";
                case "Text | Ago": return e.text + "   " + dc + Ago(e.when) + "</color>";
                case "Ago | Text": return dc + Ago(e.when) + "</color>   " + e.text;
                case "Text only": return e.text;
                default: return dc + e.when.ToString("dd/MM/yyyy  HH:mm:ss") + "</color>   " + e.text;
            }
        }
        static string Ago(DateTime w)
        {
            var d = DateTime.Now - w; if (d.TotalSeconds < 0) d = TimeSpan.Zero;
            string v = d.TotalMinutes < 1 ? "<1 min" : d.TotalHours < 1 ? (int)d.TotalMinutes + " min" : d.TotalDays < 1 ? (int)d.TotalHours + " h " + d.Minutes.ToString("00") : (int)d.TotalDays + " j " + d.Hours + " h";
            return "-" + v;
        }

        static TextMeshProUGUI graphLabel;
        static TextMeshProUGUI summary; static float rowBaseH; static int wrapLogs;
        public static void Relayout() { rowsDirty = true; if (IsOpen) Refresh(); }
        static string Summary(DateTime start, int sold, long total)
        {
            var cnt = new Dictionary<string, int>();
            foreach (var e in entries) if (e.when >= start && !string.IsNullOrEmpty(e.cat)) { int n; cnt.TryGetValue(e.cat, out n); cnt[e.cat] = n + 1; }
            var parts = new List<string>();
            if (sold > 0) parts.Add(Loc.T("c_flea") + " " + sold + " — " + total.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") + " ₽");
            foreach (string c in new[] { "c_ins", "c_quest", "c_gift", "c_btr" }) { int n; if (cnt.TryGetValue(c, out n) && n > 0) parts.Add(Loc.T(c) + " " + n); }
            if (parts.Count == 0) return Loc.T("h_session") + " —";
            return Loc.T("h_session") + "<space=1.2em>" + string.Join("<space=0.8em>·<space=0.8em>", parts.ToArray());
        }

        static void AddSummaryRow(string text)
        {
            AddRow(text, cfgSumColor != null ? cfgSumColor.Value : new Color(0.55f, 0.9f, 0.45f));
            var go = rows[rows.Count - 1];
            var lab = rowLabels.Count > 0 ? rowLabels[rowLabels.Count - 1] : null;
            rowLabels.Remove(lab); // pas d'arc-en-ciel sur le bilan
            if (lab != null)
            {
                lab.alignment = TextAlignmentOptions.Center;
                lab.fontSize = cfgSumSize != null && cfgSumSize.Value > 0 ? cfgSumSize.Value : Mathf.Max(lab.fontSize, sumDefault);
                FontStyles fs = FontStyles.Normal; string st = cfgSumStyle != null ? cfgSumStyle.Value : "Bold";
                if (st.Contains("Bold")) fs |= FontStyles.Bold; if (st.Contains("Italic")) fs |= FontStyles.Italic; if (st == "Caps") fs |= FontStyles.UpperCase;
                lab.fontStyle = fs;
            }
        }

        static void AddRow(string text, Color col)
        {
            var go = UnityEngine.Object.Instantiate(rowTemplate, listParent, false);
            go.name = "HistoryRow";
            var view = go.GetComponent<EFT.UI.Chat.ProfileEventView>();
            TextMeshProUGUI label = view != null ? view._description : null;
            if (view != null && view._applyButton != null) view._applyButton.gameObject.SetActive(false);
            if (view != null) UnityEngine.Object.DestroyImmediate(view);
            if (label == null) { var t = Find(go.transform, "EventName"); label = t != null ? t.GetComponent<TextMeshProUGUI>() : null; }
            if (label != null) rowLabels.Add(label);
            if (label != null) { label.richText = true; label.text = text; label.color = col; if (rowDefault <= 0) rowDefault = label.fontSize; label.enableAutoSizing = false; label.fontSize = cfgRowSize != null && cfgRowSize.Value > 0 ? cfgRowSize.Value : rowDefault;
                FontStyles fs = FontStyles.Normal; string st = cfgRowStyle != null ? cfgRowStyle.Value : "Normal";
                if (st.Contains("Bold")) fs |= FontStyles.Bold; if (st.Contains("Italic")) fs |= FontStyles.Italic; if (st == "Caps") fs |= FontStyles.UpperCase;
                label.fontStyle = fs; }
            Visible(go);
            // fond sombre derriere chaque ligne : le texte reste lisible meme sur une image
            try
            {
                if (cfgRowBg != null && cfgRowBg.Value > 0f)
                {
                    var rb = go.transform.Find("Background"); var rbi = rb != null ? rb.GetComponent<Image>() : go.GetComponent<Image>();
                    if (rbi == null) { rbi = go.AddComponent<Image>(); rbi.raycastTarget = false; }
                    rbi.color = new Color(0f, 0f, 0f, Mathf.Clamp01(cfgRowBg.Value));
                }
            }
            catch { }
            go.SetActive(true);
            rows.Add(go);
            if (label != null) { label.enableWordWrapping = true; label.overflowMode = TextOverflowModes.Overflow; wrapQ.Add(new KeyValuePair<GameObject, TextMeshProUGUI>(go, label)); }
        }

        // texte trop long : il continue sur la ligne suivante et la ligne s'agrandit
        // une seule mise a jour de la mise en page pour toutes les lignes (avant : une par ligne, tres lent)
        static void WrapAll()
        {
            if (wrapQ.Count == 0) return;
            Canvas.ForceUpdateCanvases();
            foreach (var kv in wrapQ)
            try
            {
                var go = kv.Key; var label = kv.Value;
                if (go == null || label == null) continue;
                {
                    var rrt = go.transform as RectTransform;
                    float baseH = rowBaseH > 0 ? rowBaseH : (rowBaseH = rrt.rect.height);
                    float w = label.rectTransform.rect.width;
                    if (w > 10f)
                    {
                        float need = label.GetPreferredValues(label.text, w, 0f).y;
                        float extra = baseH - label.rectTransform.rect.height; if (extra < 0) extra = 0;
                        float h = Mathf.Max(baseH, need + extra + 4f);
                        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
                        le.minHeight = h; le.preferredHeight = h;
                        rrt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                    }
                }
            }
            catch (Exception e) { if (wrapLogs++ < 3) Log.LogWarning("[historique] retour a la ligne : " + e.Message); }
        }

        // les fenetres du jeu apparaissent en fondu : la copie doit etre visible tout de suite
        static void Visible(GameObject go)
        {
            foreach (var cg in go.GetComponentsInChildren<CanvasGroup>(true)) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
        }

        public static DateTime SessionStart = DateTime.Now;

        // clic sur un objet du jeu qui n'a pas de Button Unity : on en ajoute un
        static void Hook(GameObject go, UnityEngine.Events.UnityAction act)
        {
            var b = go.GetComponent<Button>();
            if (b == null)
            {
                var g = go.GetComponentInChildren<Graphic>();
                b = go.AddComponent<Button>();
                if (g != null) { b.targetGraphic = g; g.raycastTarget = true; }
            }
            b.onClick.RemoveAllListeners(); b.onClick.AddListener(act);
        }


        // ---------- bouton dans la messagerie (a cote de AMIS) ----------
        static EFT.UI.Chat.ChatScreen chat; static GameObject chatBtn; static float nextChatFind; static bool btnLogged;
        static float prebuildAt = -1f;
        public static void TickButton()
        {
            // la fenetre est construite a l'avance, quelques secondes apres l'arrivee au menu : pas de saccade au premier clic
            if (window == null && prebuildAt > 0f && Time.realtimeSinceStartup > prebuildAt)
            {
                prebuildAt = -1f;
                try
                {
                    Load();
                    if (Build())
                    {
                        // affichee 3 images en invisible : le jeu prepare les textes et la mise en page une fois pour toutes
                        warmCg = canvasGo.GetComponent<CanvasGroup>() ?? canvasGo.AddComponent<CanvasGroup>();
                        warmCg.alpha = 0f; warmCg.blocksRaycasts = false; warmCg.interactable = false;
                        window.SetActive(true); HistoryBg.Reload(); Refresh(); warmFrames = 3;
                        Log.LogInfo("[historique] fenetre preparee a l'avance (menu principal)");
                    }
                }
                catch (Exception e) { Log.LogWarning("[historique] preparation : " + e.Message); }
            }
            if (warmFrames > 0 && --warmFrames == 0) { warmFrames = 1; EndWarm(); Log.LogInfo("[historique] fenetre prechauffee"); }
            if (openFrame > 0 && --openFrame == 0) Log.LogInfo(openMsg + ", image suivante " + Mathf.RoundToInt(Time.unscaledDeltaTime * 1000f) + " ms");
            try
            {
                if (cfgChatBtn != null && !cfgChatBtn.Value) { if (chatBtn != null) { UnityEngine.Object.Destroy(chatBtn); chatBtn = null; } return; }
                if (chatBtn != null) return;
                if (Time.realtimeSinceStartup < nextChatFind) return;
                nextChatFind = Time.realtimeSinceStartup + 2f;
                if (chat == null)
                {
                    foreach (var c in Resources.FindObjectsOfTypeAll<EFT.UI.Chat.ChatScreen>())
                        if (c != null && c.gameObject.scene.IsValid()) { chat = c; break; }
                    if (chat == null) return;
                }
                var fb = chat._friendsButton;
                if (fb == null) { nextChatFind = float.MaxValue; Log.LogWarning("[historique] bouton AMIS introuvable : pas de bouton dans la messagerie"); return; }
                chatBtn = UnityEngine.Object.Instantiate(fb.gameObject, fb.transform.parent, false);
                chatBtn.name = "WorldMailAlerts_HistoryButton";
                chatBtn.transform.SetSiblingIndex(fb.transform.GetSiblingIndex()); // juste a gauche de AMIS
                var b = chatBtn.GetComponent<Button>();
                b.onClick.RemoveAllListeners(); b.onClick.AddListener(Toggle);
                foreach (var t in chatBtn.GetComponentsInChildren<TextMeshProUGUI>(true)) t.text = Loc.T("h_btn");
                var icon = Find(chatBtn.transform, "Icon"); if (icon != null) icon.gameObject.SetActive(false);
                chatBtn.SetActive(true);
                FitButton(chatBtn);
                if (!btnLogged) { btnLogged = true; Log.LogInfo("[historique] bouton ajoute dans la messagerie"); }
                if (window == null) prebuildAt = Time.realtimeSinceStartup + 3f;
            }
            catch (Exception e) { nextChatFind = Time.realtimeSinceStartup + 30f; Log.LogWarning("[historique] bouton : " + e.Message); }
        }

        // le bouton prend juste la largeur du mot, comme AMIS
        static void FitButton(GameObject go) { FitButton(go, true); }
        static void FitButton(GameObject go, bool log)
        {
            try
            {
                var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
                var rt = go.transform as RectTransform;
                if (label == null || rt == null) return;
                label.enableWordWrapping = false;
                label.alignment = TextAlignmentOptions.Center;
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(0f, lrt.anchorMin.y); lrt.anchorMax = new Vector2(1f, lrt.anchorMax.y);
                lrt.offsetMin = new Vector2(0f, lrt.offsetMin.y); lrt.offsetMax = new Vector2(0f, lrt.offsetMax.y);
                float w = Mathf.Ceil(label.GetPreferredValues(label.text).x) + 12f;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                var le = go.GetComponent<LayoutElement>();
                if (le != null) { le.preferredWidth = w; le.minWidth = w; }
                if (log) Log.LogInfo("[historique] largeur du bouton : " + w);
            }
            catch (Exception e) { Log.LogWarning("[historique] largeur : " + e.Message); }
        }

        // ---------- fenetre facon Windows : deplacable par le bandeau, redimensionnable par le coin ----------
        public static RectTransform panel;
        public static Transform ListParent { get { return listParent; } }
        static string FramePath { get { return Path.Combine(BepInEx.Paths.ConfigPath, "praedlab.mailalerts.window.txt"); } }
        public const float MinW = 480f, MinH = 240f;

        static void SetupFrame()
        {
            // pas de fond noir sur tout l'ecran : on peut continuer a utiliser le jeu derriere
            var shade = Find(window.transform, "Shading"); if (shade != null) shade.gameObject.SetActive(false);
            var inner = Find(window.transform, "Inner");
            panel = inner as RectTransform;
            if (panel == null) { Log.LogWarning("[historique] cadre Inner introuvable"); return; }
            foreach (var f in panel.GetComponents<ContentSizeFitter>()) f.enabled = false;
            var le = panel.GetComponent<LayoutElement>(); if (le != null) le.ignoreLayout = true;
            // coin haut-gauche comme repere, plus simple pour deplacer et redimensionner
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 1f);
            float w = 820f, h = 440f, x = -w / 2f, y = h / 2f;
            try
            {
                if (File.Exists(FramePath))
                {
                    var p = File.ReadAllText(FramePath).Trim().Split(';');
                    var ci = CultureInfo.InvariantCulture;
                    x = float.Parse(p[0], ci); y = float.Parse(p[1], ci); w = float.Parse(p[2], ci); h = float.Parse(p[3], ci);
                }
            }
            catch { }
            panel.sizeDelta = new Vector2(Mathf.Max(MinW, w), Mathf.Max(MinH, h));
            panel.anchoredPosition = new Vector2(x, y);
            Clamp();

            var cap = Find(panel, "Caption Panel");
            if (cap != null)
            {
                var g = cap.GetComponent<Graphic>();
                if (g == null) { var im = cap.gameObject.AddComponent<Image>(); im.color = new Color(0, 0, 0, 0); }
                else g.raycastTarget = true;
                cap.gameObject.AddComponent<HistoryDrag>().resize = false;
            }
            // poignee de redimensionnement en bas a droite
            var grip = new GameObject("ResizeGrip", typeof(RectTransform), typeof(Image));
            var grt = (RectTransform)grip.transform;
            grt.SetParent(panel, false);
            grt.anchorMin = grt.anchorMax = new Vector2(1f, 0f); grt.pivot = new Vector2(1f, 0f);
            grt.sizeDelta = new Vector2(18f, 18f); grt.anchoredPosition = Vector2.zero;
            grip.GetComponent<Image>().color = new Color(0.75f, 0.75f, 0.75f, 0.35f);
            grip.AddComponent<HistoryDrag>().resize = true;
            grip.transform.SetAsLastSibling();
            ApplyLook();
            Log.LogInfo("[historique] fenetre deplacable : " + panel.anchoredPosition + " taille " + panel.sizeDelta);
        }

        public static void Clamp()
        {
            if (panel == null) return;
            var cv = canvasGo != null ? canvasGo.transform as RectTransform : null;
            if (cv == null) return;
            Vector2 half = cv.rect.size / 2f, sz = panel.sizeDelta, p = panel.anchoredPosition;
            sz.x = Mathf.Clamp(sz.x, MinW, cv.rect.width); sz.y = Mathf.Clamp(sz.y, MinH, cv.rect.height);
            p.x = Mathf.Clamp(p.x, -half.x, half.x - sz.x);
            p.y = Mathf.Clamp(p.y, -half.y + sz.y, half.y);
            panel.sizeDelta = sz; panel.anchoredPosition = p;
        }

        public static void SaveFrame()
        {
            if (panel == null) return;
            try
            {
                var ci = CultureInfo.InvariantCulture; var p = panel.anchoredPosition; var z = panel.sizeDelta;
                File.WriteAllText(FramePath, p.x.ToString(ci) + ";" + p.y.ToString(ci) + ";" + z.x.ToString(ci) + ";" + z.y.ToString(ci));
            }
            catch { }
        }

        // fond et transparence reglables
        static Image bgImg; static Color bgOrig; static bool bgSaved;
        public static void ApplyLook()
        {
            // le titre est remis a chaque ouverture : le script de traduction du jeu le remplace par "Changements de profil disponibles"
            if (caption != null && caption.text != Loc.T("h_btn")) caption.text = Loc.T("h_btn");
            try
            {
                if (panel == null) return;
                var cg = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = cfgAlpha != null ? Mathf.Clamp(cfgAlpha.Value, 0.2f, 1f) : 1f;
                if (bgImg == null)
                {
                    // le vrai grand fond noir : la plus grande image entre la fenetre et le cadre (pas celles de la liste)
                    float best = -1f;
                    var cands = new List<Image>();
                    for (Transform p = panel; p != null && p != canvasGo.transform; p = p.parent) { var im0 = p.GetComponent<Image>(); if (im0 != null) cands.Add(im0); }
                    for (int k = 0; k < panel.childCount; k++) { var im = panel.GetChild(k).GetComponent<Image>(); if (im != null) cands.Add(im); }
                    foreach (var im in cands)
                    {
                        if (listParent != null && im.transform.IsChildOf(listParent)) continue;
                        float ar = im.rectTransform.rect.width * im.rectTransform.rect.height;
                        Log.LogInfo("[historique] fond candidat : " + im.name + " " + im.rectTransform.rect.size + " couleur " + im.color);
                        if (ar > best) { best = ar; bgImg = im; }
                    }
                    // le log a montre : le grand fond noir = Inner (le cadre lui-meme). Border = le liseré, on n'y touche pas
                    var innerImg = panel.GetComponent<Image>(); if (innerImg != null) bgImg = innerImg;
                    if (bgImg != null) Log.LogInfo("[historique] fond choisi : " + bgImg.name);
                    if (bgImg != null && !bgSaved) { bgSaved = true; bgOrig = bgImg.color; Log.LogInfo("[historique] fond : " + bgImg.name + " couleur d'origine " + bgOrig); }
                }
                FixTop();
                HistoryBg.Apply(panel, bgImg != null ? bgImg.transform : null);
                if (bgImg != null) bgImg.color = cfgBgOn != null && cfgBgOn.Value && cfgBgColor != null ? cfgBgColor.Value : bgOrig;
            }
            catch (Exception e) { Log.LogWarning("[historique] fond : " + e.Message); }
        }

        // arc-en-ciel sur les lignes (chaque ligne decalee pour une vague de couleurs)
        static readonly List<TextMeshProUGUI> rowLabels = new List<TextMeshProUGUI>();
        static bool wasRainbow;
        public static void Tick()
        {
            if (!IsOpen) return;
            HistoryBg.Tick();
            bool on = cfgRowRainbow != null && cfgRowRainbow.Value;
            if (!on) { if (wasRainbow) { wasRainbow = false; Refresh(); } return; }
            wasRainbow = true;
            float t = Time.realtimeSinceStartup * 0.25f;
            for (int k = 0; k < rowLabels.Count; k++)
            {
                var l = rowLabels[k]; if (l == null) continue;
                l.color = Color.HSVToRGB(Mathf.Repeat(t + k * 0.07f, 1f), 0.75f, 1f);
            }
        }

        // ouvre le gestionnaire de reglages BepInEx (celui qui est installe), filtre sur la section Historique
        public static void OpenSettings()
        {
            try
            {
                foreach (var kv in BepInEx.Bootstrap.Chainloader.PluginInfos)
                {
                    var inst = kv.Value != null ? kv.Value.Instance : null;
                    if (inst == null) continue;
                    var t = inst.GetType();
                    var p = t.GetProperty("DisplayingWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (p == null || !p.CanWrite) continue;
                    p.SetValue(inst, true, null);
                    var ss = t.GetProperty("SearchString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    try { if (ss != null && ss.CanWrite) ss.SetValue(inst, "World Mail Alerts", null); } catch { }
                    Log.LogInfo("[historique] reglages ouverts via " + kv.Key);
                    return;
                }
                Log.LogWarning("[historique] gestionnaire de reglages introuvable : ouvre F12 a la main");
            }
            catch (Exception e) { Log.LogWarning("[historique] reglages : " + e.Message); }
        }

        static RectTransform summaryRt;
        // boutons colles en bas de la fenetre, liste etiree entre le bandeau et le bilan
        static void LayoutFrame(RectTransform bar)
        {
            try
            {
                if (panel == null) return;
                float barH = 40f;
                if (bar != null)
                {
                    barH = Mathf.Max(30f, bar.rect.height);
                    bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(0.5f, 0f);
                    bar.offsetMin = new Vector2(14f, 0f); bar.offsetMax = new Vector2(-14f, 0f);
                    bar.sizeDelta = new Vector2(-28f, barH); bar.anchoredPosition = new Vector2(0f, 10f);
                }
                Transform area = listParent; Transform found = null;
                while (area != null && area != panel) { if (area.parent == panel) { found = area; break; } area = area.parent; }
                var art = found as RectTransform;
                if (art != null && art != bar)
                {
                    foreach (var f in art.GetComponents<ContentSizeFitter>()) f.enabled = false;
                    var le = art.GetComponent<LayoutElement>(); if (le != null) le.ignoreLayout = true;
                    art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one; art.pivot = new Vector2(0.5f, 0.5f);
                    bool barInside = bar != null && bar.parent == art;
                    // si les boutons sont dans la meme zone, la zone prend tout et la liste s'arrete au-dessus du bilan
                    art.offsetMin = new Vector2(14f, barInside ? 0f : 10f + barH + 8f); art.offsetMax = new Vector2(-14f, -72f);
                    var lay = art.GetComponent<HorizontalOrVerticalLayoutGroup>(); if (lay != null) lay.enabled = false;
                    for (int k = 0; k < art.childCount; k++)
                    {
                        var c = art.GetChild(k) as RectTransform; if (c == null || c == bar) continue;
                        c.anchorMin = Vector2.zero; c.anchorMax = Vector2.one;
                        c.offsetMin = new Vector2(0f, barInside ? 10f + barH + 8f : 0f); c.offsetMax = Vector2.zero;
                    }
                }
                Log.LogInfo("[historique] mise en page : barre=" + (bar != null ? bar.name + " parent=" + bar.parent.name : "?") + " liste=" + (art != null ? art.name : "?"));
            }
            catch (Exception e) { Log.LogWarning("[historique] mise en page : " + e.Message); }
        }

        static Sprite gearSprite;
        static Sprite MakeGear()
        {
            if (gearSprite != null) return gearSprite;
            const int N = 64; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[N * N]; float c = (N - 1) / 2f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = x - c, dy = y - c, r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                    float tooth = Mathf.Cos(ang * 8f);                       // 8 dents
                    float outer = 22f + (tooth > 0.35f ? 8f : 0f);           // corps + dents
                    float a = Mathf.Clamp01(outer + 0.5f - r) * Mathf.Clamp01(r - 9.5f); // trou au centre, bords lisses
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            gearSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            gearSprite.name = "WMA_gear";
            return gearSprite;
        }

        // le bandeau n'est pas tout en haut du cadre : on mesure l'ecart reel et on fait commencer
        // la fenetre visible (fond du bandeau, cadre, fond noir, image) pile au haut du bandeau
        public static float TopGap, CapH = 17f;
        static void FixTop()
        {
            try
            {
                if (panel == null || window == null || !window.activeInHierarchy) return;
                var cap = Find(panel, "Caption Panel") as RectTransform;
                if (cap == null) return;
                Canvas.ForceUpdateCanvases();
                Vector3[] c = new Vector3[4]; cap.GetWorldCorners(c);
                float capTop = panel.InverseTransformPoint(c[1]).y, capBot = panel.InverseTransformPoint(c[0]).y;
                float gap = Mathf.Max(0f, panel.rect.yMax - capTop), ch = Mathf.Max(1f, capTop - capBot);
                if (Mathf.Abs(gap - TopGap) > 0.5f || Mathf.Abs(ch - CapH) > 0.5f) Log.LogInfo("[historique] ecart au-dessus du bandeau : " + gap + " px, hauteur du bandeau : " + ch);
                TopGap = gap; CapH = ch;
                var top = panel.Find("WMA_TopBar") as RectTransform;
                if (top != null) { top.offsetMax = new Vector2(0f, -gap); top.offsetMin = new Vector2(0f, -(gap + ch)); }
                var body = panel.Find("WMA_Body") as RectTransform;
                if (body != null) body.offsetMax = new Vector2(0f, -(gap + ch));
                var bd = panel.Find("Border") as RectTransform;
                if (bd != null) bd.offsetMax = new Vector2(bd.offsetMax.x, 5f - gap); // le cadre entoure aussi le bandeau, 5 px autour comme sur les cotes
            }
            catch (Exception e) { Log.LogWarning("[historique] haut : " + e.Message); }
        }

        // une copie du titre garde le script de traduction du jeu, qui remet son propre texte ("Chargement"...) : on l'enleve
        public static void StripScripts(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform || c is CanvasRenderer || c is TextMeshProUGUI || c is LayoutElement || c is Image || c is Button) continue;
                try { UnityEngine.Object.DestroyImmediate(c); } catch { }
            }
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++) { var r = Find(t.GetChild(i), name); if (r != null) return r; }
            return null;
        }
    }

    public class HistoryDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool resize;
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e)
        {
            var p = HistoryUI.panel; if (p == null) return;
            var cv = GetComponentInParent<Canvas>(); float sf = cv != null && cv.rootCanvas.scaleFactor > 0.0001f ? cv.rootCanvas.scaleFactor : 1f;
            Vector2 d = e.delta / sf;
            if (resize) p.sizeDelta = new Vector2(Mathf.Max(HistoryUI.MinW, p.sizeDelta.x + d.x), Mathf.Max(HistoryUI.MinH, p.sizeDelta.y - d.y));
            else p.anchoredPosition += d;
            HistoryUI.Clamp();
        }
        public void OnEndDrag(PointerEventData e) { HistoryUI.SaveFrame(); if (resize) HistoryUI.Relayout(); }
    }
}
