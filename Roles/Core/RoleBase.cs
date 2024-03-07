﻿using AmongUs.GameOptions;
using System.Text;
using UnityEngine;

namespace TOHE;

public abstract class RoleBase
{
    /// <summary>
    /// Variable resets when the game starts.
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// When role is applied in the game, beginning or during the game.
    /// </summary>
    public abstract void Add(byte playerId);

    /// <summary>
    /// If role has to be removed from player
    /// </summary>
    public virtual void Remove(byte playerId)
    { }
    /// <summary>
    /// Make a bool and apply IsEnable => {Bool};
    /// </summary>
    public abstract bool IsEnable { get; }

    /// <summary>
    /// Used to Determine the CustomRole's BASE
    /// </summary>
    public abstract CustomRoles ThisRoleBase { get; }

    /// <summary>
    /// A generic method to set if a impostor/SS base may use kill button.
    /// </summary>
    public virtual bool CanUseKillButton(PlayerControl pc) => pc.Is(CustomRoleTypes.Impostor) && pc.IsAlive();

    /// <summary>
    /// A generic method to set if a impostor/SS base may vent.
    /// </summary>
    public virtual bool CanUseImpostorVentButton(PlayerControl pc) => pc.Is(CustomRoleTypes.Impostor) && pc.IsAlive();

    /// <summary>
    /// A generic method to set if the role can use sabotage.
    /// </summary>
    public virtual bool CanUseSabotage(PlayerControl pc) => pc.Is(CustomRoleTypes.Impostor);

    /// <summary>
    /// When the player presses the sabotage button
    /// </summary>
    public virtual bool OnSabotage(PlayerControl pc) => pc != null;

    //public virtual void SetupCustomOption()
    //{ }

    /// <summary>
    /// A generic method to send a CustomRole's Gameoptions.
    /// </summary>
    public virtual void ApplyGameOptions(IGameOptions opt, byte playerId)
    { }

    /// <summary>
    /// Set a specific kill cooldown
    /// </summary>
    public virtual void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Options.DefaultKillCooldown;

    /// <summary>
    /// A local method to check conditions during gameplay, 30 times each second
    /// </summary>
    public virtual void OnFixedUpdate(PlayerControl pc)
    { }

    /// <summary>
    /// A local method to check conditions during gameplay, which aren't prioritized
    /// </summary>
    public virtual void OnFixedUpdateLowLoad(PlayerControl pc)
    { }

    /// <summary>
    /// Player completes a task
    /// </summary>
    public virtual void OnTaskComplete(PlayerControl pc, int completedTaskCount, int totalTaskCount)
    { }

    /// <summary>
    /// A method for activating actions where the role starts playing an animation when entering a vent
    /// </summary>
    public virtual void OnEnterVent(PlayerControl pc, Vent vent)
    { }

    /// <summary>
    /// A method for activating actions when role is already in vent
    /// </summary>
    public virtual void OnCoEnterVent(PlayerPhysics physics, int ventId)
    { }

    /// <summary>
    /// A generic method to activate actions once (CustomRole)player exists vent.
    /// </summary>
    public virtual void OnExitVent(PlayerControl pc, int ventId)
    { }
    /// <summary>
    /// A generic method to check a Guardian Angel protecting someone.
    /// </summary>
    public virtual void OnCheckProtect(PlayerControl angel, PlayerControl target)
    { }

    /// <summary>
    ///  When role based on Impostors need force check target
    /// </summary>
    public virtual bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target) => target != null && killer != null;

    /// <summary>
    /// When role the target requires a kill check
    /// If the target doesn't require a kill cancel, always use "return true"
    /// </summary>
    public virtual bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => target != null && killer != null;

    /// <summary>
    ///  When role the killer requires a kill check
    /// </summary>
    public virtual bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target) => target != null && killer != null;

    /// <summary>
    /// When the killer murder his target
    /// </summary>
    public virtual void OnMurder(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// When the target role died by killer
    /// </summary>
    public virtual void OnTargetDead(PlayerControl killer, PlayerControl target)
    { }

    /// <summary>
    /// Always do these tasks once the role dies
    /// </summary>
    public virtual void AfterPlayerDeathTask(PlayerControl target)
    { }

    /// <summary>
    /// A generic method to do tasks for when a (CustomRole)player is shapeshifting.
    /// </summary>
    public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    { }

    /// <summary>
    /// Checking that a dead body can be reported
    /// </summary>
    //public virtual bool CheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo deadBody, PlayerControl killer) => reporter.IsAlive();

    /// <summary>
    /// Can start meeting by press button
    /// </summary>
    public virtual bool CantStartMeeting(PlayerControl reporter) => !reporter.IsAlive();

    /// <summary>
    /// When the meeting start by press button
    /// </summary>
    public virtual void OnPressEmergencyButton(PlayerControl reporter)
    { }

    /// <summary>
    /// When reporter press report button
    /// </summary>
    public virtual bool OnPressReportButton(PlayerControl reporter, GameData.PlayerInfo deadBody, PlayerControl killer) => reporter.IsAlive();

    /// <summary>
    /// When the meeting start by report dead body
    /// </summary>
    public virtual void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    { }

    /// <summary>
    /// When guesser need check guess (Check limit or Cannot guess а role/add-on)
    /// </summary>
    public virtual bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role) => target == null;

    /// <summary>
    /// When guesser trying guess target a role
    /// </summary>
    public virtual bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role) => target == null;

    /// <summary>
    /// Check exile role
    /// </summary>
    public virtual void CheckExileTarget(PlayerControl player, bool DecidedWinner)
    { }

    /// <summary>
    /// When player was exiled
    /// </summary>
    public virtual void OnPlayerExiled(PlayerControl Bard, GameData.PlayerInfo exiled)
    { }

    /// <summary>
    /// When the meeting hud is loaded
    /// </summary>
    public virtual void OnMeetingHudStart(PlayerControl pc)
    { }

    /// <summary>
    /// Clears the initial meetinghud message
    /// </summary>
    public virtual void MeetingHudClear()
    { }

    /// <summary>
    /// Notify the playername for modded clients OnMeeting
    /// </summary>
    public virtual string PVANameText(PlayerVoteArea pva, PlayerControl target) => string.Empty;

    /// <summary>
    /// Notify a specific role about something after the meeting was ended.
    /// </summary>
    public virtual void NotifyAfterMeeting()
    { }

    /// <summary>
    /// A generic method to activate actions after a meeting has ended.
    /// </summary>
    public virtual void AfterMeetingTasks()
    { }

    /// <summary>
    /// When the game starts to ending
    /// </summary>
    public virtual void OnCoEndGame()
    { }

    /// <summary>
    /// Set PlayerName text for the role
    /// </summary>
    public virtual string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false) => string.Empty;

    /// <summary>
    /// When player vote for target
    /// </summary>
    public virtual void OnVote(PlayerControl votePlayer, PlayerControl voteTarget)
    { }
    /// <summary>
    /// When the player was voted
    /// </summary>
    public virtual void OnVoted(PlayerControl votedPlayer, PlayerControl votedTarget)
    { }
    /// <summary>
    /// When need hide vote
    /// </summary>
    public virtual bool HideVote(PlayerVoteArea votedPlayer) => false;

    /// <summary>
    /// Set text for Kill/Shapeshift/Report/Vent/Protect button
    /// </summary>
    public virtual void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton?.OverrideText(Translator.GetString("KillButtonText"));
    public virtual Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => null;
    public virtual Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => null;
    public virtual Sprite ImpostorVentButtonSprite { get; }
    public virtual Sprite ReportButtonSprite { get; }
    public virtual string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => string.Empty;
    public virtual string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false) => string.Empty;
    public virtual string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => string.Empty;
    public virtual bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => false;
    public virtual string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => string.Empty;
    public virtual bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => false;

    /// <summary>
    /// Gets & Appends the role's skill limit
    /// </summary>
    public virtual string GetProgressText(byte PlayerId, bool comms) => string.Empty;
    public virtual int CalcVote(PlayerVoteArea PVA) => 0;
}
