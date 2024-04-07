﻿using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Vigilante : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem VigilanteKillCooldown;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Vigilante);
        VigilanteKillCooldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(5f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vigilante])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = VigilanteKillCooldown.GetFloat();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Madmate)) return true;
        if (target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Madmate) && !target.GetCustomRole().IsConverted())
        {
            killer.RpcSetCustomRole(CustomRoles.Madmate);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("VigilanteNotify")));
            //Utils.NotifyRoles(SpecifySeer: killer);
            Utils.MarkEveryoneDirtySettings();
        }
        return true;
    }
}
