﻿using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Arrogance : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 500;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    public static OptionItem BardChance;

    private static readonly Dictionary<byte, float> NowCooldown = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Arrogance);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "Arrogance/Juggernaut___DefaultKillCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "Arrogance/Juggernaut___ReduceKillCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "Arrogance/Juggernaut___MinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Seconds);
        BardChance = IntegerOptionItem.Create(Id + 13, "BardChance", new(0, 100, 5), 0, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Arrogance])
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Init()
    {
        playerIdList.Clear();
        NowCooldown.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        NowCooldown.Remove(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();

        return true;
    }
}