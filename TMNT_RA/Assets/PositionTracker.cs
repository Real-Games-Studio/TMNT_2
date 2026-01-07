using UnityEngine;

public class PositionTracker : MonoBehaviour
{
    [SerializeField, Tooltip("Objeto a ser seguindo enquanto estiver ativo na hierarquia.")]
    private Transform target; // Objeto a ser movido, se ele estiver ativo na hierarchia, devemos seguir. se nao. voltar para a posicao inicial

    public Transform Target => target;

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

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        Debug.Log($"[PositionTracker] {name} Awake: target={(target != null ? target.name : "NULL")}, objectsToDisable.Length={objectsToDisable?.Length ?? 0}");

        // Validação: Verifica se o PositionTracker está associado ao FaceObject correto
        if (target != null)
        {
            var faceObject = target.GetComponent<Imagine.WebAR.FaceObject>();
            if (faceObject != null)
            {
                // Extrai o número do nome do PositionTracker (ex: "HeadTrackerObjectHolder" = 0, "HeadTrackerObjectHolder (1)" = 1)
                string trackerName = name.Replace("HeadTrackerObjectHolder", "").Trim();
                int trackerIndex = 0;

                if (trackerName.StartsWith("(") && trackerName.EndsWith(")"))
                {
                    string numberStr = trackerName.Substring(1, trackerName.Length - 2);
                    if (int.TryParse(numberStr, out int parsed))
                    {
                        trackerIndex = parsed;
                    }
                }

                if (trackerIndex != faceObject.faceIndex)
                {
                    Debug.LogError($"[PositionTracker] ❌ ERRO DE CONFIGURAÇÃO! {name} (índice {trackerIndex}) está associado ao {target.name} (faceIndex {faceObject.faceIndex}). Eles devem ter o MESMO índice!");
                }
                else
                {
                    Debug.Log($"[PositionTracker] ✓ {name} (índice {trackerIndex}) corretamente associado ao {target.name} (faceIndex {faceObject.faceIndex})");
                }
            }
        }

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

        // Log quando detecta mudança de estado do target
        if (shouldTrack && !wasTrackingLastFrame)
        {
            Debug.Log($"[PositionTracker] {name} - Target ATIVADO: {target.name} (parent={target.parent?.name})");
        }
        else if (!shouldTrack && wasTrackingLastFrame)
        {
            Debug.Log($"[PositionTracker] {name} - Target DESATIVADO: {target?.name}");
        }

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
        Debug.Log($"[PositionTracker] {name} StartTracking chamado! target={target.name}");

        // Ativa a máscara correspondente ao faceIndex do target
        if (objectsToDisable != null && objectsToDisable.Length > 0)
        {
            Debug.Log($"[PositionTracker] {name} - Ativando máscara baseada no faceIndex...");
            ActivateChildByFaceIndex();
        }
        else
        {
            Debug.LogWarning($"[PositionTracker] {name} - objectsToDisable está NULL ou vazio!");
        }
    }

    private void StopTracking()
    {
        DeactivateAllChildren();

        // Libera a máscara para que outros possam usar
        MaskDistributionManager.Instance.ReleaseMask(this);

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

    public void ActivateChildByFaceIndex()
    {
        if (objectsToDisable == null || objectsToDisable.Length == 0)
        {
            Debug.LogWarning($"[PositionTracker] {name} ActivateChildByFaceIndex: objectsToDisable NULL ou vazio!");
            return;
        }

        // Usa o MaskDistributionManager para obter um índice de máscara disponível
        int maskIndex = MaskDistributionManager.Instance.GetAvailableMaskIndex(this);

        if (maskIndex < 0 || maskIndex >= objectsToDisable.Length)
        {
            Debug.LogWarning($"[PositionTracker] {name} - Índice de máscara inválido: {maskIndex}. objectsToDisable.Length={objectsToDisable.Length}");
            return;
        }

        Debug.Log($"[PositionTracker] {name} - MaskDistributionManager atribuiu máscara {maskIndex}");
        ActivateChildAtIndex(maskIndex);
    }

    private void ActivateChildAtIndex(int index)
    {
        if (objectsToDisable == null || objectsToDisable.Length == 0) return;

        Debug.Log($"[PositionTracker] {name} ActivateChildAtIndex: desativando todos e ativando índice {index}");
        DeactivateAllChildren();

        if (index < 0 || index >= objectsToDisable.Length)
        {
            Debug.LogWarning($"[PositionTracker] {name} - Índice {index} fora do range! Total: {objectsToDisable.Length}");
            return;
        }

        GameObject childToActivate = objectsToDisable[index];
        if (childToActivate == null)
        {
            Debug.LogWarning($"[PositionTracker] {name} - Máscara no índice {index} é NULL!");
            return;
        }

        currentChildIndex = index;
        childToActivate.SetActive(true);
        Debug.Log($"[PositionTracker] ✓ {name} ATIVOU máscara: {childToActivate.name} (índice {index})");
    }

    public void ActivateRandomChild()
    {
        if (objectsToDisable == null || objectsToDisable.Length == 0)
        {
            Debug.LogWarning($"[PositionTracker] {name} ActivateRandomChild: objectsToDisable NULL ou vazio!");
            return;
        }

        Debug.Log($"[PositionTracker] {name} ActivateRandomChild: desativando todos e ativando 1 de {objectsToDisable.Length} máscaras");
        DeactivateAllChildren();

        int totalObjects = objectsToDisable.Length;
        for (int offset = 1; offset <= totalObjects; offset++)
        {
            int nextIndex = (currentChildIndex + offset) % totalObjects;
            GameObject candidate = objectsToDisable[nextIndex];

            if (candidate == null)
            {
                Debug.LogWarning($"[PositionTracker] {name} - Máscara no índice {nextIndex} é NULL!");
                continue;
            }

            currentChildIndex = nextIndex;
            candidate.SetActive(true);

            Debug.Log($"[PositionTracker] ✓ {name} ATIVOU máscara: {candidate.name} (índice {nextIndex})");

            // Notifica o PropSpawnAnimator se existir
            // PropSpawnAnimator animator = candidate.GetComponent<PropSpawnAnimator>();
            // if (animator != null)
            // {
            //     animator.StartSpawnAnimation();
            // }

            return;
        }

        Debug.LogError($"[PositionTracker] {name} - FALHOU ao ativar qualquer máscara! Todas são NULL?");
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
