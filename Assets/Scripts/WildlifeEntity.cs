using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// A discoverable animal placed in the habitat. Scanning it (pressing E
    /// nearby) opens its Fact Card. Once added to the journal it stays visible
    /// but shows a small "discovered" tick and can't be scanned again.
    /// </summary>
    public class WildlifeEntity : MonoBehaviour, IInteractable
    {
        public WildlifeData data;
        SpriteRenderer sr;
        SpriteRenderer tick;
        float bob;
        float baseY;
        bool discovered;

        public Vector3 WorldPosition => transform.position;
        public string Prompt => discovered ? "" : "Press E to scan " + data.commonName;
        public bool Available => !discovered;

        public void Init(WildlifeData d, float scale)
        {
            data = d;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/" + d.spriteName);
            sr.sortingOrder = 10;
            transform.localScale = Vector3.one * scale;
            baseY = transform.position.y;

            // Small green tick shown after discovery (built from a tiny texture).
            var tg = new GameObject("Tick");
            tg.transform.SetParent(transform, false);
            tg.transform.localPosition = new Vector3(0.0f, 1.1f, 0f);
            tick = tg.AddComponent<SpriteRenderer>();
            tick.sprite = MakeDot(UIFactory.GreenLight);
            tick.sortingOrder = 12;
            tg.transform.localScale = Vector3.one * 0.35f;
            tick.enabled = false;
        }

        void OnEnable()  { InteractableRegistry.Register(this); }
        void OnDisable() { InteractableRegistry.Unregister(this); }

        void Update()
        {
            // Gentle idle motion.
            bob += Time.deltaTime * 1.8f;
            var p = transform.position;
            p.y = baseY + Mathf.Sin(bob) * 0.06f;
            transform.position = p;
        }

        public void Interact()
        {
            if (discovered) return;
            GameManager.Instance.ShowFactCard(data);
        }

        /// <summary>Called by HabitatBuilder to sync visuals with saved progress.</summary>
        public void SetDiscovered(bool value)
        {
            discovered = value;
            if (tick != null) tick.enabled = value;
            if (sr != null) sr.color = value ? new Color(1f, 1f, 1f, 0.92f) : Color.white;
        }

        static Sprite MakeDot(Color c)
        {
            int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 mid = new Vector2(s / 2f, s / 2f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), mid);
                    tex.SetPixel(x, y, d < s / 2f - 1 ? c : new Color(0, 0, 0, 0));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16);
        }
    }
}
