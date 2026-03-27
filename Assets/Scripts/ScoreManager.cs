using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    private const string SessionLegacyScoreKey = "subsurf.session.score";
    private const string SessionHighScoreKey = "subsurf.session.highscore";
    private const string SessionCoinsKey = "subsurf.session.coins";

    [Header("References")]
    [SerializeField] private EndlessTrackLooper looper;
    [SerializeField] private bool autoFindLooper = true;
    [SerializeField] private RunnerLaneController runner;
    [SerializeField] private bool autoFindRunner = true;

    [Header("Scoring")]
    [SerializeField] private float basePointsPerSecond = 6f;
    [SerializeField] private float distancePerMultiplier = 90f;
    [SerializeField] private float maxMultiplier = 10f;
    [SerializeField] private float speedToPointsScale = 0.02f;
    [SerializeField] private bool scoreOnlyWhileWorldMoving = true;
    [SerializeField] private float minSpeedToScore = 0.01f;

    [Header("HUD")]
    [SerializeField] private bool showOnGUI = true;
    [SerializeField] private bool showMultiplier = false;
    [SerializeField] private Vector2 hudOffset = new Vector2(12f, 10f);
    [SerializeField] private float scoreFontScale = 4f;
    [SerializeField] private float highScoreFontScale = 1.8f;
    [SerializeField] private float coinsFontScale = 2f;
    [SerializeField] private bool useSafeArea = true;
    [SerializeField] private bool showCoinsOnHUD = true;
    [SerializeField] private float hudLineSpacing = 2f;
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private float coinIconScale = 1f;
    [SerializeField] private float coinIconPadding = 8f;

    private float score;
    private float distance;
    private float currentMultiplier = 1f;
    private int highScore;
    private int coins;
    private int lastSavedHighScore = int.MinValue;
    private int lastSavedCoins = int.MinValue;
    private GUIStyle centeredStyle;
    private GUIStyle coinStyle;
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

        if (autoFindRunner && runner == null)
        {
            runner = FindObjectOfType<RunnerLaneController>();
        }

        LoadSessionProgress();
        ResetRunScoreState();
    }

    private void OnDestroy()
    {
        SaveSessionProgressIfDirty();

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
        if (scoreOnlyWhileWorldMoving && (looper.IsPaused || speed <= minSpeedToScore))
        {
            return;
        }

        distance += Mathf.Max(0f, speed) * dt;

        currentMultiplier = 1f + (distance / Mathf.Max(1f, distancePerMultiplier));
        currentMultiplier = Mathf.Min(currentMultiplier, maxMultiplier);

        float speedBonus = 1f + Mathf.Max(0f, speed) * speedToPointsScale;
        score += basePointsPerSecond * currentMultiplier * speedBonus * dt;
        UpdateHighScoreFromCurrentScore();
        SaveSessionProgressIfDirty();
    }

    public int CurrentScore => Mathf.FloorToInt(score);
    public int CurrentCoins => Mathf.Max(0, coins);

    public void AddScore(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        score += amount;
        UpdateHighScoreFromCurrentScore();
        SaveSessionProgressIfDirty();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        coins += amount;
        SaveSessionProgressIfDirty();
    }

    private void LoadSessionProgress()
    {
        // Cleanup old key from previous behavior where current score persisted.
        WebSessionStorage.Remove(SessionLegacyScoreKey);

        if (WebSessionStorage.TryGetInt(SessionHighScoreKey, out int savedHighScore))
        {
            highScore = Mathf.Max(0, savedHighScore);
        }

        if (WebSessionStorage.TryGetInt(SessionCoinsKey, out int savedCoins))
        {
            coins = Mathf.Max(0, savedCoins);
        }

        lastSavedHighScore = Mathf.Max(0, highScore);
        lastSavedCoins = Mathf.Max(0, coins);
    }

    private void SaveSessionProgressIfDirty()
    {
        int highScoreInt = Mathf.Max(0, highScore);
        int coinsInt = Mathf.Max(0, coins);
        if (highScoreInt == lastSavedHighScore && coinsInt == lastSavedCoins)
        {
            return;
        }

        lastSavedHighScore = highScoreInt;
        lastSavedCoins = coinsInt;
        WebSessionStorage.SetInt(SessionHighScoreKey, highScoreInt);
        WebSessionStorage.SetInt(SessionCoinsKey, coinsInt);
    }

    private void ResetRunScoreState()
    {
        score = 0f;
        distance = 0f;
        currentMultiplier = 1f;
    }

    private void UpdateHighScoreFromCurrentScore()
    {
        int scoreInt = Mathf.FloorToInt(score);
        if (scoreInt > highScore)
        {
            highScore = scoreInt;
        }
    }

    private void OnGUI()
    {
        if (!showOnGUI)
        {
            return;
        }

        if (runner != null && runner.IsGameOverActive)
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
            coinStyle = new GUIStyle(centeredStyle)
            {
                alignment = TextAnchor.UpperLeft
            };
        }

        Color previous = GUI.color;

        int displayScore = Mathf.FloorToInt(score);
        string scoreText = displayScore.ToString();
        string highScoreText = $"Best {Mathf.Max(0, highScore)}";
        string multText = $"x{currentMultiplier:0.0}";

        int scoreHudFontSize = Mathf.RoundToInt(centeredBaseFontSize * Mathf.Max(1f, scoreFontScale));
        int highScoreHudFontSize = Mathf.RoundToInt(centeredBaseFontSize * Mathf.Max(1f, highScoreFontScale));
        int coinHudFontSize = Mathf.RoundToInt(centeredBaseFontSize * Mathf.Max(1f, coinsFontScale));
        Rect safe = GetSafeAreaRect();
        float y = safe.y + hudOffset.y;

        if (showCoinsOnHUD)
        {
            centeredStyle.fontSize = coinHudFontSize;
            coinStyle.fontSize = coinHudFontSize;
            float coinLineHeight = centeredStyle.fontSize + 6f;
            string coinText = Mathf.Max(0, coins).ToString();
            GUIContent coinContent = new GUIContent(coinText);
            float coinTextWidth = Mathf.Ceil(coinStyle.CalcSize(coinContent).x + 8f);
            float iconSize = Mathf.Ceil(coinLineHeight * Mathf.Max(0.5f, coinIconScale));
            float iconPadding = coinIcon != null ? coinIconPadding : 0f;
            float totalCoinWidth = coinTextWidth + (coinIcon != null ? iconSize + iconPadding : 0f);
            float rightEdge = safe.x + safe.width - Mathf.Max(0f, hudOffset.x);
            float x = Mathf.Floor(rightEdge - totalCoinWidth);

            if (coinIcon != null)
            {
                GUI.color = Color.white;
                Rect iconRect = new Rect(x, y + (coinLineHeight - iconSize) * 0.5f, iconSize, iconSize);
                DrawSprite(iconRect, coinIcon);
                x += iconSize + iconPadding;
            }

            GUI.color = Color.black;
            Rect coinRect = new Rect(x, y, coinTextWidth, coinLineHeight);
            GUI.Label(coinRect, coinText, coinStyle);
        }

        centeredStyle.fontSize = scoreHudFontSize;
        float scoreLineHeight = centeredStyle.fontSize + 6f;
        GUI.color = Color.black;
        Rect scoreRect = new Rect(safe.x, y, safe.width, scoreLineHeight);
        GUI.Label(scoreRect, scoreText, centeredStyle);
        centeredStyle.fontSize = highScoreHudFontSize;
        float highScoreLineHeight = centeredStyle.fontSize + 4f;
        Rect highScoreRect = new Rect(safe.x, y + scoreLineHeight + Mathf.Max(0f, hudLineSpacing), safe.width, highScoreLineHeight);
        GUI.Label(highScoreRect, highScoreText, centeredStyle);
        if (showMultiplier)
        {
            Rect r2 = new Rect(safe.x, y + scoreLineHeight + highScoreLineHeight + Mathf.Max(0f, hudLineSpacing), safe.width, scoreLineHeight);
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
