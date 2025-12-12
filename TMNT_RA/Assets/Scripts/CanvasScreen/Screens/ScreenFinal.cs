using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFinal : CanvasScreen
{
    [SerializeField] private float timeout = 15f;
    [SerializeField] private RawImage screenshotDisplay;
    [SerializeField] private bool preserveScreenshotAspect = true;

    [Header("Camera Feed Settings")]
    [Tooltip("WebCamTexture ou RawImage que mostra a câmera ao vivo")]
    [SerializeField] private RawImage cameraFeedSource;

    [Header("Screenshot Settings")]
    [SerializeField] private QrCodeCreator qrCodeCreator;
    [SerializeField] private Canvas targetCanvas; // Canvas onde está a área a ser capturada
    [SerializeField] private RectTransform screenshotArea; // Área específica para capturar
    [SerializeField] private Camera screenshotCamera; // Câmera temporária para captura (será criada se null)
    [SerializeField] private bool useFullScreen = false; // Se true, captura tela toda; se false, usa a área específica

    [Header("Upload Settings")]
    [SerializeField] private string projectCode = "bobsandy";
    [SerializeField] private string uploadUrl = "https://realgamesstudio.com.br/api/media/upload";
    [SerializeField] private bool autoUploadOnShow = false; // Desabilitado - upload já feito no ScreenVestiario

    private float timer;
    private bool isScreenActive = false;
    private bool screenshotTaken = false;
    private RenderMode originalRenderMode;
    private Camera originalWorldCamera;
    private Camera tempCamera;
    private Vector2 screenshotDisplayBaseSize;
    private bool hasScreenshotDisplayBaseSize = false;

    void Update()
    {
        if (IsOn())
        {
            if (!isScreenActive)
            {
                // This block now acts as OnShow
                // Captura um snapshot congelado apenas da câmera (sem UI)
                StartCoroutine(CaptureCameraSnapshotOnShow());

                timer = 0f;
                screenshotTaken = false;

                isScreenActive = true;
            }

            timer += Time.deltaTime;
            if (timer >= timeout)
            {
                // Clean up the texture before resetting
                if (ScreenshotHolder.ScreenshotTexture != null)
                {
                    Destroy(ScreenshotHolder.ScreenshotTexture);
                    ScreenshotHolder.ScreenshotTexture = null;
                }
                SceneManager.LoadScene(0);
            }
        }
        else
        {
            if (isScreenActive)
            {
                isScreenActive = false;
                screenshotTaken = false;
                RestoreScreenshotDisplaySize();
            }
        }
    }

    /// <summary>
    /// Captura um snapshot congelado apenas da câmera (sem UI) e exibe no screenshotDisplay
    /// </summary>
    private IEnumerator CaptureCameraSnapshotOnShow()
    {
        yield return new WaitForEndOfFrame();

        if (cameraFeedSource == null || screenshotDisplay == null)
        {
            Debug.LogWarning("[ScreenFinal] cameraFeedSource ou screenshotDisplay não configurados!");
            yield break;
        }

        // Pega a textura da webcam diretamente do RawImage da câmera
        Texture cameraTexture = cameraFeedSource.texture;

        if (cameraTexture == null)
        {
            Debug.LogWarning("[ScreenFinal] Camera feed não tem textura ativa!");
            yield break;
        }

        // Cria uma cópia congelada (snapshot) da textura da câmera
        Texture2D cameraSnapshot = new Texture2D(cameraTexture.width, cameraTexture.height, TextureFormat.RGB24, false);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture tempRT = RenderTexture.GetTemporary(cameraTexture.width, cameraTexture.height);

        Graphics.Blit(cameraTexture, tempRT);
        RenderTexture.active = tempRT;

        cameraSnapshot.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        cameraSnapshot.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(tempRT);

        // Aplica o snapshot congelado da câmera no display
        ApplyScreenshotTexture(cameraSnapshot);

        Debug.Log($"[ScreenFinal] Snapshot da câmera capturado e exibido (congelado): {cameraSnapshot.width}x{cameraSnapshot.height}");
    }

    private void ApplyScreenshotTexture(Texture texture)
    {
        if (screenshotDisplay == null || texture == null) return;

        CacheScreenshotDisplayBaseSize();

        screenshotDisplay.texture = texture;
        screenshotDisplay.gameObject.SetActive(true);

        if (preserveScreenshotAspect)
        {
            AdjustScreenshotDisplaySize(texture);
        }
    }

    private void CacheScreenshotDisplayBaseSize()
    {
        if (screenshotDisplay == null || hasScreenshotDisplayBaseSize) return;

        RectTransform rectTransform = screenshotDisplay.rectTransform;
        Vector2 size = rectTransform.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = rectTransform.sizeDelta;
        }

        if (size.x <= 0f || size.y <= 0f)
        {
            size = new Vector2(1f, 1f);
        }

        screenshotDisplayBaseSize = size;
        hasScreenshotDisplayBaseSize = true;
    }

    private void AdjustScreenshotDisplaySize(Texture texture)
    {
        if (screenshotDisplay == null || texture == null || !hasScreenshotDisplayBaseSize) return;

        RectTransform rectTransform = screenshotDisplay.rectTransform;
        Vector2 referenceSize = screenshotDisplayBaseSize;

        float referenceAspect = referenceSize.x / referenceSize.y;
        float textureAspect = (float)texture.width / texture.height;

        float targetWidth = referenceSize.x;
        float targetHeight = referenceSize.y;

        if (textureAspect > referenceAspect)
        {
            targetHeight = referenceSize.x / textureAspect;
        }
        else
        {
            targetWidth = referenceSize.y * textureAspect;
        }

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    private void RestoreScreenshotDisplaySize()
    {
        if (screenshotDisplay == null || !hasScreenshotDisplayBaseSize) return;

        RectTransform rectTransform = screenshotDisplay.rectTransform;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenshotDisplayBaseSize.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, screenshotDisplayBaseSize.y);
    }

    private void OnDestroy()
    {
        // Limpa a câmera temporária se foi criada
        if (tempCamera != null)
        {
            Destroy(tempCamera.gameObject);
        }
    }

    /// <summary>
    /// Captura screenshot da região específica e faz upload para o servidor via base64.
    /// </summary>
    private IEnumerator CaptureAndUploadScreenshot()
    {
        if (screenshotTaken)
        {
            yield break;
        }

        // Aguarda um frame para garantir que tudo foi renderizado
        yield return new WaitForEndOfFrame();

        // Captura o screenshot
        Texture2D screenshot = CaptureScreenshot();

        if (screenshot != null)
        {
            // Converte para PNG bytes
            byte[] pngBytes = screenshot.EncodeToPNG();

            // Limpa a textura da memória
            DestroyImmediate(screenshot);

            if (pngBytes != null)
            {
                // Faz upload via base64
                yield return StartCoroutine(UploadScreenshotBase64(pngBytes));
            }

            screenshotTaken = true;
        }
    }

    /// <summary>
    /// Captura screenshot baseado nas configurações (área específica ou tela completa).
    /// </summary>
    private Texture2D CaptureScreenshot()
    {
        if (useFullScreen || screenshotArea == null)
        {
            return CaptureFullScreen();
        }
        else
        {
            return CaptureSpecificArea();
        }
    }

    /// <summary>
    /// Captura a tela completa.
    /// </summary>
    private Texture2D CaptureFullScreen()
    {
        int width = Screen.width;
        int height = Screen.height;

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        Debug.Log($"[ScreenFinal] Capturou screenshot full screen: {width}x{height}");
        return screenshot;
    }

    /// <summary>
    /// Captura apenas a área específica, lidando com Canvas Overlay.
    /// </summary>
    private Texture2D CaptureSpecificArea()
    {
        if (screenshotArea == null)
        {
            Debug.LogWarning("[ScreenFinal] Screenshot area não está configurada! Usando tela completa.");
            return CaptureFullScreen();
        }

        // Se o Canvas estiver em Overlay, converte temporariamente para Camera
        bool needsRestore = PrepareCanvasForCapture();

        // Aguarda um frame para o Canvas se ajustar
        Canvas.ForceUpdateCanvases();

        // Captura a área específica
        Texture2D screenshot = CaptureAreaFromRect();

        // Restaura o Canvas ao estado original
        if (needsRestore)
        {
            RestoreCanvas();
        }

        return screenshot;
    }

    /// <summary>
    /// Prepara o Canvas para captura (converte Overlay para Camera se necessário).
    /// </summary>
    private bool PrepareCanvasForCapture()
    {
        if (targetCanvas == null)
        {
            // Tenta encontrar o Canvas automaticamente
            targetCanvas = screenshotArea.GetComponentInParent<Canvas>();

            if (targetCanvas == null)
            {
                Debug.LogWarning("[ScreenFinal] Nenhum Canvas encontrado!");
                return false;
            }
        }

        // Salva o estado original
        originalRenderMode = targetCanvas.renderMode;
        originalWorldCamera = targetCanvas.worldCamera;

        // Se já está em Screen Space - Camera, não precisa mudar
        if (originalRenderMode == RenderMode.ScreenSpaceCamera)
        {
            return false;
        }

        // Se está em Overlay, converte temporariamente para Camera
        if (originalRenderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Cria ou usa a câmera de screenshot
            Camera cam = GetOrCreateScreenshotCamera();

            targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            targetCanvas.worldCamera = cam;

            Debug.Log("[ScreenFinal] Canvas convertido temporariamente de Overlay para Camera");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Obtém ou cria uma câmera para captura de screenshot.
    /// </summary>
    private Camera GetOrCreateScreenshotCamera()
    {
        if (screenshotCamera != null)
        {
            return screenshotCamera;
        }

        // Se não tem câmera configurada, cria uma temporária
        if (tempCamera == null)
        {
            GameObject camObj = new GameObject("TempScreenshotCamera");
            tempCamera = camObj.AddComponent<Camera>();
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = new Color(0, 0, 0, 0);
            tempCamera.cullingMask = 1 << LayerMask.NameToLayer("UI"); // Só renderiza UI
            tempCamera.orthographic = true;
            tempCamera.depth = 100;
            tempCamera.enabled = false; // Não renderiza automaticamente

            Debug.Log("[ScreenFinal] Câmera temporária criada para captura");
        }

        return tempCamera;
    }

    /// <summary>
    /// Restaura o Canvas ao seu estado original.
    /// </summary>
    private void RestoreCanvas()
    {
        if (targetCanvas != null)
        {
            targetCanvas.renderMode = originalRenderMode;
            targetCanvas.worldCamera = originalWorldCamera;

            Debug.Log("[ScreenFinal] Canvas restaurado ao modo original");
        }
    }

    /// <summary>
    /// Captura apenas a área do RectTransform.
    /// </summary>
    private Texture2D CaptureAreaFromRect()
    {
        // Pega os corners do RectTransform em coordenadas de mundo
        Vector3[] corners = new Vector3[4];
        screenshotArea.GetWorldCorners(corners);

        // Calcula dimensões da área
        float width = Vector3.Distance(corners[0], corners[3]);
        float height = Vector3.Distance(corners[0], corners[1]);

        int pixelWidth = Mathf.RoundToInt(width);
        int pixelHeight = Mathf.RoundToInt(height);

        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            Debug.LogWarning($"[ScreenFinal] Dimensões inválidas ({pixelWidth}x{pixelHeight})! Usando tela completa.");
            return CaptureFullScreen();
        }

        // Converte corners para coordenadas de tela
        Camera cam = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("[ScreenFinal] Nenhuma câmera disponível! Usando captura direta.");
            return CaptureDirectFromScreen();
        }

        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        // Garante que está dentro dos limites
        min.x = Mathf.Clamp(min.x, 0, Screen.width);
        min.y = Mathf.Clamp(min.y, 0, Screen.height);
        max.x = Mathf.Clamp(max.x, 0, Screen.width);
        max.y = Mathf.Clamp(max.y, 0, Screen.height);

        int cropWidth = Mathf.RoundToInt(max.x - min.x);
        int cropHeight = Mathf.RoundToInt(max.y - min.y);

        if (cropWidth <= 0 || cropHeight <= 0)
        {
            Debug.LogWarning($"[ScreenFinal] Dimensões de crop inválidas! Usando tela completa.");
            return CaptureFullScreen();
        }

        // Captura a tela inteira
        Texture2D fullScreenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        fullScreenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        fullScreenshot.Apply();

        // Recorta apenas a área desejada
        Texture2D croppedScreenshot = new Texture2D(cropWidth, cropHeight, TextureFormat.RGB24, false);
        Color[] pixels = fullScreenshot.GetPixels(
            Mathf.RoundToInt(min.x),
            Mathf.RoundToInt(min.y),
            cropWidth,
            cropHeight
        );

        croppedScreenshot.SetPixels(pixels);
        croppedScreenshot.Apply();

        // Limpa a textura temporária
        DestroyImmediate(fullScreenshot);

        Debug.Log($"[ScreenFinal] Capturou área específica: {cropWidth}x{cropHeight} at ({min.x}, {min.y})");
        return croppedScreenshot;
    }

    /// <summary>
    /// Captura diretamente da tela (fallback).
    /// </summary>
    private Texture2D CaptureDirectFromScreen()
    {
        Rect rect = screenshotArea.rect;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        if (width <= 0 || height <= 0)
        {
            return CaptureFullScreen();
        }

        Vector2 pos = screenshotArea.position;

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(pos.x, pos.y, width, height), 0, 0);
        screenshot.Apply();

        return screenshot;
    }

    /// <summary>
    /// Faz upload do screenshot para o servidor usando base64 (sem salvar arquivo local).
    /// </summary>
    private IEnumerator UploadScreenshotBase64(byte[] pngBytes)
    {
        if (pngBytes == null || pngBytes.Length == 0)
        {
            Debug.LogError("[ScreenFinal] PNG bytes inválidos para upload.");
            yield break;
        }

        // Gera nome do arquivo com timestamp
        long unixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string fileName = $"final_{unixTime}.png";

        // Cria o formulário para upload
        WWWForm form = new WWWForm();
        form.AddField("mediaName", fileName);
        form.AddField("projectCode", projectCode);
        form.AddBinaryData("file", pngBytes, fileName, "image/png");

        // URL final do arquivo no servidor
        string fileUrl = $"https://realgamesstudio.com.br/dynamic-content/{projectCode}/{fileName}";

        Debug.Log($"[ScreenFinal] Iniciando upload de {fileName}...");

        // Cria e envia a requisição
        UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form);
        request.timeout = 10;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[ScreenFinal] Upload de {fileName} concluído com sucesso!");
            Debug.Log($"[ScreenFinal] URL: {fileUrl}");

            // Gera o QR Code com a URL da imagem
            if (qrCodeCreator != null)
            {
                qrCodeCreator.CreateAndShowBarcode(fileUrl);
                Debug.Log($"[ScreenFinal] QR Code gerado para: {fileUrl}");
            }

#if UNITY_EDITOR
            Application.OpenURL(fileUrl);
#endif
        }
        else
        {
            Debug.LogError($"[ScreenFinal] Falha no upload de {fileName}: {request.error}");
            Debug.LogError($"[ScreenFinal] Resultado: {request.result}");
        }
    }
}
