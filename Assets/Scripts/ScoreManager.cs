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

    private float score;
    private float distance;
    private float currentMultiplier = 1f;
    private GUIStyle centeredStyle;

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
        }

        Color previous = GUI.color;
        GUI.color = Color.black;

        int displayScore = Mathf.FloorToInt(score);
        string scoreText = $"Score: {displayScore}";
        string multText = $"x{currentMultiplier:0.0}";

        float width = 240f;
        float x = (Screen.width - width) * 0.5f;
        Rect r1 = new Rect(x, hudOffset.y, width, 24f);
        GUI.Label(r1, scoreText, centeredStyle);
        if (showMultiplier)
        {
            Rect r2 = new Rect(x, hudOffset.y + 20f, width, 24f);
            GUI.Label(r2, multText, centeredStyle);
        }

        GUI.color = previous;
    }
}
