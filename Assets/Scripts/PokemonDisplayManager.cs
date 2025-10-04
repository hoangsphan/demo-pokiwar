using UnityEngine;

public class PokemonDisplayManager : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite backgroundSprite;
    public Sprite playerSprite;
    public Sprite enemySprite;

    [Header("Positions (world)")]
    [Tooltip("Vị trí mặc định PlayerChar (-4.1, 2.09, 0)")]
    public Vector3 playerPos = new Vector3(-4.1f, 2.09f, 0f);

    [Tooltip("Vị trí mặc định EnemyChar (9.66, 1.89, 0)")]
    public Vector3 enemyPos = new Vector3(9.66f, 1.89f, 0f);

    [Header("Scale & Flip")]
    public float characterScale = 1.0f;
    public bool flipEnemy = true;

    [Header("Bobbing Animation")]
    public float bobAmplitude = 0.08f;  // Biên độ dao động
    public float bobFrequency = 1.5f;   // Tốc độ dao động
    public bool desyncBobbing = true;   // Lệch pha giữa 2 nhân vật

    void Start()
    {
        SetupBackground();
        SetupCharacter(true);   // player
        SetupCharacter(false);  // enemy
    }

    void SetupBackground()
    {
        if (!backgroundSprite) return;

        var go = new GameObject("BG");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;

        // Giữ đúng giá trị bạn đã set tay
        go.transform.position = new Vector3(2.6284f, 2.5977f, 10f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        go.transform.localScale = new Vector3(2.1408f, 1.962382f, 0.711111f);
    }

    void SetupCharacter(bool isPlayer)
    {
        Sprite sp = isPlayer ? playerSprite : enemySprite;
        if (!sp) return;

        var go = new GameObject(isPlayer ? "PlayerChar" : "EnemyChar");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;

        go.transform.localScale = Vector3.one * characterScale;
        go.transform.position = isPlayer ? playerPos : enemyPos;

        if (!isPlayer && flipEnemy) sr.flipX = true;

        // Thêm bobbing animation
        var bob = go.AddComponent<SimpleBobbing>();
        bob.amplitude = bobAmplitude;
        bob.frequency = bobFrequency;
        bob.phaseOffset = desyncBobbing ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }
}

/// <summary>
/// Component animation lên–xuống theo sin.
/// </summary>
public class SimpleBobbing : MonoBehaviour
{
    public float amplitude = 0.08f;
    public float frequency = 1.5f;
    public float phaseOffset = 0f;

    private Vector3 _startPos;

    void Awake()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        float dy = Mathf.Sin((Mathf.PI * 2f) * frequency * Time.time + phaseOffset) * amplitude;
        transform.position = new Vector3(_startPos.x, _startPos.y + dy, _startPos.z);
    }
}
