using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Gem; // dùng GemType

public class Match3Manager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float gemSpacing = 0.75f;
    public Vector3 gridOffset = Vector3.zero;

    [Header("Prefabs")]
    public GameObject[] gemPrefabs; // Red, Blue, Green, Yellow, Purple, Grey

    [Header("Swap Settings")]
    public float swapDuration = 0.3f;
    public AnimationCurve swapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Match Settings")]
    public int minMatchLength = 3;
    public float destroyDelay = 0.5f;
    public float fallDuration = 0.5f;

    // --- Runtime ---
    private Gem[,] gems;
    private Gem selectedGem;
    private bool isSwapping = false;
    private bool isProcessingMatches = false;
    private Camera mainCamera;
    private Transform gridParent;

    // --- Keyboard cursor (optional) ---
    private Vector2Int cursorPos;
    private GameObject cursorHighlight;

    // --- Events ---
    public System.Action<int> OnDamageDealt;                // vẫn để sẵn nếu bạn dùng
    public System.Action<List<Gem>> OnGemsDestroyed;        // vẫn để sẵn nếu bạn dùng
    public System.Action<List<List<Gem>>> OnMatchesResolved; // PATCH: bắn sau mỗi batch xử lý

    // --- Input gate (PATCH) ---
    private bool _playerInputEnabled = true;
    public void SetPlayerInputEnabled(bool enabled)
    {
        _playerInputEnabled = enabled;
        if (!enabled) DeselectGem();
    }

    // Cho script ngoài gọi swap an toàn (PATCH)
    public void DoSwap(Gem a, Gem b) => StartCoroutine(SwapGems(a, b));

    public static Match3Manager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
    }

    void Start()
    {
        InitializeGrid();
        StartCoroutine(FillGridInitial());
    }

    // ================== INIT ==================
    void InitializeGrid()
    {
        if (gemPrefabs == null || gemPrefabs.Length == 0)
            Debug.LogError("[Match3] gemPrefabs is null or empty");

        gems = new Gem[gridWidth, gridHeight];

        GameObject parent = new GameObject("Grid");
        parent.transform.position = gridOffset;
        gridParent = parent.transform;

        
    }

    void CreateGemImmediate(int x, int y)
    {
        Vector3 pos = GetWorldPosition(x, y);
        int typeIdx = GetRandomGemType(x, y);

        GameObject go = Instantiate(gemPrefabs[typeIdx], pos, Quaternion.identity);
        if (gridParent) go.transform.SetParent(gridParent, false);

        if (!go.TryGetComponent<Gem>(out var g))
        {
            Debug.LogError($"Prefab {go.name} missing Gem component");
            Destroy(go);
            return;
        }

        g.gemType = (GemType)typeIdx;
        g.gridX = x; g.gridY = y;
        gems[x, y] = g;

        // spawn scale-in nhẹ cho đẹp
        go.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateGemSpawn(go));
    }

    IEnumerator AnimateGemSpawn(GameObject obj)
    {
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - t / dur, 3f);
            obj.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, k);
            yield return null;
        }
        obj.transform.localScale = Vector3.one;
    }

    IEnumerator FillGridInitial()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                CreateGemImmediate(x, y);

        // Xử lý match ban đầu (nếu có), đảm bảo bàn cờ sạch
        yield return StartCoroutine(ProcessInitialMatches());

        // Keyboard cursor (optional)
        cursorPos = new Vector2Int(0, 0);
        cursorHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cursorHighlight.name = "CursorHighlight";
        cursorHighlight.transform.localScale = Vector3.one * gemSpacing * 1.1f;
        var mr = cursorHighlight.GetComponent<MeshRenderer>();
        if (mr) mr.material.color = new Color(1f, 1f, 0f, 0.25f);
        cursorHighlight.transform.position = GetWorldPosition(cursorPos.x, cursorPos.y);
    }

    IEnumerator ProcessInitialMatches()
    {
        List<List<Gem>> matches;
        do
        {
            matches = FindAllMatches();
            if (matches.Count > 0)
            {
                yield return StartCoroutine(DestroyMatches(matches));
                yield return StartCoroutine(FillEmptySpaces());
                yield return new WaitForSeconds(0.15f);
            }
        } while (matches.Count > 0);
    }

    // ================== UPDATE / INPUT ==================
    void Update()
    {
        // chỉ cho input khi là lượt người chơi, không swapping/processing
        if (!isSwapping && !isProcessingMatches && _playerInputEnabled)
        {
            HandleInput();
            HandleKeyboardInput();
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = mainCamera != null
                ? mainCamera.ScreenToWorldPoint(Input.mousePosition)
                : (Vector3)Input.mousePosition;
            mousePos.z = 0f;

            Gem clicked = GetGemAtPosition(mousePos);
            if (clicked != null)
            {
                if (selectedGem == null) SelectGem(clicked);
                else if (clicked == selectedGem) DeselectGem();
                else if (IsAdjacent(selectedGem, clicked)) StartCoroutine(SwapGems(selectedGem, clicked));
                else { DeselectGem(); SelectGem(clicked); }
            }
            else DeselectGem();
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursor(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursor(1, 0);
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursor(0, 1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursor(0, -1);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            Gem cur = GetGem(cursorPos.x, cursorPos.y);
            if (cur != null)
            {
                if (selectedGem == null) SelectGem(cur);
                else if (selectedGem == cur) DeselectGem();
                else if (IsAdjacent(selectedGem, cur)) StartCoroutine(SwapGems(selectedGem, cur));
                else { DeselectGem(); SelectGem(cur); }
            }
        }
    }

    void MoveCursor(int dx, int dy)
    {
        int nx = Mathf.Clamp(cursorPos.x + dx, 0, gridWidth - 1);
        int ny = Mathf.Clamp(cursorPos.y + dy, 0, gridHeight - 1);
        cursorPos = new Vector2Int(nx, ny);
        if (cursorHighlight) cursorHighlight.transform.position = GetWorldPosition(nx, ny);
    }

    // ================== SWAP ==================
    IEnumerator SwapGems(Gem gem1, Gem gem2)
    {
        isSwapping = true;

        Vector3 pos1 = gem1.transform.position;
        Vector3 pos2 = gem2.transform.position;

        // hoán đổi trong mảng
        gems[gem1.gridX, gem1.gridY] = gem2;
        gems[gem2.gridX, gem2.gridY] = gem1;

        // hoán đổi tọa độ lưới
        int tx = gem1.gridX, ty = gem1.gridY;
        gem1.gridX = gem2.gridX; gem1.gridY = gem2.gridY;
        gem2.gridX = tx; gem2.gridY = ty;

        // animate
        yield return StartCoroutine(AnimateSwap(gem1, pos2, gem2, pos1));

        // kiểm tra match
        var matches = FindAllMatches();
        if (matches.Count > 0)
        {
            DeselectGem();
            yield return StartCoroutine(ProcessMatches(matches));
        }
        else
        {
            // swap back
            yield return StartCoroutine(SwapBack(gem1, gem2, pos1, pos2));
        }

        isSwapping = false;
    }

    IEnumerator AnimateSwap(Gem g1, Vector3 t1, Gem g2, Vector3 t2)
    {
        float t = 0f;
        Vector3 s1 = g1.transform.position, s2 = g2.transform.position;
        while (t < swapDuration)
        {
            t += Time.deltaTime;
            float k = swapCurve.Evaluate(t / swapDuration);
            g1.transform.position = Vector3.Lerp(s1, t1, k);
            g2.transform.position = Vector3.Lerp(s2, t2, k);
            yield return null;
        }
        g1.transform.position = t1;
        g2.transform.position = t2;
    }

    IEnumerator SwapBack(Gem g1, Gem g2, Vector3 p1, Vector3 p2)
    {
        // revert mảng
        gems[g1.gridX, g1.gridY] = g2;
        gems[g2.gridX, g2.gridY] = g1;

        // revert tọa độ
        int tx = g1.gridX, ty = g1.gridY;
        g1.gridX = g2.gridX; g1.gridY = g2.gridY;
        g2.gridX = tx; g2.gridY = ty;

        yield return StartCoroutine(AnimateSwap(g1, p1, g2, p2));
        DeselectGem();
    }

    // ================== MATCH PIPELINE ==================
    IEnumerator ProcessMatches(List<List<Gem>> matches)
    {
        isProcessingMatches = true;

        // 1) Phá
        yield return StartCoroutine(DestroyMatches(matches));
        // 2) Đổ đầy
        yield return StartCoroutine(FillEmptySpaces());
        // 3) PATCH: báo ra ngoài cho hệ chiến đấu áp hiệu ứng
        OnMatchesResolved?.Invoke(matches);

        // 4) Kiểm tra cascade
        var newMatches = FindAllMatches();
        if (newMatches.Count > 0)
        {
            yield return new WaitForSeconds(0.25f);
            yield return StartCoroutine(ProcessMatches(newMatches));
        }

        isProcessingMatches = false;
    }

    IEnumerator DestroyMatches(List<List<Gem>> matches)
    {
        List<Gem> all = new List<Gem>();
        foreach (var m in matches)
            foreach (var g in m)
                if (!all.Contains(g)) all.Add(g);

        // Remove khỏi mảng
        foreach (var g in all) gems[g.gridX, g.gridY] = null;
        // Animate phá
        foreach (var g in all) g.DestroyGem();

        OnGemsDestroyed?.Invoke(all);
        yield return new WaitForSeconds(destroyDelay);
    }

    IEnumerator FillEmptySpaces()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            List<Gem> col = new List<Gem>();

            // gom gem hiện có trong cột
            for (int y = 0; y < gridHeight; y++)
            {
                if (gems[x, y] != null)
                {
                    col.Add(gems[x, y]);
                    gems[x, y] = null;
                }
            }

            // đặt xuống dưới
            for (int i = 0; i < col.Count; i++)
            {
                gems[x, i] = col[i];
                col[i].gridY = i;

                Vector3 targetPos = GetWorldPosition(x, i);
                StartCoroutine(AnimateGemFall(col[i], targetPos)); // chạy đồng thời
            }

            // sinh gem mới cho ô trống
            for (int y = col.Count; y < gridHeight; y++)
            {
                StartCoroutine(CreateGem(x, y, true)); // spawn+rơi đồng thời
            }
        }

        yield return new WaitForSeconds(fallDuration);
    }

    IEnumerator CreateGem(int x, int y, bool animate)
    {
        Vector3 pos = GetWorldPosition(x, y);
        int typeIdx = GetRandomGemType(x, y);

        GameObject go = Instantiate(gemPrefabs[typeIdx]);
        if (gridParent) go.transform.SetParent(gridParent, false);

        if (animate) go.transform.position = pos + Vector3.up * 10f;
        else go.transform.position = pos;

        Gem g = go.GetComponent<Gem>();
        g.gemType = (GemType)typeIdx;
        g.gridX = x; g.gridY = y;
        gems[x, y] = g;

        if (animate) yield return StartCoroutine(AnimateGemFall(g, pos));
    }

    IEnumerator AnimateGemFall(Gem g, Vector3 target)
    {
        Vector3 start = g.transform.position;
        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - t / fallDuration, 2f);
            g.transform.position = Vector3.Lerp(start, target, k);
            yield return null;
        }
        g.transform.position = target;
    }

    // ================== FIND MATCHES ==================
    List<List<Gem>> FindAllMatches()
    {
        var rawMatches = new List<List<Gem>>();

        // Horizontal
        for (int y = 0; y < gridHeight; y++)
        {
            List<Gem> cur = new List<Gem>();
            for (int x = 0; x < gridWidth; x++)
            {
                if (gems[x, y] == null)
                {
                    if (cur.Count >= minMatchLength) rawMatches.Add(new List<Gem>(cur));
                    cur.Clear();
                    continue;
                }

                if (cur.Count == 0 || gems[x, y].gemType == cur[0].gemType)
                    cur.Add(gems[x, y]);
                else
                {
                    if (cur.Count >= minMatchLength) rawMatches.Add(new List<Gem>(cur));
                    cur.Clear();
                    cur.Add(gems[x, y]);
                }
            }
            if (cur.Count >= minMatchLength) rawMatches.Add(new List<Gem>(cur));
        }

        // Vertical
        for (int x = 0; x < gridWidth; x++)
        {
            List<Gem> cur = new List<Gem>();
            for (int y = 0; y < gridHeight; y++)
            {
                if (gems[x, y] == null)
                {
                    if (cur.Count >= minMatchLength) rawMatches.Add(new List<Gem>(cur));
                    cur.Clear();
                    continue;
                }

                if (cur.Count == 0 || gems[x, y].gemType == cur[0].gemType)
                    cur.Add(gems[x, y]);
                else
                {
                    if (cur.Count >= minMatchLength) rawMatches.Add(new List<Gem>(cur));
                    cur.Clear();
                    cur.Add(gems[x, y]);
                }
            }
            if (cur.Count >= minMatchLength) rawMatches.Add(new List<Gem>(cur));
        }

        // Gộp nhóm có giao nhau → T/L/Cross ăn cùng lượt
        return MergeOverlappingGroups(rawMatches);
    }

    List<List<Gem>> MergeOverlappingGroups(List<List<Gem>> groups)
    {
        var sets = new List<HashSet<Gem>>();
        foreach (var g in groups) sets.Add(new HashSet<Gem>(g));

        bool merged;
        do
        {
            merged = false;
            for (int i = 0; i < sets.Count; i++)
            {
                for (int j = i + 1; j < sets.Count; j++)
                {
                    if (sets[i].Overlaps(sets[j]))
                    {
                        sets[i].UnionWith(sets[j]);
                        sets.RemoveAt(j);
                        merged = true;
                        j--;
                    }
                }
            }
        } while (merged);

        var res = new List<List<Gem>>();
        foreach (var s in sets) res.Add(new List<Gem>(s));
        return res;
    }

    // ================== HELPERS ==================
    Vector3 GetWorldPosition(int x, int y)
        => new Vector3(x * gemSpacing, y * gemSpacing, 0f) + gridOffset;

    int GetRandomGemType(int x, int y)
    {
        List<int> possible = new List<int>();
        for (int i = 0; i < gemPrefabs.Length; i++) possible.Add(i);

        // tránh match ngang
        if (x >= 2 && gems[x - 1, y] != null && gems[x - 2, y] != null &&
            gems[x - 1, y].gemType == gems[x - 2, y].gemType)
            possible.Remove((int)gems[x - 1, y].gemType);

        // tránh match dọc
        if (y >= 2 && gems[x, y - 1] != null && gems[x, y - 2] != null &&
            gems[x, y - 1].gemType == gems[x, y - 2].gemType)
            possible.Remove((int)gems[x, y - 1].gemType);

        if (possible.Count == 0) return Random.Range(0, gemPrefabs.Length);
        return possible[Random.Range(0, possible.Count)];
    }

    Gem GetGemAtPosition(Vector3 worldPos)
    {
        Vector3 local = worldPos - gridOffset;
        int x = Mathf.RoundToInt(local.x / gemSpacing);
        int y = Mathf.RoundToInt(local.y / gemSpacing);

        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return null;
        return gems[x, y];
    }

    void SelectGem(Gem g) { selectedGem = g; g.SelectGem(); }
    void DeselectGem() { if (selectedGem != null) { selectedGem.DeselectGem(); selectedGem = null; } }
    bool IsAdjacent(Gem a, Gem b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    // Public helpers
    public bool IsGridReady() => !isSwapping && !isProcessingMatches;
    public Gem GetGem(int x, int y)
        => (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight) ? gems[x, y] : null;
}
