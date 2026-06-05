using UnityEngine;

namespace WildlifeAdventure
{
    /// <summary>
    /// Controls Wira the Hornbill: free 2D flight across the habitat, a gentle
    /// idle bob, sprite flipping by direction, and detection of the nearest
    /// interactable so the player can scan wildlife or clean pollution.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public bool AcceptInput = false;

        [Header("Movement")]
        public float speed = 6.5f;
        public float interactRange = 1.6f;

        // World bounds (set by HabitatBuilder)
        public float minX = -2f, maxX = 40f, minY = -3.0f, maxY = 3.6f;
        Vector3 startPos = new Vector3(0f, 0f, 0f);

        SpriteRenderer sr;
        float bobTimer;
        bool facingRight = true;

        IInteractable current;          // nearest available target
        public IInteractable Current => current;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void ResetToStart()
        {
            transform.position = startPos;
            bobTimer = 0f;
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX; this.maxX = maxX; this.minY = minY; this.maxY = maxY;
        }

        void Update()
        {
            if (!AcceptInput)
            {
                current = null;
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            var dir = new Vector3(h, v, 0f);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            Vector3 pos = transform.position + dir * speed * Time.deltaTime;

            // Gentle hover bob when not pressing up/down.
            bobTimer += Time.deltaTime * 3f;
            if (Mathf.Abs(v) < 0.01f)
                pos.y += Mathf.Sin(bobTimer) * 0.0035f;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;

            // Face travel direction (hornbill sprite faces right by default).
            if (h > 0.01f && !facingRight) Flip(true);
            else if (h < -0.01f && facingRight) Flip(false);

            FindNearest();

            if (current != null &&
                (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                current.Interact();
            }
        }

        void Flip(bool right)
        {
            facingRight = right;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
            transform.localScale = s;
        }

        void FindNearest()
        {
            current = null;
            float best = interactRange * interactRange;
            foreach (var it in InteractableRegistry.All)
            {
                if (it == null || !it.Available) continue;
                float d = (it.WorldPosition - transform.position).sqrMagnitude;
                if (d <= best)
                {
                    best = d;
                    current = it;
                }
            }
        }
    }
}
