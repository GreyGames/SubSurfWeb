using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EndlessTrackLooper looper;
    [SerializeField] private bool autoFindLooper = true;

    [Header("Scoring")]
    [SerializeField] private float basePointsPerSecond = 8f;
    [SerializeField] private float distancePerMultiplier = 60f;
    [SerializeField] private float maxMultiplier = 12f;
    [SerializeField] private float speedToPointsScale = 0.03f;

    [Header("HUD")]
    [SerializeField] private bool showOnGUI = true;
    [SerializeField] private bool showMultiplier = false;
    [SerializeField] private Vector2 hudOffset = new Vector2(12f, 10f);
    [SerializeField] private float scoreFontScale = 2f;
    [SerializeField] private bool useSafeArea = true;

    private float score;
    private float distance;
    private float currentMultiplier = 1f;
    private GUIStyle centeredStyle;
    private int centeredBaseFontSize;

    private void Awake()
    {
        if (autoFindLooper && looper == null)
        {
            looper = FindObjectOfType<EndlessTrackLooper>();
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
        GUI.color = Color.black;

        int displayScore = Mathf.FloorToInt(score);
        string scoreText = $"Score: {displayScore}";
        string multText = $"x{currentMultiplier:0.0}";

        centeredStyle.fontSize = Mathf.RoundToInt(centeredBaseFontSize * Mathf.Max(1f, scoreFontScale));
        float lineHeight = centeredStyle.fontSize + 6f;
        Rect safe = GetSafeAreaRect();
        float width = safe.width;
        float x = safe.x;
        float y = safe.y + hudOffset.y;
        Rect r1 = new Rect(x, y, width, lineHeight);
        GUI.Label(r1, scoreText, centeredStyle);
        if (showMultiplier)
        {
            Rect r2 = new Rect(x, y + lineHeight, width, lineHeight);
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
}
