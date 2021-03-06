﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class CharacterBase : NetworkBehaviour {
    /*attributes*/
    [SyncVar(hook = "UpdateStr")] public int strength;
    [SyncVar(hook = "UpdateDex")] public int dexterity;
    [SyncVar(hook = "UpdateInt")] public int intelligence;
    [SyncVar(hook = "UpdateVit")] public int vitality;

    int statMin = 5;
    int statMax = 18;

    /*natural stats*/
    [SyncVar] public float maxHealth;
    [SyncVar] public float currentHealth;
    [SyncVar] public int evadeChance;

    /*artificial stats*/
    [SyncVar] public string playerName;

    [SyncVar] public int armorRating;

    [SyncVar(hook = "UpdateDamage")] public ItemEntry equipedItem;

    [SyncVar] public float weaponDamageMin;
    [SyncVar] public float weaponDamageMax;
    [SyncVar] public float weaponCritModifier;
    [SyncVar] public float weaponRange;
    [SyncVar] public float weaponAttackDelay;

    [SyncVar] public float grabRange;

    [HideInInspector] public float attackTimer = 0;


    [SyncVar] public float totalDamageMin;
    [SyncVar] public float totalDamageMax;

    [SyncVar] public bool isDead = false;

    public MeshRenderer[] meshRenderers;

    #region Startup
    void Start()
    {
        if (!isLocalPlayer)
            return;

        playerName = ("Player " + Random.Range(0, 10000).ToString());
        transform.name = playerName;
        meshRenderers = GetComponentsInChildren<MeshRenderer>();

        if (strength == 0)
            CmdRandomizeStats(GetComponent<NetworkIdentity>().netId);
        

        CmdGenerateStats(GetComponent<NetworkIdentity>().netId);
        RecalculateDamage();
    }
    #endregion

    #region Update()
    void Update()
    {
        if(attackTimer < weaponAttackDelay)
            attackTimer += Time.deltaTime;
        if (attackTimer > weaponAttackDelay)
            attackTimer = weaponAttackDelay;
    }
    #endregion

    #region Updating Stats After Changes

    void UpdateStr(int str)
    {
        //Debug.Log("Updating Stats");
        strength = str;
        GenerateStats();
    }

    void UpdateDex(int dex)
    {
        //Debug.Log("Updating Stats");
        dexterity = dex;
        GenerateStats();
    }

    void UpdateInt(int ints)
    {
        //Debug.Log("Updating Stats");
        intelligence = ints;
        GenerateStats();
    }

    void UpdateVit(int vit)
    {
        //Debug.Log("Updating Stats");
        vitality = vit;
        GenerateStats();
    }

    void UpdateDamage(ItemEntry newItem)
    {
        equipedItem = newItem;
        RecalculateDamage();
    }
    #endregion

    #region Item Equipping

    public void EquipItem(ItemEntry itemToEquip)
    {
        CmdEquipItem(GetComponent<NetworkIdentity>().netId, itemToEquip);
        //equipedItem = itemToEquip;
    }

    [Command]
    void CmdEquipItem(NetworkInstanceId playerID, ItemEntry itemToEquip)
    {
        RpcEquipItem(playerID, itemToEquip);
    }

    [ClientRpc]
    void RpcEquipItem(NetworkInstanceId playerID, ItemEntry itemToEquip)
    {
        GameObject target = ClientScene.FindLocalObject(playerID);
        target.GetComponent<CharacterBase>().equipedItem = itemToEquip;
    }

    #endregion

    #region Clear Inventory
    public void ClearInventory()
    {
        //Debug.LogWarning("Clearing Inventory...");
        CmdClearInventory(GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    public void CmdClearInventory(NetworkInstanceId targetID)
    {
        RpcClearInventory(targetID);
    }

    [ClientRpc]
    void RpcClearInventory(NetworkInstanceId targetID)
    {
        GameObject target = ClientScene.FindLocalObject(targetID);
        Player playerScript = target.GetComponent<Player>();

        Debug.Log("RPCClearInventory");

        playerScript.inventory.Clear();
        playerScript.StartRefreshInventoryCoroutine(0.2f, true);
    }
    #endregion

    #region Stat Generation

    [Command]
    public void CmdRandomizeStats(NetworkInstanceId playerID)
    {
        RpcRandomizeStats(playerID);
    }

    [ClientRpc]
    public void RpcRandomizeStats(NetworkInstanceId playerID)
    {

        GameObject player = ClientScene.FindLocalObject(playerID);
        CharacterBase cb = player.GetComponent<CharacterBase>();
        //Debug.LogWarning("Cound not find a player with the NetworkInstanceID of " + playerID);

        cb.strength = Random.Range(statMin, statMax);
        cb.dexterity = Random.Range(statMin, statMax);
        cb.intelligence = Random.Range(statMin, statMax);
        cb.vitality = Random.Range(statMin, statMax);
    }

    public void RandomizeStats()
    {
        if (!isLocalPlayer)
        {
            Debug.Log("!islocalplayer");
                return;
        }
            

        strength = Random.Range(statMin, statMax);
        dexterity = Random.Range(statMin, statMax);
        intelligence = Random.Range(statMin, statMax);
        vitality = Random.Range(statMin, statMax);
    }

    [Command]
    public void CmdGenerateStats(NetworkInstanceId playerID)
    {

        RpcGenerateStats(playerID);
    }

    [ClientRpc]
    public void RpcGenerateStats(NetworkInstanceId playerID)
    {
        GameObject player = ClientScene.FindLocalObject(playerID);
        CharacterBase cb = player.GetComponent<CharacterBase>();

        cb.maxHealth = cb.vitality + (cb.strength / 2);
        cb.evadeChance = cb.dexterity + (cb.intelligence / 2);
        cb.armorRating = cb.evadeChance;

        cb.currentHealth = cb.maxHealth;

        cb.totalDamageMin = cb.weaponDamageMin + cb.strength;
        cb.totalDamageMax = cb.weaponDamageMax + cb.strength;
    }

    private void RecalculateDamage()
    {
        if (equipedItem.damageMin != 0 && equipedItem.damageMax != 0)
        {
            weaponRange = equipedItem.weaponRange;
            weaponDamageMin = equipedItem.damageMin;
            weaponDamageMax = equipedItem.damageMax;
            weaponAttackDelay = equipedItem.attackDelay;
        }
        else
        {
            weaponRange = 1f;
            weaponDamageMin = 1;
            weaponDamageMax = 4;
            weaponAttackDelay = 0.75f;
        }

        totalDamageMin = weaponDamageMin + (strength / 2);
        totalDamageMax = weaponDamageMax + (strength / 2);
    }

    private void GenerateStatsNoNetworking()
    {
        maxHealth = 5 + vitality + (strength / 2);
        evadeChance = dexterity + (intelligence / 2);
        armorRating = evadeChance;

        currentHealth = maxHealth;
    }

    public void GenerateStats()
    {
        GenerateStatsNoNetworking();
    }

    #endregion

    #region Report Attack
    [Command]
    public void CmdReportAttack(NetworkInstanceId attackerID, NetworkInstanceId playerID, NetworkInstanceId gameManagerID, float modRoll, float damage, string source)
    {
        GameObject targetPlayer = NetworkServer.FindLocalObject(playerID);
        RpcReportAttack(attackerID, attackerID, gameManagerID, modRoll, damage, source);
        targetPlayer.GetComponent<CharacterBase>().TakeDamage(playerID, damage, source);
    }

    [ClientRpc]
    private void RpcReportAttack(NetworkInstanceId attackerID, NetworkInstanceId playerID, NetworkInstanceId gameManagerID, float modRoll, float damage, string source)
    {
        GameObject gameManager = ClientScene.FindLocalObject(gameManagerID);
        Transform player = ClientScene.FindLocalObject(playerID).transform;

        bool attackHits = false;

        if(modRoll >= armorRating)
        {
            attackHits = true;
        }

        gameManager.GetComponent<CombatPopup>().DisplayDamage(attackHits, damage, 2, player.position);
    }

    #endregion

    #region Damage Over Time
    public void StartDoT(NetworkInstanceId GameManagerID, NetworkInstanceId playerID, float damageMin, float damageMax, float duration, float interval, string source)
    {
        CmdApplyDoT(GameManagerID, playerID, damageMin, damageMax, duration, interval, source);
    }

    [Command]
    public void CmdApplyDoT(NetworkInstanceId GameManagerID, NetworkInstanceId playerID, float damageMin, float damageMax, float duration, float interval, string source)
    {
        //GameObject targetPlayer = NetworkServer.FindLocalObject(playerID);
        RpcApplyDoT(GameManagerID, playerID, damageMin, damageMax, duration, interval, source);
    }

    [ClientRpc]
    void RpcApplyDoT(NetworkInstanceId GameManagerID, NetworkInstanceId playerID, float damageMin, float damageMax, float duration, float interval, string source)
    {
        GameObject target = ClientScene.FindLocalObject(playerID);
        GameObject gameManager = ClientScene.FindLocalObject(GameManagerID);

        target.GetComponent<CharacterBase>().StartCoroutine(ApplyDamageOverTime(damageMin, damageMax, duration, interval, source));
    }

    IEnumerator ApplyDamageOverTime(float damageMin, float damageMax, float duration, float interval, string source)
    {
        float timeElapsed = 0.0f;
        timeElapsed += Time.deltaTime;
        while (timeElapsed < duration)
        {
            yield return new WaitForSeconds(interval);
            float damage = Mathf.Round(Random.Range(damageMin, damageMax));
            TakeDamage(GetComponent<NetworkIdentity>().netId, damage, source);
            GameObject.Find("GameManager").GetComponent<CombatPopup>().DisplayDamage(true, damage, 2, transform.position);
        }
    }
    #endregion

    #region Report Heal and Request Respawn Commands
    [Command]
    public void CmdReportHeal(NetworkInstanceId playerID, float amount, string source)
    {
        GameObject targetPlayer = NetworkServer.FindLocalObject(playerID);
        targetPlayer.GetComponent<CharacterBase>().HealPlayer(playerID, amount, source);
    }

    [Command]
    public void CmdRequestRespawn(NetworkInstanceId playerID)
    {
        GameObject targetPlayer = NetworkServer.FindLocalObject(playerID);
        targetPlayer.GetComponent<CharacterBase>().RpcRespawnPlayer(playerID);
    }
    #endregion

    #region Add Stats
    [Command]
    public void CmdAddStats(NetworkInstanceId playerID, int str, int dex, int ints, int vit)
    {
        GameObject targetPlayer = NetworkServer.FindLocalObject(playerID);
        targetPlayer.GetComponent<CharacterBase>().AddStats(str, dex, ints, vit);
    }

    public void AddStats(int str, int dex, int ints, int vit)
    {
        strength += str;
        dexterity += dex;
        intelligence += ints;
        vitality += vit;

        Debug.Log("Updated Stats.");
    }

    #endregion

    #region Take Damage and Heal Local Functions, Die Command

    public void TakeDamage(NetworkInstanceId playerID, float damage, string source)
    {
        if(isDead)
        {
            Debug.LogError(source + " is trying to deal damage to " + gameObject.transform.name + ", but it's already dead!");
            return;
        }

        GameObject targetPlayer = NetworkServer.FindLocalObject(playerID);

        Debug.Log(transform.name + " took " + damage + " damage from \"" + source + "\"!");
        currentHealth -= damage;
        Debug.Log(currentHealth + " / " + maxHealth);
        if(currentHealth <= 0)
        {
            CmdDie(GetComponent<NetworkIdentity>().netId);
        }
    }

    [Command]
    void CmdDie(NetworkInstanceId targetID)
    {
        RpcDie(targetID);
    }

    private void HealPlayer(NetworkInstanceId playerID, float amount, string source)
    {
        if (isDead)
            return;

        GameObject targetPlayer = NetworkServer.FindLocalObject(playerID);

        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        Debug.Log(currentHealth + " / " + maxHealth);
    }
    #endregion

    #region Death and Respawning RPCs
    [ClientRpc]
    private void RpcRespawnPlayer(NetworkInstanceId playerID)
    {
        if (!isDead)
            return;

        Debug.Log("RespawnPlayer has been called.");

        GameObject targetPlayer = ClientScene.FindLocalObject(playerID);
        //MeshRenderer[] mr = targetPlayer.GetComponentsInChildren<MeshRenderer>();

        foreach(Renderer meshRenderer in targetPlayer.GetComponentsInChildren<MeshRenderer>())
        {
            Debug.Log("Enabling MeshRenderers.");
            meshRenderer.enabled = true;
        }

        targetPlayer.gameObject.layer = LayerMask.NameToLayer("Player");

        if(isLocalPlayer)
        {
            GetComponent<Movement>().enabled = true;
            GetComponent<Rotation>().enabled = true;
            Debug.Log("Re-enabled scripts.");
        }

        currentHealth = maxHealth;
        isDead = false;
    }

    [ClientRpc]
    void RpcDie(NetworkInstanceId playerID)
    {

        GameObject targetPlayer = ClientScene.FindLocalObject(playerID);

        currentHealth = 0;
        Debug.Log("DEAD");
        isDead = true;

        /*foreach (MeshRenderer m in meshRenderers)
        {
            m.enabled = false;
        }*/

        foreach(MeshRenderer mr in targetPlayer.GetComponentsInChildren<MeshRenderer>())
        {
            mr.enabled = false;
        }

        //targetPlayer.GetComponent<CapsuleCollider>().enabled = false;
        targetPlayer.gameObject.layer = LayerMask.NameToLayer("Default");
        targetPlayer.GetComponent<CharacterBase>().StopAllCoroutines();

        if (isLocalPlayer)
        {
            targetPlayer.GetComponent<Movement>().enabled = false;
            targetPlayer.GetComponent<Rotation>().enabled = false;
        } 
    }

    [ClientRpc]
    public void RpcRespawn(NetworkInstanceId playerID)
    {
        GameObject player = ClientScene.FindLocalObject(playerID);

        if (!isDead)
        {
            Debug.LogError("Character " + gameObject.transform.name + " is trying to respawn but isn't dead!");
            return;
        }

        Debug.Log("Respawned!");
        isDead = false;

        foreach (MeshRenderer mr in meshRenderers)
        {
            mr.enabled = true;
        }

        player.GetComponent<CapsuleCollider>().enabled = false;

        player.GetComponent<Movement>().enabled = true;
        player.GetComponent<Rotation>().enabled = true;

        currentHealth = maxHealth;
    }
    #endregion

    #region Respawn and Heal Commands
    [Command]
    public void CmdRespawn()
    {
        if (!isDead)
        {
            Debug.LogError("Character " + gameObject.transform.name + " is trying to respawn but isn't dead!");
            return;
        }

        Debug.Log("Respawned!");
        isDead = false;
        MeshRenderer[] mr = GetComponentsInChildren<MeshRenderer>();
        {
            foreach (MeshRenderer m in mr)
            {
                m.enabled = true;
            }
        }

        GetComponent<Movement>().enabled = true;
        GetComponent<Rotation>().enabled = true;

        currentHealth = maxHealth;
    }

    [Command]
    public void CmdHeal(float amount, string source)
    {
        if(isDead)
        {
            Debug.LogError(source + " is trying to heal " + transform.name + " for " + amount + ", but he is dead!");
            return;
        }

        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        Debug.Log(gameObject.transform.name + " has been healed for " + amount + " health by " + source + "!");
        Debug.Log(currentHealth + " / " + maxHealth);
    }
    #endregion
}
