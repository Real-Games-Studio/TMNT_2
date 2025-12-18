using UnityEngine;
using System.Collections;

/// <summary>
/// Mostra informações sobre as webcams disponíveis e suas resoluções.
/// NOTA: Funciona apenas no Unity Editor ou builds standalone. No WebGL, as informações
/// vêm do navegador através do ARCamera.
/// </summary>
public class WebCamInfo : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool logOnStart = true;
    [SerializeField] private bool logDetailedInfo = true;

    private void Start()
    {
        if (logOnStart)
        {
            StartCoroutine(LogWebCamInfoCoroutine());
        }
    }

    private IEnumerator LogWebCamInfoCoroutine()
    {
        // Aguarda um pouco para garantir que o sistema de câmera foi inicializado
        yield return new WaitForSeconds(0.5f);

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("╔═══════════════════════════════════════════════════════════╗");
        Debug.Log("║ 📹 WEBCAM INFO (WebGL Build)");
        Debug.Log("╠═══════════════════════════════════════════════════════════╣");
        Debug.Log("║ ⚠ No WebGL, a resolução da câmera é controlada pelo navegador");
        Debug.Log("║ A resolução real será mostrada pelo CameraResolutionLogger");
        Debug.Log("║ quando o ARCamera receber o evento OnResized");
        Debug.Log("╚═══════════════════════════════════════════════════════════╝");
#else
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogWarning("╔═══════════════════════════════════════════════════════════╗");
            Debug.LogWarning("║ ⚠ NENHUMA WEBCAM DETECTADA");
            Debug.LogWarning("╚═══════════════════════════════════════════════════════════╝");
            yield break;
        }

        Debug.Log("╔═══════════════════════════════════════════════════════════╗");
        Debug.Log($"║ 📹 WEBCAMS DISPONÍVEIS ({devices.Length} encontrada(s))");
        Debug.Log("╠═══════════════════════════════════════════════════════════╣");

        for (int i = 0; i < devices.Length; i++)
        {
            WebCamDevice device = devices[i];
            Debug.Log($"║ [{i}] {device.name}");
            Debug.Log($"║     Frontal: {(device.isFrontFacing ? "SIM" : "NÃO")}");
            Debug.Log($"║     Disponível: {(device.isAutoFocusPointSupported ? "Auto-focus suportado" : "Auto-focus não suportado")}");

            if (logDetailedInfo)
            {
                // Testa resoluções comuns para ver quais são suportadas
                Debug.Log($"║     Testando resoluções suportadas...");
                yield return StartCoroutine(TestResolutions(device.name));
            }

            if (i < devices.Length - 1)
            {
                Debug.Log("╟───────────────────────────────────────────────────────────╢");
            }
        }

        Debug.Log("╚═══════════════════════════════════════════════════════════╝");
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private IEnumerator TestResolutions(string deviceName)
    {
        // Resoluções comuns para testar
        Vector2Int[] testResolutions = new Vector2Int[]
        {
            new Vector2Int(640, 480),    // VGA
            new Vector2Int(1280, 720),   // 720p HD
            new Vector2Int(1920, 1080),  // 1080p Full HD
            new Vector2Int(2560, 1440),  // 1440p 2K
            new Vector2Int(3840, 2160),  // 2160p 4K
            new Vector2Int(1280, 960),   // 4:3 variant
            new Vector2Int(1600, 1200),  // UXGA
        };

        string supportedResolutions = "║     Suportadas: ";
        int count = 0;

        foreach (Vector2Int res in testResolutions)
        {
            WebCamTexture testCam = new WebCamTexture(deviceName, res.x, res.y, 30);
            testCam.Play();

            yield return new WaitForSeconds(0.1f);

            if (testCam.width > 0 && testCam.height > 0)
            {
                if (count > 0) supportedResolutions += ", ";
                supportedResolutions += $"{testCam.width}x{testCam.height}";
                count++;
            }

            testCam.Stop();
            Destroy(testCam);

            yield return null;
        }

        if (count == 0)
        {
            supportedResolutions += "Nenhuma testada com sucesso";
        }

        Debug.Log(supportedResolutions);
    }
#endif

    /// <summary>
    /// Loga as informações das webcams manualmente (útil para chamar via código).
    /// </summary>
    public void LogWebCamInfo()
    {
        StartCoroutine(LogWebCamInfoCoroutine());
    }
}
