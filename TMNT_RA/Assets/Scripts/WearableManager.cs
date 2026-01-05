using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a distribuição de wearables garantindo que não haja repetição entre diferentes trackers.
/// Suporta até 4 pessoas com 4 wearables únicos.
/// </summary>
public class WearableManager : MonoBehaviour
{
    private static WearableManager _instance;
    public static WearableManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WearableManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("WearableManager");
                    _instance = go.AddComponent<WearableManager>();
                }
            }
            return _instance;
        }
    }

    // Dicionário que armazena qual índice de wearable está sendo usado por cada tracker
    private Dictionary<PositionTracker, int> assignedWearables = new Dictionary<PositionTracker, int>();

    // Lista de índices disponíveis (0-3 para 4 wearables)
    private List<int> availableIndices = new List<int> { 0, 1, 2, 3 };
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Atribui um índice único de wearable para um tracker.
    /// Retorna -1 se não houver índices disponíveis.
    /// </summary>
    public int AssignWearableIndex(PositionTracker tracker, int currentIndex = -1)
    {
        if (tracker == null)
        {
            Debug.LogWarning("[WearableManager] Tracker é null!");
            return -1;
        }

        // Se o tracker já tem um índice atribuído e ainda está sendo usado, mantém
        if (assignedWearables.ContainsKey(tracker))
        {
            int existingIndex = assignedWearables[tracker];
            
            // Se quer mudar para um índice diferente
            if (currentIndex >= 0 && currentIndex != existingIndex)
            {
                // Libera o índice atual
                ReleaseWearableIndex(tracker);
            }
            else
            {
                // Mantém o índice atual
                Debug.Log($"[WearableManager] {tracker.name} mantém wearable index {existingIndex}");
                return existingIndex;
            }
        }
        
        // Se não há índices disponíveis, retorna -1
        if (availableIndices.Count == 0)
        {
            Debug.LogWarning($"[WearableManager] Não há wearables disponíveis! Todos os {4 - availableIndices.Count} wearables estão em uso.");
            return -1;
        }

        // Pega um índice disponível aleatório
        int randomIndexInList = Random.Range(0, availableIndices.Count);
        int wearableIndex = availableIndices[randomIndexInList];

        // Remove da lista de disponíveis
        availableIndices.RemoveAt(randomIndexInList);

        // Atribui ao tracker
        assignedWearables[tracker] = wearableIndex;

        Debug.Log($"[WearableManager] ✓ {tracker.name} recebeu wearable index {wearableIndex} (Disponíveis: {availableIndices.Count})");
        LogCurrentAssignments();

        return wearableIndex;
    }

    /// <summary>
    /// Libera o índice de wearable usado por um tracker, tornando-o disponível novamente.
    /// </summary>
    public void ReleaseWearableIndex(PositionTracker tracker)
    {
        if (tracker == null) return;

        if (assignedWearables.ContainsKey(tracker))
        {
            int releasedIndex = assignedWearables[tracker];
            assignedWearables.Remove(tracker);

            // Adiciona de volta à lista de disponíveis se ainda não estiver lá
            if (!availableIndices.Contains(releasedIndex))
            {
                availableIndices.Add(releasedIndex);
                Debug.Log($"[WearableManager] {tracker.name} liberou wearable index {releasedIndex} (Disponíveis: {availableIndices.Count})");
            }
        }
    }

    /// <summary>
    /// Reseta todas as atribuições, liberando todos os wearables.
    /// </summary>
    public void ResetAllAssignments()
    {
        Debug.Log("[WearableManager] Resetando todas as atribuições de wearables...");
        assignedWearables.Clear();
        availableIndices.Clear();
        availableIndices.AddRange(new int[] { 0, 1, 2, 3 });
        Debug.Log("[WearableManager] ✓ Todos os wearables estão disponíveis novamente");
    }

    /// <summary>
    /// Retorna o índice de wearable atualmente atribuído a um tracker, ou -1 se não houver.
    /// </summary>
    public int GetAssignedIndex(PositionTracker tracker)
    {
        if (tracker == null) return -1;
        return assignedWearables.ContainsKey(tracker) ? assignedWearables[tracker] : -1;
    }

    /// <summary>
    /// Verifica se um tracker tem um wearable atribuído.
    /// </summary>
    public bool HasAssignment(PositionTracker tracker)
    {
        if (tracker == null) return false;
        return assignedWearables.ContainsKey(tracker);
    }

    /// <summary>
    /// Log para debug das atribuições atuais.
    /// </summary>
    private void LogCurrentAssignments()
    {
        string log = $"[WearableManager] Estado atual: {assignedWearables.Count} trackers ativos | ";
        foreach (var kvp in assignedWearables)
        {
            log += $"[{kvp.Key.name}→{kvp.Value}] ";
        }
        Debug.Log(log);
    }

    /// <summary>
    /// Retorna quantos wearables estão disponíveis no momento.
    /// </summary>
    public int GetAvailableCount()
    {
        return availableIndices.Count;
    }
}
