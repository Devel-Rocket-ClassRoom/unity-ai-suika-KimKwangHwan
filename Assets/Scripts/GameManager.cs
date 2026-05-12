using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Sprite circleSprite;
    public Fruit fruitPrefab;
    public Transform spawnPoint;
    public float spawnY = 4f;
    public float minX = -2.3f;
    public float maxX = 2.3f;
    public float gameOverY = 3.5f;
    public int maxSpawnStage = 4;
    public float spawnCooldown = 0.5f;

    static readonly Color[] stageColors = new Color[]
    {
        new Color(1.00f, 0.40f, 0.45f),
        new Color(1.00f, 0.65f, 0.30f),
        new Color(1.00f, 0.90f, 0.30f),
        new Color(0.65f, 0.95f, 0.40f),
        new Color(0.30f, 0.85f, 0.55f),
        new Color(0.30f, 0.75f, 1.00f),
        new Color(0.55f, 0.50f, 1.00f),
        new Color(0.85f, 0.45f, 1.00f),
        new Color(1.00f, 0.45f, 0.85f),
        new Color(0.95f, 0.30f, 0.30f),
        new Color(0.30f, 0.80f, 0.30f),
    };

    static readonly float[] stageDiameters = new float[]
    {
        0.5f, 0.7f, 0.9f, 1.1f, 1.35f, 1.6f, 1.9f, 2.2f, 2.55f, 2.9f, 3.3f
    };

    Fruit current;
    float lastDropTime = -999f;
    bool gameOver;
    public int score;

    public static Color StageColor(int stage) => stageColors[Mathf.Clamp(stage, 0, stageColors.Length - 1)];
    public static float StageDiameter(int stage) => stageDiameters[Mathf.Clamp(stage, 0, stageDiameters.Length - 1)];
    public static int MaxStage => stageDiameters.Length - 1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnNext();
    }

    void Update()
    {
        if (gameOver) return;

        if (current != null)
        {
            Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float x = Mathf.Clamp(mouse.x, minX, maxX);
            current.transform.position = new Vector3(x, spawnY, 0);

            if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime > spawnCooldown)
            {
                current.Drop();
                lastDropTime = Time.time;
                current = null;
                StartCoroutine(SpawnAfter(spawnCooldown));
            }
        }

        CheckGameOver();
    }

    IEnumerator SpawnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (!gameOver) SpawnNext();
    }

    void SpawnNext()
    {
        int stage = Random.Range(0, maxSpawnStage);
        Fruit f = Instantiate(fruitPrefab);
        f.Configure(stage, circleSprite);
        f.SetKinematic();
        f.transform.position = new Vector3(0, spawnY, 0);
        current = f;
    }

    public void Merge(Fruit a, Fruit b, Vector2 pos)
    {
        int newStage = a.stage + 1;
        score += (a.stage + 1) * 10;
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (newStage > MaxStage) return;

        Fruit f = Instantiate(fruitPrefab);
        f.Configure(newStage, circleSprite);
        f.transform.position = pos;
        f.Drop();
    }

    void CheckGameOver()
    {
        Fruit[] all = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        foreach (Fruit f in all)
        {
            if (f == current || !f.dropped) continue;
            if (Time.time - f.dropTime < 2f) continue;
            if (f.transform.position.y > gameOverY)
            {
                Rigidbody2D rb = f.GetComponent<Rigidbody2D>();
                if (rb.linearVelocity.magnitude < 0.1f)
                {
                    gameOver = true;
                    Debug.Log("Game Over! Score: " + score);
                    return;
                }
            }
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 300, 30), "Score: " + score, new GUIStyle { fontSize = 28, normal = { textColor = Color.white } });
        if (gameOver)
        {
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 20, 300, 50), "GAME OVER", new GUIStyle { fontSize = 40, normal = { textColor = Color.red } });
        }
    }
}
