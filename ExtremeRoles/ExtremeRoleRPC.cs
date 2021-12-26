﻿using System;
using Hazel;

namespace ExtremeRoles
{
    public enum RoleGameOverReason
    {
        AssassinationMarin = 10,
        AliceKilledByImposter,
        AliceKillAllOthers,

        UnKnown = 100,
    }

    enum CustomRPC
    {
        // Main Controls

        GameInit = 60,
        ForceEnd,
        SetRole,
        ShareOption,
        VersionHandshake,
        UseUncheckedVent,
        UncheckedMurderPlayer,
        UncheckedCmdReportDeadBody,
        UncheckedExilePlayer,

        ReplaceRole,

    }

    public class ExtremeRoleRPC
    {
        public static void GameInit()
        {
            Roles.ExtremeRoleManager.GameInit();
            Module.PlayerDataContainer.GameInit();
            Patches.AssassinMeeting.Reset();
        }

        public static void AllPlayerRPC(byte[] sendData)
        {
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    continue;
                }

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    player.NetId, (byte)CustomRPC.ReplaceRole,
                    Hazel.SendOption.Reliable, -1);

                foreach(byte data in sendData)
                {
                    writer.Write(data);
                }
                
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        public static void ForceEnd()
        {
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!player.Data.Role.IsImpostor)
                {
                    player.RemoveInfected();
                    player.MurderPlayer(player);
                    player.Data.IsDead = true;
                }
            }
        }
        public static void SetRole(byte roleId, byte playerId, byte id)
        {
            if (id != Byte.MaxValue)
            {
                Roles.ExtremeRoleManager.SetPlayerIdToMultiRoleId(roleId, playerId, id);
            }
            else
            {
                Roles.ExtremeRoleManager.SetPlyerIdToSingleRoleId(roleId, playerId);
            }
        }

        public static void ShareOption(int numOptions, MessageReader reader)
        {
            OptionsHolder.ShareOption(numOptions, reader);
        }

        public static void UncheckedMurderPlayer(
            byte sourceId, byte targetId, byte useAnimation)
        {

            PlayerControl source = Helper.Player.GetPlayerControlById(sourceId);
            PlayerControl target = Helper.Player.GetPlayerControlById(targetId);

            if (source != null && target != null)
            {
                if (useAnimation == 0)
                {
                    Patches.KillAnimationCoPerformKillPatch.hideNextAnimation = true;
                };
                source.MurderPlayer(target);
                Roles.ExtremeRoleManager.GameRole[targetId].RolePlayerKilledAction(
                    target, source);
            }
        }

        public static void ReplaceRole(
            byte callerId, byte targetId, byte operation)
        {

        }

    }

}
