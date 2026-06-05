using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// An environmental threat (plastic bag, plastic bottle, glass bottle) that
    /// the player cleans up for Conservation Points, reinforcing the
    /// conservation message of the game.
    /// </summary>
    public class PollutionItem : MonoBehaviour, IInteractable
    {
        public int points = 50;
        public string label = "litter";
        SpriteRenderer sr;
        bool cleaned;
        float bob, baseY, baseRot;

        public Vector3 WorldPosition => transform.position;
        public string Prompt => "Press E to clean up " + label;
        public bool Available => !cleaned;

        public void Init(string spriteName, string label, float scale)
        {
            this.label = label;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/" + spriteName);
            sr.sortingOrder = 9;
            transform.localScale = Vector3.one * scale;
            baseY = transform.position.y;
            baseRot = Random.Range(-12f, 12f);
            transform.rotation = Quaternion.Euler(0, 0, baseRot);
        }

        void OnEnable()  { InteractableRegistry.Register(this); }
        void OnDisable() { InteractableRegistry.Unregister(this); }

        void Update()
        {
            bob += Time.deltaTime * 1.4f;
            var p = transform.position;
            p.y = baseY + Mathf.Sin(bob + baseRot) * 0.04f;
            transform.position = p;
        }

        public void Interact()
        {
            if (cleaned) return;
            cleaned = true;
            InteractableRegistry.Unregister(this);
            GameManager.Instance.CleanPollution(points);
            StartCoroutine(PopAndDestroy());
        }

        System.Collections.IEnumerator PopAndDestroy()
        {
            float t = 0f;
            Vector3 s0 = transform.localScale;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float k = 1f - (t / 0.18f);
                transform.localScale = s0 * k;
                if (sr != null) sr.color = new Color(1f, 1f, 1f, k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
