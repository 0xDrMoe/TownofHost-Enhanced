﻿using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Taskinator : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13700;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => false;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\
    public override bool HasTasks(GameData.PlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    private static OptionItem TaskMarkPerRoundOpt;

    private static readonly Dictionary<byte, List<int>> taskIndex = [];
    private static readonly Dictionary<byte, int> TaskMarkPerRound = [];
    
    private static int maxTasksMarkedPerRound = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Taskinator);
        TaskMarkPerRoundOpt = IntegerOptionItem.Create(Id + 10, "TasksMarkPerRound", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Taskinator])
            .SetValueFormat(OptionFormat.Votes);
        Options.OverrideTasksData.Create(Id + 11, TabGroup.NeutralRoles, CustomRoles.Taskinator);
    }

    public override void Init()
    {
        playerIdList.Clear();
        taskIndex.Clear(); 
        TaskMarkPerRound.Clear();
        maxTasksMarkedPerRound = TaskMarkPerRoundOpt.GetInt();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        TaskMarkPerRound[playerId] = 0;
    }


    private static void SendRPC(byte taskinatorID, int taskIndex = -1, bool isKill = false, bool clearAll = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Taskinator); //TaskinatorMarkedTask
        writer.Write(taskinatorID);
        writer.Write(taskIndex);
        writer.Write(isKill);
        writer.Write(clearAll);
        if (!isKill)
        {            
            writer.Write(TaskMarkPerRound[taskinatorID]);   
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte taskinatorID = reader.ReadByte();
        int taskInd = reader.ReadInt32();
        bool isKill = reader.ReadBoolean();
        bool clearAll = reader.ReadBoolean();
        if (!isKill)
        {
            int uses = reader.ReadInt32();
            TaskMarkPerRound[taskinatorID] = uses;
            if (!clearAll) 
            { 
                if (!taskIndex.ContainsKey(taskinatorID)) taskIndex[taskinatorID] = [];
                taskIndex[taskinatorID].Add(taskInd);
            }
        }
        else
        {
            if (taskIndex.ContainsKey(taskinatorID)) taskIndex[taskinatorID].Remove(taskInd);
        }
        if (clearAll && taskIndex.ContainsKey(taskinatorID)) taskIndex[taskinatorID].Clear(); 
    }

    public override string GetProgressText(byte playerId, bool cooms)
    {
        if (!TaskMarkPerRound.ContainsKey(playerId)) TaskMarkPerRound[playerId] = 0;
        int markedTasks = TaskMarkPerRound[playerId];
        int x = Math.Max(maxTasksMarkedPerRound - markedTasks, 0);
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Taskinator).ShadeColor(0.25f), $"({x})");
    }

    public override void AfterMeetingTasks()
    {
        foreach (var playerId in TaskMarkPerRound.Keys)
        {
            TaskMarkPerRound[playerId] = 0;
            if (taskIndex.ContainsKey(playerId)) taskIndex[playerId].Clear();
            SendRPC(playerId, clearAll: true);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override void OnOthersTaskComplete(PlayerControl player, PlayerTask task)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if(!HasEnabled) return;
        if (player == null) return;
        if (!player.IsAlive()) return;
        byte playerId = player.PlayerId;
        if (player.Is(CustomRoles.Taskinator))
        {
            if (!TaskMarkPerRound.ContainsKey(playerId)) TaskMarkPerRound[playerId] = 0;
            if (TaskMarkPerRound[playerId] >= maxTasksMarkedPerRound)
            {
                TaskMarkPerRound[playerId] = maxTasksMarkedPerRound;
                Logger.Info($"Max task per round ({TaskMarkPerRound[playerId]}) reached for {player.GetNameWithRole()}", "Taskinator");
                return;
            }
            TaskMarkPerRound[playerId]++;
            if (!taskIndex.ContainsKey(playerId)) taskIndex[playerId] = [];
            taskIndex[playerId].Add(task.Index);
            SendRPC(taskinatorID : playerId, taskIndex : task.Index);
            player.Notify(GetString("TaskinatorBombPlanted"));
        }
        else
        {
            foreach (var taskinatorId in taskIndex.Keys)
            { 
                if (taskIndex[taskinatorId].Contains(task.Index))
                {
                    var taskinatorPC = Utils.GetPlayerById(taskinatorId);
                    if (taskinatorPC == null) continue;

                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    player.SetRealKiller(taskinatorPC);
                    player.RpcMurderPlayerV3(player);

                    taskIndex[taskinatorId].Remove(task.Index);
                    SendRPC(taskinatorID : taskinatorId, taskIndex:task.Index, isKill : true);
                    Logger.Info($"{player.GetAllRoleName()} died because of {taskinatorPC.GetNameWithRole()}", "Taskinator");
                }
            }
        }
    }
}

