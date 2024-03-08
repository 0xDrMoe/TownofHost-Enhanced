﻿using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Deputy : RoleBase
{
    private const int Id = 7800;
    public static bool On = false;
    public override bool IsEnable => On;
    private static List<byte> playerIdList = [];
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem HandcuffCooldown;
    private static OptionItem HandcuffMax;
    private static OptionItem DeputyHandcuffCDForTarget;
    
    private static int HandcuffLimit = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Deputy);
        HandcuffCooldown = FloatOptionItem.Create(Id + 10, "DeputyHandcuffCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Seconds);
        DeputyHandcuffCDForTarget = FloatOptionItem.Create(Id + 14, "DeputyHandcuffCDForTarget", new(0f, 180f, 2.5f), 45f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Seconds);
        HandcuffMax = IntegerOptionItem.Create(Id + 12, "DeputyHandcuffMax", new(1, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Deputy])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList = [];
        HandcuffLimit = new();
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        HandcuffLimit = HandcuffMax.GetInt();
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Deputy);
        writer.Write(HandcuffLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        HandcuffLimit = reader.ReadInt32();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = HandcuffCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && HandcuffLimit >= 1;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.SerialKiller)) return true;
        if (HandcuffLimit < 1) return false;

        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} : Limit {HandcuffLimit}", "Deputy");

        if (!target.Is(CustomRoles.Deputy))
        {
            HandcuffLimit--;
            SendRPC();

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("DeputyHandcuffedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("HandcuffedByDeputy")));

            target.SetKillCooldownV3(DeputyHandcuffCDForTarget.GetFloat());
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            if (!DisableShieldAnimations.GetBool()) target.RpcGuardAndKill(target);

            return true;
        }
        
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Deputy), GetString("DeputyInvalidTarget")));
        return false;
    }
    public override string GetProgressText(byte PlayerId, bool comms) => Utils.ColorString(HandcuffLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Deputy) : Color.gray, $"({HandcuffLimit})");
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("DeputyHandcuffText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Deputy");
}
