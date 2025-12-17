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
    [Tooltip("Objetos de UI que serão DESATIVADOS durante a captura da câmera (wearables, countdown, etc). A câmera ficará visível.")]
    [SerializeField] private List<GameObject> uiObjectsToHide;

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

            // Always handle inactivity check (exceto em modo debug)
            if (!DebugPositionAdjuster.IsDebugMode())
            {
                HandleInactivityCheck();
            }

            // Don't proceed with countdown flow if we are already counting down OR if in debug mode
            if (isCountingDown || DebugPositionAdjuster.IsDebugMode()) return;

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

        // Esconde o countdown antes de qualquer captura
        if (countdownImage)
        {
            countdownImage.sprite = null;
            countdownImage.enabled = false;
            countdownImage.gameObject.SetActive(false);
        }

        // ========== CAPTURA OTIMIZADA - SEM TRAVADAS ==========
        // Desativa os objetos de UI ANTES da captura para ter a foto limpa
        List<GameObject> disabledObjects = new List<GameObject>();
        if (uiObjectsToHide != null && uiObjectsToHide.Count > 0)
        {
            foreach (GameObject obj in uiObjectsToHide)
            {
                if (obj != null && obj.activeSelf)
                {
                    obj.SetActive(false);
                    disabledObjects.Add(obj);
                }
            }
            Debug.Log($"[ScreenVestiario] {disabledObjects.Count} objetos de UI desativados para foto limpa");
        }

        // Força atualização do Canvas
        Canvas.ForceUpdateCanvases();
        
        // Aguarda a renderização sem UI
        yield return new WaitForEndOfFrame();

        // FOTO 1: Captura SEM UI (para mostrar no ScreenFinal)
        Debug.Log("[ScreenVestiario] Capturando foto SEM UI...");
        Texture2D srgbScreenshotWithoutUI = ScreenCapture.CaptureScreenshotAsTexture();
        Texture2D cameraOnlyTexture = new Texture2D(srgbScreenshotWithoutUI.width, srgbScreenshotWithoutUI.height, TextureFormat.ARGB32, false, true);
        Color32[] pixelsWithoutUI = srgbScreenshotWithoutUI.GetPixels32();
        cameraOnlyTexture.SetPixels32(pixelsWithoutUI);
        cameraOnlyTexture.Apply();
        Destroy(srgbScreenshotWithoutUI);

        if (ScreenshotHolder.CameraOnlyTexture != null) Destroy(ScreenshotHolder.CameraOnlyTexture);
        ScreenshotHolder.CameraOnlyTexture = cameraOnlyTexture;
        Debug.Log($"[ScreenVestiario] ✓ Foto SEM UI capturada: {cameraOnlyTexture.width}x{cameraOnlyTexture.height}");

        // REATIVA os objetos de UI imediatamente
        foreach (GameObject obj in disabledObjects)
        {
            if (obj != null) obj.SetActive(true);
        }
        Canvas.ForceUpdateCanvases();
        
        // Aguarda a renderização COM UI
        yield return new WaitForEndOfFrame();

        // FOTO 2: Captura COM UI (para o PictureDataController fazer upload)
        Debug.Log("[ScreenVestiario] Capturando foto COM UI para upload...");
        Texture2D srgbScreenshotWithUI = ScreenCapture.CaptureScreenshotAsTexture();
        Texture2D linearScreenshotWithUI = new Texture2D(srgbScreenshotWithUI.width, srgbScreenshotWithUI.height, TextureFormat.ARGB32, false, true);
        Color32[] pixelsWithUI = srgbScreenshotWithUI.GetPixels32();
        linearScreenshotWithUI.SetPixels32(pixelsWithUI);
        linearScreenshotWithUI.Apply();
        Destroy(srgbScreenshotWithUI);

        // Send to PictureDataController for upload (COM UI)
        if (pictureDataController != null)
        {
            pictureDataController.SetCapturedTexture(linearScreenshotWithUI);
            Debug.Log("[ScreenVestiario] ✓ Foto COM UI enviada para upload");
        }

        // Store the screenshot with UI for compatibility
        if (ScreenshotHolder.ScreenshotTexture != null) Destroy(ScreenshotHolder.ScreenshotTexture);
        ScreenshotHolder.ScreenshotTexture = linearScreenshotWithUI;

        // Show flash effect
        StartCoroutine(ShowFlashEffect());

        // Play capture audio
        PlayCaptureAudio();

        // Mostra o freeze frame da câmera (SEM UI) no ScreenVestiario
        if (freezeFrameImage != null && cameraOnlyTexture != null)
        {
            freezeFrameImage.texture = cameraOnlyTexture;
            freezeFrameImage.gameObject.SetActive(true);
            Debug.Log("[ScreenVestiario] Mostrando freeze frame da câmera (SEM UI)");
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
    /// Desativa temporariamente os objetos configurados em uiObjectsToHide e captura a tela.
    /// NOTA: Os objetos permanecem desativados após a captura (são resetados ao trocar de tela).
    /// </summary>
    private Texture2D CaptureCameraOnly()
    {
        Debug.Log($"[CaptureCameraOnly] Iniciando captura APENAS da câmera (sem UI)...");

        if (uiObjectsToHide == null || uiObjectsToHide.Count == 0)
        {
            Debug.LogWarning("[CaptureCameraOnly] ⚠ Lista 'uiObjectsToHide' está vazia!");
            Debug.LogWarning("[CaptureCameraOnly] Configure os objetos de UI para esconder no Inspector (wearables, countdown, etc.)");
            Debug.LogWarning("[CaptureCameraOnly] Capturando tela completa como fallback...");

            // Fallback: captura a tela como está
            Texture2D fallbackScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
            Texture2D linearFallback = new Texture2D(fallbackScreenshot.width, fallbackScreenshot.height, TextureFormat.ARGB32, false, true);
            Color32[] pixels = fallbackScreenshot.GetPixels32();
            linearFallback.SetPixels32(pixels);
            linearFallback.Apply();
            Destroy(fallbackScreenshot);
            return linearFallback;
        }

        // Desativa os objetos configurados para a captura limpa
        int disabledCount = 0;
        foreach (GameObject obj in uiObjectsToHide)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                disabledCount++;
                Debug.Log($"[CaptureCameraOnly] Desativado: {obj.name}");
            }
        }

        Debug.Log($"[CaptureCameraOnly] {disabledCount} objetos de UI desativados - agora só a câmera está visível");

        // Força atualização do Canvas
        Canvas.ForceUpdateCanvases();

        // Captura a tela (agora SÓ tem a câmera, sem UI)
        Texture2D cameraOnlyScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log($"[CaptureCameraOnly] Screenshot SEM UI capturado: {cameraOnlyScreenshot.width}x{cameraOnlyScreenshot.height}");

        // Converte para formato linear (consistente com a outra captura)
        Texture2D linearTexture = new Texture2D(cameraOnlyScreenshot.width, cameraOnlyScreenshot.height, TextureFormat.ARGB32, false, true);
        Color32[] pixelsConverted = cameraOnlyScreenshot.GetPixels32();
        linearTexture.SetPixels32(pixelsConverted);
        linearTexture.Apply();

        Destroy(cameraOnlyScreenshot);

        Debug.Log($"[CaptureCameraOnly] ✓ Foto APENAS da câmera capturada com sucesso: {linearTexture.width}x{linearTexture.height}");
        return linearTexture;
    }

}
