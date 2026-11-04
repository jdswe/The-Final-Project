using UnityEngine;

/// <summary>
/// Utilidad para instanciar los efectos de los packs de VFX del proyecto
/// (Assets/VFX y Magic effects pack) y destruirlos solos al terminar.
/// </summary>
public static class VfxUtil
{
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 2.5f, Transform parent = null)
    {
        if (prefab == null) return null;
        GameObject vfx = Object.Instantiate(prefab, position, rotation, parent);
        foreach (ParticleSystem ps in vfx.GetComponentsInChildren<ParticleSystem>()) ps.Play();
        if (lifetime > 0f) Object.Destroy(vfx, lifetime);
        return vfx;
    }
}
