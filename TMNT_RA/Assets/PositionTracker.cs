using UnityEngine;

public class PositionTracker : MonoBehaviour
{
    [SerializeField, Tooltip("Objeto a ser seguindo enquanto estiver ativo na hierarquia.")]
    private Transform target; // Objeto a ser movido, se ele estiver ativo na hierarchia, devemos seguir. se nao. voltar para a posicao inicial

    [SerializeField, Tooltip("Lista de objetos filhos onde apenas um deve ser ativado de forma aleatória durante o tracking.")]
    private GameObject[] objectsToDisable; // Este objeto tera sempre 3 objetos filhos, e so um deles deve estar ativo qndo o tracking for chamado. isso de forma randomica

    [Header("Suavização")]
    [SerializeField, Range(0.01f, 20f), Tooltip("Velocidade de interpolação para alcançar a posição do alvo.")]
    private float positionLerpSpeed = 8f;

    [SerializeField, Range(0.01f, 20f), Tooltip("Velocidade de interpolação para alcançar a rotação do alvo.")]
    private float rotationLerpSpeed = 10f;

    private Vector3 initialPosition; // Posicao de referencia qndo o tracking n estiver rodando
    private Quaternion initialRotation;
    private bool isTracking;

    [Header("Re-tracking")]
    [SerializeField, Range(0.1f, 2f), Tooltip("Tempo de tolerância para perda temporária de tracking antes de desativar.")]
    private float trackingLossTolerance = 0.5f;

    private float lastTrackingTime;
    private bool wasTrackingLastFrame;
    private int currentChildIndex = -1;
    private bool hasAssignedWearable = false;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        DeactivateAllChildren();
        lastTrackingTime = Time.time;
        wasTrackingLastFrame = false;
        currentChildIndex = -1;
    }

    private void OnEnable()
    {
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        isTracking = false;
    }

    private void Update()
    {
        bool shouldTrack = target != null && target.gameObject.activeInHierarchy;

        if (shouldTrack)
        {
            lastTrackingTime = Time.time;

            if (!isTracking)
            {
                StartTracking();
            }

            FollowTarget();
            wasTrackingLastFrame = true;
        }
        else
        {
            // Implement tolerance for temporary tracking loss
            float timeSinceLastTracking = Time.time - lastTrackingTime;

            if (wasTrackingLastFrame && timeSinceLastTracking < trackingLossTolerance)
            {
                // Still within tolerance, keep tracking state but don't update position
                // This prevents flickering when tracking is temporarily lost
                return;
            }

            if (isTracking)
            {
                StopTracking();
            }

            ReturnToInitialPosition();
            wasTrackingLastFrame = false;
        }
    }

    private void StartTracking()
    {
        isTracking = true;
        // Ensure at least one wearable is visible when tracking resumes
        if (objectsToDisable != null && objectsToDisable.Length > 0)
        {
            bool anyActive = false;
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null && obj.activeSelf)
                {
                    anyActive = true;
                    break;
                }
            }

            if (!anyActive)
            {
                ActivateRandomChild();
            }
        }
    }

    private void StopTracking()
    {
        DeactivateAllChildren();

        // Libera o wearable quando para de rastrear
        if (hasAssignedWearable && WearableManager.Instance != null)
        {
            WearableManager.Instance.ReleaseWearableIndex(this);
            hasAssignedWearable = false;
            currentChildIndex = -1;
        }

        isTracking = false;
    }

    private void FollowTarget()
    {
        if (target == null)
        {
            return;
        }

        float tPos = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
        float tRot = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, target.position, tPos);
        Quaternion smoothedRotation = Quaternion.Slerp(transform.rotation, target.rotation, tRot);

        transform.SetPositionAndRotation(smoothedPosition, smoothedRotation);
    }

    private void ReturnToInitialPosition()
    {
        transform.SetPositionAndRotation(initialPosition, initialRotation);
    }

    public void ActivateRandomChild()
    {
        if (objectsToDisable == null || objectsToDisable.Length == 0)
        {
            return;
        }

        DeactivateAllChildren();

        int wearableIndex = -1;

        // Usa o WearableManager para garantir que não haja repetição entre trackers
        if (WearableManager.Instance != null)
        {
            wearableIndex = WearableManager.Instance.AssignWearableIndex(this);
            hasAssignedWearable = true;

            if (wearableIndex == -1)
            {
                Debug.LogWarning($"[PositionTracker] {name} não conseguiu obter um wearable único - todos estão em uso!");
                return;
            }

            // Garante que o índice está dentro dos limites do array
            if (wearableIndex >= objectsToDisable.Length)
            {
                Debug.LogError($"[PositionTracker] {name} recebeu índice {wearableIndex} mas só tem {objectsToDisable.Length} wearables!");
                return;
            }

            currentChildIndex = wearableIndex;
        }
        else
        {
            // Fallback: comportamento antigo se o WearableManager não existir
            Debug.LogWarning("[PositionTracker] WearableManager não encontrado! Usando sistema antigo (pode repetir wearables)");

            if (objectsToDisable.Length > 1)
            {
                // Sorteia até pegar um índice diferente do último
                do
                {
                    wearableIndex = Random.Range(0, objectsToDisable.Length);
                }
                while (wearableIndex == currentChildIndex);
            }
            else
            {
                wearableIndex = 0;
            }

            currentChildIndex = wearableIndex;
        }

        GameObject childToActivate = objectsToDisable[currentChildIndex];

        if (childToActivate != null)
        {
            childToActivate.SetActive(true);
            Debug.Log($"[PositionTracker] {name} ativou wearable {currentChildIndex}: {childToActivate.name}");
        }
    }

    /// <summary>
    /// Força a reativação do tracking, útil para recuperar de perdas temporárias
    /// </summary>
    public void ForceReactivateTracking()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            lastTrackingTime = Time.time;
            wasTrackingLastFrame = true;

            if (!isTracking)
            {
                StartTracking();
            }

            // Reativa um filho aleatório se há objetos para controlar
            ActivateRandomChild();
        }
    }

    private void DeactivateAllChildren()
    {
        if (objectsToDisable == null)
        {
            return;
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (objectsToDisable == null)
        {
            return;
        }

        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            GameObject obj = objectsToDisable[i];
            if (obj != null && obj.transform.parent != transform)
            {
                Debug.LogWarning($"{name}: '{obj.name}' não é filho deste objeto e não será controlado corretamente.", this);
            }
        }
    }
#endif


}
