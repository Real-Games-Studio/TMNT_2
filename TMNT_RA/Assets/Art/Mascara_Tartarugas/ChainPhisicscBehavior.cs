using UnityEngine;

[DisallowMultipleComponent]
public class ChainPhysicsBuilder_FollowTarget : MonoBehaviour
{
    [Header("Bones (root -> tip)")]
    public Transform[] bones;

    [Header("Simulation Settings")]
    public Transform rootFollowTarget;
    public int iterations = 8;
    public float damping = 0.97f;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Header("Flex Settings")]
    [Range(0f, 1f)] public float stiffnessRoot = 0.9f; // base mais firme
    [Range(0f, 1f)] public float stiffnessTip = 0.1f;  // ponta mais solta

    [Header("Peso progressivo")]
    [Range(0f, 1f)] public float gravityRootMultiplier = 0.2f;
    [Range(0f, 2f)] public float gravityTipMultiplier = 1.2f;

    private Vector3[] positions;
    private Vector3[] prevPositions;
    private float[] segmentLengths;
    private float[] boneStiffness;
    private float[] boneGravity;

    void Start()
    {
        if (bones == null || bones.Length < 2)
        {
            Debug.LogError("Chain precisa de pelo menos 2 ossos!");
            enabled = false;
            return;
        }

        int n = bones.Length;
        positions = new Vector3[n];
        prevPositions = new Vector3[n];
        segmentLengths = new float[n - 1];
        boneStiffness = new float[n];
        boneGravity = new float[n];

        // inicializa posições e comprimentos
        for (int i = 0; i < n; i++)
        {
            positions[i] = bones[i].position;
            prevPositions[i] = positions[i];

            float t = (float)i / (n - 1);
            boneStiffness[i] = Mathf.Lerp(stiffnessRoot, stiffnessTip, t);
            boneGravity[i] = Mathf.Lerp(gravityRootMultiplier, gravityTipMultiplier, t);
        }

        for (int i = 0; i < n - 1; i++)
        {
            segmentLengths[i] = Vector3.Distance(bones[i].position, bones[i + 1].position);
        }
    }

    void LateUpdate()
    {
        Simulate(Time.deltaTime);
        ApplyToBones();
    }

    void Simulate(float dt)
    {
        int n = bones.Length;

        // 1️⃣ Verlet integration — aplica gravidade e amortecimento
        for (int i = 1; i < n; i++)
        {
            Vector3 velocity = (positions[i] - prevPositions[i]) * damping;
            prevPositions[i] = positions[i];
            positions[i] += velocity + gravity * boneGravity[i] * dt * dt;
        }

        // 2️⃣ Corrigir raiz
        if (rootFollowTarget != null)
            positions[0] = rootFollowTarget.position;

        // 3️⃣ Resolver restrições (distâncias e rigidez)
        for (int it = 0; it < iterations; it++)
        {
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p1 = positions[i];
                Vector3 p2 = positions[i + 1];

                Vector3 delta = p2 - p1;
                float len = delta.magnitude;
                if (len < 0.0001f) continue;

                float diff = (len - segmentLengths[i]) / len;
                Vector3 offset = delta * 0.5f * diff;

                float stiffnessA = boneStiffness[i];
                float stiffnessB = boneStiffness[i + 1];

                if (i == 0 && rootFollowTarget != null)
                {
                    // raiz fixa, só move o próximo
                    positions[i + 1] -= offset * 2f * (1f - stiffnessB);
                }
                else
                {
                    positions[i] += offset * (1f - stiffnessA);
                    positions[i + 1] -= offset * (1f - stiffnessB);
                }
            }
        }
    }

    void ApplyToBones()
    {
        for (int i = 0; i < bones.Length; i++)
            bones[i].position = positions[i];
    }
}
