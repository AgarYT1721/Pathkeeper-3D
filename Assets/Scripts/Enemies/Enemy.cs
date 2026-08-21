using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy unit in 3D/2.5D space supporting all 6 Master Stat Table classes with passive auras,
/// special ability mechanics (Cleansing Aura, Inspirational Buff, Lay on Hands),
/// advanced GDD status effects (Bleed, Burn, Poison, Freeze, Slow, Static, Curse),
/// DP rewards, hit recoil, death pop-dissolve animations, and Arknights-style floating combat text.
/// </summary>
public class Enemy : MonoBehaviour
{
    public enum EnemyClass { Swordsman, Tanker, Rogue, Priest, Supporter, Paladin }
    public EnemyClass currentClass;

    [Header("Current Live Stats")]
    public float maxHP = 100f;
    public float currentHP = 100f;
    public float armorPercent = 0f; // e.g. 0.50 = 50% damage reduction
    public float baseSpeed = 1.0f;

    [Header("Special Ability State")]
    public bool hasTriggeredSpecial = false;
    private int supporterBuffsCast = 0;
    private float abilityCooldownTimer = 0f;

    // Status effect timers
    private float dotDamagePerSecond;
    private float effectDurationTimer;
    private bool isEffectInfinite;
    private float bleedDurationTimer = 0f;
    private float bleedDamageMultiplier = 25f;
    private float dotTickTimer = 0f;
    private float bleedDistanceAccumulator = 0f;
    private float slowDurationTimer = 0f;
    private float currentSlowMult = 1f;

    private Vector3 lastPosition;
    private bool isDying = false;

    private EnemyPathFinding pathfindingScript;
    private HealthBar healthBar;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        pathfindingScript = GetComponent<EnemyPathFinding>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        lastPosition = transform.position;
        isDying = false;

        // Ensure 2.5D billboard component is attached
        if (GetComponent<Billboard>() == null)
        {
            gameObject.AddComponent<Billboard>();
        }

        // Attach tactical Arknights floating health bar
        healthBar = GetComponent<HealthBar>();
        if (healthBar == null)
        {
            healthBar = gameObject.AddComponent<HealthBar>();
        }
    }

    public void InitializeEnemy(EnemyClass targetClass)
    {
        currentClass = targetClass;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        hasTriggeredSpecial = false;
        supporterBuffsCast = 0;
        abilityCooldownTimer = 2.0f;
        lastPosition = transform.position;
        isDying = false;
        slowDurationTimer = 0f;
        currentSlowMult = 1f;

        switch (currentClass)
        {
            case EnemyClass.Swordsman:
                maxHP = 100f; armorPercent = 0.00f; baseSpeed = 1.0f;
                SetPlaceholderVisuals(spriteRenderer, new Vector3(1.1f, 1.1f, 1f));
                break;

            case EnemyClass.Tanker:
                maxHP = 200f; armorPercent = 0.50f; baseSpeed = 0.6f;
                SetPlaceholderVisuals(spriteRenderer, new Vector3(1.4f, 1.4f, 1f));
                break;

            case EnemyClass.Rogue:
                maxHP = 60f; armorPercent = 0.00f; baseSpeed = 1.8f;
                SetPlaceholderVisuals(spriteRenderer, new Vector3(0.9f, 0.9f, 1f));
                break;

            case EnemyClass.Priest:
                maxHP = 75f; armorPercent = 0.00f; baseSpeed = 0.9f;
                SetPlaceholderVisuals(spriteRenderer, new Vector3(1.05f, 1.05f, 1f));
                break;

            case EnemyClass.Supporter:
                maxHP = 80f; armorPercent = 0.05f; baseSpeed = 1.0f;
                SetPlaceholderVisuals(spriteRenderer, new Vector3(1.0f, 1.0f, 1f));
                break;

            case EnemyClass.Paladin:
                maxHP = 350f; armorPercent = 0.40f; baseSpeed = 0.7f;
                SetPlaceholderVisuals(spriteRenderer, new Vector3(1.5f, 1.5f, 1f));
                break;
        }

        currentHP = maxHP;

        if (pathfindingScript != null)
        {
            pathfindingScript.speed = baseSpeed;
        }

        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHP, maxHP, armorPercent);
        }
    }

    void SetPlaceholderVisuals(SpriteRenderer sr, Vector3 scale)
    {
        if (sr != null)
        {
            sr.transform.localScale = scale * 1.4f;
            sr.color = Color.white;
        }
    }

    void Update()
    {
        if (isDying) return;

        // 1. Process active Status DoTs (Burn, Poison) in 0.5s ticks
        if (effectDurationTimer > 0 || isEffectInfinite)
        {
            if (dotDamagePerSecond > 0)
            {
                dotTickTimer += Time.deltaTime;
                if (dotTickTimer >= 0.5f)
                {
                    dotTickTimer = 0f;
                    FloatingTextManager.TextType dotType = isEffectInfinite ? FloatingTextManager.TextType.Poison : FloatingTextManager.TextType.Burn;
                    TakeDamage(dotDamagePerSecond * 0.5f, isStatusEffect: true, dotType);
                }
            }

            if (!isEffectInfinite)
            {
                effectDurationTimer -= Time.deltaTime;
                if (effectDurationTimer <= 0) ResetStatusEffects();
            }
        }

        // 2. Process Kinetic Bleed (Damage triggered by movement distance)
        if (bleedDurationTimer > 0)
        {
            bleedDurationTimer -= Time.deltaTime;
            float distMoved = Vector3.Distance(transform.position, lastPosition);
            bleedDistanceAccumulator += distMoved;

            if (bleedDistanceAccumulator >= 0.35f)
            {
                float bleedDmg = bleedDistanceAccumulator * bleedDamageMultiplier;
                bleedDistanceAccumulator = 0f;
                TakeDamage(bleedDmg, isStatusEffect: true, FloatingTextManager.TextType.Bleed);
            }
        }
        lastPosition = transform.position;

        // 3. Process Slow & Root Timers
        if (slowDurationTimer > 0)
        {
            slowDurationTimer -= Time.deltaTime;
            if (slowDurationTimer <= 0)
            {
                currentSlowMult = 1f;
                if (pathfindingScript != null && !isDying)
                {
                    pathfindingScript.speed = baseSpeed;
                }
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
            }
        }

        // 4. Process Class-Specific Special Abilities
        UpdateSpecialAbilities();
    }

    private void UpdateSpecialAbilities()
    {
        if (isDying) return;
        abilityCooldownTimer -= Time.deltaTime;

        // Priest: Cleansing Aura
        if (currentClass == EnemyClass.Priest && !hasTriggeredSpecial && abilityCooldownTimer <= 0)
        {
            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            Enemy targetToHeal = null;
            float lowestHpRatio = 1f;

            foreach (Enemy other in allEnemies)
            {
                if (other == null || other == this || other.isDying) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist <= 3.5f)
                {
                    float ratio = other.currentHP / other.maxHP;
                    if (ratio < 0.75f && ratio < lowestHpRatio)
                    {
                        lowestHpRatio = ratio;
                        targetToHeal = other;
                    }
                }
            }

            if (targetToHeal != null)
            {
                hasTriggeredSpecial = true;
                targetToHeal.CleanseStatusEffects();
                targetToHeal.Heal(40f);
                StartCoroutine(AbilityPulseRoutine(new Color(1f, 0.95f, 0.4f, 1f)));
            }
            else
            {
                abilityCooldownTimer = 1.0f;
            }
        }

        // Supporter (Bard): Inspirational Buff
        if (currentClass == EnemyClass.Supporter && supporterBuffsCast < 2 && abilityCooldownTimer <= 0)
        {
            supporterBuffsCast++;
            abilityCooldownTimer = 4.5f;

            int buffType = Random.Range(0, 3);
            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            foreach (Enemy ally in allEnemies)
            {
                if (ally == null || ally.isDying) continue;
                float dist = Vector3.Distance(transform.position, ally.transform.position);
                if (dist <= 3.5f)
                {
                    if (buffType == 0) ally.ApplySpeedBuff(1.4f, 3.5f);
                    else if (buffType == 1) ally.ApplyArmorBuff(0.20f, 4.0f);
                    else ally.Heal(30f);
                }
            }

            StartCoroutine(AbilityPulseRoutine(new Color(0.9f, 0.4f, 1f, 1f)));
        }

        // Paladin: Lay on Hands
        if (currentClass == EnemyClass.Paladin && !hasTriggeredSpecial)
        {
            if (currentHP < maxHP * 0.50f)
            {
                hasTriggeredSpecial = true;
                CleanseStatusEffects();
                Heal(maxHP, "LAY ON HANDS! 👑");
                StartCoroutine(AbilityPulseRoutine(new Color(1f, 0.85f, 0.2f, 1f)));
            }
        }
    }

    public void Heal(float amount, string customLabel = null)
    {
        if (isDying) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHP, maxHP, armorPercent);
        }

        FloatingTextManager.SpawnHeal(transform.position, amount, customLabel);
        StartCoroutine(HealFlashRoutine());
    }

    public void CleanseStatusEffects()
    {
        ResetStatusEffects();
        bleedDurationTimer = 0f;
        bleedDistanceAccumulator = 0f;
        slowDurationTimer = 0f;
        currentSlowMult = 1f;
        if (pathfindingScript != null && !isDying) pathfindingScript.speed = baseSpeed;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        FloatingTextManager.SpawnStatus(transform.position, "CLEANSED! ✝", FloatingTextManager.TextType.Cleanse);
    }

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        if (pathfindingScript != null && !isDying)
        {
            FloatingTextManager.SpawnStatus(transform.position, "+40% HASTE! 🎵", FloatingTextManager.TextType.Buff);
            StartCoroutine(SpeedBuffRoutine(multiplier, duration));
        }
    }

    public void ApplyArmorBuff(float bonusArmor, float duration)
    {
        if (!isDying)
        {
            FloatingTextManager.SpawnStatus(transform.position, "+20% ARMOR! 🛡️", FloatingTextManager.TextType.Buff);
            StartCoroutine(ArmorBuffRoutine(bonusArmor, duration));
        }
    }

    private IEnumerator SpeedBuffRoutine(float mult, float dur)
    {
        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed * mult;
        yield return new WaitForSeconds(dur);
        if (pathfindingScript != null) pathfindingScript.speed = baseSpeed * currentSlowMult;
    }

    private IEnumerator ArmorBuffRoutine(float bonus, float dur)
    {
        armorPercent = Mathf.Clamp01(armorPercent + bonus);
        if (healthBar != null) healthBar.UpdateHealth(currentHP, maxHP, armorPercent);
        yield return new WaitForSeconds(dur);
        armorPercent = Mathf.Clamp01(armorPercent - bonus);
        if (healthBar != null) healthBar.UpdateHealth(currentHP, maxHP, armorPercent);
    }

    public void ApplySlow(float speedMult, float duration)
    {
        if (isDying) return;
        currentSlowMult = Mathf.Min(currentSlowMult, speedMult);
        slowDurationTimer = Mathf.Max(slowDurationTimer, duration);
        if (pathfindingScript != null)
        {
            pathfindingScript.speed = baseSpeed * currentSlowMult;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = speedMult == 0f ? new Color(0.4f, 0.85f, 1f, 1f) : new Color(0.75f, 0.75f, 0.90f, 1f);
        }
    }

    public void ApplyTileHazard(TileProperty.TileType trapType, TileProperty.HazardData hazard)
    {
        if (isDying) return;

        switch (trapType)
        {
            case TileProperty.TileType.Spike:
                TakeDamage(hazard.damage, isStatusEffect: false, FloatingTextManager.TextType.Physical);
                break;

            case TileProperty.TileType.Pitfall:
                TakeDamage(hazard.damage, isStatusEffect: false, FloatingTextManager.TextType.Physical);
                break;

            case TileProperty.TileType.Burn:
                TakeDamage(hazard.damage, isStatusEffect: false, FloatingTextManager.TextType.Burn);
                dotDamagePerSecond = hazard.dotDamage;
                isEffectInfinite = false;
                effectDurationTimer = hazard.duration;
                FloatingTextManager.SpawnStatus(transform.position, "IGNITED! 🔥", FloatingTextManager.TextType.Burn);
                break;

            case TileProperty.TileType.Poison:
                dotDamagePerSecond = hazard.dotDamage;
                isEffectInfinite = true;
                effectDurationTimer = 0;
                FloatingTextManager.SpawnStatus(transform.position, "POISONED! ☠️", FloatingTextManager.TextType.Poison);
                break;

            case TileProperty.TileType.Freeze:
                ApplySlow(0f, hazard.duration);
                FloatingTextManager.SpawnStatus(transform.position, "FROZEN! ❄️", FloatingTextManager.TextType.Freeze);
                break;

            case TileProperty.TileType.Static:
                TakeDamage(hazard.damage, isStatusEffect: false, FloatingTextManager.TextType.Static);
                ApplySlow(hazard.speedMult, hazard.duration);
                break;

            case TileProperty.TileType.Slow:
                ApplySlow(hazard.speedMult, hazard.duration);
                FloatingTextManager.SpawnStatus(transform.position, "SLOWED! 🕸️", FloatingTextManager.TextType.Static);
                break;

            case TileProperty.TileType.Bleed:
                bleedDamageMultiplier = hazard.damage > 0 ? hazard.damage : 25f;
                bleedDurationTimer = hazard.duration;
                FloatingTextManager.SpawnStatus(transform.position, "BLEEDING! 🩸", FloatingTextManager.TextType.Bleed);
                break;

            case TileProperty.TileType.Curse:
                float shred = hazard.damage > 0 ? hazard.damage : 0.35f;
                armorPercent = Mathf.Max(0f, armorPercent - shred);
                if (healthBar != null) healthBar.UpdateHealth(currentHP, maxHP, armorPercent);
                FloatingTextManager.SpawnStatus(transform.position, $"ARMOR SHRED! -{Mathf.RoundToInt(shred * 100)}%", FloatingTextManager.TextType.Curse);
                StartCoroutine(AbilityPulseRoutine(new Color(0.6f, 0.1f, 0.9f, 1f)));
                break;
        }
    }

    public void ApplyTileHazard(TileProperty.HazardData hazard)
    {
        ApplyTileHazard(TileProperty.TileType.Spike, hazard);
    }

    void ResetStatusEffects()
    {
        dotDamagePerSecond = 0;
        dotTickTimer = 0f;
        isEffectInfinite = false;
    }

    public void TakeDamage(float incomingDamage, bool isStatusEffect, FloatingTextManager.TextType damageType = FloatingTextManager.TextType.Physical)
    {
        if (isDying) return;

        float finalDamage = incomingDamage;
        if (!isStatusEffect) finalDamage = incomingDamage * (1f - armorPercent);

        currentHP -= finalDamage;

        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHP, maxHP, armorPercent);
        }

        // Spawn floating damage numbers in 3D space
        FloatingTextManager.SpawnDamage(transform.position, finalDamage, damageType);

        if (gameObject.activeInHierarchy && currentHP > 0)
        {
            StartCoroutine(DamageFlashRoutine());
        }

        if (currentHP <= 0)
        {
            isDying = true;
            StartCoroutine(DeathDissolveRoutine());
        }
    }

    public void TakeDamage(float incomingDamage, bool isStatusEffect)
    {
        TakeDamage(incomingDamage, isStatusEffect, FloatingTextManager.TextType.Physical);
    }

    private IEnumerator DeathDissolveRoutine()
    {
        int dpReward = 15;
        switch (currentClass)
        {
            case EnemyClass.Swordsman: dpReward = 15; break;
            case EnemyClass.Tanker: dpReward = 30; break;
            case EnemyClass.Rogue: dpReward = 20; break;
            case EnemyClass.Priest: dpReward = 35; break;
            case EnemyClass.Supporter: dpReward = 35; break;
            case EnemyClass.Paladin: dpReward = 100; break;
        }

        if (EconomyManager.Instance != null) EconomyManager.Instance.AddDP(dpReward);
        if (WaveManager.Instance != null) WaveManager.Instance.OnEnemyDefeated(this);

        if (pathfindingScript != null) pathfindingScript.speed = 0f;
        if (healthBar != null) healthBar.gameObject.SetActive(false);

        float duration = 0.28f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 startScale = spriteRenderer != null ? spriteRenderer.transform.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float popY = Mathf.Sin(t * Mathf.PI) * 0.45f;
            transform.position = startPos + new Vector3(0f, popY, 0f);

            if (spriteRenderer != null)
            {
                spriteRenderer.transform.Rotate(0f, 0f, 720f * Time.deltaTime);
                spriteRenderer.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                Color c = spriteRenderer.color;
                c.a = 1f - t;
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            Color orig = spriteRenderer.color;
            spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f);
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null) spriteRenderer.color = orig;
        }
    }

    private IEnumerator HealFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            Color orig = spriteRenderer.color;
            spriteRenderer.color = new Color(0.4f, 1f, 0.5f, 1f);
            yield return new WaitForSeconds(0.15f);
            if (spriteRenderer != null) spriteRenderer.color = orig;
        }
    }

    private IEnumerator AbilityPulseRoutine(Color pulseColor)
    {
        if (spriteRenderer != null)
        {
            Color orig = spriteRenderer.color;
            spriteRenderer.color = pulseColor;
            yield return new WaitForSeconds(0.25f);
            if (spriteRenderer != null) spriteRenderer.color = orig;
        }
    }
}
