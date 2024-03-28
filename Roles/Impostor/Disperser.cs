﻿using AmongUs.GameOptions;
using Il2CppSystem.Collections.Generic;
using TOHE.Modules;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Disperser : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 24400;
    private static readonly HashSet<byte> PlayerIds = new();
    public static bool HasEnabled => PlayerIds.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    //==================================================================\\

    private static OptionItem DisperserShapeshiftCooldown;
    private static OptionItem DisperserShapeshiftDuration;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Disperser);
        DisperserShapeshiftCooldown = FloatOptionItem.Create(Id + 5, "ShapeshiftCooldown", new(1f, 180f, 1f), 20f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disperser])
            .SetValueFormat(OptionFormat.Seconds);
        DisperserShapeshiftDuration = FloatOptionItem.Create(Id + 7, "ShapeshiftDuration", new(1f, 60f, 1f), 15f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Disperser])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = DisperserShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = DisperserShapeshiftDuration.GetFloat();
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting && !shapeshiftIsHidden) return;
        
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if ((!shapeshiftIsHidden && shapeshifter.PlayerId == pc.PlayerId) || !pc.CanBeTeleported())
            {
                if (!pc.Is(CustomRoles.Disperser) && pc.IsAlive())
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), GetString("ErrorTeleport")));
                
                continue;
            }

            pc.RPCPlayCustomSound("Teleport");
            pc.RpcRandomVentTeleport();
            pc.Notify(ColorString(GetRoleColor(CustomRoles.Disperser), string.Format(GetString("TeleportedInRndVentByDisperser"), pc.GetRealName())));
        }
    }
}