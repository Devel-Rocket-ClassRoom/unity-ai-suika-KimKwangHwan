using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Fruit : MonoBehaviour
{
    public int stage;
    public bool dropped;
    public bool merged;
    public float dropTime;

    SpriteRenderer sr;
    Rigidbody2D rb;
    CircleCollider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
    }

    public void Configure(int stage)
    {
        this.stage = stage;
        sr.sprite = GameManager.StageSprite(stage);
        sr.color = Color.white;
        float diameter = GameManager.StageDiameter(stage);
        float r = GameManager.StageColliderRadius(stage);
        float scale = r > 0.001f ? diameter / (2f * r) : diameter;
        transform.localScale = Vector3.one * scale;
        if (col != null)
        {
            col.radius = r;
            col.offset = GameManager.StageColliderOffset(stage);
        }
    }

    public void Drop()
    {
        dropped = true;
        dropTime = Time.time;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void SetKinematic()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    void OnCollisionEnter2D(Collision2D other) => TryMerge(other);
    void OnCollisionStay2D(Collision2D other) => TryMerge(other);

    void TryMerge(Collision2D other)
    {
        if (!dropped || merged) return;
        Fruit otherFruit = other.gameObject.GetComponent<Fruit>();
        if (otherFruit == null || otherFruit.merged || !otherFruit.dropped) return;
        if (otherFruit.stage != stage) return;
        if (GetInstanceID() >= otherFruit.GetInstanceID()) return;

        merged = true;
        otherFruit.merged = true;
        Vector2 mid = (transform.position + other.transform.position) * 0.5f;
        GameManager.Instance.Merge(this, otherFruit, mid);
    }
}
