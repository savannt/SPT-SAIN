using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotEventHandler
{
    public event Action<Grenade, Vector3, Vector3, float> OnGrenadeThrow;
    public event Action<Vector3, string, bool, float, float> OnGrenadeExplosive;

    public void PlaySound(IPlayer player, Vector3 position, float power, AISoundType type)
    {
    }
}

public class EFTStatModifiersClass : BotSettingsInGameModif
{
    public float GainSightCoef {
        get => RuntimeVisionEffectK;
        set => RuntimeVisionEffectK = value;
    }
}

public static class CompatibilityCollectionExtensions
{
    public static T PickRandom<T>(this IList<T> items)
    {
        if (items == null || items.Count == 0)
        {
            return default;
        }
        return items[UnityEngine.Random.Range(0, items.Count)];
    }

    public static T PickRandom<T>(this T[] items)
    {
        if (items == null || items.Length == 0)
        {
            return default;
        }
        return items[UnityEngine.Random.Range(0, items.Length)];
    }

    public static KeyValuePair<TKey, TValue> Random<TKey, TValue>(this IDictionary<TKey, TValue> items)
    {
        if (items == null || items.Count == 0)
        {
            return default;
        }
        return items.ElementAt(UnityEngine.Random.Range(0, items.Count));
    }

    public static string MaskToString(this LayerMask mask)
    {
        return mask.value.ToString();
    }
}

public static class LayerMaskClass
{
    public static readonly LayerMask HighPolyWithTerrainMask = Mask("HighPolyCollider", "Terrain", "LowPolyCollider", "Default");
    public static readonly LayerMask HighPolyWithTerrainNoGrassMask = HighPolyWithTerrainMask;
    public static readonly LayerMask HighPolyWithTerrainMaskAI = HighPolyWithTerrainMask;
    public static readonly LayerMask PlayerMask = Mask("Player", "PlayerSpirit");
    public static readonly LayerMask HighPolyCollider = Mask("HighPolyCollider");
    public static readonly LayerMask TerrainLowPoly = Mask("Terrain", "LowPolyCollider");
    public static readonly LayerMask DefaultLayerMask = Mask("Default");
    public static readonly LayerMask AI = Mask("AI", "Player");
    public static readonly LayerMask Grass = Mask("Grass");
    public static readonly LayerMask TerrainLayer = Mask("Terrain");
    public static readonly LayerMask TerrainMask = Mask("Terrain");
    public static readonly LayerMask HitColliderMask = Mask("HitCollider");
    public static readonly LayerMask PlayerCollisionsMask = Mask("PlayerCollisionTest", "Player");
    public static readonly LayerMask PlayerStaticDoorMask = Mask("HighPolyCollider", "LowPolyCollider", "Interactive");
    public static readonly LayerMask PlayerStaticCollisionsMask = Mask("HighPolyCollider", "LowPolyCollider", "Default");
    public static readonly LayerMask ShellsCollisionsMask = Mask("Shells");
    public static readonly LayerMask PlayerCollisionTestMask = Mask("PlayerCollisionTest");
    public static readonly LayerMask GrenadeAffectedMask = Mask("Player", "HitCollider");
    public static readonly LayerMask GrenadeObstaclesColliderMask = HighPolyWithTerrainMask;
    public static readonly LayerMask WaterLayer = Mask("Water");
    public static readonly LayerMask LootLayerMask = Mask("Loot");
    public static readonly LayerMask LootLayer = Mask("Loot");
    public static readonly LayerMask InteractiveMask = Mask("Interactive");
    public static readonly LayerMask InteractiveLayer = Mask("Interactive");
    public static readonly LayerMask LootCollisionMask = Mask("Loot", "HighPolyCollider");
    public static readonly LayerMask TriggersLayer = Mask("Triggers");
    public static readonly LayerMask TriggersMask = Mask("Triggers");
    public static readonly LayerMask AudioControllerStepLayerMask = HighPolyWithTerrainMask;
    public static readonly LayerMask TransparentLayerMask = Mask("TransparentFX");

    public static readonly int PlayerLayer = Layer("Player");
    public static readonly int DeadbodyLayer = Layer("Deadbody");
    public static readonly int HitColliderLayer = Layer("HitCollider");
    public static readonly int DoorLayer = Layer("Interactive");
    public static readonly int LowPolyColliderLayer = Layer("LowPolyCollider");
    public static readonly LayerMask LowPolyColliderLayerMask = Mask("LowPolyCollider");
    public static readonly int ShellsLayer = Layer("Shells");
    public static readonly int PlayerCollisionTestLayer = Layer("PlayerCollisionTest");
    public static readonly int WeaponPreview = Layer("WeaponPreview");

    private static int Layer(string name)
    {
        int layer = LayerMask.NameToLayer(name);
        return layer >= 0 ? layer : 0;
    }

    private static LayerMask Mask(params string[] names)
    {
        int mask = 0;
        foreach (string name in names)
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer >= 0)
            {
                mask |= 1 << layer;
            }
        }
        return mask != 0 ? mask : Physics.DefaultRaycastLayers;
    }
}
