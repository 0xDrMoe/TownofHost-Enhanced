﻿using AmongUs.GameOptions;
using Hazel;
using System.Linq;
using System.Text;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.PlayerState;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE.Roles.Neutral;

internal class Baker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 28600;

    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem BreadNeededToTransform;
    public static OptionItem FamineStarveCooldown;
    private static OptionItem BTOS2Baker;
    private static byte BreadID = 0;

    public static readonly Dictionary<byte, List<byte>> BreadList = [];
    public static readonly Dictionary<byte, List<byte>> RevealList = [];
    public static readonly Dictionary<byte, List<byte>> BarrierList = [];
    public static readonly Dictionary<byte, List<byte>> FamineList = [];
    private static bool CanUseAbility;
    public static bool StarvedNonBreaded;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Baker, 1, zeroOne: false);
        BreadNeededToTransform = IntegerOptionItem.Create(Id + 10, "BakerBreadNeededToTransform", new(1, 5, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Baker])
            .SetValueFormat(OptionFormat.Times);
        FamineStarveCooldown = FloatOptionItem.Create(Id + 11, "FamineStarveCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Baker])
                .SetValueFormat(OptionFormat.Seconds);
        BTOS2Baker = BooleanOptionItem.Create(Id + 12, "BakerBreadGivesEffects", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Baker]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        BreadList.Clear();
        RevealList.Clear();
        BarrierList.Clear();
        FamineList.Clear();
        CanUseAbility = false;
        StarvedNonBreaded = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        BreadList[playerId] = [];
        RevealList[playerId] = [];
        BarrierList[playerId] = [];
        FamineList[playerId] = [];
        CanUseAbility = true;
        StarvedNonBreaded = false;
        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }

    private static (int, int) BreadedPlayerCount(byte playerId)
    {
        int breaded = 0, all = BreadNeededToTransform.GetInt();
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == playerId) continue;

            if (HasBread(playerId, pc.PlayerId))
                breaded++;
        }
        return (breaded, all);
    }
    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Baker);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte BakerId = reader.ReadByte();
        byte BreadHolderId = reader.ReadByte();
        BreadList[BakerId].Add(BreadHolderId);
    }
    public override string GetProgressText(byte playerId, bool comms) => ColorString(GetRoleColor(CustomRoles.Baker).ShadeColor(0.25f), $"({BreadedPlayerCount(playerId).Item1}/{BreadedPlayerCount(playerId).Item2})");
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse()) return true;
        // i swear this isn't consigliere's code i swear
        var revealed = false;
        RevealList.Do(x =>
        {
            if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && GetPlayerById(x.Key).IsAlive())
                revealed = true;
        });
        return revealed;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        StringBuilder sb = new StringBuilder();
        if (BarrierList[seer.PlayerId].Contains(seen.PlayerId))
        {
            sb.Append(ColorString(GetRoleColor(CustomRoles.Baker), "●") + ColorString(GetRoleColor(CustomRoles.Medic), "✚"));
            return sb.ToString();
        }
        else if (HasBread(seer.PlayerId, seen.PlayerId))
        {
            sb.Append(ColorString(GetRoleColor(CustomRoles.Baker), "●"));
            return sb.ToString();
        }
        return string.Empty;
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (HasBread(playerIdList.First(), target.PlayerId) && seer.IsNeutralApocalypse() && seer.PlayerId != playerIdList.First())
        {
            return ColorString(GetRoleColor(CustomRoles.Baker), "●");
        }
        return string.Empty;
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Main.AllPlayerKillCooldown[id];
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(GetString("BakerKillButtonText"));
    public static bool HasBread(byte pc, byte target)
    {
        return BreadList[pc].Contains(target);
    }
    private static bool AllHasBread(PlayerControl player)
    {
        if (!player.Is(CustomRoles.Baker)) return false;

        var (countItem1, countItem2) = BreadedPlayerCount(player.PlayerId);
        return countItem1 >= countItem2;
    }

    public override void OnReportDeadBody(PlayerControl marg, NetworkedPlayerInfo iscute)
    {
        CanUseAbility = true;
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        foreach (var playerId in BreadList.Keys.ToArray())
        {
            if (deadPlayer.PlayerId == playerId)
            {
                BreadList[playerId].Remove(playerId);
            }
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (BTOS2Baker.GetBool()) { 
            switch (BreadID) // 0 = Reveal, 1 = Roleblock, 2 = Barrier
            {
                case 0: // Switch to Roleblock
                    BreadID = 1;
                    break;
                case 1: // Switch to Barrier
                    BreadID = 2;
                    break;
                case 2: // Switch to Reveal
                    BreadID = 0;
                    break;
            }
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || !seer.IsAlive() || isForMeeting || !isForHud) return string.Empty;
        if (!BTOS2Baker.GetBool()) return string.Empty;
        else
        {
            var sb = new StringBuilder();
            switch (BreadID)
            {
                case 0: // Reveal
                    sb.Append(GetString("BakerCurrentBread") + GetString("BakerRevealBread"));
                    break;
                case 1: // Roleblock
                    sb.Append(GetString("BakerCurrentBread") + GetString("BakerRoleblockBread"));
                    break;
                case 2: // Barrier
                    sb.Append(GetString("BakerCurrentBread") + GetString("BakerBarrierBread"));
                    break;
            }
            return sb.ToString();
        }
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!CanUseAbility)
            killer.Notify(GetString("BakerBreadUsedAlready"));

        else if (target.IsNeutralApocalypse())
            killer.Notify(GetString("BakerCantBreadApoc"));

        else if (HasBread(killer.PlayerId, target.PlayerId))
            killer.Notify(GetString("BakerAlreadyBreaded"));

        else {
            BreadList[killer.PlayerId].Add(target.PlayerId);
            SendRPC(killer, target);
            Utils.NotifyRoles(SpecifySeer: killer);
            killer.Notify(GetString("BakerBreaded"));
            Logger.Info($"Bread given to " + target.GetRealName(), "Baker");
            CanUseAbility = false;
            if (BTOS2Baker.GetBool()) { 
                switch (BreadID)
                {
                    case 0: // Reveal
                        RevealList[killer.PlayerId].Add(target.PlayerId);
                        break;
                    case 1: // Roleblock
                        target.SetKillCooldownV3(999f);
                        break;
                    case 2: // Barrier
                        BarrierList[killer.PlayerId].Add(target.PlayerId);
                        break;
                } 
            }
        }
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        var baker = Utils.GetPlayerListByRole(CustomRoles.Baker);
        if (killer == null || target == null || baker == null || !baker.Any()) return true;
        if (!BarrierList[playerIdList.First()].Contains(target.PlayerId)) return false;

        killer.RpcGuardAndKill(target);
        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);
        return true;
    }
    public override void AfterMeetingTasks()
    {
        BarrierList[playerIdList.First()].Clear();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AllHasBread(player) || player.Is(CustomRoles.Famine)) return;

        player.RpcSetCustomRole(CustomRoles.Famine);
        player.Notify(GetString("BakerToFamine"));
        player.RpcGuardAndKill(player);
    }
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!HasEnabled || deathReason != PlayerState.DeathReason.Vote) return;
        if (!CustomRoles.Famine.RoleExist()) return;
        if (exileIds.Contains(playerIdList.First())) return;
        if (StarvedNonBreaded) return;
        var deathList = new List<byte>();
        PlayerControl baker = GetPlayerById(playerIdList.First());
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsNeutralApocalypse() || HasBread(baker.PlayerId, pc.PlayerId)) continue;
            if (baker != null && baker.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(baker);
                    deathList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Starved, [.. deathList]);
        BreadList[baker.PlayerId].Clear();
        StarvedNonBreaded = true;
    }
}
internal class Famine : RoleBase
{
    //===========================SETUP================================\\
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Baker);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\
    public override void Add(byte playerId)
    {

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => CustomRoles.Baker.GetStaticRoleClass().KnowRoleTarget(seer, target);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Baker.FamineStarveCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => false;
    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(GetString("FamineKillButtonText"));
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => Baker.FamineList[seer.PlayerId].Contains(seen.PlayerId) ? $"<color={GetRoleColorCode(seer.GetCustomRole())}>⁂</color>" : string.Empty;
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsNeutralApocalypse()) killer.Notify(GetString("FamineCantStarveApoc"));
        else if (Baker.FamineList[killer.PlayerId].Contains(target.PlayerId))
            killer.Notify(GetString("FamineAlreadyStarved"));
        else if (Baker.StarvedNonBreaded)
        {
            Baker.FamineList[killer.PlayerId].Add(target.PlayerId);
            Baker.SendRPC(killer, target);
            NotifyRoles(SpecifySeer: killer);
            killer.Notify(GetString("FamineStarved"));
            Logger.Info(target.GetRealName() + $" has been starved", "Famine");
        }
        return false;
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        foreach (var playerId in Baker.FamineList.Keys.ToArray())
        {
            if (deadPlayer.PlayerId == playerId)
            {
                Baker.FamineList[playerId].Remove(playerId);
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl sylveon, NetworkedPlayerInfo iscute)
    {
        foreach (var pc in Baker.FamineList)
        {
            foreach (var tar in pc.Value)
            {
                var target = GetPlayerById(tar);
                var killer = GetPlayerById(pc.Key);
                if (killer == null || target == null) continue;
                target.RpcExileV2();
                target.SetRealKiller(killer);
                tar.SetDeathReason(PlayerState.DeathReason.Starved);
                Main.PlayerStates[tar].SetDead();
                MurderPlayerPatch.AfterPlayerDeathTasks(killer, target, true);
                Logger.Info($"{killer.GetRealName()} has starved {target.GetRealName()}", "Famine");
            }
        }

    }
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        Baker.OnCheckForEndVoting(deathReason, exileIds);
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (TransformedNeutralApocalypseCanBeGuessed.GetBool()) 
        { 
            guesser.ShowInfoMessage(isUI, GetString("GuessImmune"));
            return true; 
        }
        return false;
    }
}