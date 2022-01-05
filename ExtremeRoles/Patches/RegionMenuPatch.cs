// Adapted from https://github.com/MoltenMods/Unify
/*
MIT License

Copyright (c) 2021 Daemon

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using HarmonyLib;
using UnityEngine;
using System;
using UnityEngine.Events;

namespace ExtremeRoles.Patches
{
    [HarmonyPatch(typeof(RegionMenu), nameof(RegionMenu.Open))]
    public static class RegionMenuOpenPatch
    {

        private static TextBoxTMP ipField;
        private static TextBoxTMP portField;

        public static void Postfix(RegionMenu __instance)
        {

            var template = DestroyableSingleton<JoinGameButton>.Instance;
            if (template == null || template.GameIdText == null) { return; }

            if (ipField == null || ipField.gameObject == null)
            {
                ipField = UnityEngine.Object.Instantiate(
                    template.GameIdText, __instance.transform);

                ipField.gameObject.name = "IpTextBox";


                var arrow = ipField.transform.FindChild("arrowEnter");
                if (arrow == null || arrow.gameObject == null) { return; }
                UnityEngine.Object.DestroyImmediate(arrow.gameObject);

                ipField.transform.localPosition = new Vector3(0.2f, -1f, -100f);
                ipField.characterLimit = 30;
                ipField.AllowSymbols = true;
                ipField.ForceUppercase = false;
                ipField.SetText(
                    OptionsHolder.ConfigParser.Ip.Value);
                __instance.StartCoroutine(
                    Effects.Lerp(0.1f, new Action<float>(
                        (p) =>
                        {
                            ipField.outputText.SetText(OptionsHolder.ConfigParser.Ip.Value);
                            ipField.SetText(OptionsHolder.ConfigParser.Ip.Value);
                        })));

                ipField.ClearOnFocus = false; 
                ipField.OnEnter = ipField.OnChange = new UnityEngine.UI.Button.ButtonClickedEvent();
                ipField.OnFocusLost = new UnityEngine.UI.Button.ButtonClickedEvent();
                ipField.OnChange.AddListener((UnityAction)onEnterOrIpChange);
                ipField.OnFocusLost.AddListener((UnityAction)onFocusLost);
            }

            if (portField == null || portField.gameObject == null)
            {
                portField = UnityEngine.Object.Instantiate(
                    template.GameIdText, __instance.transform);

                portField.gameObject.name = "PortTextBox";


                var arrow = portField.transform.FindChild("arrowEnter");
                if (arrow == null || arrow.gameObject == null) { return; }
                UnityEngine.Object.DestroyImmediate(arrow.gameObject);

                portField.transform.localPosition = new Vector3(0.2f, -1.55f, -100f);
                portField.characterLimit = 5;
                portField.SetText(
                    OptionsHolder.ConfigParser.Port.Value.ToString());
                __instance.StartCoroutine(
                    Effects.Lerp(0.1f, new Action<float>(
                        (p) =>
                        {
                            portField.outputText.SetText(OptionsHolder.ConfigParser.Port.Value.ToString());
                            portField.SetText(OptionsHolder.ConfigParser.Port.Value.ToString()); 
                        })));


                portField.ClearOnFocus = false;
                portField.OnEnter = portField.OnChange = new UnityEngine.UI.Button.ButtonClickedEvent();
                portField.OnFocusLost = new UnityEngine.UI.Button.ButtonClickedEvent();
                portField.OnChange.AddListener((UnityAction)onEnterOrPortFieldChange);
                portField.OnFocusLost.AddListener((UnityAction)onFocusLost);

            }

            void onEnterOrIpChange()
            {
                OptionsHolder.ConfigParser.Ip.Value = ipField.text;
            }

            void onFocusLost()
            {
                OptionsHolder.UpdateRegion();
                __instance.ChooseOption(
                    ServerManager.DefaultRegions[ServerManager.DefaultRegions.Length - 1]);
            }

            void onEnterOrPortFieldChange()
            {
                ushort port = 0;
                if (ushort.TryParse(portField.text, out port))
                {
                    OptionsHolder.ConfigParser.Port.Value = port;
                    portField.outputText.color = Color.white;
                }
                else
                {
                    portField.outputText.color = Color.red;
                }
            }

        }
    }
}