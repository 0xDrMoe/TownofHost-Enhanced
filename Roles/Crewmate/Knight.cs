﻿using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

internal class Knight : RoleBase
{
    private const int Id = 10800;
    public static List<byte> playerIdList = [];
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Knight.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem CanVent;
    private static OptionItem KillCooldown;

    private static List<byte> killed = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Knight);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight]);
    }
    public override void Init()
    {
        killed = [];
        playerIdList = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }
    
    public static bool CheckCanUseVent(PlayerControl player) => player.Is(CustomRoles.Knight) && CanVent.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CheckCanUseVent(pc);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : KillCooldown.GetFloat();
    public override string GetProgressText(byte id, bool comms) => Utils.ColorString(!IsKilled(id) ? Utils.GetRoleColor(CustomRoles.Knight).ShadeColor(0.25f) : Color.gray, !IsKilled(id) ? "(1)" : "(0)");
    public override bool CanUseKillButton(PlayerControl pc)
        => !Main.PlayerStates[pc.PlayerId].IsDead
        && !IsKilled(pc.PlayerId);
    
    private static bool IsKilled(byte playerId) => killed.Contains(playerId);

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Knight);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte KnightId = reader.ReadByte();
        if (!killed.Contains(KnightId))
            killed.Add(KnightId);
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl banana)
    {
        SendRPC(killer.PlayerId);
        killed.Add(killer.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} : " + (IsKilled(killer.PlayerId) ? "Kill chance used" : "Kill chance not used"), "Knight");
        killer.ResetKillCooldown();
        Utils.NotifyRoles(SpecifySeer: killer);
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.SabotageButton.ToggleVisible(false);
        hud.AbilityButton.ToggleVisible(false);
        hud.ImpostorVentButton.ToggleVisible(false);
    }
}