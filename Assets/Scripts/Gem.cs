using System.Collections;
using UnityEngine;

[System.Serializable]
public class Gem : MonoBehaviour
{
    public enum GemType { Red, Blue, Green, Yellow, Purple, Grey }

    [Header("Gem Properties")]
    public GemType gemType;
    public int damage = 10;
    public Sprite gemSprite;

    [Header("Grid Position")]
    public int gridX;
    public int gridY;

    private SpriteRenderer spriteRenderer;
    private bool isSelected = false;
    public bool IsSelected => isSelected;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (gemSprite != null) spriteRenderer.sprite = gemSprite;
    }

    public void SetPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
        transform.position = new Vector3(x, y, 0);
    }

    public void SelectGem()
    {
        isSelected = true;
        transform.localScale = Vector3.one * 1.1f;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    public void DeselectGem()
    {
        isSelected = false;
        transform.localScale = Vector3.one;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    public void MoveTo(Vector3 targetPosition, float duration = 0.5f)
    {
        StartCoroutine(MoveCoroutine(targetPosition, duration));
    }

    IEnumerator MoveCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        transform.position = targetPosition;
    }

    public void DestroyGem()
    {
        StartCoroutine(DestroyAnimation());
    }

    IEnumerator DestroyAnimation()
    {
        float duration = 0.2f;
        Vector3 originalScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f - t;
                spriteRenderer.color = color;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    public bool CanMatchWith(Gem other) => other != null && other.gemType == this.gemType;

    public int GetEffectValue()
    {
        switch (gemType)
        {
            case GemType.Red: return 7;
            case GemType.Blue: return 7;
            case GemType.Green: return 7;
            case GemType.Grey: return 6;
            case GemType.Yellow: return 12;
            case GemType.Purple: return 6;
            default: return 0;
        }
    }

    public string GetSpecialEffect()
    {
        switch (gemType)
        {
            case GemType.Red: return "Tăng nộ";
            case GemType.Blue: return "Tăng mana";
            case GemType.Green: return "Hồi máu";
            case GemType.Grey: return "Hút máu";
            case GemType.Yellow: return "Tấn công";
            case GemType.Purple: return "Hút nộ đối thủ";
            default: return "Không có hiệu ứng";
        }
    }
}
