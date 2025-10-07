using UnityEngine;

public class UIButtonBobbing : MonoBehaviour
{
    public float amplitude = 10f;       // độ cao (pixel)
    public float frequency = 1.5f;      // tốc độ lắc
    public bool useUnscaledTime = false; // nếu game pause (timeScale = 0) thì bật cái này

    private RectTransform rect;
    private Vector2 startAnchoredPos;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        startAnchoredPos = rect.anchoredPosition;
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float y = Mathf.Sin(t * frequency) * amplitude;

        rect.anchoredPosition = startAnchoredPos + new Vector2(0, y);
    }
}

