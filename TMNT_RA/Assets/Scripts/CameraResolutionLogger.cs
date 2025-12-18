using UnityEngine;
using Imagine.WebAR;

/// <summary>
/// Monitora e loga a resolução da câmera WebAR.
/// Adicione este componente no mesmo GameObject que tem o ARCamera.
/// </summary>
public class CameraResolutionLogger : MonoBehaviour
{
    private ARCamera arCamera;
    private Vector2 lastResolution = Vector2.zero;
    private bool hasLoggedResolution = false;

    [Header("Debug")]
    [SerializeField] private bool logEveryFrame = false;
    [SerializeField] private bool logOnlyOnChange = true;

    private void Awake()
    {
        arCamera = GetComponent<ARCamera>();
        if (arCamera == null)
        {
            Debug.LogError("[CameraResolutionLogger] ARCamera não encontrado no GameObject! Este componente deve estar no mesmo objeto que ARCamera.");
            enabled = false;
            return;
        }

        // Se inscreve no evento de resize
        if (arCamera.OnResized != null)
        {
            arCamera.OnResized.AddListener(OnCameraResized);
        }
    }

    private void OnDestroy()
    {
        if (arCamera != null && arCamera.OnResized != null)
        {
            arCamera.OnResized.RemoveListener(OnCameraResized);
        }
    }

    private void OnCameraResized(Vector2 resolution)
    {
        if (resolution != lastResolution || !hasLoggedResolution)
        {
            LogResolution(resolution);
            lastResolution = resolution;
            hasLoggedResolution = true;
        }
    }

    private void Update()
    {
        if (logEveryFrame)
        {
            // Tenta obter a resolução atual da tela
            Vector2 currentResolution = new Vector2(Screen.width, Screen.height);

            if (!logOnlyOnChange || currentResolution != lastResolution)
            {
                LogResolution(currentResolution);
                lastResolution = currentResolution;
            }
        }
    }

    private void LogResolution(Vector2 resolution)
    {
        float megapixels = (resolution.x * resolution.y) / 1000000f;
        string aspectRatio = CalculateAspectRatio((int)resolution.x, (int)resolution.y);

        Debug.Log($"╔═══════════════════════════════════════════════════════════╗");
        Debug.Log($"║ 📷 RESOLUÇÃO DA CÂMERA");
        Debug.Log($"╠═══════════════════════════════════════════════════════════╣");
        Debug.Log($"║ Resolução: {resolution.x} x {resolution.y} pixels");
        Debug.Log($"║ Megapixels: {megapixels:F2} MP");
        Debug.Log($"║ Aspect Ratio: {aspectRatio}");
        Debug.Log($"║ Orientação: {(resolution.x > resolution.y ? "Landscape (Horizontal)" : "Portrait (Vertical)")}");
        Debug.Log($"╚═══════════════════════════════════════════════════════════╝");

        // Log adicional para facilitar busca
        Debug.Log($"[CAMERA_RESOLUTION] {resolution.x}x{resolution.y} | {megapixels:F2}MP | {aspectRatio}");
    }

    private string CalculateAspectRatio(int width, int height)
    {
        int gcd = GCD(width, height);
        int ratioWidth = width / gcd;
        int ratioHeight = height / gcd;

        // Identifica ratios comuns
        if (ratioWidth == 16 && ratioHeight == 9) return "16:9 (Widescreen)";
        if (ratioWidth == 4 && ratioHeight == 3) return "4:3 (Standard)";
        if (ratioWidth == 9 && ratioHeight == 16) return "9:16 (Mobile Portrait)";
        if (ratioWidth == 3 && ratioHeight == 4) return "3:4 (Mobile Portrait)";
        if (ratioWidth == 1 && ratioHeight == 1) return "1:1 (Square)";
        if (ratioWidth == 21 && ratioHeight == 9) return "21:9 (Ultrawide)";

        return $"{ratioWidth}:{ratioHeight}";
    }

    private int GCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    /// <summary>
    /// Força o log da resolução atual (útil para chamar manualmente).
    /// </summary>
    public void ForceLogResolution()
    {
        Vector2 currentResolution = new Vector2(Screen.width, Screen.height);
        LogResolution(currentResolution);
    }
}
