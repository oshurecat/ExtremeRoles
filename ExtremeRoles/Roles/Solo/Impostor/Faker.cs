﻿using System.Collections.Generic;

using UnityEngine;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.RoleAbilityButton;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

namespace ExtremeRoles.Roles.Solo.Impostor
{
    public class Faker : SingleRoleBase, IRoleAbility
    {
        public class FakeDeadBody
        {
            private SpriteRenderer body;
            public FakeDeadBody(
                PlayerControl rolePlayer,
                PlayerControl targetPlayer)
            {
                var killAnimation = PlayerControl.LocalPlayer.KillAnimations[0];
                this.body = Object.Instantiate(
                    killAnimation.bodyPrefab.bodyRenderer);
                targetPlayer.SetPlayerMaterialColors(this.body);

                Vector3 vector = rolePlayer.transform.position + killAnimation.BodyOffset;
                vector.z = vector.y / 1000f;
                this.body.transform.position = vector;
                this.body.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            }

            public void Clear()
            {
                Object.Destroy(this.body);
            }

        }

        public List<FakeDeadBody> DummyBody = new List<FakeDeadBody>();

        public Faker() : base(
            ExtremeRoleId.Faker,
            ExtremeRoleType.Impostor,
            ExtremeRoleId.Faker.ToString(),
            Palette.ImpostorRed,
            true, false, true, true)
        {
            this.DummyBody.Clear();
        }

        public RoleAbilityButtonBase Button
        {
            get => this.createFake;
            set
            {
                this.createFake = value;
            }
        }

        private RoleAbilityButtonBase createFake;

        public static void CreateDummy(
            byte rolePlayerId, byte targetPlayerId)
        {
            PlayerControl rolePlyaer = Player.GetPlayerControlById(rolePlayerId);
            PlayerControl targetPlyaer = Player.GetPlayerControlById(targetPlayerId);
            Faker faker = (Faker)ExtremeRoleManager.GameRole[rolePlayerId];

            faker.DummyBody.Add(
                new FakeDeadBody(
                    rolePlyaer,
                    targetPlyaer));            
        }

        public static void RemoveAllDummyPlayer(byte rolePlayerId)
        {
            Faker faker = (Faker)ExtremeRoleManager.GameRole[rolePlayerId];
            foreach (var body in faker.DummyBody)
            {
                body.Clear();
            }
            faker.DummyBody.Clear();
        }

        public void CreateAbility()
        {
            this.CreateNormalAbilityButton(
                Translation.GetString("dummy"),
                Loader.CreateSpriteFromResources(
                   Path.FakerDummy, 115f));
        }

        public bool IsAbilityUse() => this.IsCommonUse();

        public void RoleAbilityResetOnMeetingEnd()
        {
            return;
        }

        public void RoleAbilityResetOnMeetingStart()
        {
            RPCOperator.Call(
                PlayerControl.LocalPlayer.NetId,
                RPCOperator.Command.FakerRemoveAllDummy,
                new List<byte> { PlayerControl.LocalPlayer.PlayerId });
            RemoveAllDummyPlayer(PlayerControl.LocalPlayer.PlayerId);
        }

        public bool UseAbility()
        {

            var allPlayer = GameData.Instance.AllPlayers;

            bool contine;
            byte targetPlayerId;

            do
            {
                int index = Random.RandomRange(0, allPlayer.Count);
                var player = allPlayer[index];
                contine = player.IsDead || player.Disconnected;
                targetPlayerId = player.PlayerId;

            } while (contine);

            RPCOperator.Call(
                PlayerControl.LocalPlayer.NetId,
                RPCOperator.Command.FakerCreateDummy,
                new List<byte>
                { 
                    PlayerControl.LocalPlayer.PlayerId,
                    targetPlayerId
                });
            CreateDummy(
                PlayerControl.LocalPlayer.PlayerId,
                targetPlayerId);
            return true;
        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            this.CreateCommonAbilityOption(
                parentOps);
        }

        protected override void RoleSpecificInit()
        {
            this.DummyBody.Clear();
            this.RoleAbilityInit();
        }
    }
}
