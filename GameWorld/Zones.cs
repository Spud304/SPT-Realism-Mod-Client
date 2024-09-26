﻿using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;

namespace RealismMod
{
    public interface IHazardZone
    {
        EZoneType ZoneType { get; } 
        float ZoneStrengthModifier { get; set; }
        bool BlocksNav { get; set; }
        bool UsesDistanceFalloff { get; set; }
    }

    public class AmbientAudioPlayer : MonoBehaviour
    {
        public List<AudioClip> AudioClips = new List<AudioClip>();
        public Transform ParentTransform;
        public float MinTimeBetweenClips = 15f;
        public float MaxTimeBetweenClips = 90f;
        private AudioSource _audioSource;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();

            _audioSource = this.gameObject.AddComponent<AudioSource>();
            _audioSource.volume = 1f;
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.25f;
            _audioSource.maxDistance = 25f;
            _audioSource.maxDistance = 130f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;

            StartCoroutine(PlayRandomAudio());
        }
    
        private IEnumerator PlayRandomAudio()
        {
            while (true)
            {
                if (Utils.PlayerIsReady) 
                {
                    if (ParentTransform == null) 
                    {
                        ParentTransform = Utils.GetYourPlayer().gameObject.transform;
                    }

                    AudioClip selectedClip = AudioClips[Random.Range(0, AudioClips.Count)];

                    float randomDistance = UnityEngine.Random.Range(45f, 95f);
                    Vector3 randomPosition = ParentTransform.position + Random.onUnitSphere * randomDistance;
                    randomPosition.y = Mathf.Clamp(randomPosition.y, ParentTransform.position.y - 25f, ParentTransform.position.y + 25f);
                    transform.position = randomPosition;

                    if (PluginConfig.ZoneDebug.Value) 
                    {
                        GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        visualRepresentation.name = "AmbientAudioPlayerVisual";
                        visualRepresentation.transform.parent = transform;
                        visualRepresentation.transform.localScale = Vector3.one;
                        visualRepresentation.transform.position = randomPosition;
                        visualRepresentation.transform.rotation = ParentTransform.transform.rotation;
                        visualRepresentation.GetComponent<Renderer>().material.color = new UnityEngine.Color(1, 0, 0, 1);
                    }

                    _audioSource.clip = selectedClip;
                    _audioSource.Play();

                    yield return new WaitForSeconds(selectedClip.length);
                    float waitTime = Random.Range(MinTimeBetweenClips, MaxTimeBetweenClips);
                    yield return new WaitForSeconds(waitTime);
                }
            }
        }
    }

    public class QuestZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Quest;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for RadiationZone");
            }
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge;
                player.TryGetComponent<PlayerZoneBridge>(out hazardBridge);
                if (hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                    hazardBridge._Player = player;
                }
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge = _containedPlayers[player];
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.5f)
            {
                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerZoneBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue;
                    }
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                _tick = 0f;
            }
        }
    }

    public class GasZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Gas;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
        private Collider _zoneCollider;
        private bool _isSphere = false;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<Collider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for GasZone");
            }

            SphereCollider sphereCollider = _zoneCollider as SphereCollider;
            if (sphereCollider != null)
            {
                _isSphere = true;
                _maxDistance = sphereCollider.radius;
            }
            else 
            {
                BoxCollider box = _zoneCollider as BoxCollider;
                Vector3 boxSize = box.size;
                _maxDistance = boxSize.magnitude / 2f;
            }
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge;
                player.TryGetComponent<PlayerZoneBridge>(out hazardBridge);
                if (hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.GasZoneCount++;
                hazardBridge.GasRates.Add(this.name, 0f);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge = _containedPlayers[player];
                hazardBridge.GasZoneCount--;
                hazardBridge.GasRates.Remove(this.name);
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.001f)
            {
                var playersToRemove = new List<Player>();

                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerZoneBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        return;
                    }
                    float gasAmount = _isSphere ? CalculateGasStrengthSphere(player.gameObject.transform.position) : CalculateGasStrengthBox(player.gameObject.transform.position);
                    hazardBridge.GasRates[this.name] = Mathf.Max(gasAmount, 0f);
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                _tick = 0f;
            }
        }

        float CalculateGasStrengthBox(Vector3 playerPosition)
        {
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateGasStrengthSphere(Vector3 playerPosition)
        {
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distanceToCenter = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float radius = _zoneCollider.bounds.extents.magnitude;
            float effectiveDistance = Mathf.Max(0, distanceToCenter - radius); 
            float invertedDistance = _maxDistance - effectiveDistance; 
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); 
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }
    }

    public class RadiationZone : TriggerWithId, IHazardZone
    {
        public EZoneType ZoneType { get; } = EZoneType.Radiation;
        public float ZoneStrengthModifier { get; set; } = 1f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
        private Collider _zoneCollider;
        private bool _isSphere = false;
        private float _tick = 0f;
        private float _maxDistance = 0f;

        void Start()
        {
            _zoneCollider = GetComponentInParent<Collider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for RadiationZone");
            }
            SphereCollider sphereCollider = _zoneCollider as SphereCollider;
            if (sphereCollider != null)
            {
                _isSphere = true;
                _maxDistance = sphereCollider.radius;
            }
            else
            {
                BoxCollider box = _zoneCollider as BoxCollider;
                Vector3 boxSize = box.size;
                _maxDistance = boxSize.magnitude / 2f;
            }
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge;
                player.TryGetComponent<PlayerZoneBridge>(out hazardBridge);
                if (hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.RadZoneCount++;
                hazardBridge.RadRates.Add(this.name, 0f);
                hazardBridge.ZoneBlocksNav = BlocksNav;
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge = _containedPlayers[player];
                hazardBridge.RadZoneCount--;
                hazardBridge.RadRates.Remove(this.name);
                hazardBridge.ZoneBlocksNav = false;
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;

            if (_tick >= 0.001f)
            {
                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerZoneBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue; 
                    }  
                    float radAmount = _isSphere ? CalculateRadStrengthSphere(player.gameObject.transform.position) : CalculateRadStrengthBox(player.gameObject.transform.position);
                    hazardBridge.RadRates[this.name] = Mathf.Max(radAmount, 0f);
                }

                foreach (var p in playersToRemove) 
                {
                    _containedPlayers.Remove(p);
                }

                _tick = 0f;
            }
        }

        float CalculateRadStrengthBox(Vector3 playerPosition)
        {
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float invertedDistance = _maxDistance - distance;  // invert the distance
            invertedDistance = Mathf.Clamp(invertedDistance, 0, _maxDistance); //clamp the inverted distance
            return invertedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }

        float CalculateRadStrengthSphere(Vector3 playerPosition)
        {
            if (!UsesDistanceFalloff) return (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f)) / 1000f;
            float distanceToCenter = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float radius = (_zoneCollider as SphereCollider).radius * transform.localScale.magnitude;
            float distanceFromSurface = radius - distanceToCenter;
            float clampedDistance = Mathf.Max(0f, distanceFromSurface);
            return clampedDistance / (ZoneStrengthModifier * (PluginConfig.ZoneDebug.Value ? PluginConfig.test10.Value : 1f));
        }
    }

    public class SafeZone : TriggerWithId, IHazardZone
    {
        const float MAIN_VOLUME = 0.6f;
        const float SHUT_VOLUME = 0.55f;
        const float OPEN_VOLUME = 0.35f;
        public EZoneType ZoneType { get; } = EZoneType.SafeZone;
        public float ZoneStrengthModifier { get; set; } = 0f;
        public bool BlocksNav { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool IsActive { get; set; } = true;
        public bool? DoorType { get; set; }
        private Dictionary<Player, PlayerZoneBridge> _containedPlayers = new Dictionary<Player, PlayerZoneBridge>();
        private BoxCollider _zoneCollider;
        private float _tick = 0f;
        private float _distanceToCenter = 0f;
        private AudioSource _mainAudioSource;
        private AudioSource _doorShutAudioSource;
        private AudioSource _doorOpenAudioSource;
        private List<Door> _doors = new List<Door>();
        private Dictionary<WorldInteractiveObject, EDoorState> _previousDoorStates = new Dictionary<WorldInteractiveObject, EDoorState>();

        void Start()
        {
            _zoneCollider = GetComponentInParent<BoxCollider>();
            if (_zoneCollider == null)
            {
                Utils.Logger.LogError("Realism Mod: No BoxCollider found in parent for SafeZone");
                return;
            }
            SetUpAndPlayMainAudio();
            SetUpDoorShutAudio();
            SetUpDoorOpenAudio();
            CheckForDoors();
        }


        private void SetUpAndPlayMainAudio()
        {
            _mainAudioSource = this.gameObject.AddComponent<AudioSource>();
            _mainAudioSource.clip = Plugin.HazardZoneClips["labs-hvac.wav"];
            _mainAudioSource.volume = MAIN_VOLUME;
            _mainAudioSource.loop = true;
            _mainAudioSource.playOnAwake = false;
            _mainAudioSource.spatialBlend = 1.0f;
            _mainAudioSource.minDistance = 3.5f;
            _mainAudioSource.maxDistance = 15f;
            _mainAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _mainAudioSource.Play();
        }

        private void SetUpDoorShutAudio()
        {
            _doorShutAudioSource = this.gameObject.AddComponent<AudioSource>();
            _doorShutAudioSource.clip = Plugin.HazardZoneClips["door_shut.wav"];
            _doorShutAudioSource.volume = SHUT_VOLUME;
            _doorShutAudioSource.loop = false;
            _doorShutAudioSource.playOnAwake = false;
            _doorShutAudioSource.spatialBlend = 1.0f;
            _doorShutAudioSource.minDistance = 3.5f;
            _doorShutAudioSource.maxDistance = 15f;
            _doorShutAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void SetUpDoorOpenAudio()
        {
            _doorOpenAudioSource = this.gameObject.AddComponent<AudioSource>();
            _doorOpenAudioSource.clip = Plugin.HazardZoneClips["door_open.wav"];
            _doorOpenAudioSource.volume = OPEN_VOLUME;
            _doorOpenAudioSource.loop = false;
            _doorOpenAudioSource.playOnAwake = false;
            _doorOpenAudioSource.spatialBlend = 1.0f;
            _doorOpenAudioSource.minDistance = 3.5f;
            _doorOpenAudioSource.maxDistance = 15f;
            _doorOpenAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        IEnumerator AdjustVolume(float targetVolume, float speed, AudioSource audioSource)
        {
            while (audioSource.volume != targetVolume)
            {
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, speed * Time.deltaTime);
                yield return null;
            }
            audioSource.volume = targetVolume;
        }

        void CheckForDoors()
        {
            BoxCollider box = (BoxCollider)_zoneCollider;
            Vector3 boxCenter = box.transform.position + box.center;
            Vector3 boxSize = box.size / 2;

            Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize, Quaternion.identity);

            foreach (Collider col in colliders)
            {
                Door door = col.GetComponent<Door>();
                if (door != null && door.Operatable)
                {
                    if (door.KeyId == "5c1d0f4986f7744bb01837fa" || door.KeyId == "5c1d0c5f86f7744bb2683cf0") door.name = "automatic_door";
                    _doors.Add(door);
                    _previousDoorStates.Add(door, door.DoorState);
                }
            }
        }

        private bool KeysMatch(Player player, string doorKey)
        {
            if (player.MovementContext.InteractionInfo.Result != null)
            {
                KeyComponent key = ((KeyInteractionResultClass)player.MovementContext.InteractionInfo.Result).Key;
                return doorKey == key.Template.KeyId;
            }
            return false;
        }

        bool AnyDoorsOpen(WorldInteractiveObject activeWorldObject)
        {
            foreach (var door in _doors) 
            {
                if (ReferenceEquals(door, activeWorldObject))
                {
                    continue;
                }
                if (door.DoorState == EDoorState.Open) return true;
            }
            return false;    
        }

        IEnumerator PlayDoorInteractionSound(WorldInteractiveObject door, EDoorState prevState, EDoorState currentState)
        {
            bool isOpening = (prevState == EDoorState.Locked && currentState == EDoorState.Interacting) || (prevState == EDoorState.Shut && currentState == EDoorState.Interacting);
            bool isClosing = prevState == EDoorState.Open && currentState == EDoorState.Interacting;

            float time = 0;
            float timeLimit = prevState == EDoorState.Locked && door.name == "automatic_door" ? 4f : isOpening ? 0.75f : 1f;

            while (time < timeLimit)
            {
                time += Time.deltaTime; 
                yield return null;
            }

            bool otherDoorsCurrentlyOpen = AnyDoorsOpen(door);
            if (!otherDoorsCurrentlyOpen && isOpening && Mathf.Abs(door.CurrentAngle) > Mathf.Abs(door.GetAngle(EDoorState.Shut)))
            {
                _doorOpenAudioSource.Play();
                StartCoroutine(AdjustVolume(OPEN_VOLUME, 1f, _doorOpenAudioSource));
                StartCoroutine(AdjustVolume(0f, 0.25f, _doorShutAudioSource));
                IsActive = false;    
      
            }
            else if (!otherDoorsCurrentlyOpen && isClosing && Mathf.Abs(door.CurrentAngle) < Mathf.Abs(door.GetAngle(EDoorState.Open)))
            {
                _doorShutAudioSource.Play();
                StartCoroutine(AdjustVolume(SHUT_VOLUME, 1f, _doorShutAudioSource));
                StartCoroutine(AdjustVolume(0f, 0.25f, _doorOpenAudioSource));
                IsActive = true;
            }

            if (!IsActive) StartCoroutine(AdjustVolume(0f, 0.1f, _mainAudioSource));
            if (IsActive) StartCoroutine(AdjustVolume(MAIN_VOLUME, 0.1f, _mainAudioSource));
        }

        private void CheckDoorState(WorldInteractiveObject door)
        {
            EDoorState prevState = _previousDoorStates[door];
            EDoorState currentState = door.DoorState;
            if (currentState != prevState) 
            {
                StartCoroutine(PlayDoorInteractionSound(door, prevState, currentState));
            }
            _previousDoorStates[door] = door.DoorState;
        }

        void CalculateSafeZoneDepth(Vector3 playerPosition)
        {
            Vector3 extents = _zoneCollider.bounds.extents;
            float distance = Vector3.Distance(playerPosition, _zoneCollider.bounds.center);
            float maxDistance = extents.magnitude;
            float distancePercentage = distance / maxDistance;
            _distanceToCenter = Mathf.Clamp01(distancePercentage);
        }

        public override void TriggerEnter(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge;
                player.TryGetComponent<PlayerZoneBridge>(out hazardBridge);
                if (hazardBridge == null)
                {
                    hazardBridge = player.gameObject.AddComponent<PlayerZoneBridge>();
                    hazardBridge._Player = player;
                }
                hazardBridge.SafeZoneCount++;
                hazardBridge.ZoneBlocksNav = BlocksNav;
                hazardBridge.SafeZones.Add(this.name, IsActive);
                _containedPlayers.Add(player, hazardBridge);
            }
        }

        public override void TriggerExit(Player player)
        {
            if (player != null)
            {
                PlayerZoneBridge hazardBridge = _containedPlayers[player];
                hazardBridge.SafeZoneCount--;
                hazardBridge.SafeZones.Remove(this.name);
                hazardBridge.ZoneBlocksNav = false;
                _containedPlayers.Remove(player);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;
            if (_tick >= 0.25f)
            {
                var playersToRemove = new List<Player>();
                foreach (var p in _containedPlayers)
                {
                    Player player = p.Key;
                    PlayerZoneBridge hazardBridge = p.Value;
                    if (player == null || hazardBridge == null)
                    {
                        playersToRemove.Add(player);
                        continue;
                    }

                    CalculateSafeZoneDepth(player.Position);
                    hazardBridge.SafeZones[this.name] = IsActive && _distanceToCenter <= 0.69f;
                }

                foreach (var p in playersToRemove)
                {
                    _containedPlayers.Remove(p);
                }

                foreach (var door in _doors) 
                {
                    CheckDoorState(door);
                }

                _tick = 0f;
            }
        }
    }
}
