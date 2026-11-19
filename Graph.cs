using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MailCountdown
{
    // Graphique de la journee : evolution des roubles (depuis le debut de la journee) + cumul des ventes du marche
    public static class MoneyGraph
    {
        public static ManualLogSource Log;
        public static Func<bool> InRaid = () => false;
        public static Func<long[]> Pending = () => new long[3];
        public static BepInEx.Configuration.ConfigEntry<bool> cfgShowGold;
        static float rateUsd = 143f, rateEur = 158f, rateGp = 7500f;
        class Pt { public DateTime t; public long rub; }
        static readonly List<Pt> pts = new List<Pt>();
        static bool loaded; static float nextSample; static long lastRead = long.MinValue;
        static string FilePath { get { return Path.Combine(BepInEx.Paths.ConfigPath, "praedlab.mailalerts.wealth2.txt"); } }
        const int KeepDays = 31;

        // ---------- releve de l'argent ----------
        public static void Tick()
        {
            if (Time.realtimeSinceStartup < nextSample) return;
            nextSample = Time.realtimeSinceStartup + 15f;
            try
            {
                if (InRaid()) return;
                long rub;
                if (!ReadRoubles(out rub)) return;
                // on attend que le montant soit stable sur 2 releves : quand tu recuperes un courrier, l'argent passe
                // de la messagerie a la planque en deux temps, et ce faux creux ne doit compter ni en depense ni en gain
                if (rub != lastRead) { lastRead = rub; return; }
                Load();
                var last = pts.Count > 0 ? pts[pts.Count - 1] : null;
                var now = DateTime.Now;
                // un point si l'argent a change, ou au moins un toutes les 10 minutes pour garder la courbe continue
                if (last == null || last.rub != rub || (now - last.t).TotalMinutes >= 10 || last.t.Date != now.Date)
                {
                    pts.Add(new Pt { t = now, rub = rub });
                    Trim(); Save();
                    if (view != null && view.activeInHierarchy) Draw();
                }
            }
            catch (Exception e) { if (errLogs++ < 5) Log.LogWarning("[graphique] releve : " + e.Message); }
        }
        static int errLogs;
        // variation entre deux releves. Ignoree s'il y a un trou de plus de 20 min (jeu ferme, changement de profil) :
        // l'argent n'a pas pu bouger pendant ce temps, ce ne sont ni des rentrees ni des depenses
        static long Dl(List<Pt> day, int i)
        {
            if ((day[i].t - day[i - 1].t).TotalMinutes > 20) return 0;
            return day[i].rub - day[i - 1].rub;
        }

        static object FindSession()
        {
            foreach (Type t in new[] { typeof(ClientApplication<IEftSession>), typeof(TarkovApplication) })
            {
                try
                {
                    Type st = typeof(Singleton<>).MakeGenericType(t);
                    if (!(bool)st.GetProperty("Instantiated").GetValue(null, null)) continue;
                    object inst = st.GetProperty("Instance").GetValue(null, null);
                    var m = inst.GetType().GetMethod("GetClientBackEndSession");
                    var s = m != null ? m.Invoke(inst, null) : null;
                    if (s != null) return s;
                }
                catch { }
            }
            return null;
        }

        static bool ReadRoubles(out long rub)
        {
            // tout l'argent en roubles : planque (₽ + $ + € convertis) + argent pas encore recupere dans la messagerie
            rub = 0;
            var s = FindSession() as IEftSession;
            if (s == null || s.Profile == null || s.Profile.Inventory == null || s.Profile.Inventory.Stash == null) return false;
            long r = 0, u = 0, e = 0, gp = 0;
            var all = new List<EFT.InventoryLogic.Item>(EFT.InventoryLogic.ItemExtensions.GetAllItems(s.Profile.Inventory.Stash));
            try { if (s.Profile.Inventory.Equipment != null) all.AddRange(EFT.InventoryLogic.ItemExtensions.GetAllItems(s.Profile.Inventory.Equipment)); } catch { }
            foreach (var it in all)
            {
                string tpl = it.TemplateId.ToString();
                if (tpl == "5449016a4bdc2d6f028b456f") r += it.StackObjectsCount;
                else if (tpl == "5696686a4bdc2da3298b456a") { u += it.StackObjectsCount; try { if (it.Template.CreditsPrice > 0) rateUsd = it.Template.CreditsPrice; } catch { } }
                else if (tpl == "569668774bdc2da2298b4568") { e += it.StackObjectsCount; try { if (it.Template.CreditsPrice > 0) rateEur = it.Template.CreditsPrice; } catch { } }
                else if (tpl == "5d235b4d86f7742e017bc88a") { gp += it.StackObjectsCount; try { if (it.Template.CreditsPrice > 0) rateGp = it.Template.CreditsPrice; } catch { } }
            }
            // seulement l'argent de la planque : le courrier se charge et se decharge avec la messagerie, ce qui faisait des faux creux
            rub = r + (long)(u * rateUsd) + (long)(e * rateEur) + (long)(gp * rateGp);
            return true;
        }

        static void Load()
        {
            if (loaded) return; loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var l in File.ReadAllLines(FilePath))
                {
                    var p = l.Split(';'); long tk, r;
                    if (p.Length == 2 && long.TryParse(p[0], out tk) && long.TryParse(p[1], out r)) pts.Add(new Pt { t = new DateTime(tk), rub = r });
                }
                Trim();
            }
            catch (Exception e) { Log.LogWarning("[graphique] lecture : " + e.Message); }
        }
        static void Trim() { var lim = DateTime.Now.Date.AddDays(-KeepDays); pts.RemoveAll(p => p.t < lim); }
        static void Save()
        {
            try { var lines = new List<string>(); foreach (var p in pts) lines.Add(p.t.Ticks + ";" + p.rub); File.WriteAllLines(FilePath, lines.ToArray()); }
            catch { }
        }

        // ---------- affichage ----------
        static GameObject view; static RawImage chart; static Texture2D tex;
        static TextMeshProUGUI title, yTop, yBot, yZero, xLeft, xRight, legend;
        static RectTransform areaRef;
        static float gW, gH;
        // taille fixe du graphique : il ne suit plus la zone de la liste, qui retrecit quand on vide l'historique
        static void FixViewSize()
        {
            try
            {
                if (view == null || areaRef == null) return;
                float w = areaRef.rect.width, h = areaRef.rect.height;
                var par = areaRef.parent as RectTransform;
                if (w < 300f && par != null) w = par.rect.width - 24f;
                // la largeur vient de la fenetre elle-meme : le graphique reste aussi grand que la fenetre, liste vide ou pas
                var win = HistoryUI.panel;
                if (win != null) w = Mathf.Max(w, win.rect.width - 40f);
                if (h < 200f && par != null) h = par.rect.height - 140f;
                if (w < 300f) w = 640f; if (h < 200f) h = 360f;
                gW = Mathf.Max(gW, w); gH = Mathf.Max(gH, h);
                var rt = (RectTransform)view.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(gW, gH); rt.anchoredPosition = Vector2.zero;
            }
            catch { }
        }
        // la zone garde sa largeur et sa hauteur meme quand la liste est vide (« Vider ») : sinon le graphique se tasse
        static void KeepAreaSize()
        {
            try
            {
                if (areaRef == null) return;
                var ale = areaRef.GetComponent<LayoutElement>() ?? areaRef.gameObject.AddComponent<LayoutElement>();
                float w = areaRef.rect.width, h = areaRef.rect.height;
                var par = areaRef.parent as RectTransform;
                if (par != null) w = Mathf.Max(w, par.rect.width - 24f);
                ale.minWidth = Mathf.Max(ale.minWidth, w > 300f ? w : 640f);
                ale.minHeight = Mathf.Max(ale.minHeight, h > 200f ? h : 360f);
                ale.flexibleWidth = Mathf.Max(ale.flexibleWidth, 1f); ale.flexibleHeight = Mathf.Max(ale.flexibleHeight, 1f);
            }
            catch { }
        }
        // periode du graphique : 0 = aujourd'hui, 1 = 7 jours, 2 = 30 jours
        static int period = -1;
        static readonly Image[] perBg = new Image[3];
        static readonly TextMeshProUGUI[] perTx = new TextMeshProUGUI[3];
        static string PeriodFile => Path.Combine(BepInEx.Paths.ConfigPath, "praedlab.mailalerts.graphperiod.txt");
        static int Period
        {
            get { if (period < 0) { period = 0; try { int v; if (File.Exists(PeriodFile) && int.TryParse(File.ReadAllText(PeriodFile).Trim(), out v) && v >= 0 && v <= 2) period = v; } catch { } } return period; }
            set { period = value; try { File.WriteAllText(PeriodFile, value.ToString()); } catch { } }
        }
        static readonly Dictionary<string, string[]> PerNames = new Dictionary<string, string[]>
        {
            { "en", new[] { "TODAY", "7 DAYS", "30 DAYS" } }, { "fr", new[] { "AUJOURD'HUI", "7 JOURS", "30 JOURS" } },
            { "ru", new[] { "СЕГОДНЯ", "7 ДНЕЙ", "30 ДНЕЙ" } }, { "de", new[] { "HEUTE", "7 TAGE", "30 TAGE" } },
            { "es", new[] { "HOY", "7 DÍAS", "30 DÍAS" } }, { "zh", new[] { "今天", "7 天", "30 天" } },
            { "ja", new[] { "今日", "7日間", "30日間" } }, { "ko", new[] { "오늘", "7일", "30일" } },
            { "pl", new[] { "DZIŚ", "7 DNI", "30 DNI" } }, { "pt", new[] { "HOJE", "7 DIAS", "30 DIAS" } },
            { "cs", new[] { "DNES", "7 DNÍ", "30 DNÍ" } }, { "tr", new[] { "BUGÜN", "7 GÜN", "30 GÜN" } },
            { "it", new[] { "OGGI", "7 GIORNI", "30 GIORNI" } }, { "hu", new[] { "MA", "7 NAP", "30 NAP" } },
            { "ro", new[] { "AZI", "7 ZILE", "30 DE ZILE" } }, { "sk", new[] { "DNES", "7 DNÍ", "30 DNÍ" } },
        };
        static string PerName(int i) { string[] a; if (!PerNames.TryGetValue(Loc.Lang ?? "en", out a)) a = PerNames["en"]; return a[i]; }
        static void PaintTabs()
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = i == Period;
                if (perBg[i] != null) perBg[i].color = on ? new Color(0.95f, 0.78f, 0.35f, 1f) : new Color(0.18f, 0.19f, 0.2f, 1f);
                if (perTx[i] != null) { perTx[i].text = PerName(i); perTx[i].color = on ? new Color(0.06f, 0.06f, 0.06f) : new Color(0.92f, 0.92f, 0.92f); }
            }
        }
        static TextMeshProUGUI fontRef; static readonly List<TextMeshProUGUI> marks = new List<TextMeshProUGUI>();
        public static bool Visible { get { return view != null && view.activeSelf; } }

        // cree le panneau du graphique par-dessus la zone de la liste (meme police que le titre de la fenetre)
        public static void Build(Transform area, TextMeshProUGUI font)
        {
            if (view != null || area == null || font == null) return;
            fontRef = font;
            try
            {
                // la zone de la liste garde sa taille meme vide : sinon le graphique s'ecrase apres « Vider »
                areaRef = area as RectTransform;
                KeepAreaSize();
            }
            catch { }
            view = new GameObject("WMA_Graph", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)view.transform; rt.SetParent(area, false);
            view.AddComponent<LayoutElement>().ignoreLayout = true;
            FixViewSize();
            view.GetComponent<Image>().color = new Color(0.03f, 0.035f, 0.04f, 1f);
            title = Label(font, view.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -32), new Vector2(-8, -2), TextAlignmentOptions.Center, 1.35f);
            // titre grand mais qui retrecit seul s'il ne tient pas sur la largeur
            title.enableAutoSizing = true; title.fontSizeMax = title.fontSize; title.fontSizeMin = 10f;
            legend = Label(font, view.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 4), new Vector2(-8, 26), TextAlignmentOptions.Center, 1.05f);
            var cg = new GameObject("Chart", typeof(RectTransform), typeof(RawImage));
            var crt = (RectTransform)cg.transform; crt.SetParent(view.transform, false);
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = new Vector2(90, 52); crt.offsetMax = new Vector2(-12, -74);
            chart = cg.GetComponent<RawImage>(); chart.raycastTarget = false;
            // 3 gros boutons bien visibles : aujourd'hui / 7 jours / 30 jours
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = new GameObject("WMA_Period" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                var br = (RectTransform)b.transform; br.SetParent(view.transform, false);
                br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f); br.pivot = new Vector2(0.5f, 1f);
                br.sizeDelta = new Vector2(150f, 30f); br.anchoredPosition = new Vector2((i - 1) * 160f, -36f);
                b.AddComponent<LayoutElement>().ignoreLayout = true;
                perBg[i] = b.GetComponent<Image>(); perBg[i].raycastTarget = true;
                perTx[i] = Label(font, b.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 1.0f);
                b.GetComponent<Button>().onClick.AddListener(() => { Period = idx; PaintTabs(); Draw(); });
            }
            PaintTabs();
            yTop = Label(font, view.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -94), new Vector2(86, -74), TextAlignmentOptions.Right, 0.95f);
            yBot = Label(font, view.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(4, 52), new Vector2(86, 72), TextAlignmentOptions.Right, 0.95f);
            yZero = Label(font, view.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(4, 0), new Vector2(86, 20), TextAlignmentOptions.Right, 0.95f);
            xLeft = Label(font, view.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(90, 28), new Vector2(0, 50), TextAlignmentOptions.Left, 0.95f);
            xRight = Label(font, view.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(0, 28), new Vector2(-12, 50), TextAlignmentOptions.Right, 0.95f);
            view.SetActive(false);
        }

        static TextMeshProUGUI Label(TextMeshProUGUI font, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, TextAlignmentOptions al, float scale)
        {
            var go = UnityEngine.Object.Instantiate(font.gameObject, parent, false);
            foreach (Transform ch in go.transform) UnityEngine.Object.Destroy(ch.gameObject);
            HistoryUI.StripScripts(go);
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); le.ignoreLayout = true;
            var t = go.GetComponent<TextMeshProUGUI>(); var r = (RectTransform)go.transform;
            r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = new Vector2(0.5f, 0.5f); r.offsetMin = oMin; r.offsetMax = oMax;
            t.alignment = al; t.enableWordWrapping = false; t.richText = true; t.raycastTarget = false; t.enableAutoSizing = false;
            t.fontSize = Mathf.Max(10f, font.fontSize * 0.72f * scale); t.color = new Color(0.95f, 0.95f, 0.95f);
            // chiffres lisibles : gras + contour sombre
            t.fontStyle = FontStyles.Bold; try { t.outlineWidth = 0.22f; t.outlineColor = new Color32(0, 0, 0, 255); } catch { }
            return t;
        }

        public static void Toggle()
        {
            if (view == null) return;
            view.SetActive(!view.activeSelf);
            view.transform.SetAsLastSibling();
            // la liste derriere est cachee pendant que le graphique est affiche
            foreach (Transform ch in view.transform.parent) if (ch != view.transform && ch.name != "Borders") { var cg = ch.GetComponent<CanvasGroup>() ?? ch.gameObject.AddComponent<CanvasGroup>(); cg.alpha = view.activeSelf ? 0f : 1f; }
            if (view.activeSelf) Draw();
        }
        public static void Hide() { if (view != null) view.SetActive(false); }

        static readonly Color cBal = new Color(0.95f, 0.78f, 0.35f), cSale = new Color(0.45f, 0.9f, 0.4f), cGrid = new Color(1f, 1f, 1f, 0.06f), cSpent = new Color(0.9f, 0.42f, 0.35f), cZero = new Color(1f, 1f, 1f, 0.25f);

        // ventes du jour : (heure, montant) venant de l'historique
        public static Func<List<KeyValuePair<DateTime, long>>> SalesToday = () => new List<KeyValuePair<DateTime, long>>();

        public static void Draw()
        {
            try
            {
                if (view == null) return;
                KeepAreaSize(); FixViewSize();
                Load();
                int per = Period; PaintTabs();
                var today = DateTime.Now.Date.AddDays(per == 0 ? 0 : per == 1 ? -6 : -29);
                var day = pts.FindAll(p => p.t >= today);
                var sales = new List<KeyValuePair<DateTime, long>>();
                long soldSum = 0;
                long gain = 0, spent = 0;
                for (int i = 1; i < day.Count; i++) { long d = Dl(day, i); if (d > 0) gain += d; else spent -= d; }
                soldSum = gain; // rentrees = tout l'argent entre dans la planque
                long bal = gain - spent;
                string ht = Loc.T("h_today"); int cut = ht.IndexOf("   "); if (per > 0 && cut > 0) ht = PerName(per) + ht.Substring(cut);
                title.text = ht.Replace("{S}", "<color=#73e666>+" + Money(soldSum) + " ₽</color>").Replace("{D}", "<color=#e66a5a>-" + Money(spent) + " ₽</color>")
                    .Replace("{B}", (bal >= 0 ? "<color=#f2c75a>+" : "<color=#e66a5a>-") + Money(Math.Abs(bal)) + " ₽</color>");
                legend.text = (cfgShowGold != null && cfgShowGold.Value ? "<color=#f2c75a>━</color> " + Loc.T("h_gbal") + "      " : "") + "<color=#73e666>━</color> " + Loc.T("h_gsales") + "      <color=#e66a5a>━</color> " + Loc.T("h_gspent");

                const int W = 760, H = 260;
                if (tex == null) { tex = new Texture2D(W, H, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; }
                var px = new Color32[W * H];
                for (int g = 1; g < 4; g++) { int y = H * g / 4; for (int x = 0; x < W; x++) px[y * W + x] = cGrid; }
                for (int g = 1; g < 6; g++) { int x = W * g / 6; for (int y = 0; y < H; y++) px[y * W + x] = cGrid; }

                foreach (var m in marks) if (m != null) m.gameObject.SetActive(false);
                if (day.Count < 2)
                {
                    yTop.text = yBot.text = xLeft.text = xRight.text = "";
                    title.text = Loc.T("h_nodata");
                    tex.SetPixels32(px); tex.Apply(false); chart.texture = tex; return;
                }
                DateTime t0 = day[0].t;
                DateTime t1 = DateTime.Now; if ((t1 - t0).TotalMinutes < 10) t0 = t1.AddMinutes(-10);
                // les deux courbes partent de 0 : variation de l'argent et cumul des ventes
                var balS = new List<KeyValuePair<DateTime, long>>(); { long acc = 0; for (int i = 0; i < day.Count; i++) { if (i > 0) acc += Dl(day, i); balS.Add(new KeyValuePair<DateTime, long>(day[i].t, acc)); } }
                if (balS.Count > 0) balS.Add(new KeyValuePair<DateTime, long>(t1, balS[balS.Count - 1].Value));
                // rentrees cumulees (vers le haut)
                var saleS = new List<KeyValuePair<DateTime, long>>(); long cum = 0;
                if (day.Count > 0) saleS.Add(new KeyValuePair<DateTime, long>(day[0].t, 0));
                for (int i = 1; i < day.Count; i++) { long d = Dl(day, i); if (d > 0) { saleS.Add(new KeyValuePair<DateTime, long>(day[i].t, cum)); cum += d; } saleS.Add(new KeyValuePair<DateTime, long>(day[i].t, cum)); }
                if (saleS.Count > 0) saleS.Add(new KeyValuePair<DateTime, long>(t1, cum));
                // depenses cumulees, vers le bas
                var spentS = new List<KeyValuePair<DateTime, long>>(); long sp = 0;
                if (day.Count > 0) spentS.Add(new KeyValuePair<DateTime, long>(day[0].t, 0));
                for (int i = 1; i < day.Count; i++) { long d = Dl(day, i); if (d < 0) { spentS.Add(new KeyValuePair<DateTime, long>(day[i].t, -sp)); sp -= d; } spentS.Add(new KeyValuePair<DateTime, long>(day[i].t, -sp)); }
                if (spentS.Count > 0) spentS.Add(new KeyValuePair<DateTime, long>(t1, -sp));
                long vmin = 0, vmax = 1;
                bool gold = cfgShowGold != null && cfgShowGold.Value;
                if (gold) foreach (var p in balS) { vmin = Math.Min(vmin, p.Value); vmax = Math.Max(vmax, p.Value); }
                foreach (var p in saleS) { vmin = Math.Min(vmin, p.Value); vmax = Math.Max(vmax, p.Value); }
                foreach (var p in spentS) { vmin = Math.Min(vmin, p.Value); vmax = Math.Max(vmax, p.Value); }
                double span = Math.Max(1, vmax - vmin); vmax += (long)(span * 0.16); vmin -= (long)(span * 0.16); span = vmax - vmin;
                Func<DateTime, int> X = d => Mathf.Clamp((int)((d - t0).TotalSeconds / Math.Max(1, (t1 - t0).TotalSeconds) * (W - 1)), 0, W - 1);
                Func<long, int> Y = v => Mathf.Clamp((int)((v - vmin) / span * (H - 1)), 0, H - 1);
                int yz = Y(0); for (int x = 0; x < W; x++) px[yz * W + x] = cZero;
                Poly(px, W, H, spentS, X, Y, cSpent);
                Poly(px, W, H, saleS, X, Y, cSale);
                // le solde par-dessus, en trait epais : sinon il se cache sous la verte ou la rouge
                if (gold) { Func<long, int> Yu = v => Mathf.Clamp(Y(v) + 2, 0, H - 1), Yd = v => Mathf.Clamp(Y(v) - 2, 0, H - 1); Poly(px, W, H, balS, X, Yd, cBal); Poly(px, W, H, balS, X, Yu, cBal); Poly(px, W, H, balS, X, Y, cBal); }
                // montants sur les courbes : seulement les mouvements qui comptent (>= 3 % du plus gros total), sinon c'est illisible
                long thr = Math.Max(1, Math.Max(gain, spent) * 3 / 100);
                int mi = 0;
                Action<int, int, long, bool> Mark = (xp, yp, amount, up) =>
                {
                    if (mi >= 30) return;
                    if (mi >= marks.Count) { var nt = Label(fontRef, chart.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 1.05f); marks.Add(nt); }
                    var t = marks[mi++]; t.gameObject.SetActive(true);
                    t.text = up ? "<color=#73e666>+" + Short(amount) + "</color>" : "<color=#e66a5a>-" + Short(amount) + "</color>";
                    float fx = xp / (float)(W - 1), fy = yp / (float)(H - 1);
                    var r = (RectTransform)t.transform; r.anchorMin = r.anchorMax = new Vector2(fx, fy); r.sizeDelta = new Vector2(90f, 20f);
                    r.pivot = new Vector2(fx > 0.93f ? 1f : (fx < 0.07f ? 0f : 0.5f), up ? 0f : 1f); r.anchoredPosition = new Vector2(0f, up ? 6f : -6f);
                };
                if (per == 0)
                {
                    // aujourd'hui : un point par mouvement important
                    long cAcc = 0, sAcc = 0; int lastX = -1000, lastXr = -1000;
                    for (int i = 1; i < day.Count; i++)
                    {
                        long d = Dl(day, i); int xp = X(day[i].t);
                        if (d > 0) { cAcc += d; if (d < thr) continue; Dot(px, W, H, xp, Y(cAcc), cSale); if (xp - lastX >= 130) { lastX = xp; Mark(xp, Y(cAcc), d, true); } }
                        else if (d < 0) { sAcc -= d; if (-d < thr) continue; Dot(px, W, H, xp, Y(-sAcc), cSpent); if (xp - lastXr >= 130) { lastXr = xp; Mark(xp, Y(-sAcc), -d, false); } }
                    }
                }
                else
                {
                    // 7 / 30 jours : un repere par jour (trait vertical) et le total du jour en fin de journee
                    long cAcc = 0, sAcc = 0; long dg = 0, ds = 0; int lastX = -1000;
                    for (int i = 1; i < day.Count; i++)
                    {
                        long d = Dl(day, i); if (d > 0) { cAcc += d; dg += d; } else { sAcc -= d; ds -= d; }
                        bool endOfDay = i == day.Count - 1 || day[i + 1].t.Date != day[i].t.Date;
                        if (!endOfDay) continue;
                        int xp = X(day[i].t);
                        for (int y = 0; y < H; y += 3) px[y * W + xp] = cZero;
                        if (dg > 0) Dot(px, W, H, xp, Y(cAcc), cSale);
                        if (ds > 0) Dot(px, W, H, xp, Y(-sAcc), cSpent);
                        if (xp - lastX >= (per == 1 ? 90 : 130))
                        {
                            lastX = xp;
                            if (dg >= thr) Mark(xp, Y(cAcc), dg, true);
                            if (ds >= thr) Mark(xp, Y(-sAcc), ds, false);
                        }
                        dg = 0; ds = 0;
                    }
                }
                tex.SetPixels32(px); tex.Apply(false); chart.texture = tex;
                yTop.text = Short(vmax) + " ₽"; yBot.text = Short(vmin) + " ₽";
                if (yZero != null) { var zr = (RectTransform)yZero.transform; float f = (float)((0 - vmin) / span); zr.anchorMin = zr.anchorMax = new Vector2(0f, 0f); zr.pivot = new Vector2(0f, 0.5f); zr.sizeDelta = new Vector2(82f, 20f); var crt2 = (RectTransform)chart.transform; zr.anchoredPosition = new Vector2(4f, crt2.offsetMin.y + f * crt2.rect.height); yZero.text = "0"; }
                string fmt = per == 0 ? "HH:mm" : "dd/MM HH:mm"; xLeft.text = t0.ToString(fmt); xRight.text = t1.ToString(fmt);
            }
            catch (Exception e) { Log.LogWarning("[graphique] dessin : " + e.Message); }
        }

        static void Poly(Color32[] px, int W, int H, List<KeyValuePair<DateTime, long>> s, Func<DateTime, int> X, Func<long, int> Y, Color c)
        {
            // trait epais (3 px) : plus lisible une fois le graphique reduit a l'ecran
            for (int i = 1; i < s.Count; i++)
                for (int o = -1; o <= 1; o++)
                {
                    int x0 = X(s[i - 1].Key), y0 = Y(s[i - 1].Value), x1 = X(s[i].Key), y1 = Y(s[i].Value);
                    Line(px, W, H, x0, Mathf.Clamp(y0 + o, 0, H - 1), x1, Mathf.Clamp(y1 + o, 0, H - 1), c);
                    Line(px, W, H, Mathf.Clamp(x0 + o, 0, W - 1), y0, Mathf.Clamp(x1 + o, 0, W - 1), y1, c);
                }
        }
        static void Dot(Color32[] px, int W, int H, int cx, int cy, Color c)
        {
            Color32 cc = c, dk = new Color32(10, 12, 14, 255);
            for (int oy = -5; oy <= 5; oy++) for (int ox = -5; ox <= 5; ox++)
            {
                int r2 = ox * ox + oy * oy; if (r2 > 25) continue; int x = cx + ox, y = cy + oy; if (x < 0 || x >= W || y < 0 || y >= H) continue;
                px[y * W + x] = r2 > 16 ? dk : cc;
            }
        }
        static void Line(Color32[] px, int W, int H, int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            Color32 cc = c;
            while (true)
            {
                for (int ox = 0; ox <= 1; ox++) for (int oy = -1; oy <= 1; oy++) { int x = x0 + ox, y = y0 + oy; if (x >= 0 && x < W && y >= 0 && y < H) px[y * W + x] = cc; }
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }
        static string Money(long v) { return v.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "); }
        static string Short(long v)
        {
            long a = Math.Abs(v); string s = v < 0 ? "-" : "";
            if (a >= 1000000) return s + (a / 1000000.0).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            if (a >= 1000) return s + (a / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + "k";
            return s + a;
        }
    }
}
