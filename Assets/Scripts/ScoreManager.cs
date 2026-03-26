using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private EndlessTrackLooper looper;
    [SerializeField] private bool autoFindLooper = true;

    [Header("Scoring")]
    [SerializeField] private float basePointsPerSecond = 6f;
    [SerializeField] private float distancePerMultiplier = 90f;
    [SerializeField] private float maxMultiplier = 10f;
    [SerializeField] private float speedToPointsScale = 0.02f;

    [Header("HUD")]
    [SerializeField] private bool showOnGUI = true;
    [SerializeField] private bool showMultiplier = false;
    [SerializeField] private Vector2 hudOffset = new Vector2(12f, 10f);
    [SerializeField] private float scoreFontScale = 4f;
    [SerializeField] private bool useSafeArea = true;
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private float coinIconScale = 1f;
    [SerializeField] private float coinIconPadding = 8f;

    private float score;
    private float distance;
    private float currentMultiplier = 1f;
    private GUIStyle centeredStyle;
    private int centeredBaseFontSize;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (autoFindLooper && looper == null)
        {
            looper = FindObjectOfType<EndlessTrackLooper>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (looper == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        float speed = looper.CurrentSpeed;
        distance += Mathf.Max(0f, speed) * dt;

        currentMultiplier = 1f + (distance / Mathf.Max(1f, distancePerMultiplier));
        currentMultiplier = Mathf.Min(currentMultiplier, maxMultiplier);

        float speedBonus = 1f + Mathf.Max(0f, speed) * speedToPointsScale;
        score += basePointsPerSecond * currentMultiplier * speedBonus * dt;
    }

    public void AddScore(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        score += amount;
    }

    private void OnGUI()
    {
        if (!showOnGUI)
        {
            return;
        }

        if (centeredStyle == null)
        {
            centeredStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter
            };
            centeredBaseFontSize = centeredStyle.fontSize;
            if (centeredBaseFontSize <= 0)
            {
                centeredBaseFontSize = GUI.skin.label.fontSize;
            }
            if (centeredBaseFontSize <= 0)
            {
                centeredBaseFontSize = 14;
            }
        }

        Color previous = GUI.color;

        int displayScore = Mathf.FloorToInt(score);
        string scoreText = displayScore.ToString();
        string multText = $"x{currentMultiplier:0.0}";

        centeredStyle.fontSize = Mathf.RoundToInt(centeredBaseFontSize * Mathf.Max(1f, scoreFontScale));
        float lineHeight = centeredStyle.fontSize + 6f;
        Rect safe = GetSafeAreaRect();
        float y = safe.y + hudOffset.y;
        GUIContent scoreContent = new GUIContent(scoreText);
        float textWidth = centeredStyle.CalcSize(scoreContent).x;
        float iconSize = lineHeight * Mathf.Max(0.5f, coinIconScale);
        float iconPadding = coinIcon != null ? coinIconPadding : 0f;
        float totalWidth = textWidth + (coinIcon != null ? iconSize + iconPadding : 0f);
        float x = safe.x + (safe.width - totalWidth) * 0.5f;

        if (coinIcon != null)
        {
            GUI.color = Color.white;
            Rect iconRect = new Rect(x, y + (lineHeight - iconSize) * 0.5f, iconSize, iconSize);
            DrawSprite(iconRect, coinIcon);
            x += iconSize + iconPadding;
        }

        GUI.color = Color.black;
        Rect r1 = new Rect(x, y, textWidth, lineHeight);
        GUI.Label(r1, scoreText, centeredStyle);
        if (showMultiplier)
        {
            Rect r2 = new Rect(safe.x, y + lineHeight, safe.width, lineHeight);
            GUI.Label(r2, multText, centeredStyle);
        }

        GUI.color = previous;
    }

    private Rect GetSafeAreaRect()
    {
        if (!useSafeArea)
        {
            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f)
        {
            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        return safe;
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Texture2D texture = sprite.texture;
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(
            tr.x / texture.width,
            tr.y / texture.height,
            tr.width / texture.width,
            tr.height / texture.height);

        GUI.DrawTextureWithTexCoords(rect, texture, uv);
    }
}
