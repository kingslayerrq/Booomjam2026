using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    [SerializeField] private MeshRenderer torso;
    [SerializeField] private MeshRenderer head;
    [SerializeField] private MeshRenderer armL;
    [SerializeField] private MeshRenderer armR;
    [SerializeField] private MeshRenderer legL;
    [SerializeField] private MeshRenderer legR;

    public void Apply(CharacterAppearance appearance)
    {
        if (appearance == null)
        {
            Debug.LogWarning($"[CharacterVisual] {name}: no appearance assigned.", this);
            return;
        }

        SetMaterial(torso, appearance.torso, "torso");
        SetMaterial(head, appearance.head, "head");
        SetMaterial(armL, appearance.armL, "armL");
        SetMaterial(armR, appearance.armR, "armR");
        SetMaterial(legL, appearance.legL, "legL");
        SetMaterial(legR, appearance.legR, "legR");
    }

    private void SetMaterial(MeshRenderer renderer, Material material, string bodyPart)
    {
        if (renderer == null)
        {
            Debug.LogWarning($"[CharacterVisual] {name}: missing renderer for {bodyPart}.", this);
            return;
        }

        if (material == null)
        {
            Debug.LogWarning($"[CharacterVisual] {name}: missing material for {bodyPart}; keeping prefab material.", this);
            return;
        }

        renderer.sharedMaterial = material;
    }
}
