using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PictureDataControlelr : MonoBehaviour
{
    [Header("Source & Preview")]
    [SerializeField] private Camera captureCamera; // Câmera para capturar a cena
    [SerializeField] private RenderTexture renderTexture; // RenderTexture opcional
    [SerializeField] private Image previewImage;
    [SerializeField] private QrCodeCreator qrCodeCreator;

    [Header("Upload Settings")]
    [SerializeField] private string projectCode = "bobsandy";
    [SerializeField] private string uploadUrl = "https://realgamesstudio.com.br/api/media/upload";
    [SerializeField] private bool autoUploadOnCapture = true;

    [Header("Buttons (optional)")]
    [SerializeField] private Button captureButton;
    [SerializeField] private Button uploadButton;
    [SerializeField] private Button downloadButton;

    private Texture2D lastCapturedTexture;

    private void Awake()
    {
        if (captureButton != null) captureButton.onClick.AddListener(OnTakePictureClicked);
        if (uploadButton != null) uploadButton.onClick.AddListener(OnUploadClicked);
        // if (downloadButton != null) downloadButton.onClick.AddListener(OnDownloadClicked);
    }

    private void OnDestroy()
    {
        if (captureButton != null) captureButton.onClick.RemoveListener(OnTakePictureClicked);
        if (uploadButton != null) uploadButton.onClick.RemoveListener(OnUploadClicked);
        // if (downloadButton != null) downloadButton.onClick.RemoveListener(OnDownloadClicked);

        // Limpa a textura ao destruir
        if (lastCapturedTexture != null)
        {
            Destroy(lastCapturedTexture);
            lastCapturedTexture = null;
        }
    }

    /// <summary>
    /// Define uma textura já capturada e inicia o processo de upload se configurado.
    /// </summary>
    public void SetCapturedTexture(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("[PictureDataControlelr] Tentativa de definir textura nula.");
            return;
        }

        // Limpa a textura anterior
        if (lastCapturedTexture != null)
        {
            Destroy(lastCapturedTexture);
        }

        // Cria uma cópia da textura para evitar problemas de referência
        lastCapturedTexture = new Texture2D(texture.width, texture.height, texture.format, false);
        Graphics.CopyTexture(texture, lastCapturedTexture);

        UpdatePreview();

        Debug.Log("[PictureDataControlelr] Textura recebida do ScreenVestiario!");

        // Se configurado, faz upload automaticamente
        if (autoUploadOnCapture)
        {
            StartCoroutine(UploadCoroutine());
        }
    }

    public void OnTakePictureClicked()
    {
        StartCoroutine(TakePictureCoroutine());
    }

    private IEnumerator TakePictureCoroutine()
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = CaptureFromCamera();

        if (screenshot != null)
        {
            // Limpa a textura anterior
            if (lastCapturedTexture != null)
            {
                Destroy(lastCapturedTexture);
            }

            lastCapturedTexture = screenshot;
            UpdatePreview();

            Debug.Log("[PictureDataControlelr] Foto capturada com sucesso!");

            // Se configurado, faz upload automaticamente
            if (autoUploadOnCapture)
            {
                StartCoroutine(UploadCoroutine());
            }
        }
    }

    /// <summary>
    /// Captura imagem da câmera ou RenderTexture.
    /// </summary>
    private Texture2D CaptureFromCamera()
    {
        if (captureCamera == null)
        {
            Debug.LogError("[PictureDataControlelr] Capture Camera não está configurada!");
            return null;
        }

        // Se tem RenderTexture configurada, usa ela
        if (renderTexture != null)
        {
            return CaptureFromRenderTexture();
        }

        // Senão, captura direto da tela
        return CaptureFromScreen();
    }

    /// <summary>
    /// Captura de uma RenderTexture existente.
    /// </summary>
    private Texture2D CaptureFromRenderTexture()
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenshot.Apply();

        RenderTexture.active = currentRT;

        Debug.Log($"[PictureDataControlelr] Capturou de RenderTexture: {renderTexture.width}x{renderTexture.height}");
        return screenshot;
    }

    /// <summary>
    /// Captura diretamente da tela.
    /// </summary>
    private Texture2D CaptureFromScreen()
    {
        int width = Screen.width;
        int height = Screen.height;

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        Debug.Log($"[PictureDataControlelr] Capturou da tela: {width}x{height}");
        return screenshot;
    }

    /// <summary>
    /// Atualiza o preview da UI com a última foto capturada.
    /// </summary>
    public void UpdatePreview()
    {
        if (previewImage == null || lastCapturedTexture == null) return;

        Sprite sprite = Sprite.Create(
            lastCapturedTexture,
            new Rect(0, 0, lastCapturedTexture.width, lastCapturedTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        previewImage.sprite = sprite;
        previewImage.color = Color.white;
    }

    private void OnUploadClicked()
    {
        StartCoroutine(UploadCoroutine());
    }

    private IEnumerator UploadCoroutine()
    {
        if (lastCapturedTexture == null)
        {
            Debug.LogError("[PictureDataControlelr] Nenhuma foto para fazer upload.");
            yield break;
        }

        byte[] png = lastCapturedTexture.EncodeToPNG();

        long unixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string fileName = $"screenshot_{unixTime}.png";

        WWWForm form = new WWWForm();
        form.AddField("mediaName", fileName);
        form.AddField("projectCode", projectCode);
        form.AddBinaryData("file", png, fileName, "image/png");

        string fileUrl = $"https://realgamesstudio.com.br/dynamic-content/{projectCode}/{fileName}";

        Debug.Log($"[PictureDataControlelr] Iniciando upload de {fileName}...");

        UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form);
        request.timeout = 10;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[PictureDataControlelr] Upload de {fileName} concluído com sucesso!");
            Debug.Log($"[PictureDataControlelr] URL: {fileUrl}");

            // Gera o QR Code com o novo QrCodeCreator
            if (qrCodeCreator != null)
            {
                qrCodeCreator.CreateAndShowBarcode(fileUrl);
                Debug.Log($"[PictureDataControlelr] QR Code gerado para: {fileUrl}");
            }
            else
            {
                Debug.LogWarning("[PictureDataControlelr] QrCodeCreator não está configurado!");
            }

#if UNITY_EDITOR
            Application.OpenURL(fileUrl);
#endif
        }
        else
        {
            Debug.LogError($"[PictureDataControlelr] Falha no upload: {request.error}");
            Debug.LogError($"[PictureDataControlelr] Resultado: {request.result}");
        }
    }

    // #if UNITY_WEBGL && !UNITY_EDITOR
    //     [DllImport("__Internal")]
    //     private static extern int DownloadFileFromBase64(string filename, string base64);
    // #endif

    //     private void OnDownloadClicked()
    //     {
    //         Debug.Log("[PictureDataControlelr] Clicou no botão Download");

    // #if UNITY_WEBGL && !UNITY_EDITOR
    //         // No WebGL, o download deve ser acionado durante o gesto do usuário (mesmo tick),
    //         // caso contrário o navegador pode bloquear. Evita coroutine/yield aqui.
    //         if (lastCapturedTexture == null)
    //         {
    //             Debug.LogError("[PictureDataControlelr] Nenhuma foto para download.");
    //             return;
    //         }

    //         byte[] png = lastCapturedTexture.EncodeToPNG();
    //         string fileName = $"screenshot_{System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.png";

    //         try
    //         {
    //             string base64 = System.Convert.ToBase64String(png);
    //             DownloadFileFromBase64(fileName, base64);
    //             Debug.Log($"[PictureDataControlelr] Download do navegador acionado para {fileName}");
    //         }
    //         catch (System.Exception e)
    //         {
    //             Debug.LogError($"[PictureDataControlelr] Falha ao acionar download WebGL: {e.Message}");
    //         }
    //         return;
    // #else
    //         StartCoroutine(DownloadToDiskCoroutine());
    // #endif
    //     }

    /// <summary>
    /// Salva a última foto localmente e abre a pasta.
    /// </summary>
    //     private IEnumerator DownloadToDiskCoroutine()
    //     {
    //         if (lastCapturedTexture == null)
    //         {
    //             Debug.LogError("[PictureDataControlelr] Nenhuma foto para download.");
    //             yield break;
    //         }

    //         byte[] png = lastCapturedTexture.EncodeToPNG();
    //         string fileName = $"screenshot_{System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.png";

    // #if UNITY_WEBGL && !UNITY_EDITOR
    //         try
    //         {
    //             string base64 = System.Convert.ToBase64String(png);
    //             DownloadFileFromBase64(fileName, base64);
    //             Debug.Log($"[PictureDataControlelr] Download do navegador acionado para {fileName}");
    //         }
    //         catch (System.Exception e)
    //         {
    //             Debug.LogError($"[PictureDataControlelr] Falha ao acionar download WebGL: {e.Message}");
    //         }
    // #else
    //         string path = Path.Combine(Application.persistentDataPath, fileName);

    //         try
    //         {
    //             File.WriteAllBytes(path, png);
    //             Debug.Log($"[PictureDataControlelr] Screenshot salvo em {path}");

    //             // No Windows, abre a pasta e seleciona o arquivo
    // #if UNITY_STANDALONE_WIN || UNITY_EDITOR
    //             string explorerArgs = "/select, \"" + path + "\"";
    //             System.Diagnostics.Process.Start("explorer.exe", explorerArgs);
    // #endif
    //         }
    //         catch (System.Exception e)
    //         {
    //             Debug.LogError($"[PictureDataControlelr] Falha ao salvar arquivo: {e.Message}");
    //         }
    // #endif

    //         yield return null;
    //     }

    /// <summary>
    /// Retorna a última foto capturada em formato PNG.
    /// </summary>
    public byte[] GetLastPicturePNG()
    {
        if (lastCapturedTexture == null)
        {
            Debug.LogWarning("[PictureDataControlelr] Nenhuma foto capturada ainda.");
            return null;
        }

        return lastCapturedTexture.EncodeToPNG();
    }

    /// <summary>
    /// Retorna a última foto capturada como Sprite.
    /// </summary>
    public Sprite GetLastPictureAsSprite()
    {
        if (lastCapturedTexture == null)
        {
            Debug.LogWarning("[PictureDataControlelr] Nenhuma foto capturada ainda.");
            return null;
        }

        return Sprite.Create(
            lastCapturedTexture,
            new Rect(0, 0, lastCapturedTexture.width, lastCapturedTexture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    /// <summary>
    /// Retorna a última textura capturada.
    /// </summary>
    public Texture2D GetLastPictureTexture()
    {
        return lastCapturedTexture;
    }
}
