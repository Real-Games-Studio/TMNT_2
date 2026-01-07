using UnityEngine;
using System.Collections.Generic;
using Imagine.WebAR;

/// <summary>
/// Script de debug para verificar a configuração de FaceObjects e PositionTrackers.
/// Adicione este componente em qualquer GameObject e execute o jogo para ver os logs.
/// </summary>
public class FaceTrackingDebugger : MonoBehaviour
{
    [Header("Auto-Detection")]
    [SerializeField] private bool autoDetectOnStart = true;
    [SerializeField] private bool logEveryFrame = false;
    [SerializeField, Range(0.5f, 5f)] private float logInterval = 2f;

    private float lastLogTime = 0f;

    private void Start()
    {
        if (autoDetectOnStart)
        {
            LogFaceTrackingSetup();
        }
    }

    private void Update()
    {
        if (logEveryFrame || (Time.time - lastLogTime >= logInterval))
        {
            LogActiveStatus();
            lastLogTime = Time.time;
        }
    }

    [ContextMenu("Log Face Tracking Setup")]
    public void LogFaceTrackingSetup()
    {
        Debug.Log("╔═══════════════════════════════════════════════════════════╗");
        Debug.Log("║ 🎭 FACE TRACKING DEBUG - CONFIGURAÇÃO");
        Debug.Log("╠═══════════════════════════════════════════════════════════╣");

        // Find all FaceObjects
        FaceObject[] faceObjects = FindObjectsByType<FaceObject>(FindObjectsSortMode.None);
        Debug.Log($"║ FaceObjects encontrados: {faceObjects.Length}");

        if (faceObjects.Length == 0)
        {
            Debug.LogWarning("║ ⚠ NENHUM FaceObject encontrado na cena!");
        }
        else
        {
            Debug.Log("╟───────────────────────────────────────────────────────────╢");
            foreach (FaceObject fo in faceObjects)
            {
                Debug.Log($"║ FaceObject: {fo.name}");
                Debug.Log($"║   ├─ Face Index: {fo.faceIndex}");
                Debug.Log($"║   ├─ Ativo: {fo.gameObject.activeSelf}");
                Debug.Log($"║   ├─ Ativo em Hierarquia: {fo.gameObject.activeInHierarchy}");
                Debug.Log($"║   └─ Path: {GetGameObjectPath(fo.gameObject)}");
                Debug.Log("╟───────────────────────────────────────────────────────────╢");
            }
        }

        // Find all PositionTrackers
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);
        Debug.Log($"║ PositionTrackers encontrados: {trackers.Length}");

        if (trackers.Length == 0)
        {
            Debug.LogWarning("║ ⚠ NENHUM PositionTracker encontrado na cena!");
        }
        else
        {
            Debug.Log("╟───────────────────────────────────────────────────────────╢");

            Dictionary<Transform, int> targetUsageCount = new Dictionary<Transform, int>();

            foreach (PositionTracker tracker in trackers)
            {
                // Usa reflection para acessar o campo privado 'target'
                var targetField = typeof(PositionTracker).GetField("target",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Transform target = targetField?.GetValue(tracker) as Transform;

                var objectsField = typeof(PositionTracker).GetField("objectsToDisable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                GameObject[] objects = objectsField?.GetValue(tracker) as GameObject[];

                Debug.Log($"║ PositionTracker: {tracker.name}");
                Debug.Log($"║   ├─ Target: {(target != null ? target.name : "NULL ⚠")}");

                if (target != null)
                {
                    FaceObject targetFaceObj = target.GetComponent<FaceObject>();
                    if (targetFaceObj != null)
                    {
                        Debug.Log($"║   │  └─ FaceIndex do Target: {targetFaceObj.faceIndex}");

                        // Conta uso do target
                        if (!targetUsageCount.ContainsKey(target))
                            targetUsageCount[target] = 0;
                        targetUsageCount[target]++;
                    }
                    else
                    {
                        Debug.LogWarning($"║   │  └─ ⚠ Target não tem componente FaceObject!");
                    }
                }

                Debug.Log($"║   ├─ Wearables (objectsToDisable): {(objects != null ? objects.Length : 0)}");

                if (objects != null && objects.Length > 0)
                {
                    for (int i = 0; i < objects.Length; i++)
                    {
                        if (objects[i] != null)
                        {
                            Debug.Log($"║   │  [{i}] {objects[i].name} (Ativo: {objects[i].activeSelf})");
                        }
                        else
                        {
                            Debug.LogWarning($"║   │  [{i}] NULL ⚠");
                        }
                    }
                }

                Debug.Log("╟───────────────────────────────────────────────────────────╢");
            }

            // Verifica duplicação de targets
            Debug.Log("║ ANÁLISE DE TARGETS:");
            bool hasDuplicates = false;
            foreach (var kvp in targetUsageCount)
            {
                if (kvp.Value > 1)
                {
                    Debug.LogError($"║ ⚠⚠⚠ PROBLEMA: {kvp.Value} PositionTrackers estão seguindo o MESMO target: {kvp.Key.name}");
                    hasDuplicates = true;
                }
                else
                {
                    Debug.Log($"║ ✓ {kvp.Key.name} tem 1 tracker (OK)");
                }
            }

            if (!hasDuplicates)
            {
                Debug.Log("║ ✓ Não há duplicação de targets!");
            }
        }

        // Check WearableManager
        Debug.Log("╟───────────────────────────────────────────────────────────╢");
        Debug.Log("║ WEARABLE MANAGER:");
        WearableManager manager = FindObjectOfType<WearableManager>();
        if (manager != null)
        {
            Debug.Log($"║ ✓ WearableManager encontrado: {manager.name}");
            Debug.Log($"║   └─ Wearables disponíveis: {manager.GetAvailableCount()}/4");
        }
        else
        {
            Debug.LogWarning("║ ⚠ WearableManager NÃO encontrado!");
            Debug.LogWarning("║   Será criado automaticamente quando necessário");
        }

        Debug.Log("╚═══════════════════════════════════════════════════════════╝");
    }

    [ContextMenu("Log Active Status")]
    public void LogActiveStatus()
    {
        FaceObject[] faceObjects = FindObjectsByType<FaceObject>(FindObjectsSortMode.None);
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);

        string log = "[FaceTrackingDebug] Status: ";

        // Conta quantas faces estão ativas
        int activeFaces = 0;
        foreach (FaceObject fo in faceObjects)
        {
            if (fo.gameObject.activeInHierarchy)
            {
                log += $"Face{fo.faceIndex}=✓ ";
                activeFaces++;
            }
        }

        // Verifica se trackers estão apontando para faces ativas
        Dictionary<int, int> trackersPerFace = new Dictionary<int, int>();
        foreach (var tracker in trackers)
        {
            var targetField = typeof(PositionTracker).GetField("target",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Transform target = targetField?.GetValue(tracker) as Transform;

            if (target != null)
            {
                FaceObject targetFace = target.GetComponent<FaceObject>();
                if (targetFace != null && targetFace.gameObject.activeInHierarchy)
                {
                    if (!trackersPerFace.ContainsKey(targetFace.faceIndex))
                        trackersPerFace[targetFace.faceIndex] = 0;
                    trackersPerFace[targetFace.faceIndex]++;
                }
            }
        }

        log += $"| Faces ativas: {activeFaces} ";

        // Mostra quantos trackers por face
        if (trackersPerFace.Count > 0)
        {
            log += "| Trackers ativos: ";
            foreach (var kvp in trackersPerFace)
            {
                log += $"Face{kvp.Key}→{kvp.Value} ";
                if (kvp.Value > 1)
                {
                    log += "⚠DUPLICADO⚠ ";
                }
            }
        }

        WearableManager manager = FindObjectOfType<WearableManager>();
        if (manager != null)
        {
            log += $"| Wearables: {manager.GetAvailableCount()}/4 disponíveis";
        }

        Debug.Log(log);
    }

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
