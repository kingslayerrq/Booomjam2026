using UnityEngine;

public class HighlightComponent : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Material highlightMaterial;
    
    [SerializeField] private Renderer[] renderers;
    
    private Material[][] originalMaterials;
    private bool isHighlighted = false;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
    }

    public void SetHighlight(bool highlighted)
    {
        if (isHighlighted == highlighted) return;
        isHighlighted = highlighted;
        if (isHighlighted)
        {
            Highlight();
        }
        else
        {
            RemoveHighlight();
        }
    }

    private void Highlight()
    {
        if (highlightMaterial == null) return;

        CacheCurrentMaterials();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Material[] highlightedMaterials = new Material[renderers[i].sharedMaterials.Length];

            for (int j = 0; j < highlightedMaterials.Length; j++)
            {
                highlightedMaterials[j] = highlightMaterial;
            }
            renderers[i].sharedMaterials = highlightedMaterials;
        }
    }

    private void RemoveHighlight()
    {
        if (originalMaterials == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || i >= originalMaterials.Length || originalMaterials[i] == null)
                continue;

            renderers[i].sharedMaterials = originalMaterials[i];
        }
    }

    private void CacheCurrentMaterials()
    {
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i] != null ? renderers[i].sharedMaterials : null;
        }
    }
}
