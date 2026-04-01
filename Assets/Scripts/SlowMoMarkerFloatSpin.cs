using UnityEngine;

public class SlowMoMarkerFloatSpin : MonoBehaviour
{
    [SerializeField] private float spinDegreesPerSecond = 90f;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 baseLocalPos;
    private bool hasBase;

    public void Configure(float spinDps, float amplitude, float speed)
    {
        spinDegreesPerSecond = spinDps;
        floatAmplitude = amplitude;
        floatSpeed = speed;
    }

    public void SetBaseLocalPosition(Vector3 localPos)
    {
        baseLocalPos = localPos;
        hasBase = true;
        transform.localPosition = localPos;
    }

    private void OnEnable()
    {
        if (!hasBase)
        {
            baseLocalPos = transform.localPosition;
            hasBase = true;
        }
    }

    private void Update()
    {
        if (Mathf.Abs(floatAmplitude) > 0.0001f)
        {
            float y = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = baseLocalPos + Vector3.up * y;
        }

        if (Mathf.Abs(spinDegreesPerSecond) > 0.001f)
        {
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }
}
