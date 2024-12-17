﻿using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ContusionInterface = GInterface314;
using DamageTypeClass = GClass2788; //this.ApplyDamage(EBodyPart.LeftLeg
using DehydrationInterface = GInterface305;
using EffectClass = HealthControllerClass.GClass2752; //ManualEffectUpdate
using EffectsDictionary = GClass2795; // Not super confident on this being right
using ExhaustionInterface = GInterface306;
using FractureInterface = GInterface304;
using HeavyBleedingInterface = GInterface302;
using IntoxicationInterface = GInterface297;
using LethalToxinInterface = GInterface309;
using LightBleedingInterface = GInterface301;
using MedUiString = GClass1352;
using PainKillerInterface = GInterface320;
using TremorInterface = GInterface323;
using TunnelVisionInterface = GInterface325;
using LootItemClass = GClass2981;
using EffectListClass = GClass2747<EFT.Player>;


namespace RealismMod
{
    public static class MedProperties
    {
        public static string MedType(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) ? med.ConflictingItems[1] : "Unknown";
        }

        public static string HBleedHealType(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) ? med.ConflictingItems[2] : "Unknown";
        }

        public static float HpPerTick(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) && float.TryParse(med.ConflictingItems[3], out float result) ? result : 1f;
        }

        public static bool CanBeUsedInRaid(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) && bool.TryParse(med.ConflictingItems[4], out bool result) ? result : false;
        }

        public static int PainKillerDuration(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[5], out int result) ? result : 1;
        }

        public static float HPRestoreAmount(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems, 7) && float.TryParse(med.ConflictingItems[6], out float result) ? result : 1;
        }

        public static int Unused2(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[7], out int result) ? result : 1;
        }

        public static float TunnelVisionStrength(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) && float.TryParse(med.ConflictingItems[8], out float result) ? result : 1f;
        }

        public static int Delay(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) && int.TryParse(med.ConflictingItems[9], out int result) ? result : 1;
        }

        public static float Strength(Item med)
        {
            return !Utils.IsConfItemNull(med.ConflictingItems) && float.TryParse(med.ConflictingItems[10], out float result) ? result : 0f;
        }

        public static readonly Dictionary<string, Type> EffectTypes = new Dictionary<string, Type>
        {
            { "PainKiller", typeof(PainKillerInterface) },
            { "Tremor", typeof(TremorInterface) },
            { "BrokenBone", typeof(FractureInterface) },
            { "TunnelVision", typeof(TunnelVisionInterface) },
            { "Contusion", typeof(ContusionInterface)  },
            { "HeavyBleeding", typeof(HeavyBleedingInterface) },
            { "LightBleeding", typeof(LightBleedingInterface) },
            { "Dehydration", typeof(DehydrationInterface) },
            { "Exhaustion", typeof(ExhaustionInterface) },
            { "LethalToxin", typeof(IntoxicationInterface) },
            { "Intoxication", typeof(LethalToxinInterface) }
        };
    }

    public enum EStimType 
    {
        Regenerative,
        Damage,
        Adrenal,
        Clotting,
        Temperature,
        Performance,
        Generic,
        Weight
    }

    public enum EHealBlockType
    {
        Splint,
        Trnqt,
        Surgery,
        GearCommon,
        GearSpecific,
        Unknown,
        Pills,
        Food
    }

    public class RealismHealthController
    {
        public const float TOXIC_ITEM_FACTOR = 0.05f;
        public const float RAD_ITEM_FACTOR = 0.15f;
        public const float MIN_COUGH_THRESHOLD = 0.09f;
        public const float MIN_COUGH_DAMAGE_THRESHOLD = 0.14f;

        HashSet<string> ToxicItems = new HashSet<string>(new string[] {
            "593a87af86f774122f54a951",
            "5b4c81bd86f77418a75ae159",
            "5b4c81a086f77417d26be63f",
            "5b43237186f7742f3a4ab252",
            "5a687e7886f7740c4a5133fb",
            "63927b29c115f907b14700b9",
            "66fd588956f73c4f38dd07ae"
        });

        HashSet<string> RadioactiveItems = new HashSet<string>(new string[] {
            "66fd57171f981640e667fbe2"
        });

        public Dictionary<string, EStimType> StimTypes = new Dictionary<string, EStimType>()
        {
            {"5c10c8fd86f7743d7d706df3", EStimType.Adrenal},
            {"5ed515e03a40a50460332579", EStimType.Adrenal},
            {"637b620db7afa97bfc3d7009", EStimType.Adrenal},
            {"5c0e533786f7747fa23f4d47", EStimType.Clotting},
            {"5ed515f6915ec335206e4152", EStimType.Clotting},
            {"5ed515ece452db0eb56fc028", EStimType.Damage},
            {"637b6179104668754b72f8f5", EStimType.Damage},
            {"5ed5160a87bb8443d10680b5", EStimType.Performance},
            {"5ed515c8d380ab312177c0fa", EStimType.Performance},
            {"5c0e531286f7747fa54205c2", EStimType.Performance},
            {"5c0e531d86f7747fa23f4d42", EStimType.Performance},
            {"5c0e530286f7747fa1419862", EStimType.Regenerative},
            {"5c0e534186f7747fa1419867", EStimType.Regenerative},
            {"SJ0", EStimType.Regenerative},
            {"637b6251104668754b72f8f9", EStimType.Generic},
            {"637b612fb7afa97bfc3d7005", EStimType.Generic},
            {"5fca13ca637ee0341a484f46", EStimType.Generic},
            {"637b60c3b7afa97bfc3d7001", EStimType.Generic},
            {"5ed5166ad380ab312177c100", EStimType.Generic},
            {"5ed51652f6c34d2cc26336a1", EStimType.Weight },
            {"66507eabf5ddb0818b085b68", EStimType.Weight }
        };

        public List<EBodyPart> PossibleBodyParts = new List<EBodyPart>
        {
            EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm
        };

        public EBodyPart[] BodyPartsArr = { EBodyPart.Head, EBodyPart.Chest, EBodyPart.Stomach, EBodyPart.RightLeg, EBodyPart.LeftLeg, EBodyPart.RightArm, EBodyPart.LeftArm };

        private List<ICustomHealthEffect> _activeHealthEffects = new List<ICustomHealthEffect>();

        private List<EStimType> _activeStimOverdoses = new List<EStimType>();

        public DamageTracker DmgeTracker { get; }

        public PlayerZoneBridge PlayerHazardBridge { get; private set; }

        public bool HasAdrenalineEffect { get; set; } = false;

        public bool IsCoughingInGas { get; private set; } = false;

        public bool DoCoughingAudio { get; private set; } = false;

        public int ToxicItemCount { get; private set; } = 0;
        public int RadItemCount { get; private set; } = 0;

        public bool IsPoisoned { get; set; } = false;

        public bool CancelPassiveRegen { get; set; } = false;

        public bool IsDehydrated { get; set; } = false;

        public bool IsExhausted { get; set; } = false;

        public float CurrentPassiveRegenBlockDuration { get; set; } = 0f;

        public float BlockPassiveRegenBaseDuration
        {
            get
            {
                return 600f * (1f - PlayerState.VitalityFactorStrong);
            }
        }

        public bool BlockPassiveRegen
        {
            get
            {
                return PlayerState.IsSprinting || HasOverdosed || IsPoisoned ||
                    HazardTracker.TotalToxicity > 15f || HazardTracker.TotalRadiation > 15f ||
                    IsCoughingInGas;
            }
        }

        public float AdrenalineMovementBonus
        {
            get
            {
                return HasAdrenalineEffect ? 0.9f + Mathf.Pow(PlayerState.StressResistanceFactor, 1.5f) : 1f;
            }
        }

        public float AdrenalineReloadBonus
        {
            get
            {
                return HasAdrenalineEffect ? 0.9f + Mathf.Pow(PlayerState.StressResistanceFactor, 1.25f) : 1f;
            }
        }

        public float AdrenalineStanceBonus
        {
            get
            {
                return HasAdrenalineEffect ? 0.9f + Mathf.Pow(PlayerState.StressResistanceFactor, 1.25f) : 1f;
            }
        }

        public float AdrenalineADSBonus
        {
            get
            {
                return HasAdrenalineEffect ? 0.9f + (PlayerState.StressResistanceFactor * 1.5f) : 1f;
            }
        }

        public bool ArmsAreIncapacitated
        {
            get
            {
                return (_rightArmRuined || _leftArmRuined || (PainStrength > PAIN_ARM_THRESHOLD && PainStrength > PainReliefStrength)) && !IsOnPKStims;
            }
        }

        public bool HealthConditionPreventsTacSprint
        {
            get
            {
                return HazardTracker.TotalToxicity > 20f || HazardTracker.TotalRadiation > 40f || ArmsAreIncapacitated || HasOverdosed || IsPoisoned || IsCoughingInGas || IsDehydrated || IsExhausted;
            }
        }

        public bool HealthConditionForcedLowReady
        {
            get
            {
                return HazardTracker.TotalToxicity > 40f || HazardTracker.TotalRadiation > 60f || ArmsAreIncapacitated || HasOverdosed || IsPoisoned || IsCoughingInGas || IsDehydrated || IsExhausted;
            }
        }

        public bool HasOverdosed
        {
            get
            {
                return PainReliefStrength > PKOverdoseThreshold || _hasOverdosedStim;
            }
        }

        public float PKOverdoseThreshold
        {
            get
            {
                return BASE_OK_OVERDOSETHRESHOLD * (1f + PlayerState.ImmuneSkillStrong);
            }
        }

        public bool IsOnPKStims
        {
            get
            {
                return _hasPKStims && !_hasOverdosedStim;
            }
        }

        private float _percentReources = 1f;

        private float _healthControllerTime = 0f;
        private float _effectsTime = 0f;
        private float _reliefWaitTime = 0f;

        private float _stimOverdoseWaitTime = 0f;
        private bool _doStimOverdoseTimer = false;
        private string _overdoseEffectToAdd = "";

        public const float DOUBLE_CLICK_TIME = 0.2f;
        private float _timeSinceLastClicked = 0f;
        private bool _clickTriggered = false;

        public const float ADRENALINE_BASE_VALUE = 70f;
        private float _adrenalineCooldownTime = ADRENALINE_BASE_VALUE * (1f - PlayerState.StressResistanceFactor);
        public bool AdrenalineCooldownActive = false;

        //temporary solution
        private bool _reset1 = false;
        private bool _reset2 = false;
        private bool _reset3 = false;
        private bool _reset4 = false;
        private bool _reset5 = false;

        private float _baseMaxHPRestore = 86f;

        public float PainStrength = 0f;
        public float PainEffectThreshold = 10f;
        public float PainReliefStrength = 0f;
        public float PainTunnelStrength = 0f;
        public int ReliefDuration = 0;
        private bool _hasPKStims = false;

        public const float PAIN_RELIEF_INTERVAL = 15f;
        public const float PAIN_ARM_THRESHOLD = 30f;
        public const float PAIN_RELIEF_THRESHOLD = 30f;
        public const float BASE_OK_OVERDOSETHRESHOLD = 45f;

        public const float TOXICITY_THRESHOLD = 15f;
        public const float RADIATION_THRESHOLD = 30f;
        public const float RAD_TREATMENT_THRESHOLD = 40f;
        public const float BASE_TOX_RECOVERY_RATE = -0.05f;
        public const float HAZARD_INTERVAL = 10f;
        private float _hazardWaitTime = 0f;

        private bool _rightArmRuined = false;
        private bool _leftArmRuined = false;

        private bool _hasOverdosedStim = false;

        public float ResourcePerTick = 0;

        private bool _haveNotifiedPKOverdose = false;

        private bool _addedCustomEffectsToDict = false;

        public RealismHealthController(DamageTracker dmgTracker)
        {
            DmgeTracker = dmgTracker;
        }

        public void ControllerUpdate()
        {
            //needed for Fika
            if (!_addedCustomEffectsToDict)
            {
                AddCustomEffectsToDict();
                _addedCustomEffectsToDict = true;
            }

            if (!Utils.IsInHideout && Utils.PlayerIsReady)
            {
                _healthControllerTime += Time.deltaTime;
                _effectsTime += Time.deltaTime;
                _reliefWaitTime += Time.deltaTime;
                _hazardWaitTime += Time.deltaTime;

                HealthEffecTick();

                if (Input.GetKeyDown(PluginConfig.AddEffectKeybind.Value.MainKey))
                {
                    /*                    AddStimDebuffs(Utils.GetYourPlayer(), Plugin.AddEffectType.Value);*/ // use this to test stim debuffs
                    TestAddBaseEFTEffect(PluginConfig.AddEffectBodyPart.Value, Utils.GetYourPlayer(), PluginConfig.AddEffectType.Value);
                    if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayMessageNotification("Adding Health Effect " + PluginConfig.AddEffectType.Value + " To Part " + (EBodyPart)PluginConfig.AddEffectBodyPart.Value);
                }

                if (Input.GetKeyDown(PluginConfig.DropGearKeybind.Value.MainKey))
                {
                    if (_clickTriggered)
                    {
                        DropBlockingGear(Utils.GetYourPlayer());
                        _clickTriggered = false;
                    }
                    else
                    {
                        _clickTriggered = true;
                    }
                    _timeSinceLastClicked = 0f;
                }
                _timeSinceLastClicked += Time.deltaTime;
                if (_timeSinceLastClicked > DOUBLE_CLICK_TIME)
                {
                    _clickTriggered = false;
                }

                if (_doStimOverdoseTimer)
                {
                    _stimOverdoseWaitTime += Time.deltaTime;
                    if (_stimOverdoseWaitTime >= 10f)
                    {
                        AddStimDebuffs(Utils.GetYourPlayer(), _overdoseEffectToAdd);
                        _doStimOverdoseTimer = false;
                        _stimOverdoseWaitTime = 0f;
                    }
                }
            }

            if (Utils.IsInHideout || !Utils.PlayerIsReady)
            {
                ResetAllEffects();
                DmgeTracker.ResetTracker();
            }

            if (AdrenalineCooldownActive)
            {
                _adrenalineCooldownTime -= Time.deltaTime;

                if (_adrenalineCooldownTime <= 0.0f)
                {
                    _adrenalineCooldownTime = ADRENALINE_BASE_VALUE * (1f - PlayerState.StressResistanceFactor);
                    AdrenalineCooldownActive = false;
                }
            }

            if (CancelPassiveRegen)
            {
                CurrentPassiveRegenBlockDuration -= Time.deltaTime;

                if (CurrentPassiveRegenBlockDuration <= 0.0f)
                {
                    CancelPassiveRegen = false;
                    CurrentPassiveRegenBlockDuration = BlockPassiveRegenBaseDuration;
                }
            }
        }

        //To prevent null ref exceptions while using Fika, Realism's custom effects must be added to a dicitionary of existing EFT effects
        public void AddCustomEffectsToDict()
        {
            Type[] customTypes = new Type[] { typeof(ResourceRateDrain), typeof(HealthChange), typeof(HealthDrain), typeof(ToxicityDamage), typeof(RadiationDamage) };

            Type type0 = typeof(EffectsDictionary);
            FieldInfo dictionaryField0 = type0.GetField("dictionary_0", BindingFlags.NonPublic | BindingFlags.Static);
            var effectDict0 = (Dictionary<string, byte>)dictionaryField0.GetValue(null);
            foreach (var customType in customTypes)
            {
                effectDict0.Add(customType.ToString(), Convert.ToByte(effectDict0.Count + 1));
            }
            dictionaryField0.SetValue(null, effectDict0);

            Type type1 = typeof(EffectsDictionary);
            FieldInfo dictionaryField1 = type1.GetField("dictionary_1", BindingFlags.NonPublic | BindingFlags.Static);
            var effectDict1 = (Dictionary<byte, string>)dictionaryField1.GetValue(null);
            foreach (var customType in customTypes)
            {
                effectDict1.Add(Convert.ToByte(effectDict1.Count + 1), customType.ToString());
            }
            dictionaryField1.SetValue(null, effectDict1);

            Type typeType = typeof(EffectsDictionary);
            FieldInfo typeArrFieldInfo = typeType.GetField("type_0", BindingFlags.NonPublic | BindingFlags.Static);
            var typeArr = (Type[])typeArrFieldInfo.GetValue(null);
            customTypes.CopyTo(typeArr, 0);
            typeArrFieldInfo.SetValue(null, customTypes);
        }

        public void TestAddBaseEFTEffect(int partIndex, Player player, String effect)
        {
            if (effect == "")
            {
                return;
            }

            if (effect == "removeHP")
            {
                player.ActiveHealthController.ChangeHealth((EBodyPart)partIndex, -player.ActiveHealthController.GetBodyPartHealth((EBodyPart)partIndex).Maximum, DamageTypeClass.Existence);
                return;
            }
            if (effect == "addHP")
            {
                player.ActiveHealthController.ChangeHealth((EBodyPart)partIndex, player.ActiveHealthController.GetBodyPartHealth((EBodyPart)partIndex).Maximum, DamageTypeClass.Existence);
                return;
            }
            int healAmount = 0;
            if (int.TryParse(effect, out healAmount))
            {
                player.ActiveHealthController.ChangeHealth((EBodyPart)partIndex, healAmount, DamageTypeClass.Existence);
                return;
            }

            Type effectType = typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance);
            if (effectType == null)
            {
                Utils.Logger.LogError("nest type is null: " + effect);
                return;
            }
            MethodInfo effectMethod = GetAddBaseEFTEffectMethodInfo();
            effectMethod.MakeGenericMethod(effectType).Invoke(player.ActiveHealthController, new object[] { (EBodyPart)partIndex, null, null, null, null, null });
        }

        public void AddBasesEFTEffect(Player player, String effect, EBodyPart bodyPart, float? delayTime, float? duration, float? residueTime, float? strength)
        {
            MethodInfo effectMethod = GetAddBaseEFTEffectMethodInfo();
            effectMethod.MakeGenericMethod(typeof(EFT.HealthSystem.ActiveHealthController).GetNestedType(effect, BindingFlags.NonPublic | BindingFlags.Instance)).Invoke(player.ActiveHealthController, new object[] { bodyPart, delayTime, duration, residueTime, strength, null });
        }

        public void AddBaseEFTEffectIfNoneExisting(Player player, string effect, EBodyPart bodyPart, float? delayTime, float? duration, float? residueTime, float? strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(e => e.Key == effect))
            {
                AddBasesEFTEffect(player, effect, bodyPart, delayTime, duration, residueTime, strength);
            }
        }

        public void AddToExistingBaseEFTEffect(Player player, string targetEffect, EBodyPart bodyPart, float delayTime, float? duration, float residueTime, float strength)
        {
            if (!player.ActiveHealthController.BodyPartEffects.Effects[0].Any(e => e.Key == targetEffect))
            {
                AddBasesEFTEffect(player, targetEffect, bodyPart, delayTime, duration, residueTime, strength);
            }
            else
            {
                IReadOnlyList<EffectClass> effectsList = player.ActiveHealthController.IReadOnlyList_0;
                Type targetType = null;
                MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType);
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    EffectClass existingEffect = effectsList[i];
                    Type effectType = existingEffect.Type;
                    EBodyPart effectPart = existingEffect.BodyPart;

                    if (effectType == targetType)
                    {
                        existingEffect.AddWorkTime(duration, false); // class 2746 is the only one with this method, but cannot find a way to get a list of it
                    }
                }
            }
        }

        public void TryAddAdrenaline(Player player, float painkillerDuration, float negativeEffectDuration, float negativeEffectStrength)
        {
            if (PluginConfig.EnableAdrenaline.Value && !AdrenalineCooldownActive)
            {
                AdrenalineCooldownActive = true;
                AdrenalineEffect adrenalineEffect = new AdrenalineEffect(player, (int)painkillerDuration, 0, negativeEffectDuration, painkillerDuration, negativeEffectStrength, this);
                Plugin.RealHealthController.AddCustomEffect(adrenalineEffect, true);
            }
        }

        public MethodInfo GetAddBaseEFTEffectMethodInfo()
        {
            MethodInfo effectMethodInfo = typeof(EFT.HealthSystem.ActiveHealthController).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(e =>
            e.GetParameters().Length == 6
            && e.GetParameters()[0].Name == "bodyPart"
            && e.GetParameters()[5].Name == "initCallback"
            && e.IsGenericMethod);
            return effectMethodInfo;
        }

        public void RemoveBaseEFTEffect(Player player, EBodyPart targetBodyPart, string targetEffect)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(targetBodyPart);
            IReadOnlyList<EffectClass> effectsList = player.ActiveHealthController.IReadOnlyList_0;

            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    EffectClass effect = effectsList[i];
                    Type effectType = effect.Type;
                    EBodyPart effectPart = effect.BodyPart;

                    if (effectType == targetType && effectPart == targetBodyPart)
                    {
                        effect.ForceResidue();
                    }
                }
            }
        }

        public void AddCustomEffect(ICustomHealthEffect newEffect, bool canStack)
        {
            //need to decide if it's better to keep the old effect or to replace it with a new one.
            if (!canStack)
            {
                foreach (ICustomHealthEffect existingEff in _activeHealthEffects)
                {
                    if (existingEff.GetType() == newEffect.GetType() && existingEff.BodyPart == newEffect.BodyPart)
                    {
                        RemoveCustomEffectOfType(newEffect.GetType(), newEffect.BodyPart);
                        break;
                    }
                }
            }

            _activeHealthEffects.Add(newEffect);
        }

        public bool AddCustomEffectIfNoneExisting(ICustomHealthEffect newEffect)
        {
            foreach (ICustomHealthEffect existingEff in _activeHealthEffects)
            {
                if (existingEff.GetType() == newEffect.GetType() && existingEff.BodyPart == newEffect.BodyPart)
                {
                    return false;
                }
            }

            _activeHealthEffects.Add(newEffect);
            return true;
        }


        public void RemoveCustomEffectOfType(Type effect, EBodyPart bodyPart)
        {
            for (int i = _activeHealthEffects.Count - 1; i >= 0; i--)
            {
                ICustomHealthEffect activeHealthEffect = _activeHealthEffects[i];
                if (activeHealthEffect.GetType() == effect && activeHealthEffect.BodyPart == bodyPart)
                {
                    _activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public bool HasCustomEffectOfType(Type effect, EBodyPart bodyPart = EBodyPart.Common)
        {
            bool hasEffect = false;
            for (int i = _activeHealthEffects.Count - 1; i >= 0; i--)
            {
                ICustomHealthEffect activeHealthEffect = _activeHealthEffects[i];
                bool partMathces = bodyPart == EBodyPart.Common ? true : activeHealthEffect.BodyPart == bodyPart;
                if (activeHealthEffect.GetType() == effect && partMathces)
                {
                    hasEffect = true;
                }
            }
            return hasEffect;
        }

        public ICustomHealthEffect GetCustomEffectOfType<T>(EBodyPart bodyPart) where T : ICustomHealthEffect
        {
            ICustomHealthEffect effect = null;
            for (int i = _activeHealthEffects.Count - 1; i >= 0; i--)
            {
                ICustomHealthEffect activeHealthEffect = _activeHealthEffects[i];
                if (activeHealthEffect.GetType() == typeof(T) && activeHealthEffect.BodyPart == bodyPart)
                {
                    effect = activeHealthEffect;
                }
            }
            return effect;
        }

        public void CancelPendingEffects()
        {
            for (int i = _activeHealthEffects.Count - 1; i >= 0; i--)
            {
                if (_activeHealthEffects[i].Delay > 0f)
                {
                    _activeHealthEffects.RemoveAt(i);
                }
            }
        }

        public void RemoveRegenEffectsOfType(EDamageType damageType)
        {
            List<HealthRegenEffect> regenEffects = _activeHealthEffects.OfType<HealthRegenEffect>().ToList();
            regenEffects.RemoveAll(r => r.DamageType == damageType);
            _activeHealthEffects.RemoveAll(a => !regenEffects.Contains(a));
        }

        public void RemoveEffectsOfType(EHealthEffectType effectType)
        {
            _activeHealthEffects.RemoveAll(a => a.EffectType == effectType);
        }

        public void ResetAllEffects()
        {
            _activeStimOverdoses.Clear();
            _activeHealthEffects.Clear();
            PainStrength = 0f;
            PainReliefStrength = 0f;
            PainTunnelStrength = 0f;
            ReliefDuration = 0;
            _hasOverdosedStim = false;
            _leftArmRuined = false;
            _rightArmRuined = false;
            ResetHealhPenalties();
        }

        public void ResetBleedDamageRecord(Player player)
        {
            bool hasHeavyBleed = false;
            bool hasLightBleed = false;

            Type heavyBleedType;
            Type lightBleedType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);

            foreach (EBodyPart part in BodyPartsArr)
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

                if (heavyBleedType != null && effects.Any(h => h.Type == heavyBleedType))
                {
                    hasHeavyBleed = true;
                }
                if (lightBleedType != null && effects.Any(l => l.Type == lightBleedType))
                {
                    hasLightBleed = true;
                }
            }
            if (!hasHeavyBleed)
            {
                DmgeTracker.TotalHeavyBleedDamage = 0f;
            }
            if (!hasLightBleed)
            {
                DmgeTracker.TotalLightBleedDamage = 0f;
            }
        }

        public bool HasBaseEFTEffect(Player player, string targetEffect)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllEffects();
            IReadOnlyList<EffectClass> effectsList = player.ActiveHealthController.IReadOnlyList_0;
            Type targetType = null;
            if (MedProperties.EffectTypes.TryGetValue(targetEffect, out targetType))
            {
                for (int i = effectsList.Count - 1; i >= 0; i--)
                {
                    EffectClass effect = effectsList[i];
                    Type effectType = effect.Type;
                    if (effectType == targetType)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private void AddStimDebuffs(Player player, string debuffId)
        {
            MedsItemClass placeHolderItem = (MedsItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(Utils.GenId(), debuffId, null);
            placeHolderItem.CurrentAddress = player.InventoryController.FindQuestGridToPickUp(placeHolderItem); //item needs an address to be valid, this is a hacky workaround
            player.ActiveHealthController.DoMedEffect(placeHolderItem, EBodyPart.Head, null);

            if (PluginConfig.EnableLogging.Value)
            {
                Utils.Logger.LogWarning("is null " + (placeHolderItem == null));
                Utils.Logger.LogWarning("" + placeHolderItem.HealthEffectsComponent.StimulatorBuffs);
                Utils.Logger.LogWarning("added " + debuffId);
            }
        }

        private void EvaluateStimSingles(Player player, IEnumerable<IGrouping<EStimType, StimEffectShell>> stimGroups)
        {

            _hasPKStims = false;
            foreach (var group in stimGroups)
            {
                switch (group.Key)
                {
                    case EStimType.Adrenal:
                        _activeStimOverdoses.Remove(EStimType.Adrenal);
                        _hasPKStims = true;
                        break;
                    case EStimType.Regenerative:
                        _activeStimOverdoses.Remove(EStimType.Regenerative);
                        break;
                    case EStimType.Damage:
                        _activeStimOverdoses.Remove(EStimType.Damage);
                        _hasPKStims = true;
                        break;
                    case EStimType.Clotting:
                        _activeStimOverdoses.Remove(EStimType.Clotting);
                        break;
                    case EStimType.Weight:
                        _activeStimOverdoses.Remove(EStimType.Weight);
                        break;
                    case EStimType.Performance:
                        _activeStimOverdoses.Remove(EStimType.Performance);
                        break;
                    case EStimType.Generic:
                        _activeStimOverdoses.Remove(EStimType.Generic);
                        break;
                }
            }
        }

        private void EvaluateStimDuplicates(Player player, IEnumerable<IGrouping<EStimType, StimEffectShell>> stimGroups)
        {
            foreach (var group in stimGroups) // use this to count duplicates per category
            {
                switch (group.Key)
                {
                    case EStimType.Adrenal:
                        if (!_activeStimOverdoses.Contains(EStimType.Adrenal)) //if no active adrenal overdose
                        {

                            _activeStimOverdoses.Add(EStimType.Adrenal);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "adrenal_debuff";
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("Overdosed On Adrenal Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Regenerative:
                        if (!_activeStimOverdoses.Contains(EStimType.Regenerative))
                        {
                            _activeStimOverdoses.Add(EStimType.Regenerative);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "regen_debuff";
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("Overdosed On Regenerative Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Damage:
                        if (!_activeStimOverdoses.Contains(EStimType.Damage))
                        {
                            _activeStimOverdoses.Add(EStimType.Damage);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "damage_debuff";
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("Overdosed On Combat Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Clotting:
                        if (!_activeStimOverdoses.Contains(EStimType.Clotting))
                        {
                            _activeStimOverdoses.Add(EStimType.Clotting);
                            AddStimDebuffs(player, "clotting_debuff");
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("Overdosed On Coagulating Stims", EFT.Communications.ENotificationDurationType.Long);
                        }

                        break;
                    case EStimType.Weight:
                        if (!_activeStimOverdoses.Contains(EStimType.Weight))
                        {
                            _activeStimOverdoses.Add(EStimType.Weight);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "weight_debuff";
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("Overdosed On Weight-Reducing Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Performance:
                        if (!_activeStimOverdoses.Contains(EStimType.Performance))
                        {
                            _activeStimOverdoses.Add(EStimType.Performance);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "performance_debuff";
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("Overdosed On Performance-Enhancing Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                    case EStimType.Generic:
                        if (!_activeStimOverdoses.Contains(EStimType.Generic))
                        {
                            _activeStimOverdoses.Add(EStimType.Generic);
                            _doStimOverdoseTimer = true;
                            _overdoseEffectToAdd = "generic_debuff";
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("Overdosed On Stims", EFT.Communications.ENotificationDurationType.Long);
                        }
                        break;
                }
            }
        }

        public void EvaluateActiveStims(Player player)
        {
            IEnumerable<StimEffectShell> activeStims = _activeHealthEffects.OfType<StimEffectShell>();
            var stimTypeGroups = activeStims.GroupBy(effect => effect.StimType);
            var duplicatesGrouping = stimTypeGroups.Where(group => group.Count() > 1);
            var singlesGrouping = stimTypeGroups.Where(group => group.Count() <= 1);
            int totalDuplicates = duplicatesGrouping.Sum(group => group.Count());
            EvaluateStimDuplicates(player, duplicatesGrouping);
            EvaluateStimSingles(player, singlesGrouping);
            if (totalDuplicates > 1)
            {
                _hasOverdosedStim = true;
            }
            else _hasOverdosedStim = false;
        }

        public EStimType GetStimType(string id)
        {
            return StimTypes.TryGetValue(id, out EStimType type) ? type : EStimType.Generic;
        }

        public void ResourceRegenCheck(Player player)
        {
            float vitalitySkill = player.Skills.VitalityBuffSurviobilityInc.Value;
            int delay = (int)Math.Round(15f * (1f - vitalitySkill), 2);
            float tickRate = (float)Math.Round(0.22f * (1f + vitalitySkill), 2);

            IsDehydrated = HasBaseEFTEffect(player, "Dehydration");
            IsExhausted = HasBaseEFTEffect(player, "Exhaustion");

            if (IsDehydrated)
            {
                RemoveRegenEffectsOfType(EDamageType.Dehydration);
            }
            if (!IsDehydrated && DmgeTracker.TotalDehydrationDamage > 0f)
            {
                RestoreHPArossBody(player, DmgeTracker.TotalDehydrationDamage, delay, EDamageType.Dehydration, tickRate);
                DmgeTracker.TotalDehydrationDamage = 0;
            }

            if (IsExhausted)
            {
                RemoveRegenEffectsOfType(EDamageType.Exhaustion);
            }
            if (!IsExhausted && DmgeTracker.TotalExhaustionDamage > 0f)
            {
                RestoreHPArossBody(player, DmgeTracker.TotalExhaustionDamage, delay, EDamageType.Exhaustion, tickRate);
                DmgeTracker.TotalExhaustionDamage = 0;
            }
        }

        private void PainReliefCheck(Player player)
        {
            if (PainStrength >= PainEffectThreshold && !IsOnPKStims)
            {
                AddBaseEFTEffectIfNoneExisting(player, "Pain", EBodyPart.Chest, 0f, 15f, 1f, 1f);
            }

            ReliefDuration = Math.Max(ReliefDuration - 1, 0);
            if (ReliefDuration > 0)
            {
                if (PainReliefStrength >= PainStrength || IsOnPKStims)
                {
                    AddBaseEFTEffectIfNoneExisting(player, "PainKiller", EBodyPart.Head, 1f, ReliefDuration, 5f, 1f);
                }
                else if (PainStrength > PainReliefStrength)
                {
                    RemoveBaseEFTEffect(player, EBodyPart.Head, "PainKiller");
                }

                if (_reliefWaitTime >= PAIN_RELIEF_INTERVAL)
                {
                    AddBasesEFTEffect(player, "TunnelVision", EBodyPart.Head, 1f, PAIN_RELIEF_INTERVAL, 5f, PainTunnelStrength);

                    if (PainReliefStrength > PKOverdoseThreshold)
                    {
                        if (!_haveNotifiedPKOverdose)
                        {
                            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("You Have Overdosed", EFT.Communications.ENotificationDurationType.Long);
                            _haveNotifiedPKOverdose = true;
                        }
                        AddBasesEFTEffect(player, "Contusion", EBodyPart.Head, 1f, PAIN_RELIEF_INTERVAL, 5f, 0.35f);
                        AddToExistingBaseEFTEffect(player, "Tremor", EBodyPart.Head, 1f, PAIN_RELIEF_INTERVAL, 5f, 1f);
                    }
                    else _haveNotifiedPKOverdose = false;

                    _reliefWaitTime = 0f;
                }
            }
        }

        private void TickEffects()
        {
            for (int i = _activeHealthEffects.Count - 1; i >= 0; i--)
            {
                ICustomHealthEffect effect = _activeHealthEffects[i];
                /*      if (PluginConfig.EnableLogging.Value)
                      {
                          Utils.Logger.LogWarning("Type = " + effect.GetType().ToString());
                          Utils.Logger.LogWarning("Delay = " + effect.Delay);
                      }*/

                effect.Delay = Math.Max(effect.Delay - 1, 0);

                if (effect.Duration == null || effect.Duration > 0f)
                {
                    effect.Tick();
                }
                else
                {
                    if (PluginConfig.EnableLogging.Value)
                    {
                        Utils.Logger.LogWarning("Removing Effect Due to Duration");
                    }
                    _activeHealthEffects.RemoveAt(i);
                }
            }
        }

        //replace all this logic with a schedular class to control execution timing
        public void HealthEffecTick()
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            PlayerState.ImmuneSkillWeak = player.Skills.ImmunityPainKiller.Value;
            PlayerState.ImmuneSkillStrong = player.Skills.ImmunityMiscEffects.Value;
            PlayerState.StressResistanceFactor = player.Skills.StressPain.Value;
            PlayerState.VitalityFactorStrong = player.Skills.VitalityBuffBleedChanceRed.Value;

            if (_healthControllerTime >= 0.5f && !_reset1)
            {
                ResetBleedDamageRecord(player);
                _reset1 = true;
            }
            if (_healthControllerTime >= 1f && !_reset2)
            {
                ResourceRegenCheck(player);
                _reset2 = true;
            }
            if (_healthControllerTime >= 2f && !_reset3)
            {
                DoubleBleedCheck(player);
                _reset3 = true;
            }
            if (_healthControllerTime >= 2.5f && !_reset4)
            {
                EvaluateActiveStims(player);
                _reset4 = true;
            }
            if (_healthControllerTime >= 3f && !_reset5)
            {
                PlayerInjuryStateCheck(player);
                _reset5 = true;
            }

            if (_effectsTime >= 1f)
            {
                if (Plugin.ServerConfig.enable_hazard_zones) HazardZoneHealthTick(player);
                PainReliefCheck(player);
                TickEffects();
                _effectsTime = 0f;
            }

            DoResourceDrain(player.ActiveHealthController, Time.deltaTime);

            if (PluginConfig.PassiveRegen.Value && !HasCustomEffectOfType(typeof(PassiveHealthRegenEffect), EBodyPart.Common))
            {
                PassiveHealthRegenEffect resEffect = new PassiveHealthRegenEffect(player, this);
                AddCustomEffect(resEffect, false);
            }

            //temporary timer solution :')
            if (_healthControllerTime >= 3f)
            {
                _healthControllerTime = 0f;
                _reset1 = false;
                _reset2 = false;
                _reset3 = false;
                _reset4 = false;
                _reset5 = false;
            }
        }

        public void DropBlockingGear(Player player)
        {
            Player.ItemHandsController itemHandsController = player.HandsController as Player.ItemHandsController;
            if (itemHandsController != null && itemHandsController.CurrentCompassState)
            {
                itemHandsController.SetCompassState(false);
                return;
            }

            if (player.MovementContext.StationaryWeapon == null && !player.HandsController.IsPlacingBeacon() && !player.HandsController.IsInInteractionStrictCheck() && player.CurrentStateName != EPlayerState.BreachDoor && !player.IsSprintEnabled)
            {
                InventoryController inventoryController = (InventoryController)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
                InventoryEquipment equipment = player.Equipment;

                List<Item> gear = new List<Item>();
                List<EquipmentSlot> slots = new List<EquipmentSlot>();

                if (BodyPartHasBleed(player, EBodyPart.Head))
                {
                    slots.Add(EquipmentSlot.Headwear);
                    slots.Add(EquipmentSlot.Earpiece);
                    slots.Add(EquipmentSlot.FaceCover);
                }
                if (BodyPartHasBleed(player, EBodyPart.Stomach) || BodyPartHasBleed(player, EBodyPart.Chest))
                {
                    slots.Add(EquipmentSlot.TacticalVest);
                    slots.Add(EquipmentSlot.Backpack);
                    slots.Add(EquipmentSlot.ArmorVest);
                }
;
                if (slots.Count < 1)
                {
                    return;
                }

                foreach (EquipmentSlot slot in slots)
                {
                    Item item = equipment.GetSlot(slot).ContainedItem;
                    if (item != null)
                    {
                        gear.Add(item);
                    }
                }

                if (gear.Count < 1)
                {
                    return;
                }

                foreach (Item item in gear)
                {
                    if (inventoryController.CanThrow(item))
                    {
                        inventoryController.TryThrowItem(item, null, false);
                    }
                }
            }
        }

        public bool MouthIsBlocked(Item head, Item face, InventoryEquipment equipment)
        {
            bool faceGearBlocksMouth = false;
            bool headGearBlocksMouth = false;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            IEnumerable<Item> nestedItems = headwear != null ? headwear.GetAllItemsFromCollection().OfType<Item>() : null;

            if (nestedItems != null)
            {
                foreach (Item item in nestedItems)
                {
                    FaceShieldComponent fs = item.GetItemComponent<FaceShieldComponent>();
                    if (GearStats.BlocksMouth(item) && fs == null)
                    {
                        return true;
                    }
                }
            }

            if (head != null)
            {
                faceGearBlocksMouth = GearStats.BlocksMouth(head);
            }
            if (face != null)
            {
                headGearBlocksMouth = GearStats.BlocksMouth(face);
            }

            return faceGearBlocksMouth || headGearBlocksMouth || IsCoughingInGas;
        }

        public bool BodyPartHasBleed(Player player, EBodyPart part)
        {
            IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

            Type heavyBleedType;
            Type lightBleedType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);

            bool hasHeavyBleed = heavyBleedType != null && effects.Any(e => e.Type == heavyBleedType);
            bool hasLightBleed = lightBleedType != null && effects.Any(e => e.Type == lightBleedType);

            if (hasHeavyBleed || hasLightBleed)
            {
                return true;
            }
            return false;
        }

        public IEnumerable<IEffect> GetInjuriesOnBodyPart(Player player, EBodyPart part, ref bool hasHeavyBleed, ref bool hasLightBleed, ref bool hasFracture)
        {
            IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);

            Type heavyBleedType;
            Type lightBleedType;
            Type fractureType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);
            MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

            hasHeavyBleed = heavyBleedType != null && effects.Any(e => e.Type == heavyBleedType);
            hasLightBleed = lightBleedType != null && effects.Any(e => e.Type == lightBleedType);
            hasFracture = fractureType != null && effects.Any(e => e.Type == fractureType);

            return effects;
        }

        public void GetBodyPartType(EBodyPart part, ref bool isNotLimb, ref bool isHead, ref bool isBody)
        {
            isHead = part == EBodyPart.Head;
            isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;
            isNotLimb = part == EBodyPart.Chest || part == EBodyPart.Stomach || part == EBodyPart.Head;
        }

        public void CanConsume(Player player, Item item, ref bool canUse)
        {
            InventoryEquipment equipment = player.Equipment;
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On) && GearStats.BlocksMouth(fsComponent.Item);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            bool mouthBlocked = MouthIsBlocked(head, face, equipment);

            //will have to make mask exception for moustache, balaclava etc.
            if (fsIsON || nvgIsOn || mouthBlocked)
            {
                if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.Food), EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            ConsumeAlcohol(player, item);
            CheckIfReducesHazardInRaid(item, player, false);
            PlayerState.BlockFSWhileConsooming = true;
        }

        private void ConsumeAlcohol(Player player, Item item)
        {
            if (MedProperties.MedType(item) == "alcohol")
            {
                AddPainkillerEffect(player, item);
            }
        }

        public void DoPassiveRegen(float tickRate, EBodyPart bodyPart, Player player, int delay, float hpToRestore, EDamageType damageType)
        {
            if (!HasCustomEffectOfType(typeof(TourniquetEffect), bodyPart) && player.HealthController.GetBodyPartHealth(bodyPart).Current > 0f)
            {
                HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, bodyPart, player, delay, hpToRestore, damageType, this);
                AddCustomEffect(regenEffect, false);
            }
        }

        public void RestoreHPArossBody(Player player, float hpToRestore, int delay, EDamageType damageType, float tickRate)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / BodyPartsArr.Length);

            foreach (EBodyPart part in BodyPartsArr)
            {
                if (!HasCustomEffectOfType(typeof(TourniquetEffect), part) && player.HealthController.GetBodyPartHealth(part).Current > 0f)
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType, this);
                    AddCustomEffect(regenEffect, false);
                }
            }
        }

        public void TrnqtRestoreHPArossBody(Player player, float hpToRestore, int delay, EBodyPart bodyPart, EDamageType damageType, float vitalitySkill)
        {
            hpToRestore = Mathf.RoundToInt((hpToRestore) / (BodyPartsArr.Length - 1));
            float tickRate = (float)Math.Round(0.85f * (1f + vitalitySkill), 2);

            foreach (EBodyPart part in BodyPartsArr)
            {
                if (part != bodyPart && !HasCustomEffectOfType(typeof(TourniquetEffect), part) && player.HealthController.GetBodyPartHealth(part).Current > 0f)
                {
                    HealthRegenEffect regenEffect = new HealthRegenEffect(tickRate, null, part, player, delay, hpToRestore, damageType, this);
                    AddCustomEffect(regenEffect, false);
                }
            }
        }

        private void HandleHeavyBleedHeal(string medType, MedsItemClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;

            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayMessageNotification("Heavy Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(_baseMaxHPRestore * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DmgeTracker.TotalHeavyBleedDamage, maxHpToRestore);

            if ((hBleedHealType == "combo" || hBleedHealType == "trnqt") && !isNotLimb)
            {
                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, delay, this);
                AddCustomEffect(trnqt, false);

                if (DmgeTracker.TotalHeavyBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, delay, bodyPart, EDamageType.HeavyBleeding, vitalitySkill);
                }
            }
            else if (DmgeTracker.TotalHeavyBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, delay, EDamageType.HeavyBleeding, regenTickRate);
            }
            DmgeTracker.TotalHeavyBleedDamage = Mathf.Max(DmgeTracker.TotalHeavyBleedDamage - hpToRestore, 0f);
        }

        private void HandleLightBleedHeal(string medType, MedsItemClass meds, EBodyPart bodyPart, Player player, bool isNotLimb, float vitalitySkill, float regenTickRate)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;

            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayMessageNotification("Light Bleed On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);

            float trnqtTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f - vitalitySkill), 2);
            float maxHpToRestore = Mathf.Round(_baseMaxHPRestore * (1f + vitalitySkill));
            float hpToRestore = Mathf.Min(DmgeTracker.TotalLightBleedDamage, maxHpToRestore);

            if (medType == "trnqt" && !isNotLimb)
            {
                TourniquetEffect trnqt = new TourniquetEffect(trnqtTickRate, null, bodyPart, player, delay, this);
                AddCustomEffect(trnqt, false);

                if (DmgeTracker.TotalLightBleedDamage > 0f)
                {
                    TrnqtRestoreHPArossBody(player, hpToRestore, delay, bodyPart, EDamageType.LightBleeding, vitalitySkill);
                }
            }
            else if (DmgeTracker.TotalLightBleedDamage > 0f)
            {
                RestoreHPArossBody(player, hpToRestore, delay, EDamageType.LightBleeding, regenTickRate);
            }
            DmgeTracker.TotalLightBleedDamage = Mathf.Max(DmgeTracker.TotalLightBleedDamage - hpToRestore, 0f);
        }

        private void HandleSurgery(string medType, MedsItemClass meds, EBodyPart bodyPart, Player player, float surgerySkill)
        {
            int delay = (int)meds.HealthEffectsComponent.UseTime;
            float regenLimitFactor = 0.5f * (1f + surgerySkill);
            float surgTickRate = (float)Math.Round(MedProperties.HpPerTick(meds) * (1f + surgerySkill), 2);
            SurgeryEffect surg = new SurgeryEffect(surgTickRate, null, bodyPart, player, delay, regenLimitFactor, this);
            AddCustomEffect(surg, false);
        }

        private void HandleSplint(MedsItemClass meds, float regenTickRate, EBodyPart bodyPart, Player player)
        {
            if (player.HealthController.GetBodyPartHealth(bodyPart).Current <= 0f) return;
            if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayMessageNotification("Fracture On " + bodyPart + " Healed, Restoring HP.", EFT.Communications.ENotificationDurationType.Long);
            int delay = (int)meds.HealthEffectsComponent.UseTime;
            HealthRegenEffect regenEffect = new HealthRegenEffect(regenTickRate, null, bodyPart, player, delay, 12f, EDamageType.Impact, this);
            AddCustomEffect(regenEffect, false);
        }

        public void HandleHealthEffects(string medType, MedsItemClass meds, EBodyPart bodyPart, Player player, string hBleedHealType, bool canHealHBleed, bool canHealLBleed, bool canHealFract)
        {
            float vitalitySkill = player.Skills.VitalityBuffBleedChanceRed.Value;
            float surgerySkill = player.Skills.SurgeryReducePenalty.Value;
            float regenTickRate = (float)Math.Round(0.4f * (1f + vitalitySkill), 2);

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = GetInjuriesOnBodyPart(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            bool isHead = false;
            bool isBody = false;
            bool isNotLimb = false;

            GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            if (PluginConfig.EnableTrnqtEffect.Value && hasHeavyBleed && canHealHBleed)
            {
                HandleHeavyBleedHeal(medType, meds, bodyPart, player, hBleedHealType, isNotLimb, vitalitySkill, regenTickRate);
            }

            if (medType == "surg")
            {
                HandleSurgery(medType, meds, bodyPart, player, surgerySkill);
            }

            if (canHealLBleed && hasLightBleed && !hasHeavyBleed && (medType == "trnqt" && !isNotLimb || medType != "trnqt"))
            {
                HandleLightBleedHeal(medType, meds, bodyPart, player, isNotLimb, vitalitySkill, regenTickRate);
            }

            if (canHealFract && hasFracture && (medType == "splint" || (medType == "medkit" && !hasHeavyBleed && !hasLightBleed)))
            {
                HandleSplint(meds, regenTickRate, bodyPart, player);
            }
        }

        private void AddPainkillerEffect(Player player, Item item)
        {
            int duration = (int)(MedProperties.PainKillerDuration(item) * (1f + PlayerState.ImmuneSkillWeak));
            int delay = (int)Mathf.Round(MedProperties.Delay(item) * (1f - player.Skills.HealthEnergy.Value));
            float tunnelVisionStr = MedProperties.TunnelVisionStrength(item) * (1f - PlayerState.ImmuneSkillWeak);
            float painKillStr = MedProperties.Strength(item);

            PainKillerEffect painKillerEffect = new PainKillerEffect(duration, player, delay, tunnelVisionStr, painKillStr, this);
            Plugin.RealHealthController.AddCustomEffect(painKillerEffect, true);
        }

        public void CheckIfReducesHazardInStash(Item item, bool isMed, HealthControllerClass hc)
        {
            if (HazardTracker.TotalToxicity <= 0) return;

            MedUiString details = null;
            if (isMed && item as MedsItemClass != null)
            {
                MedsItemClass med = item as MedsItemClass;
                details = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? med.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
            }
            if (!isMed && item as FoodItemClass != null)
            {
                FoodItemClass food = item as FoodItemClass;
                details = food.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? food.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
            }

            if (details != null)
            {
                float strength = details.FadeOut;
                int duration = (int)details.Duration;
                HazardTracker.TotalToxicity -= strength * duration;
                HazardTracker.UpdateHazardValues(ProfileData.PMCProfileId);
                HazardTracker.SaveHazardValues();

                //doesn't work :(
                /*       if (isMed)
                       {
                           var med = item as MedsItemClass;
                           med.MedKitComponent.HpResource -= 1f;
                           med.MedKitComponent.Item.RaiseRefreshEvent(false, true);
                       }*/
            }

        }

        public void CheckIfReducesHazardInRaid(Item item, Player player, bool isMed)
        {
            MedUiString detoxDetails = null;
            MedUiString deradDetails = null;
            if (isMed && item as MedsItemClass != null)
            {
                MedsItemClass med = item as MedsItemClass;
                detoxDetails = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? med.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
                deradDetails = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.RadExposure) ? med.HealthEffectsComponent.DamageEffects[EDamageEffectType.RadExposure] : null;
            }
            if (!isMed && item as FoodItemClass != null)
            {
                FoodItemClass food = item as FoodItemClass;
                detoxDetails = food.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Intoxication) ? food.HealthEffectsComponent.DamageEffects[EDamageEffectType.Intoxication] : null;
                deradDetails = food.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.RadExposure) ? food.HealthEffectsComponent.DamageEffects[EDamageEffectType.RadExposure] : null;
            }

            if (detoxDetails != null)
            {
                float strength = -detoxDetails.FadeOut;
                int delay = (int)detoxDetails.Delay;
                int duration = (int)detoxDetails.Duration;

                DetoxificationEffect detox = new DetoxificationEffect(player, duration, delay, this, strength);
                Plugin.RealHealthController.AddCustomEffect(detox, true);
            }
            if (deradDetails != null)
            {
                float strength = -deradDetails.FadeOut;
                int delay = (int)deradDetails.Delay;
                int duration = (int)deradDetails.Duration;

                RadationTreatmentEffect derad = new RadationTreatmentEffect(player, duration, delay, this, strength);
                Plugin.RealHealthController.AddCustomEffect(derad, true);
            }
        }

        private string GetHealBlockMessage(EHealBlockType blockType)
        {
            switch (blockType)
            {
                case EHealBlockType.Trnqt:
                    return "Tourniquets Can Only Stop Bleeds On Limbs";
                case EHealBlockType.Splint:
                    return "Splints Can Only Fix Fractures On Limbs";
                case EHealBlockType.GearCommon:
                    return "Gear Is Blocking Wound";
                case EHealBlockType.GearSpecific:
                    return " Has Gear On, Remove Gear First To Be Able To Heal";
                case EHealBlockType.Surgery:
                    return "No Suitable Bodypart Was Found For Surgery Kit";
                case EHealBlockType.Pills:
                    return "Can't Take Pills, Mouth Is Blocked By Active Faceshield/NVGs Or Mask";
                case EHealBlockType.Food:
                    return "Can't Eat/Drink, Mouth Is Blocked By Active Faceshield/NVGs Or Mask";
                case EHealBlockType.Unknown:
                    return "No Suitable Bodypart Was Found For Healing";
                default:
                    return "No Suitable Bodypart Was Found For Healing";
            }
        }

        public void CanUseMedItemCommon(MedsItemClass meds, Player player, ref EBodyPart bodyPart, ref bool shouldAllowHeal)
        {
            CheckIfReducesHazardInRaid(meds, player, true); //the types of item that can reduce toxicity and radiation can't be blocked so should be fine

            if (meds.Template.ParentId == "5448f3a64bdc2d60728b456a")
            {
                int duration = (int)meds.HealthEffectsComponent.BuffSettings[0].Duration * 2;
                int delay = Mathf.Max((int)meds.HealthEffectsComponent.BuffSettings[0].Delay, 5);
                EStimType stimType = Plugin.RealHealthController.GetStimType(meds.Template._id);

                StimEffectShell stimEffect = new StimEffectShell(player, duration, delay, stimType, this);
                Plugin.RealHealthController.AddCustomEffect(stimEffect, true);

                shouldAllowHeal = true;
                return;
            }

            string medType = MedProperties.MedType(meds);

            if (MedProperties.CanBeUsedInRaid(meds) == false)
            {
                if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("This Item Can Not Be Used In Raid", EFT.Communications.ENotificationDurationType.Long);
                shouldAllowHeal = false;
                return;
            }

            float medHPRes = meds.MedKitComponent.HpResource;
            string hBleedHealType = MedProperties.HBleedHealType(meds);

            bool canHealLBleed =
                meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding) &&
                meds.HealthEffectsComponent.DamageEffects[EDamageEffectType.LightBleeding].Cost + 1 <= meds.MedKitComponent.HpResource;
            bool canHealHBleed =
                meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding) &&
                meds.HealthEffectsComponent.DamageEffects[EDamageEffectType.HeavyBleeding].Cost + 1 <= meds.MedKitComponent.HpResource;
            bool canHealFract =
                meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) &&
                meds.HealthEffectsComponent.DamageEffects[EDamageEffectType.Fracture].Cost + 1 <= meds.MedKitComponent.HpResource;

            /*          bool canHealFract = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");
                      bool canHealLBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding);
                      bool canHealHBleed = meds.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding) && ((medType == "medkit" && medHPRes >= 3) || medType != "medkit");
          */

            if (bodyPart == EBodyPart.Common)
            {
                int gearBlockedHealCount = 0;
                InventoryEquipment equipment = (InventoryEquipment)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
                Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
                Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
                Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
                Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
                Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
                Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
                Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

                bool mouthBlocked = Plugin.RealHealthController.MouthIsBlocked(head, face, equipment);
                bool hasBodyGear = vest != null || tacrig != null; //|| bag != null
                bool hasHeadGear = head != null || ears != null || face != null;

                EHealBlockType blockType = EHealBlockType.Unknown;

                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                if (PluginConfig.GearBlocksHeal.Value && medType.Contains("pills") && (mouthBlocked || fsIsON || nvgIsOn))
                {
                    if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.Pills), EFT.Communications.ENotificationDurationType.Long);
                    shouldAllowHeal = false;
                    return;
                }
                if (medType.Contains("pain"))
                {
                    AddPainkillerEffect(player, meds);
                    shouldAllowHeal = true;
                    return;
                }
                if (medType.Contains("pills") || medType.Contains("drug"))
                {
                    shouldAllowHeal = true;
                    return;
                }

                Type heavyBleedType;
                Type lightBleedType;
                Type fractureType;
                MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
                MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);
                MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

                if (medType == "surg")
                {
                    bool isHead = false;
                    bool isBody = false;
                    bool isNotLimb = false;

                    bodyPart = Plugin.RealHealthController.BodyPartsArr
                        .Where(b => player.ActiveHealthController.GetBodyPartHealth(b).Current / player.ActiveHealthController.GetBodyPartHealth(b).Maximum < 1)
                        .OrderBy(b => player.ActiveHealthController.GetBodyPartHealth(b).Current / player.ActiveHealthController.GetBodyPartHealth(b).Maximum).FirstOrDefault();

                    //IDE is a liar, it can be null
#pragma warning disable CS0472 
                    if (bodyPart == null) bodyPart = EBodyPart.Common;
#pragma warning restore CS0472 

                    if (bodyPart == EBodyPart.Common)
                    {
                        if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.Surgery), EFT.Communications.ENotificationDurationType.Long);
                        shouldAllowHeal = false;
                        return;
                    }

                    Plugin.RealHealthController.GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

                    if (PluginConfig.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear)))
                    {
                        if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.GearCommon), EFT.Communications.ENotificationDurationType.Long);
                    }
                    Plugin.RealHealthController.HandleHealthEffects(medType, meds, bodyPart, player, hBleedHealType, canHealHBleed, canHealLBleed, canHealFract);
                    return;
                }
                else
                {
                    foreach (EBodyPart part in Plugin.RealHealthController.BodyPartsArr)
                    {
                        bool isHead = false;
                        bool isBody = false;
                        bool isNotLimb = false;

                        Plugin.RealHealthController.GetBodyPartType(part, ref isNotLimb, ref isHead, ref isBody);

                        bool hasHeavyBleed = false;
                        bool hasLightBleed = false;
                        bool hasFracture = false;

                        IEnumerable<IEffect> effects = Plugin.RealHealthController.GetInjuriesOnBodyPart(player, part, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

                        float currentHp = player.ActiveHealthController.GetBodyPartHealth(part).Current;
                        float maxHp = player.ActiveHealthController.GetBodyPartHealth(part).Maximum;


                        foreach (IEffect effect in effects)
                        {
                            if (PluginConfig.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear)))
                            {
                                gearBlockedHealCount++;
                                continue;
                            }

                            if (canHealHBleed && effect.Type == heavyBleedType)
                            {
                                if (!isNotLimb)
                                {
                                    bodyPart = part;
                                    break;
                                }
                                if ((isBody || isHead) && hBleedHealType == "trnqt")
                                {
                                    blockType = EHealBlockType.Trnqt;
                                    continue;
                                }
                                if ((isBody || isHead) && (hBleedHealType == "clot" || hBleedHealType == "combo" || hBleedHealType == "surg"))
                                {
                                    bodyPart = part;
                                    break;
                                }

                                bodyPart = part;
                                break;
                            }
                            if (canHealLBleed && effect.Type == lightBleedType)
                            {
                                if (!isNotLimb)
                                {
                                    bodyPart = part;
                                    break;
                                }
                                if ((isBody || isHead) && hBleedHealType == "trnqt")
                                {
                                    blockType = EHealBlockType.Trnqt;
                                    continue;
                                }
                                if ((isBody || isHead) && hasHeavyBleed)
                                {
                                    continue;
                                }

                                bodyPart = part;
                                break;
                            }
                            if (canHealFract && effect.Type == fractureType)
                            {
                                if (!isNotLimb)
                                {
                                    bodyPart = part;
                                    break;
                                }
                                if (isNotLimb)
                                {
                                    blockType = EHealBlockType.Splint;
                                    continue;
                                }

                                bodyPart = part;
                                break;
                            }
                        }

                        if (bodyPart != EBodyPart.Common)
                        {
                            break;
                        }
                    }
                }

                if (bodyPart == EBodyPart.Common)
                {
                    if (medType == "vas")
                    {
                        shouldAllowHeal = true;
                        return;
                    }

                    if (blockType == EHealBlockType.Unknown && gearBlockedHealCount > 0) blockType = EHealBlockType.GearCommon;
                    if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(blockType), EFT.Communications.ENotificationDurationType.Long);

                    shouldAllowHeal = false;
                    return;
                }
            }

            //determine if any effects should be applied based on what is being healed
            if (bodyPart != EBodyPart.Common)
            {
                Plugin.RealHealthController.HandleHealthEffects(medType, meds, bodyPart, player, hBleedHealType, canHealHBleed, canHealLBleed, canHealFract);
            }
        }


        public void CanUseMedItem(Player player, EBodyPart bodyPart, Item item, ref bool canUse)
        {
            if (item.Template.Parent._id == "5448f3a64bdc2d60728b456a" || MedProperties.MedType(item).Contains("drug"))
            {
                return;
            }

            if (MedProperties.CanBeUsedInRaid(item) == false)
            {
                if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification("This Item Can Not Be Used In Raid", EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            MedsItemClass med = item as MedsItemClass;
            InventoryEquipment equipment = player.Equipment;

            Item head = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            Item ears = equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
            Item glasses = equipment.GetSlot(EquipmentSlot.Eyewear).ContainedItem;
            Item face = equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem;
            Item vest = equipment.GetSlot(EquipmentSlot.ArmorVest).ContainedItem;
            Item tacrig = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem;
            Item bag = equipment.GetSlot(EquipmentSlot.Backpack).ContainedItem;

            bool mouthBlocked = MouthIsBlocked(head, face, equipment);

            bool hasHeadGear = head != null || ears != null || face != null;
            bool hasBodyGear = vest != null || tacrig != null; // bag != null

            bool isHead = false;
            bool isBody = false;
            bool isNotLimb = false;

            string medType = MedProperties.MedType(item);

            GetBodyPartType(bodyPart, ref isNotLimb, ref isHead, ref isBody);

            FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
            NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
            bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
            bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

            float medHPRes = med.MedKitComponent.HpResource;

            if (medType == "vas")
            {
                return;
            }

            if (medType.Contains("pills"))
            {
                if (PluginConfig.GearBlocksEat.Value && (mouthBlocked || fsIsON || nvgIsOn))
                {
                    if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.Pills), EFT.Communications.ENotificationDurationType.Long);
                    canUse = false;
                    return;
                }
                return;
            }


            if (PluginConfig.GearBlocksHeal.Value && ((isBody && hasBodyGear) || (isHead && hasHeadGear)))
            {
                if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(bodyPart + GetHealBlockMessage(EHealBlockType.GearSpecific), EFT.Communications.ENotificationDurationType.Long);

                canUse = false;
                return;
            }

            bool hasHeavyBleed = false;
            bool hasLightBleed = false;
            bool hasFracture = false;

            IEnumerable<IEffect> effects = GetInjuriesOnBodyPart(player, bodyPart, ref hasHeavyBleed, ref hasLightBleed, ref hasFracture);

            bool canHealLightBleed = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding) && med.HealthEffectsComponent.DamageEffects[EDamageEffectType.LightBleeding].Cost + 1 <= med.MedKitComponent.HpResource && hasLightBleed;
            bool canHealHeavyBleed = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding) && med.HealthEffectsComponent.DamageEffects[EDamageEffectType.HeavyBleeding].Cost + 1 <= med.MedKitComponent.HpResource && hasHeavyBleed;
            bool canHealFracture = med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && med.HealthEffectsComponent.DamageEffects[EDamageEffectType.Fracture].Cost + 1 <= med.MedKitComponent.HpResource && hasFracture;
            bool partHasTreatableInjury = canHealLightBleed || canHealHeavyBleed || canHealFracture;


            if (medType == "medkit" && !partHasTreatableInjury)
            {
                canUse = false;
                return;
            }

            if (isNotLimb && MedProperties.HBleedHealType(item) == "trnqt")
            {
                if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.Trnqt), EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            if (medType == "splint" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && isNotLimb)
            {
                if (PluginConfig.EnableMedNotes.Value) NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.Splint), EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            if (medType == "medkit" && med.HealthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.Fracture) && hasFracture && isNotLimb && !hasHeavyBleed && !hasLightBleed)
            {
                NotificationManagerClass.DisplayWarningNotification(GetHealBlockMessage(EHealBlockType.Splint), EFT.Communications.ENotificationDurationType.Long);
                canUse = false;
                return;
            }

            return;
        }

        public void DoubleBleedCheck(Player player)
        {
            IEnumerable<IEffect> commonEffects = player.ActiveHealthController.GetAllActiveEffects(EBodyPart.Common);

            Type heavyBleedType;
            Type lightBleedType;
            MedProperties.EffectTypes.TryGetValue("HeavyBleeding", out heavyBleedType);
            MedProperties.EffectTypes.TryGetValue("LightBleeding", out lightBleedType);

            bool hasCommonHeavyBleed = heavyBleedType != null && commonEffects.Any(e => e.Type == heavyBleedType);
            bool hasCommonLightBleed = lightBleedType != null && commonEffects.Any(e => e.Type == lightBleedType);

            if (hasCommonHeavyBleed && hasCommonLightBleed)
            {
                //IReadOnlyList<GClass2747> effectsList = new GClass2747<EffectClass>.GetAllEffects();
                IEnumerable<IEffect> effectsList = player.ActiveHealthController.GetAllEffects();

                foreach (IEffect effect in effectsList)
                {
                    Type effectType = effect.Type;
                    EBodyPart effectPart = effect.BodyPart;

                    IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(effectPart);
                    bool hasHeavyBleed = heavyBleedType != null && effects.Any(e => e.Type == heavyBleedType);
                    bool hasLightBleed = lightBleedType != null && effects.Any(e => e.Type == lightBleedType);

                    if (hasHeavyBleed && hasLightBleed && effectType == lightBleedType)
                    {
                        effect.ForceResidue();
                    }

                }
            }
        }

        private void ResetHealhPenalties()
        {
            PlayerState.AimMoveSpeedInjuryMulti = 1;
            PlayerState.ADSInjuryMulti = 1;
            PlayerState.StanceInjuryMulti = 1;
            PlayerState.ReloadInjuryMulti = 1;
            PlayerState.HealthSprintSpeedFactor = 1;
            PlayerState.HealthSprintAccelFactor = 1;
            PlayerState.HealthWalkSpeedFactor = 1;
            PlayerState.HealthStamRegenFactor = 1;
            PlayerState.ErgoDeltaInjuryMulti = 1;
            PlayerState.RecoilInjuryMulti = 1;
        }

        private void DoResourceDrain(ActiveHealthController hc, float dt)
        {
            hc.ChangeEnergy(-ResourcePerTick * dt * PluginConfig.EnergyRateMulti.Value);
            hc.ChangeHydration(-ResourcePerTick * dt * PluginConfig.HydrationRateMulti.Value);
        }

        public void PlayerInjuryStateCheck(Player player)
        {
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);

            bool hasTremor = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.Tremor);
            float tremorFactor = hasTremor ? 0.95f : 1f;

            float aimMoveSpeedMulti = 1f;
            float ergoDeltaInjuryMulti = 1f;
            float adsInjuryMulti = 1f;
            float stanceInjuryMulti = 1f;
            float reloadInjuryMulti = 1f;
            float recoilInjuryMulti = 1f;
            float sprintSpeedInjuryMulti = 1f;
            float sprintAccelInjuryMulti = 1f;
            float walkSpeedInjuryMulti = 1f;
            float stamRegenInjuryMulti = 1f;
            float resourceRateInjuryMulti = 0f;

            float drugFactor = _hasOverdosedStim ? 90f + PainReliefStrength : PainReliefStrength;
            float resourcePainReliefFactor = drugFactor / 200f;

            float currentEnergy = player.ActiveHealthController.Energy.Current;
            float maxEnergy = player.ActiveHealthController.Energy.Maximum;
            float percentEnergy = currentEnergy / maxEnergy;

            float currentHydro = player.ActiveHealthController.Hydration.Current;
            float maxHydro = player.ActiveHealthController.Hydration.Maximum;
            float percentHydro = currentHydro / maxHydro;

            _percentReources = Mathf.Clamp01((percentEnergy + percentHydro) / 2f);

            IsPoisoned = HasBaseEFTEffect(player, "LethalToxin");

            float totalMaxHp = 0f;
            float totalCurrentHp = 0f;

            PainStrength = 0f;

            Type fractureType;
            MedProperties.EffectTypes.TryGetValue("BrokenBone", out fractureType);

            foreach (EBodyPart part in BodyPartsArr)
            {
                IEnumerable<IEffect> effects = player.ActiveHealthController.GetAllActiveEffects(part);
                bool hasFracture = fractureType != null && effects.Any(e => e.Type == fractureType);

                if (hasFracture)
                {
                    PainStrength += 15f * (1f - PlayerState.StressResistanceFactor);
                }

                bool isLeftArm = part == EBodyPart.LeftArm;
                bool isRightArm = part == EBodyPart.RightArm;
                bool isArm = isLeftArm || isRightArm;
                bool isLeg = part == EBodyPart.LeftLeg || part == EBodyPart.RightLeg;
                bool isBody = part == EBodyPart.Chest || part == EBodyPart.Stomach;

                float currentHp = player.ActiveHealthController.GetBodyPartHealth(part).Current;
                float maxHp = player.ActiveHealthController.GetBodyPartHealth(part).Maximum;
                totalMaxHp += maxHp;
                totalCurrentHp += currentHp;

                float percentHp = currentHp / maxHp;
                float percentHpStamRegen = 1f - ((1f - percentHp) / (isBody ? 10f : 5f));
                float percentHpWalk = 1f - ((1f - percentHp) / (isBody ? 15f : 7.5f));
                float percentHpSprint = 1f - ((1f - percentHp) / (isBody ? 8f : 4f));
                float percentHpAimMove = 1f - ((1f - percentHp) / (isArm ? 20f : 14f));
                float percentHpADS = 1f - ((1f - percentHp) / (isRightArm ? 1f : 2f));
                float percentHpStance = 1f - ((1f - percentHp) / (isRightArm ? 1.5f : 3f));
                float percentHpReload = 1f - ((1f - percentHp) / (isLeftArm ? 2f : isRightArm ? 3f : 4f));
                float percentHpRecoil = 1f - ((1f - percentHp) / (isLeftArm ? 10f : 20f));

                if (currentHp <= 0f) PainStrength += 20f * (1f - PlayerState.StressResistanceFactor);
                else if (percentHp <= 0.5f) PainStrength += 5f * (1f - PlayerState.StressResistanceFactor);

                if (isLeg || isBody)
                {
                    aimMoveSpeedMulti *= percentHpAimMove;
                    sprintSpeedInjuryMulti *= percentHpSprint;
                    sprintAccelInjuryMulti *= percentHp;
                    walkSpeedInjuryMulti *= percentHpWalk;
                    stamRegenInjuryMulti *= percentHpStamRegen;
                }

                if (isArm)
                {
                    bool isArmRuined = (currentHp <= 0f || hasFracture) && !HasBaseEFTEffect(player, "PainKiller");
                    if (isLeftArm) _leftArmRuined = isArmRuined;
                    if (isRightArm) _rightArmRuined = isArmRuined;

                    float armFractureFactor = isLeftArm && hasFracture ? 0.75f : isRightArm && hasFracture ? 0.85f : 1f;

                    aimMoveSpeedMulti *= percentHpAimMove * armFractureFactor;
                    adsInjuryMulti *= percentHpADS * armFractureFactor;
                    stanceInjuryMulti *= percentHpStance * armFractureFactor;
                    reloadInjuryMulti *= percentHpReload * armFractureFactor;
                    ergoDeltaInjuryMulti *= (1f + (1f - percentHp)) * armFractureFactor;
                    recoilInjuryMulti *= (1f + (1f - percentHpRecoil)) * armFractureFactor;
                }
            }

            float totalHpPercent = totalCurrentHp / totalMaxHp;
            resourceRateInjuryMulti = Mathf.Clamp(1f - totalHpPercent, 0f, 1f) * 0.25f;
            float percentEnergyFactor = Mathf.Max(percentEnergy * 1.1f, 0.01f);

            float percentEnergySprint = 1f - ((1f - percentEnergyFactor) / 8f);
            float percentEnergyWalk = 1f - ((1f - percentEnergyFactor) / 12f);
            float percentEnergyAimMove = 1f - ((1f - percentEnergyFactor) / 20f);
            float percentEnergyADS = 1f - ((1f - percentEnergyFactor) / 5f);
            float percentEnergyStance = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyReload = 1f - ((1f - percentEnergyFactor) / 10f);
            float percentEnergyRecoil = 1f - ((1f - percentEnergyFactor) / 40f);
            float percentEnergyErgo = 1f - ((1f - percentEnergyFactor) / 2f);
            float percentEnergyStamRegen = 1f - ((1f - percentEnergyFactor) / 10f);

            float percentHydroLowerLimit = (1f - ((1f - percentHydro) / 4f));
            float percentHydroLimitRecoil = (1f + ((1f - percentHydro) / 20f));
            float percentHydroLimitErgo = (1f + ((1f - percentHydro) / 4f));

            float painFactor = Mathf.Max(PainStrength - PainReliefStrength, 0f);
            painFactor = _hasOverdosedStim ? 90f + painFactor : painFactor;
            float painKillerFactor = Mathf.Clamp(1f - (painFactor / 1000f), 0.85f, 1f);
            float painKillerFactorInverse = Mathf.Clamp(1f + (painFactor / 1000f), 1f, 1.15f);
            float skillFactor = (1f + (player.Skills.HealthEnergy.Value / 4));
            float skillFactorInverse = (1f - (player.Skills.HealthEnergy.Value / 4));

            //gas
            float coofFactor = IsCoughingInGas ? 100f : 0f;
            float toxicity = ((HazardTracker.TotalToxicity + coofFactor) * (1f - PlayerState.ImmuneSkillWeak)) / 225f;
            float toxicityFactor = 1f - toxicity;
            float toxicityInverse = 1f + toxicity;

            float radiation = (HazardTracker.TotalRadiation * (1f - PlayerState.ImmuneSkillWeak)) / 300f;
            float radiationFactor = 1f - radiation;
            float radiationInverse = 1f + radiation;

            //cultist toxin, food poisoning
            float poisonDebuffFactor = IsPoisoned ? 0.8f : 1f;
            float poisonDebuffFactorInverse = IsPoisoned ? 1.2f : 1f;

            float hazardFactor = toxicityFactor * radiationFactor * poisonDebuffFactor;
            float hazardFactorInverse = toxicityInverse * radiationInverse * poisonDebuffFactorInverse;

            PlayerState.AimMoveSpeedInjuryMulti = Mathf.Clamp(aimMoveSpeedMulti * percentEnergyAimMove * painKillerFactor * skillFactor * hazardFactor, 0.6f * percentHydroLowerLimit, 1f);
            PlayerState.ADSInjuryMulti = Mathf.Clamp(adsInjuryMulti * percentEnergyADS * painKillerFactor * skillFactor * hazardFactor, 0.3f * percentHydroLowerLimit, 1f);
            PlayerState.StanceInjuryMulti = Mathf.Clamp(stanceInjuryMulti * percentEnergyStance * painKillerFactor * skillFactor * hazardFactor, 0.65f * percentHydroLowerLimit, 1f);
            PlayerState.ReloadInjuryMulti = Mathf.Clamp(reloadInjuryMulti * percentEnergyReload * painKillerFactor * skillFactor * hazardFactor, 0.75f * percentHydroLowerLimit, 1f);
            PlayerState.HealthSprintSpeedFactor = Mathf.Clamp(sprintSpeedInjuryMulti * percentEnergySprint * painKillerFactor * skillFactor * hazardFactor, 0.4f * percentHydroLowerLimit, 1f);
            PlayerState.HealthSprintAccelFactor = Mathf.Clamp(sprintAccelInjuryMulti * percentEnergySprint * painKillerFactor * skillFactor * hazardFactor, 0.4f * percentHydroLowerLimit, 1f);
            PlayerState.HealthWalkSpeedFactor = Mathf.Clamp(walkSpeedInjuryMulti * percentEnergyWalk * painKillerFactor * skillFactor * hazardFactor, 0.6f * percentHydroLowerLimit, 1f);
            PlayerState.HealthStamRegenFactor = Mathf.Clamp(stamRegenInjuryMulti * percentEnergyStamRegen * painKillerFactor * skillFactor * hazardFactor, 0.5f * percentHydroLowerLimit, 1f);
            PlayerState.ErgoDeltaInjuryMulti = Mathf.Clamp(ergoDeltaInjuryMulti * (1f + (1f - percentEnergyErgo)) * painKillerFactorInverse * skillFactorInverse * hazardFactorInverse, 1f, 1.3f * percentHydroLimitErgo);
            PlayerState.RecoilInjuryMulti = Mathf.Clamp(recoilInjuryMulti * (1f + (1f - percentEnergyRecoil)) * painKillerFactorInverse * skillFactorInverse * hazardFactorInverse, 1f, 1.12f * percentHydroLimitRecoil);

            if (PluginConfig.ResourceRateChanges.Value)
            {
                if (!HasCustomEffectOfType(typeof(ResourceRateEffect), EBodyPart.Chest))
                {
                    ResourceRateEffect resEffect = new ResourceRateEffect(null, player, 0, this);
                    AddCustomEffect(resEffect, false);
                }

                float weight = PlayerState.TotalModifiedWeight * (1f - player.Skills.EnduranceBuffJumpCostRed.Value);
                float weightInverse = PlayerState.TotalModifiedWeight * (1f + player.Skills.EnduranceBuffJumpCostRed.Value);
                float playerWeightFactor = weightInverse >= 10f ? weight / 500f : 0f;
                float sprintMulti = PlayerState.IsSprinting ? 1.46f : 1f;
                float sprintFactor = PlayerState.IsSprinting ? 0.11f : 0f;
                float poisonSprintFactor = IsPoisoned ? Mathf.Max(1.5f * (1f - PlayerState.ImmuneSkillWeak), 1.1f) : 1f;
                float toxicityResourceFactor = 1f + (HazardTracker.TotalToxicity * (1f - PlayerState.ImmuneSkillWeak)) / 150f;
                float radiationResourceFactor = 1f + (HazardTracker.TotalRadiation * (1f - PlayerState.ImmuneSkillWeak)) / 190f;
                float totalResourceRate = (resourceRateInjuryMulti + resourcePainReliefFactor + sprintFactor + playerWeightFactor) * sprintMulti * toxicityResourceFactor * radiationResourceFactor * poisonSprintFactor * (1f - player.Skills.HealthEnergy.Value);
                ResourcePerTick = totalResourceRate;
            }
        }

        private void HazardZoneHealthTick(Player player)
        {
            if (!GameWorldController.GameStarted || PluginConfig.DevMode.Value) return;

            if (PlayerHazardBridge == null)
            {
                PlayerHazardBridge = player.gameObject.GetComponent<PlayerZoneBridge>();
            }

            if (!player.HealthController.IsAlive || player.HealthController.DamageCoeff <= 0f) return;

            if (GearController.HasGasMask && !PlayerHazardBridge.IsProtectedFromSafeZone && (PlayerHazardBridge.GasZoneCount > 0 || PlayerHazardBridge.RadZoneCount > 0 || ToxicItemCount > 0 || RadItemCount > 0 || GameWorldController.DoMapGasEvent || GameWorldController.DoMapRads))
            {
                GearController.UpdateFilterResource(player, PlayerHazardBridge);
                GearController.CalcGasMaskDuraFactor(player);
            }

            GasZoneTick(player);
            RadiationZoneTick(player);
            HazardEffectsTick(player);
            CoughController(player);
        }

        private void RadiationZoneTick(Player player)
        {
            bool isInRadZone = PlayerHazardBridge.RadZoneCount > 0 && !PlayerHazardBridge.IsProtectedFromSafeZone;
            bool IsBeingHazarded = (isInRadZone || GameWorldController.DoMapRads) && GearController.CurrentRadProtection <= 0f; //CurrentRadProtection accounts for both respirators, and gas masks and whether they have filters or dura is too low

            float sprintFactor = PlayerState.IsSprinting ? 1.1f : 1f;
            float radItemFactor = RadItemCount * RAD_ITEM_FACTOR;
            float mapRadFactor = GameWorldController.DoMapRads ? GameWorldController.CurrentMapRadStrength : 0f;
            float protectiveFactors = (1f - GearController.CurrentRadProtection) * (1f - PlayerState.ImmuneSkillWeak);

            float reductionRate = !IsBeingHazarded ? HazardTracker.RadTreatmentRate : 0f; //not sure if I should allow treatment while in radiation zone or not
            reductionRate = isInRadZone ? reductionRate * 0.5f : reductionRate;
            float baseRadRate = PlayerHazardBridge.TotalRadRate + radItemFactor + mapRadFactor;
            float radRate = ((PlayerHazardBridge.TotalRadRate * sprintFactor) + radItemFactor + mapRadFactor) * protectiveFactors;
            float totalRate = radRate + reductionRate;

            float speedBase = baseRadRate > 0f ? 10f : PlayerHazardBridge.IsProtectedFromSafeZone ? 8f : 6f;
            float speed = totalRate > 0f ? 10f : PlayerHazardBridge.IsProtectedFromSafeZone ? 6f : 2f;
            HazardTracker.BaseTotalRadiationRate = Mathf.MoveTowards(HazardTracker.BaseTotalRadiationRate, baseRadRate, speedBase * Time.deltaTime);
            HazardTracker.TotalRadiationRate = Mathf.MoveTowards(HazardTracker.TotalRadiationRate, totalRate, speed * Time.deltaTime);
     
            float lowerThreshold = isInRadZone && GearController.CurrentGasProtection < 1f ? HazardTracker.TotalRadiation : !isInRadZone && HazardTracker.TotalRadiation <= RAD_TREATMENT_THRESHOLD ? 0f : HazardTracker.GetNextLowestHazardLevel((int)HazardTracker.TotalRadiation);
            HazardTracker.TotalRadiation = Mathf.Clamp(HazardTracker.TotalRadiation + HazardTracker.TotalRadiationRate, lowerThreshold, 100f);
        }

        private void GasZoneTick(Player player)
        {
            bool isInGasZone = PlayerHazardBridge.GasZoneCount > 0 && !PlayerHazardBridge.IsProtectedFromSafeZone;
            bool zonePreventsHeal = isInGasZone && GearController.CurrentGasProtection <= 0f;
            bool isBeingHazarded = zonePreventsHeal || IsCoughingInGas;

            float sprintFactor = PlayerState.IsSprinting ? 1.5f : 1f;
            float toxicItemFactor = ToxicItemCount * TOXIC_ITEM_FACTOR;
            float mapGasEventFactor = GameWorldController.DoMapGasEvent ? GameWorldController.CurrentGasEventStrength : 0f;
            float protectiveFactors = (1f - GearController.CurrentGasProtection) * (1f - PlayerState.ImmuneSkillWeak);

            float passiveRegenRate = mapGasEventFactor <= 0f && ToxicItemCount <= 0 && !isInGasZone && HazardTracker.TotalToxicity > 0f ? BASE_TOX_RECOVERY_RATE * (2f - _percentReources) : 0f;
            float reductionRate = !isBeingHazarded ? HazardTracker.DetoxicationRate + passiveRegenRate : 0f;
            reductionRate = isInGasZone ? reductionRate * 0.5f : reductionRate;
            float baseGasRate = PlayerHazardBridge.TotalGasRate + toxicItemFactor + mapGasEventFactor;
            float gasRate = ((PlayerHazardBridge.TotalGasRate * sprintFactor) + toxicItemFactor + mapGasEventFactor) * protectiveFactors; //only actual zone rate should be affected by spritning
            float totalRate = gasRate + reductionRate;

            float speedBase = baseGasRate > 0f ? 11f : PlayerHazardBridge.IsProtectedFromSafeZone ? 7f : 5f;
            float speed = totalRate > 0f ? 11f : PlayerHazardBridge.IsProtectedFromSafeZone ? 6f : 1.5f;
            HazardTracker.BaseTotalToxicityRate = Mathf.MoveTowards(HazardTracker.BaseTotalToxicityRate, baseGasRate, speedBase * Time.deltaTime);
            HazardTracker.TotalToxicityRate = Mathf.MoveTowards(HazardTracker.TotalToxicityRate, totalRate, speed * Time.deltaTime);

            float lowerThreshold = isInGasZone && GearController.CurrentGasProtection < 1f ? HazardTracker.TotalToxicity : !isBeingHazarded && HazardTracker.DetoxicationRate < 0f ? 0f : HazardTracker.GetNextLowestHazardLevel((int)HazardTracker.TotalToxicity);
            HazardTracker.TotalToxicity = Mathf.Clamp(HazardTracker.TotalToxicity + HazardTracker.TotalToxicityRate, lowerThreshold, 100f);
        }

        private void HazardEffectsTick(Player player)
        {
            if (_hazardWaitTime > HAZARD_INTERVAL)
            {
                if (HazardTracker.TotalToxicity >= 10f || IsCoughingInGas)
                {
                    if ((HazardTracker.TotalToxicity >= TOXICITY_THRESHOLD || IsCoughingInGas) && !HasCustomEffectOfType(typeof(ToxicityEffect), EBodyPart.Chest))
                    {
                        ToxicityEffect toxicity = new ToxicityEffect(null, player, 0, this);
                        AddCustomEffect(toxicity, false);
                    }

                    float effectStrength = HazardTracker.TotalToxicity / 100f;
                    float coofFactor = IsCoughingInGas ? 1f : 0f;
                    //AddBasesEFTEffect(player, "TunnelVision", EBodyPart.Head, 1f, _hazardInterval, 5f, Mathf.Min(effectStrength * 2f, 1f)); maybe I'm relying too much on tunnel vision effect...
                    if (HazardTracker.TotalToxicity >= TOXICITY_THRESHOLD || IsCoughingInGas) AddToExistingBaseEFTEffect(player, "Contusion", EBodyPart.Head, 1f, HAZARD_INTERVAL, 5f, (effectStrength * 0.7f) + coofFactor);
                }

                if (HazardTracker.TotalRadiation >= 10f)
                {
                    if (HazardTracker.TotalRadiation >= RADIATION_THRESHOLD && !HasCustomEffectOfType(typeof(ToxicityEffect), EBodyPart.Chest))
                    {
                        RadiationEffect radiation = new RadiationEffect(null, player, 0, this);
                        AddCustomEffect(radiation, false);
                    }

                    float effectStrength = HazardTracker.TotalRadiation / 100f;
                    AddBasesEFTEffect(player, "TunnelVision", EBodyPart.Head, 1f, HAZARD_INTERVAL, 5f, Mathf.Min(effectStrength, 1f));
                }

                _hazardWaitTime = 0f;
            }
        }


        private void InventoryCheckerHelper(IEnumerable<Item> items, bool areQuestItems = false) 
        {
            if (areQuestItems && GearController.HasSafeContainer == true) return;

            foreach (var item in items)
            {
                if (item == null || item?.TemplateId == null) continue;
                if(item.TemplateId == GearController.SAFE_CONTAINER_ID) GearController.HasSafeContainer = true;

                bool isInSafeContainer = item?.Parent != null && item?.Parent?.Container != null && item.Parent.Container.ParentItem.TemplateId == GearController.SAFE_CONTAINER_ID;
                if (isInSafeContainer) continue;

                if (ToxicItems.Contains(item.TemplateId)) ToxicItemCount++;
                if (RadioactiveItems.Contains(item.TemplateId)) RadItemCount++;
            }
        }

        public void CheckInventoryForHazardousMaterials(Inventory inventory)
        {
            ToxicItemCount = 0;
            RadItemCount = 0;
            GearController.HasSafeContainer = false;

            IEnumerable<Item> questItems = inventory.QuestRaidItems.GetAllItems();
            IEnumerable<Item> inventoryItems = Enumerable.Empty<Item>(); 
            foreach (var inventorySlot in GearController.MainInventorySlots) 
            {
                var slot = inventory.GetItemsInSlots(new EquipmentSlot[] { inventorySlot }) ?? Enumerable.Empty<Item>();
                inventoryItems = inventoryItems.Concat(slot);    
            }

            InventoryCheckerHelper(inventoryItems);
            InventoryCheckerHelper(questItems, true); //needs tp be handled differently
        }

        private bool RollRadsCoughChance() 
        {
            int rnd = UnityEngine.Random.Range(0, 10);
            return rnd < HazardTracker.TotalRadiation / 10;
        }
        
        private void CoughController(Player player)
        {
            bool isBeingIrradiated = (HazardTracker.TotalRadiation >= RADIATION_THRESHOLD && !Plugin.RealHealthController.HasBaseEFTEffect(player, "PainKiller"));
            bool isBeingGassed = HazardTracker.TotalToxicity >= 30f;
            bool hasHazardification = isBeingGassed || isBeingIrradiated;
            bool isGettingHazarded = HazardTracker.TotalToxicityRate >= MIN_COUGH_THRESHOLD * (1f + PlayerState.ImmuneSkillStrong);

            if (player.HealthController.IsAlive && !(GearController.HasGasMaskWithFilter && GearController.GasMaskDurabilityFactor > 0f) && (hasHazardification || isGettingHazarded))
            {
                if (isBeingIrradiated && !isBeingGassed) 
                {
                    float timerFactor = (1f + PlayerState.ImmuneSkillStrong) * (1f - (HazardTracker.TotalRadiation / 200f));
                    int timer =  Mathf.RoundToInt(300f * timerFactor);
                    if (Time.time % timer < 1f) DoCoughingAudio = true;
               
                }
                else DoCoughingAudio = true;

                if (isGettingHazarded) IsCoughingInGas = true;
                else IsCoughingInGas = false;
            }
            else 
            {
                IsCoughingInGas = false;
                DoCoughingAudio = false;
            } 
        }
    }
}
 