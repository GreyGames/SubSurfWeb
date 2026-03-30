using UnityEngine;
using UnityEngine.SceneManagement;

public class RunnerLaneController : MonoBehaviour
{
    [Header("Lane Setup")]
    [Tooltip("Optional explicit lane centers (left -> right). If assigned, these drive centering.")]
    [SerializeField] private Transform[] laneCenters;
    [SerializeField] private int laneCount = 3;
    [SerializeField] private float laneWidth = 2.5f;
    [SerializeField] private float centerX = 0f;

    [Header("Movement")]
    [SerializeField] private float laneLerpSpeed = 14f;
    [SerializeField] private bool lockZPosition = true;
    [SerializeField] private bool alsoUseArrowKeys = true;

    [Header("Touch Input")]
    [SerializeField] private bool enableTouchInput = true;
    [Tooltip("Minimum swipe distance in pixels before it counts as a swipe.")]
    [SerializeField] private float swipeMinDistance = 60f;
    [Tooltip("Tap time threshold in seconds.")]
    [SerializeField] private float tapMaxDuration = 0.22f;

    [Header("Jump")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float jumpHeight = 2.2f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float groundSnapVelocity = -2f;
    [SerializeField] private Animator animator;
    [SerializeField] private bool autoFindAnimator = true;
    [SerializeField] private string jumpTriggerName = "Jump";
    [SerializeField] private string groundedBoolName = "IsGrounded";

    [Header("Directional Sprint Anim")]
    [SerializeField] private bool useDirectionalSprintStates = true;
    [SerializeField] private int animatorLayerIndex = 0;
    [SerializeField] private float laneSwitchBlendTime = 0.07f;
    [SerializeField] private float laneSwitchHoldTime = 0.16f;
    [SerializeField] private string sprintForwardStateName = "HumanM@Sprint01_Forward";
    [SerializeField] private string sprintLeftStateName = "HumanM@Sprint01_ForwardLeft";
    [SerializeField] private string sprintRightStateName = "HumanM@Sprint01_ForwardRight";

    [Header("Death/Restart")]
    [SerializeField] private bool enableDeathOnObstacle = true;
    [Tooltip("If true, check collider tag before name prefix.")]
    [SerializeField] private bool useTagCheck = false;
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField] private bool useNamePrefixCheck = true;
    [SerializeField] private string obstacleNamePrefix = "Obstacle";
    [SerializeField] private float restartDelay = 0.1f;
    [SerializeField] private bool useOverlapCheck = true;
    [SerializeField] private float overlapRadius = 0.45f;
    [SerializeField] private Vector3 overlapOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private bool useMarkerDistanceCheck = true;
    [SerializeField] private float markerDistance = 0.9f;

    [Header("Lives / Game Over")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private float hitInvulnerableSeconds = 0.6f;
    [SerializeField] private bool flickerOnHit = true;
    [SerializeField] private float hitFlickerSeconds = 0.45f;
    [SerializeField] private float hitFlickerInterval = 0.08f;
    [SerializeField] private bool showLivesHud = true;
    [SerializeField] private bool useSafeAreaForHud = true;
    [SerializeField] private Vector2 heartsOffset = new Vector2(12f, 10f);
    [SerializeField] private float heartSize = 26f;
    [SerializeField] private float heartSpacing = 6f;
    [SerializeField] private float heartScale = 1f;
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartEmptySprite;
    [SerializeField] private Color heartFullColor = new Color(0.85f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color heartEmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    [SerializeField] private float heartFlickerSeconds = 0.5f;
    [SerializeField] private float heartFlickerInterval = 0.08f;
    [SerializeField] private float gameOverFadeDuration = 0.8f;
    [SerializeField] private float gameOverHoldDuration = 0.6f;

    private int laneIndex;
    private float fixedZ;
    private float fixedY;
    private float verticalVelocity;
    private bool inputLocked;
    private bool isGrounded;
    private bool hasJumpTriggerParam;
    private bool hasGroundedBoolParam;
    private bool hasSprintForwardState;
    private bool hasSprintLeftState;
    private bool hasSprintRightState;
    private int sprintForwardStateHash;
    private int sprintLeftStateHash;
    private int sprintRightStateHash;
    private int runtimeAnimatorLayerIndex;
    private float laneSwitchAnimTimer;
    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool touchTracking;
    private bool isDead;
    private Rigidbody cachedBody;
    private Collider cachedCollider;
    private readonly Collider[] overlapHits = new Collider[8];
    private int lives;
    private float lastHitTime;
    private int flickerIndex = -1;
    private float flickerUntil;
    private float nextFlickerToggle;
    private bool flickerVisible;
    private bool gameOverActive;
    private float gameOverStartTime;
    private float gameOverAlpha;
    private int gameOverFontSize;
    private Rect gameOverTextRect;
    private int gameOverScoreFontSize;
    private Rect gameOverScoreRect;
    private Texture2D solidTex;
    private EndlessTrackLooper cachedLooper;
    private GUIStyle gameOverStyle;
    private GUIStyle gameOverScoreStyle;
    private Renderer[] cachedRenderers;
    private bool hitFlickerActive;
    private float hitFlickerUntil;
    private float nextHitFlickerToggle;
    private bool hitFlickerVisible = true;
    private int finalScore;

    public bool IsGameOverActive => gameOverActive;

    private void Awake()
    {
        fixedZ = transform.position.z;
        fixedY = transform.position.y;
        isGrounded = true;
        verticalVelocity = groundSnapVelocity;

        if (laneCenters != null && laneCenters.Length > 0)
        {
            laneCount = laneCenters.Length;
            laneIndex = GetClosestLaneIndex(transform.position.x);
        }
        else
        {
            laneIndex = laneCount / 2;
            if (Mathf.Abs(centerX) < 0.0001f)
            {
                centerX = transform.position.x;
            }
        }

        if (autoFindAnimator && animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheAnimatorParams();
        SetupDeathCollision();
        lives = Mathf.Max(1, maxLives);
        cachedLooper = FindObjectOfType<EndlessTrackLooper>();
        EnsureSolidTexture();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        UpdateHeartFlicker();
        UpdateHitFlicker();

        if (gameOverActive)
        {
            UpdateGameOverFade();
            return;
        }

        bool allowInput = !inputLocked;

        if (allowInput)
        {
            if (enableTouchInput)
            {
                HandleTouchInput();
            }

            bool leftPressed = Input.GetKeyDown(KeyCode.A) || (alsoUseArrowKeys && Input.GetKeyDown(KeyCode.LeftArrow));
            bool rightPressed = Input.GetKeyDown(KeyCode.D) || (alsoUseArrowKeys && Input.GetKeyDown(KeyCode.RightArrow));

            if (leftPressed)
            {
                MoveLeft();
            }
            else if (rightPressed)
            {
                MoveRight();
            }

            if (Input.GetKeyDown(jumpKey))
            {
                TriggerJumpAnimation();
                if (isGrounded)
                {
                    StartJump();
                }
            }
        }

        UpdateDirectionalSprintAnimation();

        float targetX = GetLaneX(laneIndex);

        Vector3 p = transform.position;
        float t = 1f - Mathf.Exp(-laneLerpSpeed * Time.deltaTime);
        p.x = Mathf.Lerp(p.x, targetX, t);
        p.y = UpdateVertical(p.y);

        if (lockZPosition)
        {
            p.z = fixedZ;
        }

        transform.position = p;
        UpdateAnimatorGroundedState();

        if (enableDeathOnObstacle && useOverlapCheck && !isDead)
        {
            CheckOverlapForObstacles();
        }

        if (enableDeathOnObstacle && useMarkerDistanceCheck && !isDead)
        {
            CheckMarkerDistance();
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableDeathOnObstacle || isDead)
        {
            return;
        }

        if (IsObstacle(collision.collider))
        {
            HandleObstacleHit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enableDeathOnObstacle || isDead)
        {
            return;
        }

        if (IsObstacle(other))
        {
            HandleObstacleHit();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            touchTracking = false;
            return;
        }

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            touchTracking = true;
            touchStartPos = touch.position;
            touchStartTime = Time.unscaledTime;
            return;
        }

        if (!touchTracking)
        {
            return;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            Vector2 delta = touch.position - touchStartPos;
            float duration = Time.unscaledTime - touchStartTime;

            if (delta.magnitude < swipeMinDistance && duration <= tapMaxDuration)
            {
                TriggerJumpAnimation();
                if (isGrounded)
                {
                    StartJump();
                }
            }
            else
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x < 0f) MoveLeft();
                    else MoveRight();
                }
                else
                {
                    if (delta.y > 0f)
                    {
                        TriggerJumpAnimation();
                        if (isGrounded)
                        {
                            StartJump();
                        }
                    }
                }
            }

            touchTracking = false;
        }
    }

    public void MoveLeft()
    {
        int previous = laneIndex;
        laneIndex = Mathf.Clamp(laneIndex - 1, 0, laneCount - 1);
        if (laneIndex != previous)
        {
            PlayLaneSwitchAnimation(-1);
        }
    }

    public void MoveRight()
    {
        int previous = laneIndex;
        laneIndex = Mathf.Clamp(laneIndex + 1, 0, laneCount - 1);
        if (laneIndex != previous)
        {
            PlayLaneSwitchAnimation(1);
        }
    }

    public void LockInput(bool shouldLock)
    {
        inputLocked = shouldLock;
    }

    public void ForceLane(int index)
    {
        laneIndex = Mathf.Clamp(index, 0, laneCount - 1);
        Vector3 p = transform.position;
        p.x = GetLaneX(laneIndex);
        p.y = Mathf.Max(p.y, fixedY);
        if (lockZPosition) p.z = fixedZ;
        transform.position = p;
    }

    private void StartJump()
    {
        isGrounded = false;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void TriggerJumpAnimation()
    {
        if (animator != null && hasJumpTriggerParam)
        {
            animator.SetTrigger(jumpTriggerName);
        }
    }

    private float UpdateVertical(float currentY)
    {
        verticalVelocity += gravity * Time.deltaTime;
        float nextY = currentY + verticalVelocity * Time.deltaTime;

        if (nextY <= fixedY)
        {
            nextY = fixedY;
            isGrounded = true;
            if (verticalVelocity < groundSnapVelocity)
            {
                verticalVelocity = groundSnapVelocity;
            }
        }
        else
        {
            isGrounded = false;
        }

        return nextY;
    }

    private void UpdateAnimatorGroundedState()
    {
        if (animator == null || !hasGroundedBoolParam)
        {
            return;
        }

        animator.SetBool(groundedBoolName, isGrounded);
    }

    private void CacheAnimatorParams()
    {
        hasJumpTriggerParam = false;
        hasGroundedBoolParam = false;
        hasSprintForwardState = false;
        hasSprintLeftState = false;
        hasSprintRightState = false;
        runtimeAnimatorLayerIndex = 0;

        if (animator == null)
        {
            return;
        }

        runtimeAnimatorLayerIndex = Mathf.Clamp(animatorLayerIndex, 0, animator.layerCount - 1);

        if (!string.IsNullOrEmpty(jumpTriggerName))
        {
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var p = animator.parameters[i];
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == jumpTriggerName)
                {
                    hasJumpTriggerParam = true;
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(groundedBoolName))
        {
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var p = animator.parameters[i];
                if (p.type == AnimatorControllerParameterType.Bool && p.name == groundedBoolName)
                {
                    hasGroundedBoolParam = true;
                    break;
                }
            }
        }

        hasSprintForwardState = TryResolveStateHash(sprintForwardStateName, out sprintForwardStateHash);
        hasSprintLeftState = TryResolveStateHash(sprintLeftStateName, out sprintLeftStateHash);
        hasSprintRightState = TryResolveStateHash(sprintRightStateName, out sprintRightStateHash);
    }

    private void PlayLaneSwitchAnimation(int direction)
    {
        if (!useDirectionalSprintStates || animator == null || !isGrounded)
        {
            return;
        }

        if (direction < 0 && hasSprintLeftState)
        {
            animator.CrossFade(sprintLeftStateHash, laneSwitchBlendTime, runtimeAnimatorLayerIndex);
            laneSwitchAnimTimer = laneSwitchHoldTime;
            return;
        }

        if (direction > 0 && hasSprintRightState)
        {
            animator.CrossFade(sprintRightStateHash, laneSwitchBlendTime, runtimeAnimatorLayerIndex);
            laneSwitchAnimTimer = laneSwitchHoldTime;
        }
    }

    private void UpdateDirectionalSprintAnimation()
    {
        if (!useDirectionalSprintStates || animator == null || !hasSprintForwardState || !isGrounded)
        {
            return;
        }

        if (laneSwitchAnimTimer > 0f)
        {
            laneSwitchAnimTimer -= Time.deltaTime;
            return;
        }

        if (animator.IsInTransition(runtimeAnimatorLayerIndex))
        {
            return;
        }

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(runtimeAnimatorLayerIndex);
        bool isForward = IsStateMatch(info, sprintForwardStateHash);
        if (isForward)
        {
            return;
        }

        bool isLeft = IsStateMatch(info, sprintLeftStateHash);
        bool isRight = IsStateMatch(info, sprintRightStateHash);
        if (!isLeft && !isRight)
        {
            return;
        }

        animator.CrossFade(sprintForwardStateHash, laneSwitchBlendTime, runtimeAnimatorLayerIndex);
        laneSwitchAnimTimer = laneSwitchBlendTime;
    }

    private static bool IsStateMatch(AnimatorStateInfo stateInfo, int targetStateHash)
    {
        return stateInfo.shortNameHash == targetStateHash || stateInfo.fullPathHash == targetStateHash;
    }

    private bool TryResolveStateHash(string stateName, out int resolvedHash)
    {
        resolvedHash = 0;
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        int direct = Animator.StringToHash(stateName);
        if (animator.HasState(runtimeAnimatorLayerIndex, direct))
        {
            resolvedHash = direct;
            return true;
        }

        string layerName = animator.GetLayerName(runtimeAnimatorLayerIndex);
        int fullPath = Animator.StringToHash(layerName + "." + stateName);
        if (animator.HasState(runtimeAnimatorLayerIndex, fullPath))
        {
            resolvedHash = fullPath;
            return true;
        }

        return false;
    }

    private float GetLaneX(int index)
    {
        if (laneCenters != null && laneCenters.Length > 0)
        {
            if (index < laneCenters.Length && laneCenters[index] != null)
            {
                return laneCenters[index].position.x;
            }
        }

        float centerLane = (laneCount - 1) * 0.5f;
        return centerX + (index - centerLane) * laneWidth;
    }

    private int GetClosestLaneIndex(float xPos)
    {
        int bestIndex = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < laneCenters.Length; i++)
        {
            if (laneCenters[i] == null) continue;
            float d = Mathf.Abs(laneCenters[i].position.x - xPos);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void SetupDeathCollision()
    {
        if (!enableDeathOnObstacle)
        {
            return;
        }

        cachedBody = GetComponent<Rigidbody>();
        if (cachedBody == null)
        {
            cachedBody = gameObject.AddComponent<Rigidbody>();
        }

        cachedBody.isKinematic = true;
        cachedBody.useGravity = false;
        // Player is moved manually in Update(); interpolation here can introduce
        // visible camera jitter when camera is parented to the player.
        cachedBody.interpolation = RigidbodyInterpolation.None;
        cachedBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        cachedCollider = GetComponent<Collider>();
        if (cachedCollider is CapsuleCollider capsule)
        {
            overlapRadius = Mathf.Max(overlapRadius, capsule.radius * 0.9f);
            overlapOffset = capsule.center;
        }
    }

    private bool IsObstacle(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponentInParent<ObstacleMarker>() != null)
        {
            return true;
        }

        if (useTagCheck && other.CompareTag(obstacleTag))
        {
            return true;
        }

        if (useNamePrefixCheck)
        {
            if (other.name.StartsWith(obstacleNamePrefix, System.StringComparison.Ordinal))
            {
                return true;
            }

            Transform root = other.transform.root;
            if (root != null && root.name.StartsWith(obstacleNamePrefix, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void DieAndRestart()
    {
        HandleObstacleHit();
    }

    private void RestartScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void CheckOverlapForObstacles()
    {
        Vector3 center = transform.TransformPoint(overlapOffset);
        int count = Physics.OverlapSphereNonAlloc(center, overlapRadius, overlapHits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapHits[i];
            if (hit == null || hit == cachedCollider)
            {
                continue;
            }

            if (hit.attachedRigidbody == cachedBody)
            {
                continue;
            }

            if (IsObstacle(hit))
            {
                HandleObstacleHit();
                return;
            }
        }
    }

    private void CheckMarkerDistance()
    {
        var active = ObstacleMarker.Active;
        if (active == null || active.Count == 0)
        {
            return;
        }

        Vector3 center = transform.TransformPoint(overlapOffset);
        float thresholdSqr = markerDistance * markerDistance;

        for (int i = 0; i < active.Count; i++)
        {
            ObstacleMarker marker = active[i];
            if (marker == null)
            {
                continue;
            }

            Vector3 delta = marker.transform.position - center;
            if (delta.sqrMagnitude <= thresholdSqr)
            {
                HandleObstacleHit();
                return;
            }
        }
    }

    private void HandleObstacleHit()
    {
        if (!enableDeathOnObstacle || gameOverActive)
        {
            return;
        }

        if (Time.unscaledTime - lastHitTime < hitInvulnerableSeconds)
        {
            return;
        }

        lastHitTime = Time.unscaledTime;
        lives = Mathf.Max(0, lives - 1);
        StartHeartFlicker(lives);
        StartHitFlicker();

        if (lives <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        gameOverActive = true;
        WebGameEvents.SendGameLose();
        gameOverStartTime = Time.unscaledTime;
        gameOverAlpha = 0f;
        gameOverFontSize = 0;
        gameOverScoreFontSize = 0;
        finalScore = GetCurrentScore();
        isDead = true;
        LockInput(true);
        Time.timeScale = 1f;

        if (cachedLooper == null)
        {
            cachedLooper = FindObjectOfType<EndlessTrackLooper>();
        }

        if (cachedLooper != null)
        {
            cachedLooper.PauseWorld(true);
        }
    }

    private void UpdateGameOverFade()
    {
        float elapsed = Time.unscaledTime - gameOverStartTime;
        float t = gameOverFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / gameOverFadeDuration);
        gameOverAlpha = t;

        if (elapsed >= gameOverFadeDuration + gameOverHoldDuration)
        {
            StopHitFlicker();
            RestartScene();
        }
    }

    private void StartHeartFlicker(int index)
    {
        if (heartFlickerSeconds <= 0f || index < 0 || index >= maxLives)
        {
            flickerIndex = -1;
            return;
        }

        flickerIndex = index;
        flickerUntil = Time.unscaledTime + heartFlickerSeconds;
        nextFlickerToggle = Time.unscaledTime + heartFlickerInterval;
        flickerVisible = true;
    }

    private void UpdateHeartFlicker()
    {
        if (flickerIndex < 0)
        {
            return;
        }

        if (Time.unscaledTime >= flickerUntil)
        {
            flickerIndex = -1;
            return;
        }

        if (Time.unscaledTime >= nextFlickerToggle)
        {
            flickerVisible = !flickerVisible;
            nextFlickerToggle = Time.unscaledTime + heartFlickerInterval;
        }
    }

    private void StartHitFlicker()
    {
        if (!flickerOnHit || hitFlickerSeconds <= 0f)
        {
            return;
        }

        hitFlickerActive = true;
        hitFlickerUntil = Time.unscaledTime + hitFlickerSeconds;
        nextHitFlickerToggle = Time.unscaledTime + hitFlickerInterval;
        hitFlickerVisible = true;
        SetRenderersVisible(true);
    }

    private void StopHitFlicker()
    {
        hitFlickerActive = false;
        hitFlickerVisible = true;
        SetRenderersVisible(true);
    }

    private void UpdateHitFlicker()
    {
        if (!hitFlickerActive)
        {
            return;
        }

        if (Time.unscaledTime >= hitFlickerUntil)
        {
            StopHitFlicker();
            return;
        }

        if (Time.unscaledTime >= nextHitFlickerToggle)
        {
            hitFlickerVisible = !hitFlickerVisible;
            SetRenderersVisible(hitFlickerVisible);
            nextHitFlickerToggle = Time.unscaledTime + hitFlickerInterval;
        }
    }

    private void EnsureSolidTexture()
    {
        if (solidTex != null)
        {
            return;
        }

        solidTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        solidTex.SetPixel(0, 0, Color.white);
        solidTex.Apply();
    }

    private void OnGUI()
    {
        if (showLivesHud)
        {
            DrawHearts();
        }

        if (gameOverActive)
        {
            DrawGameOverOverlay();
        }
    }

    private void DrawHearts()
    {
        EnsureSolidTexture();

        float screenScale = Mathf.Max(1f, Screen.height / 720f);
        float size = heartSize * heartScale * screenScale;
        float spacing = heartSpacing * heartScale * screenScale;
        Rect safe = GetSafeAreaRect();
        float x = safe.x + heartsOffset.x * screenScale;
        float yBase = safe.y + heartsOffset.y * screenScale;

        Color previous = GUI.color;
        for (int i = 0; i < maxLives; i++)
        {
            int slotIndex = maxLives - 1 - i;
            float y = yBase + slotIndex * (size + spacing);
            bool isFull = i < lives;
            bool isFlicker = i == flickerIndex;
            if (isFlicker && !flickerVisible)
            {
                continue;
            }

            bool drawFull = isFull || isFlicker;
            Color c = drawFull ? heartFullColor : heartEmptyColor;
            GUI.color = c;

            Rect rect = new Rect(x, y, size, size);
            if (drawFull && heartFullSprite != null)
            {
                DrawSprite(rect, heartFullSprite);
            }
            else if (!drawFull && heartEmptySprite != null)
            {
                DrawSprite(rect, heartEmptySprite);
            }
            else
            {
                GUI.DrawTexture(rect, solidTex);
            }
        }

        GUI.color = previous;
    }

    private void DrawGameOverOverlay()
    {
        EnsureSolidTexture();

        Color previous = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = -1000;
        GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(gameOverAlpha));
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), solidTex);
        GUI.color = previous;
        GUI.depth = previousDepth;

        if (gameOverAlpha < 0.4f)
        {
            return;
        }

        if (gameOverStyle == null)
        {
            gameOverStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }

        PrepareGameOverText();
        gameOverStyle.fontSize = gameOverFontSize;
        gameOverStyle.normal.textColor = Color.white;

        GUI.Label(gameOverTextRect, "GAME OVER", gameOverStyle);

        if (gameOverScoreStyle == null)
        {
            gameOverScoreStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }

        gameOverScoreStyle.fontSize = gameOverScoreFontSize;
        gameOverScoreStyle.normal.textColor = Color.white;
        GUI.Label(gameOverScoreRect, $"Score: {finalScore}", gameOverScoreStyle);
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

    private Rect GetSafeAreaRect()
    {
        if (!useSafeAreaForHud)
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

    private void PrepareGameOverText()
    {
        if (gameOverFontSize > 0 && gameOverTextRect.width > 0f)
        {
            return;
        }

        int baseFont = 18;
        if (GUI.skin != null && GUI.skin.label != null && GUI.skin.label.fontSize > 0)
        {
            baseFont = GUI.skin.label.fontSize;
        }

        float screenScale = Mathf.Max(1f, Screen.height / 720f);
        gameOverFontSize = Mathf.RoundToInt(baseFont * 2.2f * screenScale);
        gameOverScoreFontSize = Mathf.RoundToInt(baseFont * 1.4f * screenScale);

        float centerY = Screen.height * 0.5f;
        float gameOverHeight = gameOverFontSize + 10f;
        float scoreHeight = gameOverScoreFontSize + 8f;
        float totalHeight = gameOverHeight + scoreHeight;
        float top = centerY - totalHeight * 0.5f;

        gameOverTextRect = new Rect(0f, top, Screen.width, gameOverHeight);
        gameOverScoreRect = new Rect(0f, top + gameOverHeight, Screen.width, scoreHeight);
    }

    private void SetRenderersVisible(bool visible)
    {
        if (cachedRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer r = cachedRenderers[i];
            if (r != null)
            {
                r.enabled = visible;
            }
        }
    }

    private int GetCurrentScore()
    {
        if (ScoreManager.Instance != null)
        {
            return ScoreManager.Instance.CurrentScore;
        }

        ScoreManager manager = FindObjectOfType<ScoreManager>();
        if (manager != null)
        {
            return manager.CurrentScore;
        }

        return 0;
    }
}

