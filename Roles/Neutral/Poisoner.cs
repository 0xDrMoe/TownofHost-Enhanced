﻿using AmongUs.GameOptions;
using TOHE.Roles.Crewmate;
using UnityEngine;
using TOHE.Roles.AddOns.Common;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

[Obfuscation(Exclude = true)]
internal class Poisoner : RoleBase
{
    private class PoisonedInfo(byte poisonerId, float killTimer) 
    {
        public byte PoisonerId = poisonerId;
        public float KillTimer = killTimer;
    }
    //===========================SETUP================================\\
    private const int Id = 17500;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem OptionKillDelay;
    private static OptionItem CanVent;
    public static OptionItem KillCooldown;
    private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, PoisonedInfo> PoisonedPlayers = [];

    private static float KillDelay;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Poisoner, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "PoisonCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner])
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillDelay = FloatOptionItem.Create(Id + 11, "PoisonerKillDelay", new(1f, 60f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        PoisonedPlayers.Clear();

        KillDelay = OptionKillDelay.GetFloat();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Bait)) return true;
        if (Guardian.CannotBeKilled(target)) return true;
        if (target.Is(CustomRoles.Glitch)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;

        killer.SetKillCooldown();

        //誰かに噛まれていなければ登録
        if (!PoisonedPlayers.ContainsKey(target.PlayerId))
        {
            PoisonedPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
        }
        return false;
    }

    public override void OnFixedUpdate(PlayerControl poisoner)
    {
        var poisonerID = poisoner.PlayerId;
        List<byte> targetList = new(PoisonedPlayers.Where(b => b.Value.PoisonerId == poisonerID).Select(b => b.Key));

        for (var id = 0; id < targetList.Count; id++)
        {
            var targetId = targetList[id];
            var poisonedPoisoner = PoisonedPlayers[targetId];

            if (poisonedPoisoner.KillTimer >= KillDelay)
            {
                var target = Utils.GetPlayerById(targetId);
                KillPoisoned(poisoner, target);
                PoisonedPlayers.Remove(targetId);
            }
            else
            {
                poisonedPoisoner.KillTimer += Time.fixedDeltaTime;
                PoisonedPlayers[targetId] = poisonedPoisoner;
            }
        }
    }
    private static void KillPoisoned(PlayerControl poisoner, PlayerControl target, bool isButton = false)
    {
        if (poisoner == null || target == null || target.Data.Disconnected) return;
        if (target.IsAlive())
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Poison;
            target.RpcMurderPlayer(target);
            target.SetRealKiller(poisoner);
            Logger.Info($"{target.GetRealName()} Died by Poison", "Poisoner");
            if (!isButton && poisoner.IsAlive())
            {
                RPC.PlaySoundRPC(poisoner.PlayerId, Sounds.KillSound);
                if (target.Is(CustomRoles.Trapper))
                    poisoner.TrapperKilled(target);
                poisoner.Notify(GetString("PoisonerTargetDead"));
                poisoner.SetKillCooldown();
            }
        }
        else
        {
            Logger.Info($"{target.GetRealName()} was in an unkillable state, poison was canceled", "Poisoner");
        }
    }
    public override void OnReportDeadBody(PlayerControl sans, GameData.PlayerInfo bateman)
    {
        foreach (var targetId in PoisonedPlayers.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            var poisoner = Utils.GetPlayerById(PoisonedPlayers[targetId].PoisonerId);
            KillPoisoned(poisoner, target);
        }
        PoisonedPlayers.Clear();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("PoisonerPoisonButtonText"));
    }
}

