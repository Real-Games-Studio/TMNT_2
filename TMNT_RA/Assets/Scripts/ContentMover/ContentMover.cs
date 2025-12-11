using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace RealGames
{
    public class ContentMover : MonoBehaviour
    {
        public bool SendImageToWebsocket;
        public bool serverIsOnline;


        public RawImage image;
        public string name;

        public RenderTexture RenderTextureToSend;
        public RawImage ImageToSend;

        public List<GameObject> UIToTurnOn;
        private bool _isSending;
        private void Update()
        {
            // if (Input.GetKeyDown(KeyCode.G))
            // {
            //     var send = new SendImage();
            //     NewImageModel.base64 = send.Base64FromTexture2D(toTexture2D((RenderTexture)ImageToSend.texture));
            //     Debug.Log("Hey");
            //     ServerEvents.OnEmitMessage?.Invoke(Shiz.Scripts.WebSocketController.NewImage, NewImageModel);
            // }
        }

        public void SendImageToWebsocketFunction()
        {
            //     var send = new SendImage();
            //     NewImageModel.base64 = send.Base64FromTexture2D(toTexture2D((RenderTexture)ImageToSend.texture));
            //     Debug.Log("Hey");
            //     ServerEvents.OnEmitMessage?.Invoke(Shiz.Scripts.WebSocketController.NewImage, NewImageModel);
        }

        private void Awake()
        {
            // UIEvents.OnButtonClicked += OnButtonClickedHandler;
        }

        private void OnDestroy()
        {
            // UIEvents.OnButtonClicked -= OnButtonClickedHandler;
        }

        //         private void OnButtonClickedHandler(ButtonFunction obj)
        //         {
        //             switch (obj)
        //             {
        //                 case ButtonFunction.ContinueFromPhoto:
        // #if  !UNITY_EDITOR
        //                     UploadThisRI();
        // #endif                   
        //                     break;
        //                 case ButtonFunction.PhotoDoneLoading:
        // #if !UNITY_EDITOR
        //                     UploadThisRI();
        // #endif
        //                     break;
        //             }
        //         }
        Texture2D toTexture2D(RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            // ReadPixels looks at the active RenderTexture.
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        public void UploadThisRI()
        {
            if (_isSending)
            {
                return;
            }
            _isSending = true;
            name = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            Debug.Log($"name:{name}");
            if (image.texture == null)
            {
                return;
            }
            UploadTexture(toTexture2D((RenderTexture)ImageToSend.texture), name);
            foreach (var o in UIToTurnOn)
            {
                Debug.Log(o.name);
                o.SetActive(true);
                _isSending = false;
            }
        }
        /// <summary>
        /// Upload direto de uma textura (Texture2D) sem salvar localmente.
        /// </summary>
        public void UploadTexture(Texture2D texture, string fileName, Action<bool> callback = null)
        {
            Debug.Log("UploadTexture");
            if (texture == null || string.IsNullOrEmpty(fileName))
            {
                UnityEngine.Debug.LogError("Texture or fileName is null.");
                callback?.Invoke(false);
                return;
            }
            byte[] pngData = texture.EncodeToPNG();
            if (pngData == null)
            {
                UnityEngine.Debug.LogError("Failed to encode texture to PNG.");
                callback?.Invoke(false);
                return;
            }
            Debug.Log("UploadTexture, Start Coroutine");
            StartCoroutine(UploadBytesCoroutine(fileName, pngData, callback));
        }

        /// <summary>
        /// Upload direto de um array de bytes (ex: PNG) sem salvar localmente.
        /// </summary>
        public void UploadBytes(string fileName, byte[] data, Action<bool> callback = null)
        {
            if (data == null || string.IsNullOrEmpty(fileName))
            {
                UnityEngine.Debug.LogError("Data or fileName is null.");
                callback?.Invoke(false);
                return;
            }
            StartCoroutine(UploadBytesCoroutine(fileName, data, callback));
        }

        private IEnumerator UploadBytesCoroutine(string fileName, byte[] data, Action<bool> callback)
        {
            UnityWebRequest request = CreateUploadRequest(fileName, data);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Debug.Log($"Uploading {fileName}...");
            yield return request.SendWebRequest();
            stopwatch.Stop();
            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleSuccessfulUpload(fileName, stopwatch.Elapsed.TotalSeconds);
                callback?.Invoke(true);
            }
            else
            {
                HandleFailedUpload(fileName, request.error, request.result);
                callback?.Invoke(false);
            }
        }

        /// <summary>
        /// Action que deve ser chamada toda vez que quiser subir arquivos para o servidor.
        /// </summary>
        public static Action OnCallUpdateFunction;

        public static Action<bool> UploadCallBack;

        /// <summary>
        /// Caminho onde os arquivos devem ser salvos caso queira que eles sejam "subidos" para o servidor.
        /// </summary>
        public static string contentPath;

        private string rootPath;

        /// <summary>
        /// Projeto para testes. Configurar o seu projeto aqui: https://realgamesstudio.com.br/admin
        /// </summary>
        [SerializeField] private string projectCode = "project001";

        /// <summary>
        /// Configura no Inspector se quer ou não persistir com os arquivos após sucesso de upload.
        /// </summary>
        [SerializeField] private bool deleteAfterUpdate = true;

        [Header("=== ON VALIDATE BUT MUST BE PLAY MODE ===")]
        /// <summary>
        /// Usar no editor em play mode.
        /// </summary>
        public bool editor_Upload;

        private const string UploadUrl = "https://realgamesstudio.com.br/api/media/upload";
        private const string DynamicContentUrl = "https://realgamesstudio.com.br/dynamic-content/";
        private const string EmailConfirmationUrl = "https://realgamesstudio.com.br/emailConfirmation/";

        public static string mainLink;

        private bool isUploading = false; // Controle de estado de upload

        public QrCodeCreator QrCodeGenerator;
        private void OnValidate()
        {
            if (SendImageToWebsocket)
            {
                SendImageToWebsocket = false;
                SendImageToWebsocketFunction();
            }
            if (editor_Upload)
            {
                editor_Upload = false;
                if (!isUploading)
                {
                    OnCallUpdateFunctionHandler();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Upload already in progress. Please wait.");
                }
            }
        }

        private void OnEnable()
        {
            OnCallUpdateFunction += OnCallUpdateFunctionHandler;
        }

        private void OnDisable()
        {
            OnCallUpdateFunction -= OnCallUpdateFunctionHandler;
        }

        private async void Start()
        {
            // projectCode = JsonSystem.Instance.JsonContent.projectCode;
            serverIsOnline = await SendPing("https://realgamesstudio.com.br");

            rootPath = Application.isEditor ? Path.Combine(Application.dataPath, "../ContentMover") : Path.Combine(Application.dataPath, "ContentMover");
            contentPath = Path.Combine(rootPath, "ToUpload");

            if (!Directory.Exists(contentPath))
            {
                Directory.CreateDirectory(contentPath);
                UnityEngine.Debug.Log($"ContentMover: Created a new dir: {contentPath}");
            }

            UnityEngine.Debug.Log($"Content Mover path: {contentPath}");

            OnCallUpdateFunctionHandler();
        }

        private void OnCallUpdateFunctionHandler()
        {
            UnityEngine.Debug.Log("ContentMover: Called to Start Upload Files");

            if (!isUploading)
            {
                StartCoroutine(UploadFiles());
            }
            else
            {
                UnityEngine.Debug.LogWarning("Upload already in progress. Please wait.");
            }
        }

        private IEnumerator UploadFiles()
        {
            isUploading = true;

            try
            {
                string[] files = Directory.GetFiles(contentPath);

                if (files.Length == 0)
                {
                    UnityEngine.Debug.Log("No files to upload.");
                    yield break;
                }

                foreach (string file in files)
                {
                    byte[] fileData = ReadFile(file);
                    if (fileData == null) continue;

                    string mediaName = Path.GetFileName(file);
                    UnityWebRequest request = CreateUploadRequest(mediaName, fileData);

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    yield return request.SendWebRequest();

                    stopwatch.Stop();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        HandleSuccessfulUpload(mediaName, stopwatch.Elapsed.TotalSeconds);
                        if (deleteAfterUpdate) DeleteFile(file, mediaName);
                    }
                    else
                    {
                        HandleFailedUpload(mediaName, request.error, request.result);
                    }
                }
            }
            finally
            {
                isUploading = false;
            }
        }

        private byte[] ReadFile(string filePath)
        {
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (IOException e)
            {
                UnityEngine.Debug.LogError($"Failed to read file {filePath}. Error: {e.Message}");
                return null;
            }
        }

        private UnityWebRequest CreateUploadRequest(string mediaName, byte[] fileData)
        {
            WWWForm form = new WWWForm();
            form.AddField("mediaName", mediaName);
            form.AddField("projectCode", projectCode);
            form.AddBinaryData("file", fileData, mediaName, "image/png");

            UnityWebRequest request = UnityWebRequest.Post(UploadUrl, form);
            request.timeout = 5;

            return request;
        }

        private void HandleSuccessfulUpload(string mediaName, double uploadTime)
        {
            UnityEngine.Debug.Log($"File {mediaName} uploaded successfully. Upload time: {uploadTime:F2} seconds");
            UnityEngine.Debug.Log($"Final Link: {DynamicContentUrl}{projectCode}/{mediaName}");

            QrCodeGenerator.CreateAndShowBarcode($"{DynamicContentUrl}{projectCode}/{mediaName}");
            mainLink = DynamicContentUrl;

            UploadCallBack?.Invoke(true);
#if UNITY_EDITOR
            Application.OpenURL($"{DynamicContentUrl}{projectCode}/{mediaName}");
#endif
        }

        private void HandleFailedUpload(string mediaName, string error, UnityWebRequest.Result result)
        {
            mainLink = EmailConfirmationUrl;
#if UNITY_EDITOR
            Application.OpenURL($"{EmailConfirmationUrl}{projectCode}/{mediaName}");
#endif
            UploadCallBack?.Invoke(false);
            UnityEngine.Debug.LogError($"Failed to upload file {mediaName}. Error: {error}. Result: {result}");
        }

        private void DeleteFile(string filePath, string mediaName)
        {
            try
            {
                File.Delete(filePath);
                UnityEngine.Debug.Log($"File {mediaName} deleted locally.");
            }
            catch (IOException e)
            {
                UnityEngine.Debug.LogError($"Failed to delete file {mediaName}. Error: {e.Message}");
            }
        }

        public static async Task<bool> SendPing(string url)
        {
            using (var ping = new System.Net.NetworkInformation.Ping())
            {
                try
                {
                    PingReply result = await ping.SendPingAsync(url);
                    return result.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static string GetLink(string projectCode = "project001", string fileName = "")
        {
            if (string.IsNullOrWhiteSpace(projectCode))
                throw new ArgumentException("Project code cannot be null or empty.");

            return $"{mainLink}{projectCode}/{fileName}";
        }
    }
}