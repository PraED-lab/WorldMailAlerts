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
        const int KeepDays = 14;

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
            foreach (var it in EFT.InventoryLogic.ItemExtensions.GetAllItems(s.Profile.Inventory.Stash))
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
        static TextMeshProUGUI fontRef; static readonly List<TextMeshProUGUI> marks = new List<TextMeshProUGUI>();
        public static bool Visible { get { return view != null && view.activeSelf; } }

        // cree le panneau du graphique par-dessus la zone de la liste (meme police que le titre de la fenetre)
        public static void Build(Transform area, TextMeshProUGUI font)
        {
            if (view != null || area == null || font == null) return;
            fontRef = font;
            view = new GameObject("WMA_Graph", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)view.transform; rt.SetParent(area, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            view.AddComponent<LayoutElement>().ignoreLayout = true;
            view.GetComponent<Image>().color = new Color(0.03f, 0.035f, 0.04f, 1f);
            title = Label(font, view.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -30), new Vector2(-8, -4), TextAlignmentOptions.Center, 1.0f);
            legend = Label(font, view.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 4), new Vector2(-8, 26), TextAlignmentOptions.Center, 0.85f);
            var cg = new GameObject("Chart", typeof(RectTransform), typeof(RawImage));
            var crt = (RectTransform)cg.transform; crt.SetParent(view.transform, false);
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = new Vector2(90, 52); crt.offsetMax = new Vector2(-12, -36);
            chart = cg.GetComponent<RawImage>(); chart.raycastTarget = false;
            yTop = Label(font, view.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -56), new Vector2(86, -36), TextAlignmentOptions.Right, 0.75f);
            yBot = Label(font, view.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(4, 52), new Vector2(86, 72), TextAlignmentOptions.Right, 0.75f);
            yZero = Label(font, view.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(4, 0), new Vector2(86, 20), TextAlignmentOptions.Right, 0.75f);
            xLeft = Label(font, view.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(90, 28), new Vector2(0, 50), TextAlignmentOptions.Left, 0.75f);
            xRight = Label(font, view.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(0, 28), new Vector2(-12, 50), TextAlignmentOptions.Right, 0.75f);
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
            t.fontSize = Mathf.Max(11f, font.fontSize * 1.1f * scale); t.color = new Color(0.85f, 0.85f, 0.85f);
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
                Load();
                var today = DateTime.Now.Date;
                var day = pts.FindAll(p => p.t >= today);
                var sales = new List<KeyValuePair<DateTime, long>>();
                long soldSum = 0;
                long gain = 0, spent = 0;
                for (int i = 1; i < day.Count; i++) { long d = day[i].rub - day[i - 1].rub; if (d > 0) gain += d; else spent -= d; }
                soldSum = gain; // rentrees = tout l'argent entre dans la planque
                long bal = day.Count > 0 ? day[day.Count - 1].rub - day[0].rub : 0;
                title.text = Loc.T("h_today").Replace("{S}", "<color=#73e666>+" + Money(soldSum) + " ₽</color>").Replace("{D}", "<color=#e66a5a>-" + Money(spent) + " ₽</color>")
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
                var balS = new List<KeyValuePair<DateTime, long>>(); foreach (var p in day) balS.Add(new KeyValuePair<DateTime, long>(p.t, p.rub - day[0].rub));
                if (balS.Count > 0) balS.Add(new KeyValuePair<DateTime, long>(t1, balS[balS.Count - 1].Value));
                // rentrees cumulees (vers le haut)
                var saleS = new List<KeyValuePair<DateTime, long>>(); long cum = 0;
                if (day.Count > 0) saleS.Add(new KeyValuePair<DateTime, long>(day[0].t, 0));
                for (int i = 1; i < day.Count; i++) { long d = day[i].rub - day[i - 1].rub; if (d > 0) { saleS.Add(new KeyValuePair<DateTime, long>(day[i].t, cum)); cum += d; } saleS.Add(new KeyValuePair<DateTime, long>(day[i].t, cum)); }
                if (saleS.Count > 0) saleS.Add(new KeyValuePair<DateTime, long>(t1, cum));
                // depenses cumulees, vers le bas
                var spentS = new List<KeyValuePair<DateTime, long>>(); long sp = 0;
                if (day.Count > 0) spentS.Add(new KeyValuePair<DateTime, long>(day[0].t, 0));
                for (int i = 1; i < day.Count; i++) { long d = day[i].rub - day[i - 1].rub; if (d < 0) { spentS.Add(new KeyValuePair<DateTime, long>(day[i].t, -sp)); sp -= d; } spentS.Add(new KeyValuePair<DateTime, long>(day[i].t, -sp)); }
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
                if (gold) Poly(px, W, H, balS, X, Y, cBal);
                Poly(px, W, H, spentS, X, Y, cSpent);
                Poly(px, W, H, saleS, X, Y, cSale);
                // un point sur la courbe verte a chaque rentree, avec le montant en petit
                var hits = new List<KeyValuePair<DateTime, long>>(); long c2 = 0;
                for (int i = 1; i < day.Count; i++) { long d = day[i].rub - day[i - 1].rub; if (d > 0) { c2 += d; hits.Add(new KeyValuePair<DateTime, long>(day[i].t, d)); Dot(px, W, H, X(day[i].t), Y(c2), cSale); } }
                int mi = 0; int lastX = -1000; long cAcc = 0; int hi = 0;
                for (int i = 1; i < day.Count && mi < 40; i++)
                {
                    long d = day[i].rub - day[i - 1].rub; if (d <= 0) continue; cAcc += d; hi++;
                    int xp = X(day[i].t); if (xp - lastX < 46) continue; lastX = xp;
                    if (mi >= marks.Count) { var nt = Label(fontRef, chart.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 0.62f); marks.Add(nt); }
                    var t = marks[mi++]; t.gameObject.SetActive(true);
                    t.text = "<color=#73e666>+" + Short(d) + "</color>";
                    float fx = xp / (float)(W - 1), fy = Y(cAcc) / (float)(H - 1);
                    var r = (RectTransform)t.transform; r.anchorMin = r.anchorMax = new Vector2(fx, fy); r.sizeDelta = new Vector2(70f, 16f);
                    r.pivot = new Vector2(fx > 0.93f ? 1f : (fx < 0.07f ? 0f : 0.5f), 0f); r.anchoredPosition = new Vector2(0f, 6f);
                }
                // pareil pour les depenses : point rouge et montant en dessous
                long sAcc = 0; int lastXr = -1000;
                for (int i = 1; i < day.Count; i++)
                {
                    long d = day[i].rub - day[i - 1].rub; if (d >= 0) continue; sAcc -= d;
                    int xp = X(day[i].t), yp = Y(-sAcc); Dot(px, W, H, xp, yp, cSpent);
                    if (mi >= 40 || xp - lastXr < 46) continue; lastXr = xp;
                    if (mi >= marks.Count) { var nt = Label(fontRef, chart.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, 0.62f); marks.Add(nt); }
                    var t = marks[mi++]; t.gameObject.SetActive(true);
                    t.text = "<color=#e66a5a>-" + Short(-d) + "</color>";
                    float fx = xp / (float)(W - 1), fy = yp / (float)(H - 1);
                    var r = (RectTransform)t.transform; r.anchorMin = r.anchorMax = new Vector2(fx, fy); r.sizeDelta = new Vector2(70f, 16f);
                    r.pivot = new Vector2(fx > 0.93f ? 1f : (fx < 0.07f ? 0f : 0.5f), 1f); r.anchoredPosition = new Vector2(0f, -6f);
                }
                tex.SetPixels32(px); tex.Apply(false); chart.texture = tex;
                yTop.text = Short(vmax) + " ₽"; yBot.text = Short(vmin) + " ₽";
                if (yZero != null) { var zr = (RectTransform)yZero.transform; float f = (float)((0 - vmin) / span); zr.anchorMin = zr.anchorMax = new Vector2(0f, 0f); zr.pivot = new Vector2(0f, 0.5f); zr.sizeDelta = new Vector2(82f, 20f); var crt2 = (RectTransform)chart.transform; zr.anchoredPosition = new Vector2(4f, crt2.offsetMin.y + f * crt2.rect.height); yZero.text = "0"; }
                xLeft.text = t0.ToString("HH:mm"); xRight.text = t1.ToString("HH:mm");
            }
            catch (Exception e) { Log.LogWarning("[graphique] dessin : " + e.Message); }
        }

        static void Poly(Color32[] px, int W, int H, List<KeyValuePair<DateTime, long>> s, Func<DateTime, int> X, Func<long, int> Y, Color c)
        {
            for (int i = 1; i < s.Count; i++) Line(px, W, H, X(s[i - 1].Key), Y(s[i - 1].Value), X(s[i].Key), Y(s[i].Value), c);
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
