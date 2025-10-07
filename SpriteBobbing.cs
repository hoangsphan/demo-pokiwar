using UnityEngine;

public class BobbingAny : MonoBehaviour
{
    public float amplitude = 8f;          // UI: tính theo pixel; Non-UI: theo world units
    public float frequency = 1.5f;        // chu kỳ lắc
    public bool useUnscaledTime = false;  // bật nếu có pause (timeScale=0)

    Vector3 startLocalPos;
    RectTransform rect;
    Vector2 startAnchoredPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            startAnchoredPos = rect.anchoredPosition;
        }
        else
        {
            startLocalPos = transform.localPosition;
        }
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float y = Mathf.Sin(t * frequency) * amplitude;

        if (rect != null)
            rect.anchoredPosition = startAnchoredPos + new Vector2(0f, y);
        else
            transform.localPosition = startLocalPos + new Vector3(0f, y, 0f);
    }
}
