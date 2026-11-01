using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private class FallingItem
    {
        public Transform transform;
        public bool isBomb;
        public float speed;
    }

    private Transform player;
    private readonly List<FallingItem> items = new List<FallingItem>();

    private int score = 0;
    private int bombs = 0;
    private const int MaxBombs = 3;
    private bool isGameOver = false;

    private float spawnTimer = 0f;
    private float spawnInterval = 0.75f;
    private float playerSpeed = 10f;
    private float horizontalLimit = 7.5f;

    private Sprite squareSprite;
    private Sprite circleSprite;

    // Automatically starts the game when you press Play in Unity!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        if (GameObject.Find("CoinCatcherGame") == null)
        {
            GameObject go = new GameObject("CoinCatcherGame");
            go.AddComponent<NewMonoBehaviourScript>();
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.22f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        squareSprite = CreateSquareSprite();
        circleSprite = CreateCircleSprite();

        GameObject playerObj = new GameObject("PlayerBasket");
        player = playerObj.transform;
        player.position = new Vector3(0f, -4.0f, 0f);
        player.localScale = new Vector3(1.8f, 0.4f, 1f);

        SpriteRenderer sr = playerObj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.2f, 0.85f, 1f);
    }

    private void Update()
    {
        if (isGameOver)
        {
            if (IsRestartPressed())
            {
                RestartGame();
            }
            return;
        }

        float moveInput = ReadHorizontalInput();
        Vector3 pPos = player.position;
        pPos.x = Mathf.Clamp(pPos.x + moveInput * playerSpeed * Time.deltaTime, -horizontalLimit, horizontalLimit);
        player.position = pPos;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnFallingItem();
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            FallingItem item = items[i];
            if (item.transform == null)
            {
                items.RemoveAt(i);
                continue;
            }

            item.transform.position += Vector3.down * (item.speed * Time.deltaTime);
            Vector3 iPos = item.transform.position;

            bool hitX = Mathf.Abs(iPos.x - pPos.x) < 1.15f;
            bool hitY = Mathf.Abs(iPos.y - pPos.y) < 0.45f;

            if (hitX && hitY)
            {
                if (item.isBomb)
                {
                    bombs++;
                    if (bombs >= MaxBombs)
                    {
                        isGameOver = true;
                    }
                }
                else
                {
                    score++;
                }

                Destroy(item.transform.gameObject);
                items.RemoveAt(i);
                continue;
            }

            if (iPos.y < -6f)
            {
                Destroy(item.transform.gameObject);
                items.RemoveAt(i);
            }
        }
    }

    private void SpawnFallingItem()
    {
        bool isBomb = Random.value < 0.3f;
        GameObject obj = new GameObject(isBomb ? "Bomb" : "Coin");
        obj.transform.position = new Vector3(Random.Range(-horizontalLimit, horizontalLimit), 5.8f, 0f);
        obj.transform.localScale = isBomb ? new Vector3(0.65f, 0.65f, 1f) : new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = isBomb ? new Color(0.95f, 0.25f, 0.25f) : new Color(1f, 0.85f, 0.1f);

        items.Add(new FallingItem
        {
            transform = obj.transform,
            isBomb = isBomb,
            speed = Random.Range(3.5f, 6.0f)
        });
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private float ReadHorizontalInput()
    {
        float move = 0f;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += 1f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Mathf.Approximately(move, 0f))
        {
            move = Input.GetAxisRaw("Horizontal");
        }
#endif
        return Mathf.Clamp(move, -1f, 1f);
    }

    private bool IsRestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R))
            return true;
#endif
        return false;
    }

    private void OnGUI()
    {
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold
        };
        scoreStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(20, 20, 320, 40), $"Score: {score}", scoreStyle);

        scoreStyle.normal.textColor = new Color(1f, 0.55f, 0.55f);
        GUI.Label(new Rect(20, 58, 320, 40), $"Bombs: {bombs} / {MaxBombs}", scoreStyle);

        if (isGameOver)
        {
            float boxW = 380f;
            float boxH = 190f;
            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = (Screen.height - boxH) * 0.5f;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(0, 0, 25, 0)
            };

            GUI.Box(new Rect(boxX, boxY, boxW, boxH), $"GAME OVER (3 Bombs!)\nFinal Score: {score}", boxStyle);

            if (GUI.Button(new Rect(boxX + 90f, boxY + 125f, 200f, 42f), "Play Again (or press R)"))
            {
                RestartGame();
            }
        }
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color[] cols = new Color[16 * 16];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    private static Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, d <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}