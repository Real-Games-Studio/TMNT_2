using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenVestiario : CanvasScreen
{
    [Header("Tracking")]
    [Tooltip("Objects that are active when the face is tracked.")]
    [SerializeField] private List<GameObject> faceTrackedObjects;
    [Tooltip("How long the face must be lost before returning to the previous screen.")]
    [SerializeField] private float inactiveSecondsThreshold = 3f;
    [Tooltip("Delay after face is found again before inactivity check is re-enabled.")]
    [SerializeField] private float retrackingDelay = 1f;

    [Header("Wearables")]
    [SerializeField] private List<PositionTracker> positionTrackers;

    [Header("Screen Flow")]
    [Tooltip("How long the face must be tracked continuously to start the countdown.")]
    [SerializeField] private float trackingTimeThreshold = 2f;
    [Tooltip("The CanvasGroup that is initially visible.")]
    [SerializeField] private CanvasGroup initialGroup;
    [Tooltip("The CanvasGroup that shows the countdown text.")]
    [SerializeField] private CanvasGroup countdownGroup;

    [Header("Countdown")]
    [SerializeField] private Image countdownImage;
    [SerializeField] private Sprite[] countdownSprites;
    [Tooltip("Interval in seconds between sprite changes.")]
    [SerializeField] private float spriteChangeInterval = 1f;

    [Header("Countdown Pulse Animation")]
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.2f;

    [Header("Result")]
    [Tooltip("The RawImage to display the final screenshot.")]
    [SerializeField] private RawImage screenshotImage;

    [Header("Capture Feedback")]
    [Tooltip("Optional RawImage that will momentarily show the captured photo to \"freeze\" the webcam feed.")]
    [SerializeField] private RawImage freezeFrameImage;
    [Tooltip("AudioSource used to play a shutter sound when the photo is taken.")]
    [SerializeField] private AudioSource captureAudioSource;
    [Tooltip("Optional clip to play through the AudioSource when the photo is captured.")]
    [SerializeField] private AudioClip captureAudioClip;

    [Tooltip("CanvasGroup for flash effect when photo is captured.")]
    [SerializeField] private CanvasGroup flashEffect;
    [Tooltip("Duration of the flash effect in seconds.")]
    [SerializeField] private float flashDuration = 0.3f;

    [Header("Upload")]
    [Tooltip("PictureDataControlelr to handle upload and QR code generation.")]
    [SerializeField] private PictureDataControlelr pictureDataController;

    [Header("Camera Capture")]
    [Tooltip("RawImage que mostra a câmera ao vivo (para capturar apenas a webcam). Se deixar vazio, procura automaticamente.")]
    [SerializeField] private RawImage cameraFeedSource;
    [Tooltip("Se não encontrar RawImage com textura, captura a região da tela onde a câmera está visível")]
    [SerializeField] private RectTransform cameraRegion; // Área da tela onde a câmera é exibida
    private bool cameraFeedSearched = false;

    // Inactivity detection
    private readonly Dictionary<GameObject, float> inactiveTimers = new Dictionary<GameObject, float>();
    private bool previousScreenCalled = false;
    private float lastActiveTime = 0f;

    // Countdown flow
    private float continuousTrackingTimer = 0f;
    private Coroutine countdownCoroutine;
    private bool isCountingDown = false;
    private bool isScreenActive = false;
    private bool wasFaceTrackedLastFrame = false;


    void Update()
    {
        if (IsOn())
        {
            // One-time setup when screen becomes active
            if (!isScreenActive)
            {
                SetupScreen();
                isScreenActive = true;
            }

            bool isFaceCurrentlyTracked = IsFaceTracked();
            if (isFaceCurrentlyTracked && !wasFaceTrackedLastFrame)
            {
                // Face was just detected, randomize wearables
                foreach (var tracker in positionTrackers)
                {
                    if (tracker != null)
                    {
                        tracker.ActivateRandomChild();
                    }
                }
            }
            wasFaceTrackedLastFrame = isFaceCurrentlyTracked;

            // Always handle inactivity check
            HandleInactivityCheck();

            // Don't proceed with countdown flow if we are already counting down
            if (isCountingDown) return;

            if (isFaceCurrentlyTracked)
            {
                continuousTrackingTimer += Time.deltaTime;

                if (continuousTrackingTimer >= trackingTimeThreshold)
                {
                    if (countdownCoroutine == null)
                    {
                        countdownCoroutine = StartCoroutine(CountdownAndScreenshot());
                    }
                }
            }
            else
            {
                // Reset timer if tracking is lost
                continuousTrackingTimer = 0f;
            }
        }
        else
        {
            // Reset all state when the screen is no longer active
            if (isScreenActive)
            {
                ResetScreenState();
                isScreenActive = false;
            }
        }
    }

    private void SetupScreen()
    {
        if (initialGroup)
        {
            initialGroup.alpha = 1f;
            initialGroup.interactable = true;
            initialGroup.blocksRaycasts = true;
        }

        if (countdownGroup)
        {
            countdownGroup.gameObject.SetActive(false);
            countdownGroup.alpha = 0f;
            countdownGroup.interactable = false;
            countdownGroup.blocksRaycasts = false;
        }

        HideFreezeFrame();

        if (countdownImage)
        {
            countdownImage.sprite = null;
            countdownImage.enabled = false;
            countdownImage.gameObject.SetActive(false);
        }

        // Reset timers and flags
        continuousTrackingTimer = 0f;
        isCountingDown = false;
        lastActiveTime = Time.time; // Prevents immediate timeout on screen start
        previousScreenCalled = false;
        inactiveTimers.Clear();
        wasFaceTrackedLastFrame = false;
    }

    private void ResetScreenState()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        inactiveTimers.Clear();
        previousScreenCalled = false;
        lastActiveTime = 0f;
        continuousTrackingTimer = 0f;
        isCountingDown = false;
        wasFaceTrackedLastFrame = false;
    }

    private void HideFreezeFrame()
    {
        if (freezeFrameImage)
        {
            freezeFrameImage.texture = null;
            freezeFrameImage.gameObject.SetActive(false);
        }

        if (screenshotImage && screenshotImage != freezeFrameImage)
        {
            screenshotImage.texture = null;
            screenshotImage.gameObject.SetActive(false);
        }
    }

    private void ShowFreezeFrame(Texture2D capturedTexture)
    {
        if (capturedTexture == null) return;

        if (freezeFrameImage)
        {
            freezeFrameImage.texture = capturedTexture;
            freezeFrameImage.gameObject.SetActive(true);
        }

        if (screenshotImage)
        {
            screenshotImage.texture = capturedTexture;
            screenshotImage.gameObject.SetActive(true);
        }
    }

    private void PlayCaptureAudio()
    {
        if (captureAudioSource == null) return;

        if (captureAudioClip != null)
        {
            captureAudioSource.PlayOneShot(captureAudioClip);
        }
        else
        {
            if (captureAudioSource.isPlaying)
            {
                captureAudioSource.Stop();
            }
            captureAudioSource.Play();
        }
    }

    private void HideFlashEffect()
    {
        if (flashEffect)
        {
            flashEffect.alpha = 0f;
            flashEffect.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowFlashEffect()
    {
        if (flashEffect)
        {
            flashEffect.gameObject.SetActive(true);
            flashEffect.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                flashEffect.alpha = 1f - (elapsed / flashDuration);
                yield return null;
            }

            flashEffect.alpha = 0f;
            flashEffect.gameObject.SetActive(false);
        }
    }


    private bool IsFaceTracked()
    {
        if (faceTrackedObjects == null || faceTrackedObjects.Count == 0) return false;

        foreach (var obj in faceTrackedObjects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                return true; // At least one tracking object is active, so face is tracked
            }
        }
        return false; // No active tracking objects found
    }

    private void HandleInactivityCheck()
    {
        if (faceTrackedObjects == null || faceTrackedObjects.Count == 0) return;

        bool allObjectsInactive = true;

        foreach (var obj in faceTrackedObjects)
        {
            if (obj == null) continue;
            if (!inactiveTimers.ContainsKey(obj)) inactiveTimers[obj] = 0f;

            if (obj.activeInHierarchy)
            {
                allObjectsInactive = false;
                inactiveTimers[obj] = 0f;
                lastActiveTime = Time.time;
                if (previousScreenCalled) previousScreenCalled = false;
            }
            else
            {
                inactiveTimers[obj] += Time.deltaTime;
            }
        }

        bool canTimeout = (Time.time - lastActiveTime) > retrackingDelay;

        if (!previousScreenCalled && allObjectsInactive && canTimeout)
        {
            bool allTimersExceededThreshold = true;
            foreach (var timer in inactiveTimers.Values)
            {
                if (timer < inactiveSecondsThreshold)
                {
                    allTimersExceededThreshold = false;
                    break;
                }
            }

            if (allTimersExceededThreshold)
            {
                previousScreenCalled = true;
                CallPreviusScreen();
            }
        }
    }
    private IEnumerator PulseCountdown()
    {
        if (countdownImage == null) yield break;

        Vector3 originalScale = countdownImage.transform.localScale;
        Vector3 targetScale = originalScale * pulseScale;

        float t = 0f;
        // Cresce
        while (t < 1f)
        {
            t += Time.deltaTime / (pulseDuration / 2f);
            countdownImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        t = 0f;
        // Volta
        while (t < 1f)
        {
            t += Time.deltaTime / (pulseDuration / 2f);
            countdownImage.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        countdownImage.transform.localScale = originalScale;
    }

    private IEnumerator CountdownAndScreenshot()
    {
        isCountingDown = true;
        continuousTrackingTimer = 0f;

        if (initialGroup)
        {
            initialGroup.alpha = 0f;
            initialGroup.interactable = false;
            initialGroup.blocksRaycasts = false;
        }

        bool hasCountdownSprites = countdownImage && countdownSprites != null && countdownSprites.Length > 0;

        if (countdownGroup)
        {
            if (hasCountdownSprites)
            {
                countdownGroup.gameObject.SetActive(true);
                countdownGroup.alpha = 1f;
                countdownGroup.interactable = true;
                countdownGroup.blocksRaycasts = true;
            }
            else
            {
                countdownGroup.alpha = 0f;
                countdownGroup.interactable = false;
                countdownGroup.blocksRaycasts = false;
                countdownGroup.gameObject.SetActive(false);
            }
        }

        if (hasCountdownSprites)
        {
            countdownImage.gameObject.SetActive(true);
            countdownImage.enabled = true;
            Debug.Log($"[ScreenVestiario] Starting countdown with {countdownSprites.Length} sprites, interval: {spriteChangeInterval}s");

            for (int i = 0; i < countdownSprites.Length; i++)
            {
                // NA PRIMEIRA SPRITE: Procura a câmera AGORA (quando está visível)
                if (i == 0 && cameraFeedSource == null && !cameraFeedSearched)
                {
                    Debug.Log("[ScreenVestiario] Primeira sprite do countdown - procurando câmera AGORA (durante countdown visível)...");
                    cameraFeedSource = FindCameraRawImage();
                    cameraFeedSearched = true;

                    if (cameraFeedSource != null)
                    {
                        Debug.Log($"[ScreenVestiario] ✓ Câmera encontrada durante countdown: {cameraFeedSource.name}");
                    }
                    else
                    {
                        Debug.LogError("[ScreenVestiario] ✗ Câmera NÃO encontrada durante countdown!");
                    }
                }

                Sprite currentSprite = countdownSprites[i];
                if (currentSprite == null)
                {
                    Debug.LogWarning($"[ScreenVestiario] Countdown sprite at index {i} is null - SKIPPING");
                    continue;
                }

                // Atualiza a sprite
                countdownImage.sprite = currentSprite;
                countdownImage.SetNativeSize();
                Debug.Log($"[ScreenVestiario] Showing sprite {i + 1}/{countdownSprites.Length}: {currentSprite.name}");

                // Inicia a animação de pulso
                StartCoroutine(PulseCountdown());

                // Aguarda alguns frames para garantir que a sprite foi aplicada
                yield return new WaitForEndOfFrame();

                // Verifica se o rosto ainda está sendo rastreado
                if (!IsFaceTracked())
                {
                    Debug.LogWarning("[ScreenVestiario] Face lost during countdown - resetting");
                    ResetToInitialState();
                    yield break;
                }

                // Aguarda o intervalo antes de mostrar a próxima sprite
                // (exceto na última sprite)
                if (i < countdownSprites.Length - 1)
                {
                    if (spriteChangeInterval > 0f)
                    {
                        Debug.Log($"[ScreenVestiario] Waiting {spriteChangeInterval}s before next sprite...");
                        yield return new WaitForSeconds(spriteChangeInterval);
                    }

                    // Verifica novamente após o intervalo
                    if (!IsFaceTracked())
                    {
                        Debug.LogWarning("[ScreenVestiario] Face lost during countdown interval - resetting");
                        ResetToInitialState();
                        yield break;
                    }
                }
            }

            Debug.Log("[ScreenVestiario] Countdown finished");
        }
        else
        {
            Debug.LogWarning("ScreenVestiario countdown sprites are not configured.");
        }

        // FOTO 1: Captura apenas a câmera (sem UI) ANTES de esconder qualquer coisa
        // A câmera ainda está visível neste ponto
        Debug.Log("[ScreenVestiario] Tentando capturar apenas a câmera...");
        Texture2D cameraOnlyTexture = CaptureCameraOnly();

        // Agora sim esconde o countdown
        if (countdownImage)
        {
            countdownImage.sprite = null;
            countdownImage.enabled = false;
            countdownImage.gameObject.SetActive(false);
        }
        if (cameraOnlyTexture != null)
        {
            if (ScreenshotHolder.CameraOnlyTexture != null) Destroy(ScreenshotHolder.CameraOnlyTexture);
            ScreenshotHolder.CameraOnlyTexture = cameraOnlyTexture;
            Debug.Log($"[ScreenVestiario] ✓ Foto APENAS da câmera capturada: {cameraOnlyTexture.width}x{cameraOnlyTexture.height}");
        }
        else
        {
            Debug.LogError("[ScreenVestiario] ✗ FALHA ao capturar foto da câmera! Verifique se 'cameraFeedSource' está configurado no Inspector.");
        }

        yield return new WaitForEndOfFrame();

        // FOTO 2: Captura screenshot completo (com UI) para upload
        Debug.Log("[ScreenVestiario] Capturando tela completa com UI...");
        Texture2D srgbScreenshot = ScreenCapture.CaptureScreenshotAsTexture();

        // Create a new texture marked as linear and copy the pixels to correct color
        Texture2D linearScreenshot = new Texture2D(srgbScreenshot.width, srgbScreenshot.height, TextureFormat.ARGB32, false, true);
        Color32[] pixels = srgbScreenshot.GetPixels32();
        linearScreenshot.SetPixels32(pixels);
        linearScreenshot.Apply();

        // Clean up the original texture
        Destroy(srgbScreenshot);

        // Store the corrected texture for the final screen (mantido para compatibilidade)
        if (ScreenshotHolder.ScreenshotTexture != null) Destroy(ScreenshotHolder.ScreenshotTexture);
        ScreenshotHolder.ScreenshotTexture = linearScreenshot;

        // Send to PictureDataController for upload (TELA COMPLETA COM UI)
        if (pictureDataController != null)
        {
            pictureDataController.SetCapturedTexture(linearScreenshot);
            Debug.Log("[ScreenVestiario] Foto completa (com UI) enviada para upload");
        }

        // Show flash effect
        StartCoroutine(ShowFlashEffect());

        // Play capture audio
        PlayCaptureAudio();

        // Mostra o freeze frame da câmera (sem UI) no ScreenVestiario
        if (freezeFrameImage != null && cameraOnlyTexture != null)
        {
            freezeFrameImage.texture = cameraOnlyTexture;
            freezeFrameImage.gameObject.SetActive(true);
            Debug.Log("[ScreenVestiario] Mostrando freeze frame da câmera (sem UI)");
        }

        if (countdownGroup)
        {
            countdownGroup.alpha = 0f;
            countdownGroup.interactable = false;
            countdownGroup.blocksRaycasts = false;
            countdownGroup.gameObject.SetActive(false);
        }

        // Aguarda 1 segundo mostrando o freeze frame e vai para a próxima tela
        yield return new WaitForSeconds(1f);
        CallNextScreen();
    }

    private void ResetToInitialState()
    {
        HideFreezeFrame();
        HideFlashEffect();

        if (initialGroup)
        {
            initialGroup.alpha = 1f;
            initialGroup.interactable = true;
            initialGroup.blocksRaycasts = true;
        }

        if (countdownGroup)
        {
            countdownGroup.alpha = 0f;
            countdownGroup.interactable = false;
            countdownGroup.blocksRaycasts = false;
            countdownGroup.gameObject.SetActive(false);
        }

        if (countdownImage)
        {
            countdownImage.enabled = false;
            countdownImage.sprite = null;
            countdownImage.gameObject.SetActive(false);
        }

        isCountingDown = false;
        continuousTrackingTimer = 0f;
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    /// <summary>
    /// Captura apenas a textura da webcam (sem UI, overlays, etc.)
    /// A webcam é renderizada via OpenCV/WebGL como VideoBackground (quad 3D).
    /// Para capturar sem UI: desativa o Canvas, captura a tela (só fica a câmera), e reativa o Canvas.
    /// </summary>
    private Texture2D CaptureCameraOnly()
    {
        Debug.Log($"[CaptureCameraOnly] Iniciando captura APENAS da câmera (sem UI)...");

        // Encontra o Canvas principal
        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[CaptureCameraOnly] ✗ Canvas não encontrado!");
            return null;
        }

        Debug.Log($"[CaptureCameraOnly] Canvas encontrado: '{mainCanvas.name}'");
        Debug.Log($"[CaptureCameraOnly] Canvas ativo antes: {mainCanvas.enabled}");

        // Desativa o Canvas para esconder TODA a UI
        mainCanvas.enabled = false;
        Debug.Log("[CaptureCameraOnly] Canvas DESATIVADO - agora só a câmera está visível");

        // Aguarda 1 frame para garantir que a UI foi escondida
        // (não pode usar yield return aqui pois não é coroutine, mas vamos forçar um render)
        Canvas.ForceUpdateCanvases();

        // Captura a tela (agora SÓ tem a câmera, sem UI)
        Texture2D cameraOnlyScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log($"[CaptureCameraOnly] Screenshot SEM UI capturado: {cameraOnlyScreenshot.width}x{cameraOnlyScreenshot.height}");

        // REATIVA o Canvas imediatamente
        mainCanvas.enabled = true;
        Debug.Log("[CaptureCameraOnly] Canvas REATIVADO - UI restaurada");

        // Converte para formato linear (igual faz no screenshot completo)
        Texture2D linearTexture = new Texture2D(cameraOnlyScreenshot.width, cameraOnlyScreenshot.height, TextureFormat.ARGB32, false, true);
        Color32[] pixels = cameraOnlyScreenshot.GetPixels32();
        linearTexture.SetPixels32(pixels);
        linearTexture.Apply();

        Destroy(cameraOnlyScreenshot);

        Debug.Log($"[CaptureCameraOnly] ✓ Foto APENAS da câmera capturada com sucesso: {linearTexture.width}x{linearTexture.height}");
        return linearTexture;
    }

    /// <summary>
    /// Captura a textura de um RawImage (método antigo)
    /// </summary>
    private Texture2D CaptureFromRawImageTexture(RawImage rawImage)
    {
        Texture cameraTexture = rawImage.texture;
        Debug.Log($"[CaptureFromRawImageTexture] Capturando de textura {cameraTexture.width}x{cameraTexture.height}");

        Texture2D cameraSnapshot = new Texture2D(cameraTexture.width, cameraTexture.height, TextureFormat.RGB24, false);

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture tempRT = RenderTexture.GetTemporary(cameraTexture.width, cameraTexture.height);

        Graphics.Blit(cameraTexture, tempRT);
        RenderTexture.active = tempRT;

        cameraSnapshot.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        cameraSnapshot.Apply();

        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(tempRT);

        Debug.Log($"[CaptureFromRawImageTexture] ✓ Capturado: {cameraSnapshot.width}x{cameraSnapshot.height}");
        return cameraSnapshot;
    }

    /// <summary>
    /// Captura uma região específica da tela baseado em um RectTransform
    /// </summary>
    private Texture2D CaptureScreenRegion(RectTransform region)
    {
        if (region == null)
        {
            Debug.LogError("[CaptureScreenRegion] ✗ Region é NULL!");
            return null;
        }

        // Converte a posição do RectTransform para coordenadas de tela
        Canvas canvas = region.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CaptureScreenRegion] ✗ Não encontrou Canvas pai!");
            return null;
        }

        // Pega os cantos do RectTransform em coordenadas de tela
        Vector3[] corners = new Vector3[4];
        region.GetWorldCorners(corners);

        // Converte para coordenadas de tela
        Camera cam = canvas.worldCamera ?? Camera.main;
        Vector2 min = cam.WorldToScreenPoint(corners[0]);
        Vector2 max = cam.WorldToScreenPoint(corners[2]);

        // Calcula a região a ser capturada
        int x = Mathf.RoundToInt(min.x);
        int y = Mathf.RoundToInt(min.y);
        int width = Mathf.RoundToInt(max.x - min.x);
        int height = Mathf.RoundToInt(max.y - min.y);

        Debug.Log($"[CaptureScreenRegion] Região calculada: x={x}, y={y}, w={width}, h={height}");
        Debug.Log($"[CaptureScreenRegion] Tela: {Screen.width}x{Screen.height}");

        // Garante que está dentro dos limites da tela
        x = Mathf.Clamp(x, 0, Screen.width);
        y = Mathf.Clamp(y, 0, Screen.height);
        width = Mathf.Clamp(width, 1, Screen.width - x);
        height = Mathf.Clamp(height, 1, Screen.height - y);

        // Captura a tela inteira primeiro
        Texture2D fullScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log($"[CaptureScreenRegion] Screenshot completo: {fullScreenshot.width}x{fullScreenshot.height}");

        // Recorta apenas a região da câmera
        Texture2D croppedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color[] pixels = fullScreenshot.GetPixels(x, y, width, height);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();

        // Limpa o screenshot completo
        Destroy(fullScreenshot);

        Debug.Log($"[CaptureScreenRegion] ✓ Região capturada: {croppedTexture.width}x{croppedTexture.height}");
        return croppedTexture;
    }

    /// <summary>
    /// Encontra qualquer RawImage no Vestiario para usar como referência
    /// </summary>
    private RawImage FindAnyRawImageInVestiario()
    {
        Debug.Log("[FindAnyRawImageInVestiario] Procurando qualquer RawImage no Vestiario...");

        // Procura RawImages filhos deste GameObject
        RawImage[] rawImages = GetComponentsInChildren<RawImage>(true);

        foreach (RawImage rawImg in rawImages)
        {
            // Ignora o FreezeFrame
            if (rawImg == freezeFrameImage) continue;

            Debug.Log($"[FindAnyRawImageInVestiario] ✓ Encontrado: '{rawImg.name}' em {GetGameObjectPath(rawImg.gameObject)}");
            return rawImg;
        }

        Debug.LogWarning("[FindAnyRawImageInVestiario] ✗ Nenhum RawImage encontrado!");
        return null;
    }

    /// <summary>
    /// Procura automaticamente por um RawImage que tenha uma textura ativa (webcam)
    /// </summary>
    private RawImage FindCameraRawImage()
    {
        Debug.Log("[FindCameraRawImage] Procurando por RawImages na cena (incluindo inativos)...");

        // Procura TODOS os RawImages na cena (incluindo inativos)
        RawImage[] allRawImages = Resources.FindObjectsOfTypeAll<RawImage>();
        Debug.Log($"[FindCameraRawImage] Encontrados {allRawImages.Length} RawImages TOTAL na cena");

        // Log detalhado de CADA RawImage encontrado
        for (int i = 0; i < allRawImages.Length; i++)
        {
            RawImage rawImg = allRawImages[i];
            if (rawImg == null) continue;

            bool isActive = rawImg.gameObject.activeInHierarchy;
            bool hasTexture = rawImg.texture != null;
            string texInfo = hasTexture ? $"{rawImg.texture.width}x{rawImg.texture.height}" : "NULL";
            string path = GetGameObjectPath(rawImg.gameObject);

            Debug.Log($"[FindCameraRawImage] [{i}] '{rawImg.name}' | Ativo: {isActive} | Textura: {texInfo} | Path: {path}");

            // Procura o primeiro RawImage com textura ativa (independente se está ativo ou não)
            if (hasTexture)
            {
                Debug.Log($"[FindCameraRawImage] ✓ Usando RawImage '{rawImg.name}' com textura {texInfo}");
                return rawImg;
            }
        }

        Debug.LogWarning("[FindCameraRawImage] ✗ Nenhum RawImage com textura foi encontrado!");
        return null;
    }

    /// <summary>
    /// Helper para obter o caminho completo de um GameObject na hierarquia
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
