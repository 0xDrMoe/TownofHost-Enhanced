﻿
namespace TOHE.Roles.Impostor;

internal class Convict : RoleBase // Loonie ass role 💀💀💀
{
    public static bool On;
    public override bool IsEnable => On;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
}
