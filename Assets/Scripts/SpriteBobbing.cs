using UnityEngine;

public class SpriteBobbing : MonoBehaviour
{
    public float amplitude = 0.1f;
    public float frequency = 1f;
    private Vector3 startPos;

    void Start() => startPos = transform.localPosition;

    void Update()
    {
        float y = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = startPos + new Vector3(0, y, 0);
    }
}
