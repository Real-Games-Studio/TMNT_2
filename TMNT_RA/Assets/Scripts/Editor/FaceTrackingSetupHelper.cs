using UnityEngine;
using UnityEditor;
using Imagine.WebAR;
using System.Collections.Generic;

/// <summary>
/// Helper no Editor para configurar automaticamente o link entre PositionTrackers e FaceObjects.
/// Menu: Tools > Face Tracking > Auto-Link Position Trackers
/// </summary>
public class FaceTrackingSetupHelper : EditorWindow
{
    [MenuItem("Tools/Face Tracking/Auto-Link Position Trackers to FaceObjects")]
    public static void AutoLinkPositionTrackers()
    {
        FaceObject[] faceObjects = FindObjectsByType<FaceObject>(FindObjectsSortMode.None);
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);

        if (faceObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Erro", "Nenhum FaceObject encontrado na cena!", "OK");
            return;
        }

        if (trackers.Length == 0)
        {
            EditorUtility.DisplayDialog("Erro", "Nenhum PositionTracker encontrado na cena!", "OK");
            return;
        }

        // Ordena FaceObjects por faceIndex
        System.Array.Sort(faceObjects, (a, b) => a.faceIndex.CompareTo(b.faceIndex));

        int linked = 0;
        for (int i = 0; i < Mathf.Min(faceObjects.Length, trackers.Length); i++)
        {
            FaceObject faceObj = faceObjects[i];
            PositionTracker tracker = trackers[i];

            // Usa reflection para setar o target
            var targetField = typeof(PositionTracker).GetField("target",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (targetField != null)
            {
                Undo.RecordObject(tracker, "Link PositionTracker to FaceObject");
                targetField.SetValue(tracker, faceObj.transform);
                EditorUtility.SetDirty(tracker);
                linked++;

                Debug.Log($"✓ Linked {tracker.name} → {faceObj.name} (FaceIndex {faceObj.faceIndex})");
            }
        }

        if (linked > 0)
        {
            EditorUtility.DisplayDialog("Sucesso!",
                $"✓ {linked} PositionTrackers foram vinculados aos FaceObjects!\n\n" +
                $"Verifique o Console para detalhes.",
                "OK");
        }
    }

    [MenuItem("Tools/Face Tracking/Verify Setup")]
    public static void VerifySetup()
    {
        FaceObject[] faceObjects = FindObjectsByType<FaceObject>(FindObjectsSortMode.None);
        PositionTracker[] trackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);

        string report = "VERIFICAÇÃO DE CONFIGURAÇÃO:\n\n";

        report += $"FaceObjects: {faceObjects.Length}\n";
        foreach (var fo in faceObjects)
        {
            report += $"  • {fo.name} (FaceIndex: {fo.faceIndex})\n";
        }

        report += $"\nPositionTrackers: {trackers.Length}\n";

        Dictionary<Transform, int> targetCount = new Dictionary<Transform, int>();
        bool hasProblems = false;

        foreach (var tracker in trackers)
        {
            var targetField = typeof(PositionTracker).GetField("target",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Transform target = targetField?.GetValue(tracker) as Transform;

            var objectsField = typeof(PositionTracker).GetField("objectsToDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject[] objects = objectsField?.GetValue(tracker) as GameObject[];

            report += $"  • {tracker.name}\n";

            if (target == null)
            {
                report += $"    ⚠ Target: NULL (PROBLEMA!)\n";
                hasProblems = true;
            }
            else
            {
                FaceObject targetFace = target.GetComponent<FaceObject>();
                if (targetFace != null)
                {
                    report += $"    Target: {target.name} (FaceIndex {targetFace.faceIndex})\n";

                    if (!targetCount.ContainsKey(target))
                        targetCount[target] = 0;
                    targetCount[target]++;
                }
                else
                {
                    report += $"    ⚠ Target: {target.name} (NÃO É FaceObject - PROBLEMA!)\n";
                    hasProblems = true;
                }
            }

            if (objects == null || objects.Length != 4)
            {
                report += $"    ⚠ Wearables: {(objects != null ? objects.Length : 0)} (Deveria ser 4 - PROBLEMA!)\n";
                hasProblems = true;
            }
            else
            {
                report += $"    Wearables: {objects.Length} ✓\n";
            }
        }

        // Check duplicates
        report += "\nANÁLISE:\n";
        foreach (var kvp in targetCount)
        {
            if (kvp.Value > 1)
            {
                report += $"  ⚠⚠⚠ PROBLEMA: {kvp.Value} trackers seguem o mesmo FaceObject ({kvp.Key.name})!\n";
                hasProblems = true;
            }
        }

        if (!hasProblems)
        {
            report += "  ✓ Tudo configurado corretamente!\n";
        }
        else
        {
            report += "\n⚠ Use 'Auto-Link Position Trackers' para corrigir automaticamente.";
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("Verificação Completa", report, "OK");
    }

    [MenuItem("Tools/Face Tracking/Create Missing FaceObjects")]
    public static void CreateMissingFaceObjects()
    {
        FaceObject[] existing = FindObjectsByType<FaceObject>(FindObjectsSortMode.None);
        HashSet<int> existingIndices = new HashSet<int>();

        foreach (var fo in existing)
        {
            existingIndices.Add(fo.faceIndex);
        }

        int created = 0;
        for (int i = 0; i < 4; i++)
        {
            if (!existingIndices.Contains(i))
            {
                GameObject go = new GameObject($"FaceObject_{i}");
                FaceObject fo = go.AddComponent<FaceObject>();
                fo.faceIndex = i;
                go.SetActive(false);

                Debug.Log($"✓ Criado FaceObject_{i} (FaceIndex: {i})");
                created++;
            }
        }

        if (created > 0)
        {
            EditorUtility.DisplayDialog("FaceObjects Criados",
                $"✓ {created} FaceObjects foram criados!\n\n" +
                "Configure-os manualmente com máscaras/wearables.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Info",
                "Todos os 4 FaceObjects já existem na cena.",
                "OK");
        }
    }
}
