using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private bool autoFindScoreManager = true;

    private bool collected;
    private ScoreManager scoreManager;

    private void Awake()
    {
        EnsureTriggerCollider();
        if (autoFindScoreManager)
        {
            scoreManager = ScoreManager.Instance;
            if (scoreManager == null)
            {
                scoreManager = FindObjectOfType<ScoreManager>();
            }
        }
    }

    private void OnEnable()
    {
        collected = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryCollect(collision.collider);
    }

    private void TryCollect(Collider other)
    {
        if (collected)
        {
            return;
        }

        if (!IsCollector(other))
        {
            return;
        }

        collected = true;

        if (scoreManager == null)
        {
            scoreManager = ScoreManager.Instance;
            if (scoreManager == null)
            {
                scoreManager = FindObjectOfType<ScoreManager>();
            }
        }

        scoreManager?.AddCoins(value);

        gameObject.SetActive(false);
    }

    private static bool IsCollector(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponentInParent<RunnerLaneController>() != null)
        {
            return true;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.GetComponentInParent<RunnerLaneController>() != null)
        {
            return true;
        }

        return other.CompareTag("Player");
    }

    public void SetValue(int newValue)
    {
        value = Mathf.Max(0, newValue);
    }

    private void EnsureTriggerCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.5f;
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }
    }
}
