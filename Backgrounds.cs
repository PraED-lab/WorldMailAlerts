using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace MailCountdown
{
    // Image de fond de la fenetre d'historique : fonds du jeu ou images perso (dossier WorldMailAlerts_backgrounds)
    public static class HistoryBg
    {
        public static ConfigEntry<string> cfgImage, cfgFit;
        public static ConfigEntry<float> cfgImgAlpha, cfgZoom, cfgOffX, cfgOffY; public static ConfigEntry<Color> cfgTint;
        public static ConfigEntry<string> cfgMove; public static ConfigEntry<float> cfgSpeed;
        public static readonly string[] Moves = { "None", "Slow", "PanHorizontal", "PanVertical", "ZoomPulse", "Diagonal", "Circle", "Breath", "Shake" };
        public const string None = "None";
        public const int MaxSide = 2560;
        public static string Dir { get { return Path.Combine(BepInEx.Paths.ConfigPath, "WorldMailAlerts_backgrounds"); } }
        static string GameListFile { get { return Path.Combine(BepInEx.Paths.ConfigPath, "praedlab.mailalerts.gamebackgrounds.txt"); } }

        // choix proposes dans F12 : Aucune + images perso + fonds du jeu deja reperes
        public static string[] Choices()
        {
            var list = new List<string> { None };
            try
            {
                Directory.CreateDirectory(Dir);
                foreach (var f in Directory.GetFiles(Dir))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") list.Add("Image: " + Path.GetFileName(f));
                }
            }
            catch { }
            try { if (File.Exists(GameListFile)) foreach (var l in File.ReadAllLines(GameListFile)) if (l.Trim().Length > 0) list.Add("Jeu: " + l.Trim()); } catch { }
            return list.Distinct().ToArray();
        }

        // repere les grands fonds du jeu deja charges et les note pour le prochain lancement
        static bool scanned;
        public static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        public static Sprite FindSprite(string name)
        {
            Sprite sp;
            if (spriteCache.TryGetValue(name, out sp) && sp != null && sp.texture != null) return sp;
            foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>()) if (s != null && s.name == name) { spriteCache[name] = s; return s; }
            return null;
        }
        public static void ScanGame()
        {
            if (scanned) return; scanned = true;
            try
            {
                var names = new SortedSet<string>();
                foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (s == null || s.texture == null) continue;
                    if (!spriteCache.ContainsKey(s.name)) spriteCache[s.name] = s; // une seule recherche, gardee pour la suite
                    var r = s.rect;
                    if (r.width >= 1024 && r.height >= 512 && !string.IsNullOrEmpty(s.name) && s.name.Trim().Length > 0 && !s.name.Contains(":")) names.Add(s.name);
                }
                var old = new HashSet<string>();
                if (File.Exists(GameListFile)) foreach (var l in File.ReadAllLines(GameListFile)) if (l.Trim().Length > 0) old.Add(l.Trim());
                foreach (var n in old) if (n.Trim().Length > 0) names.Add(n);
                File.WriteAllLines(GameListFile, names.ToArray());
                HistoryUI.Log.LogInfo("[fond] " + names.Count + " fond(s) du jeu reperes : " + string.Join(", ", names.ToArray()));
            }
            catch (Exception e) { HistoryUI.Log.LogWarning("[fond] reperage : " + e.Message); }
        }

        static RectTransform holder; static RawImage img; static AspectRatioFitter fitter;
        static string loadedKey = ""; static Texture ownTex;

        public static void Apply(RectTransform panel, Transform afterBackground)
        {
            try
            {
                if (panel == null) return;
                ScanGame();
                string choice = cfgImage != null ? cfgImage.Value : None;
                if (holder == null)
                {
                    var h = new GameObject("WMA_BgHolder", typeof(RectTransform), typeof(RectMask2D));
                    holder = (RectTransform)h.transform; holder.SetParent(panel, false);
                    h.AddComponent<LayoutElement>().ignoreLayout = true; // hors du rangement vertical de la fenetre (sinon il prend la place du bandeau et l'image disparait)
                    holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one; holder.offsetMin = new Vector2(2, 2); holder.offsetMax = new Vector2(-2, -2);
                    var ig = new GameObject("WMA_BgImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                    ig.transform.SetParent(holder, false);
                    img = ig.GetComponent<RawImage>(); img.raycastTarget = false;
                    fitter = ig.GetComponent<AspectRatioFitter>();
                    var irt = (RectTransform)ig.transform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.pivot = new Vector2(0.5f, 0.5f);
                }
                // juste au-dessus du fond de la fenetre, sous la liste
                // l'image va TOUT EN DESSOUS de tout le contenu du cadre (titre, croix, liste, boutons), et commence sous le bandeau
                if (holder.parent != panel) holder.SetParent(panel, false);
                { var hle = holder.GetComponent<LayoutElement>() ?? holder.gameObject.AddComponent<LayoutElement>(); hle.ignoreLayout = true; }
                float capH = 17f;
                try { var cap = panel.Find("Caption Panel") as RectTransform; if (cap != null && cap.rect.height > 1f) capH = cap.rect.height; } catch { }
                holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one; holder.offsetMin = new Vector2(2, 2); holder.offsetMax = new Vector2(-2, -(HistoryUI.TopGap + HistoryUI.CapH));
                holder.SetAsFirstSibling();
                var bdy = panel.Find("WMA_Body"); if (bdy != null) bdy.SetAsFirstSibling(); // fond noir sous l'image
                var tbar = panel.Find("WMA_TopBar"); if (tbar != null) tbar.SetAsFirstSibling(); // fond plein du bandeau
                var bdr = panel.Find("Border"); if (bdr != null) bdr.SetAsFirstSibling(); // liseré tout derriere

                if (choice != loadedKey) { Load(choice); loadedKey = choice; }
                bool on = img.texture != null;
                holder.gameObject.SetActive(on);
                if (!on) return;
                Color tint = cfgTint != null ? cfgTint.Value : Color.white;
                img.color = new Color(tint.r, tint.g, tint.b, cfgImgAlpha != null ? Mathf.Clamp01(cfgImgAlpha.Value) : 0.5f);
                float ratio = img.texture.height > 0 ? (float)img.texture.width / img.texture.height : 1.78f;
                if (img.uvRect.height > 0 && img.uvRect.width > 0) ratio *= img.uvRect.width / img.uvRect.height;
                string mode = cfgFit != null ? cfgFit.Value : "Fill";
                var bimg = (RectTransform)img.transform;
                if (mode == "Stretch")
                {
                    // etiree : remplit exactement la fenetre, quitte a deformer l'image
                    fitter.enabled = false;
                    bimg.anchorMin = Vector2.zero; bimg.anchorMax = Vector2.one; bimg.offsetMin = bimg.offsetMax = Vector2.zero;
                }
                else if (mode == "Center")
                {
                    // taille reelle, centree
                    fitter.enabled = false;
                    bimg.anchorMin = bimg.anchorMax = new Vector2(0.5f, 0.5f);
                    bimg.sizeDelta = new Vector2(img.texture.width * img.uvRect.width, img.texture.height * img.uvRect.height);
                    bimg.anchoredPosition = Vector2.zero;
                }
                else
                {
                    fitter.enabled = true;
                    bimg.anchorMin = bimg.anchorMax = new Vector2(0.5f, 0.5f);
                    fitter.aspectRatio = ratio;
                    fitter.aspectMode = mode == "Fit" ? AspectRatioFitter.AspectMode.FitInParent : AspectRatioFitter.AspectMode.EnvelopeParent;
                }
                ApplyUv();
            }
            catch (Exception e) { HistoryUI.Log.LogWarning("[fond] " + e.Message); }
        }

        static Rect baseUv = new Rect(0, 0, 1, 1);
        static void Load(string choice)
        {
            if (ownTex != null) { UnityEngine.Object.Destroy(ownTex); ownTex = null; }
            img.texture = null; baseUv = new Rect(0, 0, 1, 1); img.uvRect = baseUv;
            if (string.IsNullOrEmpty(choice) || choice == None) return;
            try
            {
                if (choice.StartsWith("Image: "))
                {
                    string path = Path.Combine(Dir, choice.Substring(7));
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(t, File.ReadAllBytes(path))) { UnityEngine.Object.Destroy(t); HistoryUI.Log.LogWarning("[fond] image illisible : " + path); return; }
                    int w = t.width, h = t.height;
                    if (Mathf.Max(w, h) > MaxSide)
                    {
                        // image trop grande : reduite pour ne pas charger la carte graphique
                        float k = (float)MaxSide / Mathf.Max(w, h);
                        var rtx = new RenderTexture(Mathf.RoundToInt(w * k), Mathf.RoundToInt(h * k), 0);
                        Graphics.Blit(t, rtx); UnityEngine.Object.Destroy(t);
                        ownTex = rtx; HistoryUI.Log.LogInfo("[fond] image reduite de " + w + "x" + h + " a " + rtx.width + "x" + rtx.height);
                    }
                    else ownTex = t;
                    img.texture = ownTex;
                    HistoryUI.Log.LogInfo("[fond] image perso : " + choice.Substring(7));
                }
                else if (choice.StartsWith("Jeu: "))
                {
                    string name = choice.Substring(5);
                    Sprite sp = FindSprite(name);
                    if (sp == null) { HistoryUI.Log.LogWarning("[fond] fond du jeu pas encore charge : " + name); loadedKey = ""; return; }
                    img.texture = sp.texture;
                    var tr = sp.textureRect;
                    baseUv = new Rect(tr.x / sp.texture.width, tr.y / sp.texture.height, tr.width / sp.texture.width, tr.height / sp.texture.height);
                    img.uvRect = baseUv;
                    HistoryUI.Log.LogInfo("[fond] fond du jeu : " + name);
                }
            }
            catch (Exception e) { HistoryUI.Log.LogWarning("[fond] chargement : " + e.Message); }
        }

        // zoom et decalage choisis, plus le mouvement lent s'il est actif
        static Rect UserUv()
        {
            float zoom = cfgZoom != null ? Mathf.Clamp(cfgZoom.Value, 1f, 4f) : 1f;
            float w = baseUv.width / zoom, h = baseUv.height / zoom;
            float ox = cfgOffX != null ? Mathf.Clamp(cfgOffX.Value, -1f, 1f) : 0f, oy = cfgOffY != null ? Mathf.Clamp(cfgOffY.Value, -1f, 1f) : 0f;
            float x = baseUv.x + (baseUv.width - w) * (0.5f + 0.5f * ox);
            float y = baseUv.y + (baseUv.height - h) * (0.5f + 0.5f * oy);
            return new Rect(x, y, w, h);
        }
        static void ApplyUv() { if (img != null && img.texture != null) img.uvRect = UserUv(); }

        // mouvement lent : leger zoom et glissement en boucle
        public static void Tick()
        {
            if (img == null || img.texture == null || holder == null || !holder.gameObject.activeInHierarchy) return;
            Rect u = UserUv();
            string m = cfgMove != null ? cfgMove.Value : "None";
            if (m == "None") { if (img.uvRect != u) img.uvRect = u; return; }
            float sp = cfgSpeed != null ? Mathf.Clamp(cfgSpeed.Value, 0.1f, 5f) : 1f;
            float t = Time.realtimeSinceStartup * sp;
            float z = 1f, px = 0.5f, py = 0.5f; // z = part de l'image visible, px/py = ou on regarde (0..1)
            switch (m)
            {
                case "Slow": z = 0.88f + 0.05f * Mathf.Sin(t * 0.15f); px = 0.5f + 0.5f * Mathf.Sin(t * 0.11f); py = 0.5f + 0.5f * Mathf.Cos(t * 0.08f); break;
                case "PanHorizontal": z = 0.75f; px = 0.5f + 0.5f * Mathf.Sin(t * 0.35f); break;
                case "PanVertical": z = 0.75f; py = 0.5f + 0.5f * Mathf.Sin(t * 0.35f); break;
                case "ZoomPulse": z = 0.8f + 0.15f * Mathf.Sin(t * 0.6f); break;
                case "Diagonal": z = 0.75f; px = 0.5f + 0.5f * Mathf.Sin(t * 0.3f); py = px; break;
                case "Circle": z = 0.75f; px = 0.5f + 0.5f * Mathf.Cos(t * 0.4f); py = 0.5f + 0.5f * Mathf.Sin(t * 0.4f); break;
                case "Breath": z = 0.88f + 0.08f * Mathf.Sin(t * 1.5f); break;
                case "Shake": z = 0.92f; px = 0.5f + 0.25f * (Mathf.PerlinNoise(t * 3f, 0f) - 0.5f) * 2f; py = 0.5f + 0.25f * (Mathf.PerlinNoise(0f, t * 3f) - 0.5f) * 2f; break;
            }
            img.uvRect = new Rect(u.x + (1f - z) * px * u.width, u.y + (1f - z) * py * u.height, u.width * z, u.height * z);
        }

        public static void Reload() { loadedKey = "\u0000"; }
    }
}
