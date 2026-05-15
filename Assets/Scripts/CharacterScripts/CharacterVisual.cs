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
        if (appearance == null) return;
        SetMaterial(torso, appearance.torso);
        SetMaterial(head, appearance.head);
        SetMaterial(armL, appearance.armL);
        SetMaterial(armR, appearance.armR);
        SetMaterial(legL, appearance.legL);
        SetMaterial(legR, appearance.legR);
    }

    private static void SetMaterial(MeshRenderer renderer, Material material)
    {
        if (renderer == null || material == null) return;
        renderer.material = material;
    }
}
