using UnityEngine;

/// <summary>
/// Enemy unit in 3D/2.5D space with armor damage reduction and tile hazard status handling.
/// </summary>
public class Enemy : MonoBehaviour
{
    public enum EnemyClass { Swordsman, Tanker, Rogue }
    public EnemyClass currentClass;

    [Header("Current Live Stats")]
    public float maxHP;
    public float currentHP;
    public float armorPercent; // e.g. 0.50 = 50% damage reduction

    private float baseSpeed;
    private float dotDamagePerSecond;
    private float effectDurationTimer;
    private bool isEffectInfinite;

    private EnemyPathFinding pathfindingScript;

    void Awake()
    {
        pathfindingScript = GetComponent<EnemyPathFinding>();

        // Ensure 2.5D billboard component is attached
        if (GetComponent<Billboard>() == null)
        {
            gameObject.AddComponent<Billboard>();
        }
    }

    public void InitializeEnemy(EnemyClass targetClass)
    {
        currentClass = targetClass;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        switch (currentClass)
        {
            case EnemyClass.Swordsman:
                maxHP = 100f; armorPercent = 0f; baseSpeed = 1.0f;
                SetPlaceholderVisuals(sr, new Vector3(1.1f, 1.1f, 1f));
                break;
            case EnemyClass.Tanker:
                maxHP = 200f; armorPercent = 0.50f; baseSpeed = 0.6f;
                SetPlaceholderVisuals(sr, new Vector3(1.4f, 1.4f, 1f));
                break;
            case EnemyClass.Rogue:
                maxHP = 60f; armorPercent = 0f; baseSpeed = 1.8f;
                SetPlaceholderVisuals(sr, new Vector3(0.9f, 0.9f, 1f));
                break;
        }

        currentHP = maxHP;
    }

    void SetPlaceholderVisuals(SpriteRenderer sr, Vector3 scale)
    {
        if (sr != null)
        {
            sr.transform.localScale = scale * 1.5f;
            sr.color = Color.white;
        }
    }

    void Update()
    {
        if (effectDurationTimer > 0 || isEffectInfinite)
        {
            if (dotDamagePerSecond > 0)
            {
                TakeDamage(dotDamagePerSecond * Time.deltaTime, isStatusEffect: true);
            }

            if (!isEffectInfinite)
            {
                effectDurationTimer -= Time.deltaTime;
                if (effectDurationTimer <= 0) ResetStatusEffects();
            }
        }
    }

    public void ApplyTileHazard(TileProperty.HazardData hazard)
    {
        if (hazard.damage > 0) TakeDamage(hazard.damage, isStatusEffect: false);

        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed * hazard.speedMult;

        dotDamagePerSecond = hazard.dotDamage;
        if (hazard.duration == -1)
        {
            isEffectInfinite = true;
            effectDurationTimer = 0;
        }
        else
        {
            isEffectInfinite = false;
            effectDurationTimer = hazard.duration;
        }
    }

    void ResetStatusEffects()
    {
        dotDamagePerSecond = 0;
        isEffectInfinite = false;
        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed;
    }

    public void TakeDamage(float incomingDamage, bool isStatusEffect)
    {
        float finalDamage = incomingDamage;
        if (!isStatusEffect) finalDamage = incomingDamage * (1f - armorPercent);

        currentHP -= finalDamage;

        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }
}
