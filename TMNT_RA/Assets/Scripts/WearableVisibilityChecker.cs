using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Verifica se os wearables estão visíveis e configurados corretamente.
/// Adicione este componente na cena para diagnosticar problemas de visibilidade.
/// </summary>
public class WearableVisibilityChecker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool checkOnStart = true;
    [SerializeField] private bool checkEveryFrame = false;
    [SerializeField, Range(1f, 10f)] private float checkInterval = 3f;

    private float lastCheckTime = 0f;

    private void Start()
    {
        if (checkOnStart)
        {
            Invoke(nameof(CheckAllWearables), 2f); // Aguarda 2s para tudo inicializar
        }
    }

    private void Update()
    {
        if (checkEveryFrame || (Time.time - lastCheckTime >= checkInterval))
        {
            CheckActiveWearables();
            lastCheckTime = Time.time;
        }
    }

    [ContextMenu("Check All Wearables")]
    public void CheckAllWearables()
    {
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);

        Debug.Log("╔═══════════════════════════════════════════════════════════╗");
        Debug.Log("║ 👁 VERIFICAÇÃO DE VISIBILIDADE DOS WEARABLES");
        Debug.Log("╠═══════════════════════════════════════════════════════════╣");

        if (trackers.Length == 0)
        {
            Debug.LogWarning("║ ⚠ Nenhum PositionTracker encontrado!");
            Debug.Log("╚═══════════════════════════════════════════════════════════╝");
            return;
        }

        int totalWearables = 0;
        int activeWearables = 0;
        int visibleWearables = 0;

        foreach (var tracker in trackers)
        {
            var objectsField = typeof(PositionTracker).GetField("objectsToDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject[] objects = objectsField?.GetValue(tracker) as GameObject[];

            if (objects == null || objects.Length == 0)
                continue;

            Debug.Log($"║ Tracker: {tracker.name}");

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject wearable = objects[i];
                totalWearables++;

                if (wearable == null)
                {
                    Debug.LogWarning($"║   [{i}] NULL ⚠");
                    continue;
                }

                bool isActive = wearable.activeSelf;
                if (isActive) activeWearables++;

                // Verifica renderizadores
                Renderer[] renderers = wearable.GetComponentsInChildren<Renderer>(true);
                int enabledRenderers = 0;
                int totalRenderers = renderers.Length;

                foreach (var r in renderers)
                {
                    if (r.enabled && r.gameObject.activeInHierarchy)
                        enabledRenderers++;
                }

                bool isVisible = isActive && enabledRenderers > 0;
                if (isVisible) visibleWearables++;

                string status = isActive ? "✓ Ativo" : "✗ Inativo";
                string visibility = "";

                if (isActive)
                {
                    if (totalRenderers == 0)
                    {
                        visibility = "⚠ SEM RENDERER!";
                    }
                    else if (enabledRenderers == 0)
                    {
                        visibility = $"⚠ Renderers desabilitados ({totalRenderers} total)";
                    }
                    else if (enabledRenderers < totalRenderers)
                    {
                        visibility = $"⚠ Parcial ({enabledRenderers}/{totalRenderers} renderers)";
                    }
                    else
                    {
                        visibility = $"👁 Visível ({enabledRenderers} renderers)";
                    }

                    // Verifica escala
                    Vector3 scale = wearable.transform.lossyScale;
                    if (scale.magnitude < 0.001f)
                    {
                        visibility += " ⚠ ESCALA MUITO PEQUENA!";
                    }

                    // Verifica materiais
                    foreach (var r in renderers)
                    {
                        if (r.sharedMaterial == null)
                        {
                            visibility += " ⚠ Material NULL!";
                            break;
                        }
                    }
                }

                Debug.Log($"║   [{i}] {wearable.name} - {status} {visibility}");
            }

            Debug.Log("╟───────────────────────────────────────────────────────────╢");
        }

        Debug.Log($"║ RESUMO:");
        Debug.Log($"║   Total de wearables: {totalWearables}");
        Debug.Log($"║   Ativos: {activeWearables}");
        Debug.Log($"║   Visíveis: {visibleWearables}");

        if (activeWearables > 0 && visibleWearables == 0)
        {
            Debug.LogError("║ ⚠⚠⚠ PROBLEMA: Wearables estão ativos mas NÃO VISÍVEIS!");
            Debug.LogError("║ Possíveis causas:");
            Debug.LogError("║   • Renderers desabilitados");
            Debug.LogError("║   • Materiais faltando");
            Debug.LogError("║   • Escala muito pequena");
            Debug.LogError("║   • Camadas (Layers) erradas");
        }

        Debug.Log("╚═══════════════════════════════════════════════════════════╝");
    }

    [ContextMenu("Check Active Wearables Only")]
    public void CheckActiveWearables()
    {
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);
        List<string> activeWearables = new List<string>();

        foreach (var tracker in trackers)
        {
            var objectsField = typeof(PositionTracker).GetField("objectsToDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject[] objects = objectsField?.GetValue(tracker) as GameObject[];

            if (objects == null) continue;

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject wearable = objects[i];
                if (wearable != null && wearable.activeSelf)
                {
                    Renderer[] renderers = wearable.GetComponentsInChildren<Renderer>();
                    int visibleCount = 0;
                    foreach (var r in renderers)
                    {
                        if (r.enabled && r.gameObject.activeInHierarchy)
                            visibleCount++;
                    }

                    string visibility = visibleCount > 0 ? "👁 Visível" : "⚠ Invisível";
                    activeWearables.Add($"{tracker.name}[{i}] {wearable.name} {visibility} ({visibleCount} renderers)");
                }
            }
        }

        if (activeWearables.Count > 0)
        {
            Debug.Log($"[WearableVisibility] Ativos agora ({activeWearables.Count}):");
            foreach (var info in activeWearables)
            {
                Debug.Log($"  • {info}");
            }
        }
        else
        {
            Debug.Log("[WearableVisibility] Nenhum wearable ativo no momento");
        }
    }

    [ContextMenu("Enable All Renderers")]
    public void EnableAllRenderers()
    {
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var tracker in trackers)
        {
            var objectsField = typeof(PositionTracker).GetField("objectsToDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject[] objects = objectsField?.GetValue(tracker) as GameObject[];

            if (objects == null) continue;

            foreach (GameObject wearable in objects)
            {
                if (wearable == null) continue;

                Renderer[] renderers = wearable.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (!r.enabled)
                    {
                        r.enabled = true;
                        count++;
                    }
                }
            }
        }

        Debug.Log($"[WearableVisibility] ✓ {count} renderers foram habilitados!");
    }

    [ContextMenu("Log Camera Info")]
    public void LogCameraInfo()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[WearableVisibility] Câmera principal não encontrada!");
            return;
        }

        Debug.Log("╔═══════════════════════════════════════════════════════════╗");
        Debug.Log("║ 📷 INFORMAÇÕES DA CÂMERA");
        Debug.Log("╠═══════════════════════════════════════════════════════════╣");
        Debug.Log($"║ Culling Mask: {LayerMask.LayerToName(mainCam.cullingMask)}");
        Debug.Log($"║ Near Clip: {mainCam.nearClipPlane}");
        Debug.Log($"║ Far Clip: {mainCam.farClipPlane}");
        Debug.Log($"║ FOV: {mainCam.fieldOfView}");
        Debug.Log("╚═══════════════════════════════════════════════════════════╝");

        // Verifica se os wearables estão em layers visíveis
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);
        foreach (var tracker in trackers)
        {
            var objectsField = typeof(PositionTracker).GetField("objectsToDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject[] objects = objectsField?.GetValue(tracker) as GameObject[];

            if (objects == null) continue;

            foreach (GameObject wearable in objects)
            {
                if (wearable == null || !wearable.activeSelf) continue;

                int layer = wearable.layer;
                bool isVisible = (mainCam.cullingMask & (1 << layer)) != 0;

                if (!isVisible)
                {
                    Debug.LogWarning($"⚠ {wearable.name} está no layer {LayerMask.LayerToName(layer)} que NÃO é visível pela câmera!");
                }
            }
        }
    }
}
