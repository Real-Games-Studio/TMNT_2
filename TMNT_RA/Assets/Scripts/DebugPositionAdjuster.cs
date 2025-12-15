using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema de debug para ajustar posição e escala de objetos em tempo real.
/// Controles:
/// - 1: Diminui escala
/// - 2: Aumenta escala
/// - Setas Cima/Baixo: Move no eixo Y
/// - Setas Esquerda/Direita: Move no eixo X
/// - D: Ativa/desativa modo debug (vai para ScreenVestiario sem countdown)
/// </summary>
public class DebugPositionAdjuster : MonoBehaviour
{
    [Header("Objetos para Ajustar")]
    [Tooltip("Lista de objetos que terão posição e escala ajustáveis")]
    [SerializeField] private List<Transform> adjustableObjects;

    [Header("Configurações de Ajuste")]
    [SerializeField] private float scaleStep = 0.05f; // Quanto aumenta/diminui a escala por tecla
    [SerializeField] private float positionStep = 5f; // Quanto move em pixels por tecla
    [SerializeField] private bool showDebugInfo = true; // Mostra info na tela

    [Header("PlayerPrefs Keys")]
    [SerializeField] private string positionXSuffix = "_PosX";
    [SerializeField] private string positionYSuffix = "_PosY";
    [SerializeField] private string scaleSuffix = "_Scale";

    private bool isDebugMode = false;
    private static bool debugModeActive = false; // Persiste entre cenas

    private void Awake()
    {
        // Carrega as configurações salvas
        LoadAllSettings();
    }

    private void Update()
    {
        // Ativa/desativa modo debug
        if (Input.GetKeyDown(KeyCode.D))
        {
            ToggleDebugMode();
        }

        // Se modo debug está ativo, permite ajustes
        if (isDebugMode || debugModeActive)
        {
            HandleScaleAdjustment();
            HandlePositionAdjustment();
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo || (!isDebugMode && !debugModeActive)) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.UpperLeft;

        string info = "=== MODO DEBUG ATIVO ===\n";
        info += "1: Diminuir escala | 2: Aumentar escala\n";
        info += "Setas: Mover posicao (Y cima/baixo, X esq/dir)\n";
        info += "D: Reiniciar app e salvar\n\n";

        if (adjustableObjects != null && adjustableObjects.Count > 0)
        {
            info += "Objetos ajustáveis:\n";
            foreach (var obj in adjustableObjects)
            {
                if (obj != null)
                {
                    info += $"• {obj.name}\n";
                    info += $"  Pos: ({obj.localPosition.x:F1}, {obj.localPosition.y:F1})\n";
                    info += $"  Scale: {obj.localScale.x:F2}\n";
                }
            }
        }

        GUI.Label(new Rect(10, 10, Screen.width - 20, Screen.height - 20), info, style);
    }

    private void ToggleDebugMode()
    {
        debugModeActive = !debugModeActive;

        if (debugModeActive)
        {
            Debug.Log("[DebugPositionAdjuster] ✓ MODO DEBUG ATIVADO");

            // Se não estiver no ScreenVestiario, vai para lá
            if (!SceneManager.GetActiveScene().name.Contains("Vestiario"))
            {
                // Marca que está em modo debug antes de trocar de cena
                PlayerPrefs.SetInt("DebugMode", 1);
                PlayerPrefs.Save();

                // Tenta ir para o ScreenVestiario (você pode ajustar conforme sua navegação)
                var screenVestiario = FindFirstObjectByType<ScreenVestiario>();
                if (screenVestiario != null)
                {
                    screenVestiario.CallScreenByName("Vestiario");
                    Debug.Log("[DebugPositionAdjuster] Indo para ScreenVestiario...");
                }
            }

            isDebugMode = true;
        }
        else
        {
            Debug.Log("[DebugPositionAdjuster] Salvando configurações e reiniciando app...");

            // Salva todas as configurações
            SaveAllSettings();

            // Remove flag de debug mode
            PlayerPrefs.SetInt("DebugMode", 0);
            PlayerPrefs.Save();

            // Reinicia o app (volta para cena 0)
            SceneManager.LoadScene(0);
        }
    }

    private void HandleScaleAdjustment()
    {
        bool scaleChanged = false;

        // Diminui escala (tecla 1)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            AdjustScale(-scaleStep);
            scaleChanged = true;
        }

        // Aumenta escala (tecla 2)
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            AdjustScale(scaleStep);
            scaleChanged = true;
        }

        if (scaleChanged)
        {
            SaveAllSettings();
        }
    }

    private void HandlePositionAdjustment()
    {
        bool positionChanged = false;
        Vector2 adjustment = Vector2.zero;

        // Seta para cima - aumenta Y
        if (Input.GetKey(KeyCode.UpArrow))
        {
            adjustment.y = positionStep * Time.deltaTime * 60f; // Normalizado para 60 FPS
            positionChanged = true;
        }

        // Seta para baixo - diminui Y
        if (Input.GetKey(KeyCode.DownArrow))
        {
            adjustment.y = -positionStep * Time.deltaTime * 60f;
            positionChanged = true;
        }

        // Seta para direita - aumenta X
        if (Input.GetKey(KeyCode.RightArrow))
        {
            adjustment.x = positionStep * Time.deltaTime * 60f;
            positionChanged = true;
        }

        // Seta para esquerda - diminui X
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            adjustment.x = -positionStep * Time.deltaTime * 60f;
            positionChanged = true;
        }

        if (positionChanged)
        {
            AdjustPosition(adjustment);

            // Salva a cada 0.5 segundos para não sobrecarregar
            if (Time.frameCount % 30 == 0)
            {
                SaveAllSettings();
            }
        }
    }

    private void AdjustScale(float delta)
    {
        if (adjustableObjects == null || adjustableObjects.Count == 0) return;

        foreach (var obj in adjustableObjects)
        {
            if (obj != null)
            {
                Vector3 newScale = obj.localScale + Vector3.one * delta;
                newScale.x = Mathf.Max(0.1f, newScale.x); // Mínimo de 0.1
                newScale.y = Mathf.Max(0.1f, newScale.y);
                newScale.z = Mathf.Max(0.1f, newScale.z);

                obj.localScale = newScale;

                Debug.Log($"[DebugPositionAdjuster] {obj.name} scale: {newScale.x:F2}");
            }
        }
    }

    private void AdjustPosition(Vector2 delta)
    {
        if (adjustableObjects == null || adjustableObjects.Count == 0) return;

        foreach (var obj in adjustableObjects)
        {
            if (obj != null)
            {
                // Move diretamente no localPosition (X e Y)
                Vector3 currentPos = obj.localPosition;
                currentPos.x += delta.x;
                currentPos.y += delta.y;
                obj.localPosition = currentPos;
            }
        }
    }

    private void SaveAllSettings()
    {
        if (adjustableObjects == null || adjustableObjects.Count == 0) return;

        foreach (var obj in adjustableObjects)
        {
            if (obj != null)
            {
                string objName = obj.name;

                // Salva posição (localPosition X e Y)
                PlayerPrefs.SetFloat(objName + positionXSuffix, obj.localPosition.x);
                PlayerPrefs.SetFloat(objName + positionYSuffix, obj.localPosition.y);

                // Salva escala
                PlayerPrefs.SetFloat(objName + scaleSuffix, obj.localScale.x);
            }
        }

        PlayerPrefs.Save();
        Debug.Log("[DebugPositionAdjuster] ✓ Configurações salvas!");
    }

    private void LoadAllSettings()
    {
        if (adjustableObjects == null || adjustableObjects.Count == 0) return;

        int loadedCount = 0;

        foreach (var obj in adjustableObjects)
        {
            if (obj != null)
            {
                string objName = obj.name;

                // Carrega posição (localPosition X e Y)
                if (PlayerPrefs.HasKey(objName + positionXSuffix))
                {
                    float x = PlayerPrefs.GetFloat(objName + positionXSuffix);
                    float y = PlayerPrefs.GetFloat(objName + positionYSuffix);
                    Vector3 pos = obj.localPosition;
                    pos.x = x;
                    pos.y = y;
                    obj.localPosition = pos;
                    loadedCount++;
                }

                // Carrega escala
                if (PlayerPrefs.HasKey(objName + scaleSuffix))
                {
                    float scale = PlayerPrefs.GetFloat(objName + scaleSuffix);
                    obj.localScale = Vector3.one * scale;
                }
            }
        }

        if (loadedCount > 0)
        {
            Debug.Log($"[DebugPositionAdjuster] ✓ Carregadas configurações de {loadedCount} objetos");
        }

        // Verifica se estava em modo debug
        if (PlayerPrefs.GetInt("DebugMode", 0) == 1)
        {
            debugModeActive = true;
            isDebugMode = true;
            Debug.Log("[DebugPositionAdjuster] Modo debug restaurado");
        }
    }

    /// <summary>
    /// Método público para verificar se está em modo debug (usado pelo ScreenVestiario)
    /// </summary>
    public static bool IsDebugMode()
    {
        return debugModeActive || PlayerPrefs.GetInt("DebugMode", 0) == 1;
    }

    /// <summary>
    /// Adiciona um objeto à lista de ajustáveis em runtime
    /// </summary>
    public void AddAdjustableObject(Transform obj)
    {
        if (adjustableObjects == null)
            adjustableObjects = new List<Transform>();

        if (!adjustableObjects.Contains(obj))
        {
            adjustableObjects.Add(obj);
            Debug.Log($"[DebugPositionAdjuster] Adicionado objeto ajustável: {obj.name}");
        }
    }

    /// <summary>
    /// Remove todos os dados salvos (útil para resetar)
    /// </summary>
    public void ClearSavedSettings()
    {
        if (adjustableObjects == null) return;

        foreach (var obj in adjustableObjects)
        {
            if (obj != null)
            {
                string objName = obj.name;
                PlayerPrefs.DeleteKey(objName + positionXSuffix);
                PlayerPrefs.DeleteKey(objName + positionYSuffix);
                PlayerPrefs.DeleteKey(objName + scaleSuffix);
            }
        }

        PlayerPrefs.DeleteKey("DebugMode");
        PlayerPrefs.Save();

        Debug.Log("[DebugPositionAdjuster] ✓ Configurações limpas!");
    }
}
