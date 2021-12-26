﻿using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using Hazel;

using UnityEngine;

using ExtremeRoles.Helper;
using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

namespace ExtremeRoles.Patches
{

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
    public static class PlayerControlExiledPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (!ExtremeRoleManager.GameRole[__instance.PlayerId].HasTask)
            {
                Task.ClearAllTasks(ref __instance);
            }
        }

    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class PlayerControlFixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (ExtremeRoleManager.GameRole.Count == 0) { return; }
            if (PlayerControl.LocalPlayer != __instance) { return; }

            playerInfoUpdate();
            resetNameTagsAndColors();
            setPlayerNameColor(__instance);
            buttonUpdate(__instance);
            refreshRoleDescription(__instance);
        }

        private static Color getColorFromRoleAbility(
            SingleRoleBase role,
            byte targetPlayerId)
        {
            Color defaultColor = Palette.ClearWhite;

            var lookRole = ExtremeRoleManager.GetLocalPlayerRole();
            var targetRole = ExtremeRoleManager.GameRole[targetPlayerId];

            switch (role.Id)
            {
                case ExtremeRoleId.Marlin:
                    if (targetRole.IsImposter())
                    {
                        return Palette.ImpostorRed;
                    }
                    else if (targetRole.IsNeutral() &&
                        ((Roles.Combination.Marlin)role).CanSeeNeutral)
                    {
                        return Palette.DisabledGrey;
                    }
                    return defaultColor;

                case ExtremeRoleId.VanillaRole:

                    var vanilaRole = (Roles.Solo.VanillaRoleWrapper)role;
                    switch (vanilaRole.VanilaRoleId)
                    {
                        case RoleTypes.Impostor:
                        case RoleTypes.Shapeshifter:
                            if (targetRole.IsImposter())
                            {
                                return Palette.ImpostorRed;
                            }
                            return defaultColor;
                        default:
                            return defaultColor;
                    }
                case ExtremeRoleId.Sidekick:

                    var sidekick = (Roles.Solo.Neutral.Sidekick)role;

                    if (lookRole.IsImposter() && sidekick.IsPrevRoleImpostor)
                    {
                        if (sidekick.CanSeeImpostorToSideKickImpostor)
                        {
                            return defaultColor;
                        }
                        else
                        {
                            return Palette.ImpostorRed;
                        }
                    }
                    else if (
                        (lookRole.Id == ExtremeRoleId.Jackal) ||
                        (lookRole.Id == ExtremeRoleId.Sidekick))
                    {
                        return sidekick.NameColor;
                    }

                    return defaultColor;

                default:
                    return defaultColor;
            }
        }
        private static void resetNameTagsAndColors()
        {

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                player.nameText.text = player.Data.PlayerName;
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor && player.Data.Role.IsImpostor)
                {
                    player.nameText.color = Palette.ImpostorRed;
                }
                else
                {
                    player.nameText.color = Color.white;
                }
                if (MeetingHud.Instance != null)
                {
                    foreach (PlayerVoteArea pva in MeetingHud.Instance.playerStates)
                    {
                        pva.NameText.text = player.Data.PlayerName;
                        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor &&
                            player.Data.Role.IsImpostor)
                        {
                            pva.NameText.color = Palette.ImpostorRed;
                        }
                        else
                        {
                            pva.NameText.color = Palette.White;
                        }
                    }
                }
            }

            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                List<PlayerControl> impostors = PlayerControl.AllPlayerControls.ToArray().ToList();
                impostors.RemoveAll(x => !(x.Data.Role.IsImpostor));
                foreach (PlayerControl player in impostors)
                {
                    player.nameText.color = Palette.ImpostorRed;
                    if (MeetingHud.Instance != null)
                    {
                        foreach (PlayerVoteArea pva in MeetingHud.Instance.playerStates)
                        {
                            if (player.PlayerId != pva.TargetPlayerId) { continue; }
                            pva.NameText.color = Palette.ImpostorRed;
                        }
                    }
                }
            }

        }

        private static void setPlayerNameColor(PlayerControl player)
        {
            var localPlayerId = player.PlayerId;
            var role = ExtremeRoleManager.GameRole[localPlayerId];

            bool voteNamePaintBlock = false;
            bool playerNamePaintBlock = false;
            bool isBlocked = AssassinMeeting.AssassinMeetingTrigger;
            if (role.Id == ExtremeRoleId.Assassin)
            {
                voteNamePaintBlock = true;
                playerNamePaintBlock = ((Roles.Combination.Assassin)role).CanSeeRoleBeforeFirstMeeting || isBlocked;
            }

            // Modules.Helpers.DebugLog($"Player Name:{role.NameColor}");

            // まずは自分のプレイヤー名の色を変える
            player.nameText.color = role.NameColor;
            setVoteAreaColor(localPlayerId, role.NameColor);

            foreach (PlayerControl targetPlayer in PlayerControl.AllPlayerControls)
            {
                if (targetPlayer.PlayerId == player.PlayerId) { continue; }

                byte targetPlayerId = targetPlayer.PlayerId;

                if (!MapOption.GhostsSeeRoles || !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Color paintColor = getColorFromRoleAbility(
                        role, targetPlayerId);
                    if (paintColor == Palette.ClearWhite) { continue; }

                    targetPlayer.nameText.color = paintColor;
                    setVoteAreaColor(targetPlayerId, paintColor);
                }
                else
                {
                    var targetPlayerRole = ExtremeRoleManager.GameRole[
                        targetPlayerId];
                    Color roleColor = targetPlayerRole.NameColor;
                    if (!playerNamePaintBlock)
                    {
                        targetPlayer.nameText.color = roleColor;
                    }
                    setGhostVoteAreaColor(
                        targetPlayerId,
                        roleColor,
                        voteNamePaintBlock,
                        targetPlayerRole.Teams == role.Teams);
                }
            }

        }
        private static void setVoteAreaColor(
            byte targetPlayerId,
            Color targetColor)
        {
            if (MeetingHud.Instance != null)
            {
                foreach (PlayerVoteArea voteArea in MeetingHud.Instance.playerStates)
                {
                    if (voteArea.NameText != null && targetPlayerId == voteArea.TargetPlayerId)
                    {
                        voteArea.NameText.color = targetColor;
                    }
                }
            }
        }

        private static void setGhostVoteAreaColor(
            byte targetPlayerId,
            Color targetColor,
            bool voteNamePaintBlock,
            bool isSameTeam)
        {
            if (MeetingHud.Instance != null)
            {
                foreach (PlayerVoteArea voteArea in MeetingHud.Instance.playerStates)
                {
                    if (voteArea.NameText != null &&
                        targetPlayerId == voteArea.TargetPlayerId &&
                        (!voteNamePaintBlock || isSameTeam))
                    {
                        voteArea.NameText.color = targetColor;
                    }
                }
            }
        }

        private static void playerInfoUpdate()
        {

            bool commsActive = false;
            foreach (PlayerTask t in PlayerControl.LocalPlayer.myTasks)
            {
                if (t.TaskType == TaskTypes.FixComms)
                {
                    commsActive = true;
                    break;
                }
            }

            var isBlocked = AssassinMeeting.AssassinMeetingTrigger;
            var role = ExtremeRoleManager.GetLocalPlayerRole();

            if (role.Id == ExtremeRoleId.Assassin)
            {
                isBlocked = ((Roles.Combination.Assassin)role).IsFirstMeeting || isBlocked;
            }

            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {

                if (player != PlayerControl.LocalPlayer && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    continue;
                }

                Transform playerInfoTransform = player.nameText.transform.parent.FindChild("Info");
                TMPro.TextMeshPro playerInfo = playerInfoTransform != null ? playerInfoTransform.GetComponent<TMPro.TextMeshPro>() : null;

                if (playerInfo == null)
                {
                    playerInfo = UnityEngine.Object.Instantiate(
                        player.nameText, player.nameText.transform.parent);
                    playerInfo.fontSize *= 0.75f;
                    playerInfo.gameObject.name = "Info";
                }

                // Set the position every time bc it sometimes ends up in the wrong place due to camoflauge
                playerInfo.transform.localPosition = player.nameText.transform.localPosition + Vector3.up * 0.5f;

                PlayerVoteArea playerVoteArea = MeetingHud.Instance?.playerStates?.FirstOrDefault(x => x.TargetPlayerId == player.PlayerId);
                Transform meetingInfoTransform = playerVoteArea != null ? playerVoteArea.NameText.transform.parent.FindChild("Info") : null;
                TMPro.TextMeshPro meetingInfo = meetingInfoTransform != null ? meetingInfoTransform.GetComponent<TMPro.TextMeshPro>() : null;
                if (meetingInfo == null && playerVoteArea != null)
                {
                    meetingInfo = UnityEngine.Object.Instantiate(playerVoteArea.NameText, playerVoteArea.NameText.transform.parent);
                    meetingInfo.transform.localPosition += Vector3.down * 0.20f;
                    meetingInfo.fontSize *= 0.63f;
                    meetingInfo.autoSizeTextContainer = true;
                    meetingInfo.gameObject.name = "Info";
                }

                var (playerInfoText, meetingInfoText) = getRoleAndMeetingInfo(player, commsActive, isBlocked);
                playerInfo.text = playerInfoText;
                playerInfo.gameObject.SetActive(player.Visible);

                if (meetingInfo != null)
                {
                    meetingInfo.text = MeetingHud.Instance.state == MeetingHud.VoteStates.Results ? "" : meetingInfoText;
                }
            }
        }

        private static Tuple<string, string> getRoleAndMeetingInfo(
            PlayerControl targetPlayer, bool commsActive,
            bool IsLocalPlayerAssassinFirstMeeting = false)
        {

            var (tasksCompleted, tasksTotal) = Task.GetTaskInfo(targetPlayer.Data);
            string roleNames = ExtremeRoleManager.GameRole[targetPlayer.PlayerId].GetColoredRoleName();

            var completedStr = commsActive ? "?" : tasksCompleted.ToString();
            string taskInfo = tasksTotal > 0 ? $"<color=#FAD934FF>({completedStr}/{tasksTotal})</color>" : "";

            string playerInfoText = "";
            string meetingInfoText = "";

            if (targetPlayer == PlayerControl.LocalPlayer)
            {
                playerInfoText = $"{roleNames}";
                if (DestroyableSingleton<TaskPanelBehaviour>.InstanceExists)
                {
                    TMPro.TextMeshPro tabText = DestroyableSingleton<
                        TaskPanelBehaviour>.Instance.tab.transform.FindChild("TabText_TMP").GetComponent<TMPro.TextMeshPro>();
                    tabText.SetText($"{TranslationController.Instance.GetString(StringNames.Tasks)} {taskInfo}");
                }
                meetingInfoText = $"{roleNames} {taskInfo}".Trim();
            }
            else if (IsLocalPlayerAssassinFirstMeeting)
            {
                if (((Roles.Combination.Assassin)ExtremeRoleManager.GetLocalPlayerRole()
                    ).CanSeeRoleBeforeFirstMeeting && MapOption.GhostsSeeRoles)
                {
                    playerInfoText = $"{roleNames}";
                }
            }
            else if (MapOption.GhostsSeeRoles && MapOption.GhostsSeeTasks)
            {
                playerInfoText = $"{roleNames} {taskInfo}".Trim();
                meetingInfoText = playerInfoText;
            }
            else if (MapOption.GhostsSeeTasks)
            {
                playerInfoText = $"{taskInfo}".Trim();
                meetingInfoText = playerInfoText;
            }
            else if (MapOption.GhostsSeeRoles)
            {
                playerInfoText = $"{roleNames}";
                meetingInfoText = playerInfoText;
            }

            meetingInfoText = meetingInfoText.Replace("ラバーズ", "♥");

            return Tuple.Create(playerInfoText, meetingInfoText);

        }
        private static void refreshRoleDescription(PlayerControl player)
        {
            if (player == null) { return; };

            var role = ExtremeRoleManager.GameRole[player.PlayerId];

            if (role.Id == ExtremeRoleId.VanillaRole) { return; }

            var removedTask = new List<PlayerTask>();
            foreach (PlayerTask task in player.myTasks)
            {
                var textTask = task.gameObject.GetComponent<ImportantTextTask>();
                if (textTask != null)
                {
                    removedTask.Add(task); // TextTask does not have a corresponding RoleInfo and will hence be deleted
                }
            }

            foreach (PlayerTask task in removedTask)
            {
                task.OnRemove();
                player.myTasks.Remove(task);
                UnityEngine.Object.Destroy(task.gameObject);
            }

            var importantTextTask = new GameObject("RoleTask").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(player.transform, false);

            importantTextTask.Text = Design.ColoedString(
                role.NameColor, $"{role.RoleName}: {string.Format("{0}{1}", role.Id, "ShortDescription")}");
            player.myTasks.Insert(0, importantTextTask);

        }
        private static void buttonUpdate(PlayerControl player)
        {
            if (!player.AmOwner || !Player.ShowButtons) { return; }
            var role = ExtremeRoleManager.GameRole[player.PlayerId];
            if (role.UseVent)
            {
                if (role.Id != ExtremeRoleId.VanillaRole)
                {
                    HudManager.Instance.ImpostorVentButton.Show();
                }
                else
                {
                    if (((Roles.Solo.VanillaRoleWrapper)role).VanilaRoleId == RoleTypes.Engineer)
                    {
                        HudManager.Instance.AbilityButton.Show();
                    }
                }
            }

            if (role.UseSabotage)
            {
                HudManager.Instance.SabotageButton.Show();
                HudManager.Instance.SabotageButton.gameObject.SetActive(true);
            }
            if (role.CanKill && role.Id != ExtremeRoleId.VanillaRole)
            {
                player.SetKillTimer(player.killTimer - Time.fixedDeltaTime);
                PlayerControl target = player.FindClosestTarget(!role.IsImposter());

                // Logging.Debug($"TargetAlive?:{target}");

                DestroyableSingleton<HudManager>.Instance.KillButton.SetTarget(target);
                HudManager.Instance.KillButton.Show();
                HudManager.Instance.KillButton.gameObject.SetActive(true);
            }

            if (role is IRoleAbility)
            {
                ((IRoleAbility)role).Button.Update();
            }           


            // ToDo:インポスターのベントボタンをエンジニアが使えるようにする
            /*
            var role = Roles.ExtremeRoleManager.GameRole[__instance.PlayerId];
            if (role.UseVent)
            {
                if (!(role is Roles.Solo.VanillaRoleWrapper))
                {
                    HudManager.Instance.ImpostorVentButton.Show();
                }
                else
                {
                    if (((Roles.Solo.VanillaRoleWrapper)role).VanilaRoleId == RoleTypes.Engineer)
                    {
                        if (!OptionsHolder.AllOptions[
                            (int)OptionsHolder.CommonOptionKey.EngineerUseImpostorVent].GetBool())
                        {
                            HudManager.Instance.AbilityButton.Show();
                        }
                        else
                        {
                            //HudManager.Instance.AbilityButton.Hide();
                            HudManager.Instance.ImpostorVentButton.Show();
                            HudManager.Instance.AbilityButton.gameObject.SetActive(false);
                        }
                    }
                }    
            }
            if (role.UseSabotage)
            {
                HudManager.Instance.SabotageButton.Show();
                HudManager.Instance.SabotageButton.gameObject.SetActive(true);
            }
            */
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
    class PlayerControlFindClosestTargetPatch
    {
        static bool Prefix(
            PlayerControl __instance,
            ref PlayerControl __result,
            [HarmonyArgument(0)] bool protecting)
        {
            var gameRoles = ExtremeRoleManager.GameRole;

            if (gameRoles.Count == 0) { return true; }

            var role = ExtremeRoleManager.GameRole[__instance.PlayerId];
            if (role.Id == ExtremeRoleId.VanillaRole) { return true; }

            __result = null;

            int killRange = PlayerControl.GameOptions.KillDistance;
            if (role.HasOtherKillRange)
            {
                killRange = role.KillRange;
            }

            float num = GameOptionsData.KillDistances[Mathf.Clamp(killRange, 0, 2)];
            
            if (!ShipStatus.Instance)
            {
                return false;
            }
            Vector2 truePosition = __instance.GetTruePosition();
            Il2CppSystem.Collections.Generic.List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                GameData.PlayerInfo playerInfo = allPlayers[i];
                if (!playerInfo.Disconnected && 
                    playerInfo.PlayerId != __instance.PlayerId && 
                    !playerInfo.IsDead && 
                    (playerInfo.Role.CanBeKilled || protecting) && 
                    !playerInfo.Object.inVent)
                {
                    PlayerControl @object = playerInfo.Object;
                    if (@object && @object.Collider.enabled)
                    {
                        Vector2 vector = @object.GetTruePosition() - truePosition;
                        float magnitude = vector.magnitude;
                        if (magnitude <= num && 
                            !PhysicsHelpers.AnyNonTriggersBetween(
                                truePosition, vector.normalized,
                                magnitude, Constants.ShipAndObjectsMask))
                        {
                            __result = @object;
                            num = magnitude;
                        }
                    }
                }
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class PlayerControlHandleRpcPatch
    {
        static void Postfix([HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            switch (callId)
            {
                case (byte)CustomRPC.ForceEnd:
                    ExtremeRoleRPC.ForceEnd();
                    break;
                case (byte)CustomRPC.GameInit:
                    ExtremeRoleRPC.GameInit();
                    break;
                case (byte)CustomRPC.SetRole:
                    byte roleId = reader.ReadByte();
                    byte playerId = reader.ReadByte();
                    byte gameId = reader.ReadByte();
                    ExtremeRoleRPC.SetRole(roleId, playerId, gameId);
                    break;
                case (byte)CustomRPC.ShareOption:
                    int numOptions = (int)reader.ReadPackedUInt32();
                    ExtremeRoleRPC.ShareOption(numOptions, reader);
                    break;
                case (byte)CustomRPC.UncheckedMurderPlayer:
                    byte sourceId = reader.ReadByte();
                    byte targetId = reader.ReadByte();
                    byte useAnimationreaderreader = reader.ReadByte();
                    ExtremeRoleRPC.UncheckedMurderPlayer(
                        sourceId, targetId, useAnimationreaderreader);
                    break;
                case (byte)CustomRPC.ReplaceRole:
                    byte callerId = reader.ReadByte();
                    byte replaceTarget = reader.ReadByte();
                    byte ops = reader.ReadByte();
                    ExtremeRoleRPC.ReplaceRole(
                        callerId, replaceTarget, ops);
                    break;
                default:
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
    class PlayerControlSetCoolDownPatch
    {
        public static bool Prefix(
            PlayerControl __instance, [HarmonyArgument(0)] float time)
        {
            var roles = ExtremeRoleManager.GameRole;
            if (roles.Count == 0 || !roles.ContainsKey(__instance.PlayerId)) { return true; }

            var role = roles[__instance.PlayerId];
            if (role.Id == ExtremeRoleId.VanillaRole) { return true; }


            var killCool = PlayerControl.GameOptions.KillCooldown;
            if (killCool <= 0f) { return false; }
            float maxTime = killCool;

            if (!role.CanKill) { return false; }

            if (role.HasOtherKillCool)
            {
                maxTime = role.KillCoolTime;
            }

            __instance.killTimer = Mathf.Clamp(
                time, 0f, maxTime);
            DestroyableSingleton<HudManager>.Instance.KillButton.SetCoolDown(
                __instance.killTimer, maxTime);

            return false;

        }
    }
    
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class PlayerControlRpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionsHolder.ShareOptionSelections();
        }
    }
}
