using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Sprite[] stageSprites = new Sprite[11];
    public Fruit fruitPrefab;
    public Transform spawnPoint;
    public float spawnY = 4f;
    public float minX = -2.3f;
    public float maxX = 2.3f;
    public float gameOverY = 3.5f;
    public float floorY = -3.5f;
    public int maxSpawnStage = 4;
    public float spawnCooldown = 0.5f;
    public float aimLineWidth = 0.05f;
    public Color aimLineColor = new Color(1f, 1f, 1f, 0.35f);

    static readonly float[] stageDiameters = new float[]
    {
        0.325f, 0.455f, 0.585f, 0.715f, 0.8775f, 1.04f, 1.235f, 1.43f, 1.6575f, 1.885f, 2.145f
    };

    public float[] stageColliderRadius = new float[]
    {
        0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f
    };

    public Vector2[] stageColliderOffset = new Vector2[11];

    Fruit current;
    float lastDropTime = -999f;
    bool gameOver;
    bool victory;
    int nextStage;
    LineRenderer aimLine;
    public int score;

    public static Sprite StageSprite(int stage) => Instance.stageSprites[Mathf.Clamp(stage, 0, Instance.stageSprites.Length - 1)];
    public static float StageDiameter(int stage) => stageDiameters[Mathf.Clamp(stage, 0, stageDiameters.Length - 1)];
    public static float StageColliderRadius(int stage)
    {
        var arr = Instance.stageColliderRadius;
        if (arr == null || arr.Length == 0) return 0.5f;
        return arr[Mathf.Clamp(stage, 0, arr.Length - 1)];
    }
    public static Vector2 StageColliderOffset(int stage)
    {
        var arr = Instance.stageColliderOffset;
        if (arr == null || arr.Length == 0) return Vector2.zero;
        return arr[Mathf.Clamp(stage, 0, arr.Length - 1)];
    }
    public static int MaxStage => stageDiameters.Length - 1;

    void Awake()
    {
        Instance = this;
        SetupAimLine();
    }

    void Start()
    {
        nextStage = PickRandomStage();
        SpawnNext();
    }

    void SetupAimLine()
    {
        var go = new GameObject("AimGuide");
        go.transform.SetParent(transform, false);
        aimLine = go.AddComponent<LineRenderer>();
        aimLine.positionCount = 2;
        aimLine.startWidth = aimLineWidth;
        aimLine.endWidth = aimLineWidth;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = aimLineColor;
        aimLine.endColor = new Color(aimLineColor.r, aimLineColor.g, aimLineColor.b, aimLineColor.a * 0.25f);
        aimLine.sortingOrder = 5;
        aimLine.useWorldSpace = true;
        aimLine.enabled = false;
    }

    int PickRandomStage() => Random.Range(0, maxSpawnStage);

    void Update()
    {
        if (gameOver || victory)
        {
            if (aimLine != null) aimLine.enabled = false;
            return;
        }

        if (current != null)
        {
            Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float x = Mathf.Clamp(mouse.x, minX, maxX);
            current.transform.position = new Vector3(x, spawnY, 0);

            float effR = StageDiameter(current.stage) * 0.5f;
            float topY = spawnY - effR;

            CircleCollider2D currentCol = current.GetComponent<CircleCollider2D>();
            bool wasEnabled = currentCol != null && currentCol.enabled;
            if (currentCol != null) currentCol.enabled = false;
            RaycastHit2D hit = Physics2D.CircleCast(
                new Vector2(x, spawnY), effR, Vector2.down, Mathf.Infinity);
            if (currentCol != null) currentCol.enabled = wasEnabled;

            float endY = (hit.collider != null) ? hit.point.y : floorY;
            if (endY > topY) endY = topY;
            if (endY < floorY) endY = floorY;

            aimLine.enabled = true;
            aimLine.SetPosition(0, new Vector3(x, topY, 0));
            aimLine.SetPosition(1, new Vector3(x, endY, 0));

            if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime > spawnCooldown)
            {
                current.Drop();
                lastDropTime = Time.time;
                current = null;
                aimLine.enabled = false;
                StartCoroutine(SpawnAfter(spawnCooldown));
            }
        }
        else
        {
            aimLine.enabled = false;
        }

        CheckGameOver();
    }

    IEnumerator SpawnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (!gameOver && !victory) SpawnNext();
    }

    void SpawnNext()
    {
        int stage = nextStage;
        nextStage = PickRandomStage();
        Fruit f = Instantiate(fruitPrefab);
        f.Configure(stage);
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
        f.Configure(newStage);
        f.transform.position = pos;
        f.Drop();

        if (newStage == MaxStage)
        {
            victory = true;
        }
    }

    void CheckGameOver()
    {
        if (victory) return;

        var list = Fruit.ActiveFruits;
        float now = Time.time;

        for (int i = 0; i < list.Count; i++)
        {
            Fruit f = list[i];
            if (f == current || !f.dropped) continue;
            if (now - f.dropTime < 2f) continue;
            if (f.transform.position.y <= gameOverY) continue;

            Rigidbody2D rb = f.GetComponent<Rigidbody2D>();
            if (rb.linearVelocity.sqrMagnitude < 0.01f)
            {
                gameOver = true;
                Debug.Log("Game Over! Score: " + score);
                return;
            }
        }
    }

    void OnGUI()
    {
        GUIStyle scoreStyle = new GUIStyle
        {
            fontSize = 36,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(20, 20, 400, 60), "Score : " + score, scoreStyle);

        DrawNextPreview();

        if (victory)
        {
            DrawEndScreen("YOU WIN!", new Color(1f, 0.85f, 0.2f));
        }
        else if (gameOver)
        {
            DrawEndScreen("GAME OVER", new Color(1f, 0.3f, 0.3f));
        }
    }

    void DrawNextPreview()
    {
        if (stageSprites == null || nextStage < 0 || nextStage >= stageSprites.Length) return;
        Sprite s = stageSprites[nextStage];
        if (s == null || s.texture == null) return;

        float panelW = 120, panelH = 140;
        float panelX = Screen.width - panelW - 20;
        float panelY = 20;

        GUIStyle labelStyle = new GUIStyle
        {
            fontSize = 20,
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none);
        GUI.Label(new Rect(panelX, panelY + 4, panelW, 24), "Next", labelStyle);

        Rect tr = s.textureRect;
        Rect uv = new Rect(
            tr.x / s.texture.width,
            tr.y / s.texture.height,
            tr.width / s.texture.width,
            tr.height / s.texture.height);

        Rect imgRect = new Rect(panelX + 12, panelY + 30, panelW - 24, panelH - 38);
        GUI.DrawTextureWithTexCoords(imgRect, s.texture, uv);
    }

    void DrawEndScreen(string title, Color titleColor)
    {
        float w = 420, h = 260;
        Rect r = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
        GUI.Box(r, GUIContent.none);

        GUI.Label(new Rect(r.x, r.y + 30, r.width, 70), title, new GUIStyle
        {
            fontSize = 56,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = titleColor }
        });

        GUI.Label(new Rect(r.x, r.y + 115, r.width, 40), "Score : " + score, new GUIStyle
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        });

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
        if (GUI.Button(new Rect(r.x + w / 2f - 90, r.y + 180, 180, 56), "Restart", btnStyle))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
