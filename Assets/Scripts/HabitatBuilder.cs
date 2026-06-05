using System.Collections.Generic;
using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// The habitat-exploration module. Builds the Belum-Temenggor rainforest
    /// level entirely from code: sky, ground, layered trees for depth, the
    /// wildlife to discover, pollution to clean, and the quiz totem at the end.
    /// </summary>
    public class HabitatBuilder : MonoBehaviour
    {
        public const string HabitatName = "Belum-Temenggor Forest";

        public float worldMinX = -3f;
        public float worldMaxX = 46f;
        public float groundY = -2.6f;   // top surface of ground

        GameObject root;
        readonly List<WildlifeEntity> wildlife = new List<WildlifeEntity>();
        bool built;

        PlayerController player;
        CameraFollow cam;
        Camera mainCam;

        public void Configure(PlayerController p, CameraFollow c, Camera cameraObj)
        {
            player = p; cam = c; mainCam = cameraObj;
        }

        public void SetWorldVisible(bool v)
        {
            if (root != null) root.SetActive(v);
            if (player != null) player.gameObject.SetActive(v);
        }

        public void Build()
        {
            if (built && root != null) Destroy(root);
            InteractableRegistry.Clear();
            wildlife.Clear();

            root = new GameObject("Habitat");
            built = true;

            if (mainCam != null) mainCam.backgroundColor = UIFactory.Sky;

            BuildGround();
            BuildBackgroundTrees();
            BuildWildlife();
            BuildPollution();
            BuildQuizTotem();
            BuildForegroundTrees();

            // Bounds
            if (player != null) player.SetBounds(worldMinX + 1f, worldMaxX - 1f, groundY + 0.2f, 3.6f);
            if (cam != null) cam.SetBounds(worldMinX + 7f, worldMaxX - 7f);
        }

        // ---------- pieces ----------

        void BuildGround()
        {
            // Main soil band
            var soil = SolidQuad("Ground", UIFactory.Hex("5D7C3F"), 8);
            float width = worldMaxX - worldMinX + 8f;
            soil.transform.localScale = new Vector3(width, 4f, 1f);
            soil.transform.position = new Vector3((worldMinX + worldMaxX) / 2f, groundY - 2f, 0f);

            // Grass line on top
            var grass = SolidQuad("Grass", UIFactory.Hex("7CB342"), 9);
            grass.transform.localScale = new Vector3(width, 0.35f, 1f);
            grass.transform.position = new Vector3((worldMinX + worldMaxX) / 2f, groundY, 0f);

            // Distant hill band for depth
            var hill = SolidQuad("Hills", UIFactory.Hex("A5D6A7"), 1);
            hill.transform.localScale = new Vector3(width, 3f, 1f);
            hill.transform.position = new Vector3((worldMinX + worldMaxX) / 2f, groundY + 1.2f, 0f);
            var hsr = hill.GetComponent<SpriteRenderer>();
            hsr.color = new Color(hsr.color.r, hsr.color.g, hsr.color.b, 0.55f);
        }

        void BuildBackgroundTrees()
        {
            float[] xs = { -1f, 3f, 6.5f, 11f, 15f, 19f, 24f, 28f, 33f, 38f, 42f };
            int i = 0;
            foreach (float x in xs)
            {
                float scale = 1.0f + ((i * 37) % 5) * 0.12f;
                var t = TreeAt(x + ((i % 2) * 0.6f), scale, 3);
                var sr = t.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.82f, 0.88f, 0.82f, 1f); // hazy, further back
                i++;
            }
        }

        void BuildForegroundTrees()
        {
            float[] xs = { 1.5f, 9f, 17f, 26f, 35f };
            int i = 0;
            foreach (float x in xs)
            {
                var t = TreeAt(x, 1.5f + (i % 2) * 0.2f, 15);
                var sr = t.GetComponent<SpriteRenderer>();
                sr.color = new Color(1f, 1f, 1f, 0.9f);
                i++;
            }
        }

        GameObject TreeAt(float x, float scale, int order)
        {
            var go = new GameObject("Tree");
            go.transform.SetParent(root.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/tree");
            sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * scale;
            // place so trunk base sits roughly on the ground
            go.transform.position = new Vector3(x, groundY + scale * 0.75f, 0f);
            return go;
        }

        void BuildWildlife()
        {
            // species id -> (x position, scale)
            var placements = new (string id, float x, float scale)[]
            {
                ("malayan_tapir",   7f,  0.9f),
                ("asian_elephant", 16f,  1.7f),
                ("malayan_tiger",  25f,  0.9f),
                ("sumatran_rhino", 35f,  0.6f),
            };

            foreach (var p in placements)
            {
                var data = WildlifeDatabase.GetById(p.id);
                if (data == null) continue;
                var go = new GameObject("Wildlife_" + data.id);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(p.x, groundY + 0.6f, 0f);
                var e = go.AddComponent<WildlifeEntity>();
                e.Init(data, p.scale);
                wildlife.Add(e);
            }
        }

        void BuildPollution()
        {
            var items = new (string sprite, string label, float x, float scale)[]
            {
                ("plastic",        "plastic bag",    4f,   0.55f),
                ("plastic_bottle", "plastic bottle", 12f,  0.5f),
                ("glass_bottle",   "glass bottle",   20f,  0.5f),
                ("plastic",        "plastic bag",    29f,  0.55f),
                ("plastic_bottle", "plastic bottle", 38f,  0.5f),
                ("glass_bottle",   "glass bottle",   31f,  0.5f),
            };

            foreach (var it in items)
            {
                var go = new GameObject("Pollution");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(it.x, groundY + 0.35f, 0f);
                var p = go.AddComponent<PollutionItem>();
                p.Init(it.sprite, it.label, it.scale);
            }
        }

        void BuildQuizTotem()
        {
            var go = new GameObject("QuizTotem");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(worldMaxX - 2f, groundY, 0f);
            var t = go.AddComponent<QuizTotem>();
            t.Init(1.2f);
        }

        public void RefreshDiscoveredState()
        {
            var gm = GameManager.Instance;
            foreach (var w in wildlife)
                if (w != null && w.data != null)
                    w.SetDiscovered(gm.Discovered.Contains(w.data.id));
        }

        // ---------- helpers ----------

        static Sprite _solid;
        static Sprite SolidSprite()
        {
            if (_solid != null) return _solid;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            _solid = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
            return _solid;
        }

        GameObject SolidQuad(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SolidSprite();
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }
    }
}
