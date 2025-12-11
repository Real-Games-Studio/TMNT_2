using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a recuperação automática do face tracking quando há perda temporária.
/// Monitora objetos de face tracking e tenta reativar o sistema quando necessário.
/// </summary>
public class FaceTrackingRecoveryManager : MonoBehaviour
{
    [Header("Monitoring")]
    [SerializeField] private List<GameObject> faceObjectsToMonitor;
    [SerializeField] private List<PositionTracker> positionTrackersToRecover;
    
    [Header("Recovery Settings")]
    [SerializeField, Range(0.1f, 3f)] 
    private float recoveryCheckInterval = 0.5f; // Intervalo para verificar recuperação
    
    [SerializeField, Range(0.1f, 2f)] 
    private float maxRecoveryAttemptTime = 1f; // Tempo máximo tentando recuperar antes de desistir
    
    [SerializeField, Range(1, 10)] 
    private int maxRecoveryAttempts = 3; // Tentativas máximas de recuperação
    
    private Dictionary<GameObject, float> objectLostTimes = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, int> recoveryAttempts = new Dictionary<GameObject, int>();
    private float lastRecoveryCheck;
    
    private void Start()
    {
        // Inicializa os dicionários
        foreach (var obj in faceObjectsToMonitor)
        {
            if (obj != null)
            {
                objectLostTimes[obj] = 0f;
                recoveryAttempts[obj] = 0;
            }
        }
        
        lastRecoveryCheck = Time.time;
    }
    
    private void Update()
    {
        if (Time.time - lastRecoveryCheck >= recoveryCheckInterval)
        {
            CheckForRecoveryOpportunities();
            lastRecoveryCheck = Time.time;
        }
        
        MonitorFaceObjects();
    }
    
    private void MonitorFaceObjects()
    {
        foreach (var obj in faceObjectsToMonitor)
        {
            if (obj == null) continue;
            
            if (!obj.activeInHierarchy)
            {
                // Objeto está inativo, atualizar timer de perda
                if (!objectLostTimes.ContainsKey(obj))
                    objectLostTimes[obj] = Time.time;
            }
            else
            {
                // Objeto está ativo, resetar timers de recuperação
                if (objectLostTimes.ContainsKey(obj))
                {
                    objectLostTimes[obj] = 0f;
                    recoveryAttempts[obj] = 0;
                }
            }
        }
    }
    
    private void CheckForRecoveryOpportunities()
    {
        foreach (var obj in faceObjectsToMonitor)
        {
            if (obj == null) continue;
            
            // Verifica se o objeto está perdido mas dentro do tempo de recuperação
            if (!obj.activeInHierarchy && objectLostTimes.ContainsKey(obj) && objectLostTimes[obj] > 0f)
            {
                float timeLost = Time.time - objectLostTimes[obj];
                
                // Se está dentro do tempo de recuperação e não excedeu tentativas
                if (timeLost <= maxRecoveryAttemptTime && recoveryAttempts[obj] < maxRecoveryAttempts)
                {
                    AttemptRecovery(obj);
                }
                else if (timeLost > maxRecoveryAttemptTime)
                {
                    // Desistir da recuperação para este objeto
                    recoveryAttempts[obj] = maxRecoveryAttempts;
                }
            }
        }
    }
    
    private void AttemptRecovery(GameObject lostObject)
    {
        recoveryAttempts[lostObject]++;
        
        Debug.Log($"Attempting face tracking recovery for {lostObject.name} (attempt {recoveryAttempts[lostObject]}/{maxRecoveryAttempts})");
        
        // Tenta forçar reativação nos PositionTrackers relacionados
        foreach (var tracker in positionTrackersToRecover)
        {
            if (tracker != null)
            {
                tracker.ForceReactivateTracking();
            }
        }
    }
    
    /// <summary>
    /// Reseta o estado de recuperação para todos os objetos
    /// Útil quando mudando de tela ou reiniciando o sistema
    /// </summary>
    public void ResetRecoveryState()
    {
        foreach (var obj in faceObjectsToMonitor)
        {
            if (obj != null)
            {
                objectLostTimes[obj] = 0f;
                recoveryAttempts[obj] = 0;
            }
        }
    }
    
    /// <summary>
    /// Força uma tentativa de recuperação imediata
    /// </summary>
    public void ForceRecoveryAttempt()
    {
        foreach (var tracker in positionTrackersToRecover)
        {
            if (tracker != null)
            {
                tracker.ForceReactivateTracking();
            }
        }
        
        ResetRecoveryState();
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (recoveryCheckInterval <= 0f)
            recoveryCheckInterval = 0.1f;
            
        if (maxRecoveryAttemptTime <= 0f)
            maxRecoveryAttemptTime = 0.5f;
            
        if (maxRecoveryAttempts <= 0)
            maxRecoveryAttempts = 1;
    }
    #endif
}