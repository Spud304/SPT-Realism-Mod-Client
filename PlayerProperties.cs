﻿using EFT;

namespace RealismMod
{
    public static class PlayerProperties
    {
        public static float FixSkillMulti = 1f;

        public static float ReloadSkillMulti = 1f;

        public static float ReloadInjuryMulti = 1f;

        public static float ADSInjuryMulti = 1f;

        public static float RecoilInjuryMulti = 1f;

        public static float AimMoveSpeedBase = 0.43f;

        public static float ErgoDeltaInjuryMulti = 1f;

        public static float StrengthSkillAimBuff = 1f;

        public static bool IsAllowedADS = true;

        public static bool GearAllowsADS = true;

        public static float GearReloadMulti = 1f;

        public static float BaseSprintSpeed = 1f;

        public static EnvironmentType enviroType;

        public static bool IsClearingMalf;

        public static bool IsManipulatingWeapon;

        public static bool IsAllowedAim = true;

        public static bool IsAttemptingToReloadInternalMag = false;

        public static bool IsMagReloading = false;

        public static bool IsInReloadOpertation = false;

        public static bool NoMagazineReload = false;

        public static bool IsAttemptingRevolverReload = false;

        public static float TotalHandsIntensity = 1f;

        public static float WeaponSkillErgo = 0f;
    }
}
