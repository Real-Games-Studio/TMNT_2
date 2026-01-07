using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a distribuição de máscaras entre os PositionTrackers para evitar duplicatas.
/// Cada máscara (tartaruga) pode ser usada apenas uma vez por sessão.
/// </summary>
public class MaskDistributionManager : MonoBehaviour
{
    private static MaskDistributionManager _instance;
    public static MaskDistributionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MaskDistributionManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MaskDistributionManager");
                    _instance = go.AddComponent<MaskDistributionManager>();
                }
            }
            return _instance;
        }
    }

    // Armazena qual máscara (índice) está sendo usada por qual PositionTracker
    private Dictionary<PositionTracker, int> assignedMasks = new Dictionary<PositionTracker, int>();

    // Lista de índices disponíveis (0, 1, 2, 3 para as 4 tartarugas)
    private List<int> availableMaskIndices = new List<int>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            ResetAllMasks();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Reseta todas as máscaras, tornando todas disponíveis novamente.
    /// </summary>
    public void ResetAllMasks()
    {
        assignedMasks.Clear();
        availableMaskIndices.Clear();

        // Adiciona os 4 índices possíveis (0=SM_Mascara, 1=SM_Mascara(1), 2=SM_Mascara(2), 3=SM_Mascara(3))
        for (int i = 0; i < 4; i++)
        {
            availableMaskIndices.Add(i);
        }

        Debug.Log($"[MaskDistribution] Todas as máscaras foram resetadas. {availableMaskIndices.Count} máscaras disponíveis.");
    }

    /// <summary>
    /// Obtém um índice de máscara aleatório disponível para o PositionTracker.
    /// Se o tracker já tem uma máscara atribuída, retorna a mesma.
    /// </summary>
    public int GetAvailableMaskIndex(PositionTracker tracker)
    {
        if (tracker == null)
        {
            Debug.LogWarning("[MaskDistribution] Tracker é NULL!");
            return -1;
        }

        // Se já tem máscara atribuída, retorna a mesma
        if (assignedMasks.ContainsKey(tracker))
        {
            int existingIndex = assignedMasks[tracker];
            Debug.Log($"[MaskDistribution] {tracker.name} já tem máscara {existingIndex} atribuída.");
            return existingIndex;
        }

        // Se não tem máscaras disponíveis, retorna -1
        if (availableMaskIndices.Count == 0)
        {
            Debug.LogWarning($"[MaskDistribution] Não há máscaras disponíveis para {tracker.name}!");
            return -1;
        }

        // Escolhe um índice aleatório da lista de disponíveis
        int randomListIndex = Random.Range(0, availableMaskIndices.Count);
        int maskIndex = availableMaskIndices[randomListIndex];

        // Remove da lista de disponíveis e atribui ao tracker
        availableMaskIndices.RemoveAt(randomListIndex);
        assignedMasks[tracker] = maskIndex;

        Debug.Log($"[MaskDistribution] ✓ {tracker.name} recebeu máscara {maskIndex}. Restam {availableMaskIndices.Count} máscaras disponíveis.");

        return maskIndex;
    }

    /// <summary>
    /// Libera a máscara de um PositionTracker específico, tornando-a disponível novamente.
    /// </summary>
    public void ReleaseMask(PositionTracker tracker)
    {
        if (tracker == null) return;

        if (assignedMasks.ContainsKey(tracker))
        {
            int maskIndex = assignedMasks[tracker];
            assignedMasks.Remove(tracker);

            if (!availableMaskIndices.Contains(maskIndex))
            {
                availableMaskIndices.Add(maskIndex);
            }

            Debug.Log($"[MaskDistribution] Máscara {maskIndex} liberada por {tracker.name}. Agora {availableMaskIndices.Count} máscaras disponíveis.");
        }
    }

    /// <summary>
    /// Retorna o índice de máscara atualmente atribuído a um tracker, ou -1 se não tiver.
    /// </summary>
    public int GetAssignedMaskIndex(PositionTracker tracker)
    {
        if (tracker == null) return -1;
        return assignedMasks.ContainsKey(tracker) ? assignedMasks[tracker] : -1;
    }

    /// <summary>
    /// Verifica se um índice de máscara está disponível.
    /// </summary>
    public bool IsMaskAvailable(int maskIndex)
    {
        return availableMaskIndices.Contains(maskIndex);
    }

    /// <summary>
    /// Retorna quantas máscaras ainda estão disponíveis.
    /// </summary>
    public int GetAvailableCount()
    {
        return availableMaskIndices.Count;
    }
}
