using Harmony;
using MelonLoader;
using QMFreeze.Components;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC.SDKBase;

namespace QMFreeze
{
    public class BuildInfo
    {
        public const string Name = "QMFreeze";
        public const string Author = "tetra";
        public const string Version = "1.0.0";
        public const string DownloadLink = "https://github.com/tetra-fox/QMFreeze/releases/download/1.0.0/QMFreeze.dll";
    }

    public class Mod : MelonMod
    {
        public static bool FreezeAllowed;
        public static bool Frozen;
        private static Vector3 originalGravity;
        private static Vector3 originalVelocity;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Registering components...");
            ClassInjector.RegisterTypeInIl2Cpp<EnableDisableListener>();

            MelonLogger.Msg("Registering settings...");
            Settings.Register();
            Settings.Apply();
        }

        public override void VRChat_OnUiManagerInit()
        {
            MelonLogger.Msg("Adding QM listener...");
            // MicControls is enabled no matter the QM page that's open, so let's use that to determine whether or not the QM is open
            // Unless you have some other mod that removes this button then idk lol
            EnableDisableListener listener = GameObject.Find("/UserInterface/QuickMenu/MicControls").AddComponent<EnableDisableListener>();
            listener.OnEnabled += delegate { Freeze(); };
            listener.OnDisabled += delegate { Unfreeze(); };

            MelonLogger.Msg("Initialized!");
        }

        [HarmonyPatch(typeof(NetworkManager), "OnJoinedRoom")]
        private class OnJoinedRoomPatch
        {
            // This can definitely be exploited, so a world check is needed.
            private static void Prefix() => Utils.CheckWorld();
        }

        [HarmonyPatch(typeof(NetworkManager), "OnLeftRoom")]
        private class OnLeftRoomPatch
        {
            private static void Prefix()
            {
                Frozen = false;
                FreezeAllowed = false;
            }
        }

        public static void Freeze()
        {
            if (!FreezeAllowed || !Settings.Enabled) return;
            originalGravity = Physics.gravity;
            originalVelocity = Networking.LocalPlayer.GetVelocity();

            // Don't need to freeze if you're not moving
            if (originalVelocity == Vector3.zero) return;

            Physics.gravity = Vector3.zero;
            Networking.LocalPlayer.SetVelocity(Vector3.zero);
            Frozen = true;
            MelonLogger.Msg("Frozen");
        }

        public static void Unfreeze()
        {
            if (!FreezeAllowed || !Frozen || !Settings.Enabled) return;
            Physics.gravity = originalGravity;
            // If you're trying to respawn after being launched at a super high velocity, you might want this off so that you don't keep flying after respawning
            if (Settings.RestoreVelocity) Networking.LocalPlayer.SetVelocity(originalVelocity);
            Frozen = false;
            MelonLogger.Msg("Unfrozen");
        }

        public override void OnPreferencesLoaded() => Settings.Apply();

        public override void OnPreferencesSaved() => Settings.Apply();
    }
}