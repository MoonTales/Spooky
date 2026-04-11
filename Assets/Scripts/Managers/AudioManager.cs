using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;
using Player;
using Placeables;
using Types = System.Types;

namespace Managers
{
    public class AudioManager : Singleton<AudioManager>
    {
    //------------------//
        #region Types
    //------------------//

        // IDs for unparameterized SFX event mappings.
        public enum SfxId
        {
            // Player
            Jump, Landing, Flashlight, // CrouchIn, CrouchOut, PeekIn, PeekOut, TippytoeIn, TippytoeOut,
            // Interaction
            LetterSlide, LetterScribble,
            AlarmGood, AlarmBad,
            DoorLocked,
            // UI
            UIHover,
            // Tutorial Interactions
            TutorialButtonClick,
            TutorialDoorSlide,
            // Environment
            ClockTick,
        }

        // Inspector entry mapping SfxId -> FMOD event.
        [Serializable]
        public struct SfxEntry
        {
            public SfxId id;
            public EventReference eventRef;
        }

        // Per-call parameter payload for FMOD events.
        public readonly struct SfxParam
        {
            public readonly string name;
            public readonly float value;

            public SfxParam(string name, float value)
            {
                this.name = name;
                this.value = value;
            }

            public static SfxParam Bool(string name, bool enabled)
            {
                return new SfxParam(name, enabled ? 1f : 0f);
            }

            public static SfxParam Int(string name, int intValue)
            {
                return new SfxParam(name, intValue);
            }

            public static SfxParam Float(string name, float floatValue)
            {
                return new SfxParam(name, floatValue);
            }
        }
        #endregion

    //----------------------//
        #region Inspector
    //----------------------//

        #region Core and Debug

        [Space(10)]
        [Header("SFX Event Map")]
        [SerializeField] private SfxEntry[] sfxEvents;      // Inspector-assigned map of SfxId -> FMOD EventReference.

        [Space(10)]
        [Header("Debug")]
        [SerializeField] private bool debugAudioLogs = false;
        #endregion

        #region Player Audio

        [Space(10)]
        [Header("Player Audio")]
        [SerializeField] private EventReference footstepPlayer;     // Parameterized footstep event with Surface label parameter.
        [SerializeField] private string playerMovementBusPath = "bus:/SFX/Player/Movement"; // Bus containing player movement events for quick stops.
        [SerializeField] private string landingIntensityParameter = "LandingIntensity";
        [SerializeField] private float landingFallSpeedMax = 20f;
        [SerializeField] private float landingAirTimeMax = 1.5f;
        #endregion

        #region Environment Audio

        [Space(10)]
        [Header("Environment Audio (Lamps)")]
        [SerializeField] private bool autoAttachLampAudioOnSceneLoad = true;
        [SerializeField] private string lampAudioAutoAttachSceneName = "Tutorial";
        [SerializeField] private EventReference lampHumLoopEvent;
        [SerializeField] private string lampOnParameter = "LampOn";
        [SerializeField] private EventReference lampBuzzOffEvent;

        [Space(10)]
        [Header("Environment Audio (Tutorial Orbs)")]
        [SerializeField] private bool autoAttachTutorialOrbAudioOnSceneLoad = true;
        [SerializeField] private string tutorialOrbAudioSceneName = "Tutorial";
        [SerializeField] private string tutorialOrbRootName = "TheBuilding";
        [SerializeField] private string tutorialOrbNamePrefix = "Orb";
        [SerializeField] private EventReference[] tutorialOrbEvents;

        [Space(10)]
        [Header("Environment Audio (Spiders)")]
        [SerializeField] private bool autoAttachSpiderAudioOnSceneLoad = true;
        [SerializeField] private string[] spiderAudioSceneNames = { "Tutorial", "Nightmare1" };
        [SerializeField] private string spiderAnchorName = "TheThingy";
        [SerializeField] private EventReference spiderLoopEvent;
        [SerializeField] private string spiderIntensityParameter = "Intensity";
        [SerializeField] private float spiderIntensityMax = 100f;
        [SerializeField] private string spiderDangerParameter = "Danger";
        [SerializeField] private float spiderDangerMax = 100f;
        [SerializeField] private string spiderStateParameter = "State";

        [Space(10)]
        [Header("Environment Audio (Nightmare Roofs)")]
        [SerializeField] private bool autoAttachNightmareRoofAudioOnSceneLoad = true;
        [SerializeField] private string nightmareRoofAudioSceneName = "Nightmare1";
        [SerializeField] private string nightmareRoofRootName = "MysteriousNeighbor";
        [SerializeField] private string nightmareRoofParentName = "Roofs";
        [SerializeField] private EventReference nightmareRoofLoopEvent;
        [SerializeField] private int nightmareRoofEmitterTargetCount = 5;
        [SerializeField] private float nightmareRoofMinimumSpacing = 8f;

        [Space(10)]
        [Header("Environment Audio (Tutorial Hallway Stretch)")]
        [SerializeField] private EventReference tutorialHallwayStretchEvent;
        [SerializeField] private string tutorialHallwayStretchStateParameter = "StretchState";
        [SerializeField] private string tutorialHallwayStretchProgressParameter = "StretchProgress";
        [SerializeField] private string tutorialHallwayRunPressureParameter = "RunPressure";
        [SerializeField] private string tutorialHallwayPlayerSpeedParameter = "PlayerSpeed";
        [SerializeField] private float tutorialHallwayPlayerSpeedMax = 8f;
        [SerializeField, Range(0f, 1f)] private float tutorialHallwayPlayerSpeedFloor = 0.2f;
        [SerializeField] private float tutorialHallwaySweepDurationSeconds = 8f;
        [SerializeField] private string tutorialHallwayStateStretchingLabel = "Stretching";
        [SerializeField] private string tutorialHallwayStateContractedLabel = "Contracted";
        [SerializeField] private string tutorialDrawingAudioSceneName = "Tutorial";
        [SerializeField] private EventReference tutorialDrawingStaticLoopEvent;
        [SerializeField] private string bedroomWallClockActiveParameter = "ClockActive";
        [SerializeField] private string bedroomWallClockInspectingParameter = "IsInspecting";
        #endregion

        #region Mental Audio

        [Space(10)]
        [Header("Mental Audio")]
        [SerializeField] private string terrorDistortionParameter = "Terror";
        [SerializeField] private string mentalHealthDistortionParameter = "MentalHealth";
        [SerializeField] private bool terrorParameterIsGlobal = true;
        [SerializeField] private bool mentalHealthParameterIsGlobal = true;
        [SerializeField] private EventReference terrorLoopEvent;
        [SerializeField] private EventReference heartbeatLoopEvent;
        [SerializeField] private string heartbeatTerrorParameter = "Terror";
        [SerializeField] private string heartbeatMentalHealthParameter = "MentalHealth";
        [SerializeField] private bool heartbeatTerrorParameterIsGlobal = true;
        [SerializeField] private bool heartbeatMentalHealthParameterIsGlobal = true;
        [SerializeField] private bool logTerrorParameterValue = false;
        #endregion

        #region Sleep Tracker Audio

        [Space(10)]
        [Header("Sleep Tracker Audio")]
        [SerializeField] private string sleepTrackerActiveParameter = "SleepTracker";
        [SerializeField] private EventReference goodWakeupTransitionEvent;
        [SerializeField] private string goodWakeupTransitionParameter = "TransitionBlend";
        [SerializeField] private float goodWakeupCrossfadeFractionOfFadeOut = 0.2f;
        [SerializeField] private float goodWakeupCrossfadeMinSeconds = 0.4f;
        [SerializeField] private float goodWakeupCrossfadeMaxSeconds = 1.5f;
        #endregion

        #region World Ambience

        [Space(10)]
        [Header("World Ambience")]
        [SerializeField] private EventReference bedroomAmbLoopEvent;
        [SerializeField] private bool bedroomAmbienceRequiresGameplay = true;
        [SerializeField] private EventReference tutorialAmbLoopEvent;
        [SerializeField] private bool tutorialAmbienceRequiresGameplay = true;
        [SerializeField] private EventReference nightmareAmbLoopEvent;
        [SerializeField] private string nightmareAmbWorldClockParameter = "WorldClock";
        [SerializeField] private EventReference nightmareInteriorExteriorAmbLoopEvent;
        [SerializeField] private string nightmareInteriorAmountParameter = "InteriorAmount";
        #endregion

        #region Menu and Mix

        [Space(10)]
        [Header("Menu and Snapshot Audio")]
        [SerializeField] private EventReference mainMenuMusicEvent;
        [SerializeField] private string mainMenuTransitionParameter = "MainMenuTransition";
        [SerializeField] private EventReference pauseSnapshotEvent;

        [Space(10)]
        [Header("Mixer Bus Paths")]
        [SerializeField] private string masterBusPath = "bus:/";
        [SerializeField] private string sfxBusPath = "bus:/SFX";
        [SerializeField] private string musicBusPath = "bus:/Music";
        [SerializeField] private string ambienceBusPath = "bus:/Ambience";

        [Space(10)]
        [Header("Runtime Mutes")]
        public bool muteSFX = false;
        public bool muteMusic = false;
        #endregion
        #endregion

    //-------------------------//
        #region Runtime State
    //-------------------------//

        // Runtime lookups and bus cache.
        private Dictionary<SfxId, EventReference> _sfxMap;
        private Bus _playerMovementBus;
        private Bus _masterBus;
        private Bus _sfxBus;
        private Bus _musicBus;
        private Bus _ambienceBus;

        // Persistent FMOD instances - UI/menu and mix control.
        private EventInstance _mainMenuMusicInstance;
        private EventInstance _pauseSnapshotInstance;
        private EventInstance _uiHoverInstance;
        private EventInstance _DoorLockedInstance;

        // Persistent FMOD instances - world ambience and mental stack.
        private EventInstance _bedroomAmbienceInstance;
        private EventInstance _bedroomWallClockInstance;
        private EventInstance _tutorialAmbienceInstance;
        private EventInstance _nightmareInteriorExteriorAmbienceInstance;
        private EventInstance _tutorialHallwayStretchInstance;
        private EventInstance _nightmareAmbienceInstance;
        private EventInstance _terrorLoopInstance;
        private EventInstance _heartbeatInstance;

        // Persistent FMOD instances - sleep tracker/alarm flow.
        private EventInstance _sleepTrackerAlarmInstance;
        private EventInstance _goodWakeupTransitionInstance;

        // Runtime audio state - mental stack.
        private float _mentalStateSeverity;
        private float _terrorSeverity;
        private Transform _terrorSourceTransform;
        private bool _terrorRadiusIsActive;
        private bool _terrorLoopIsPlaying;
        private readonly List<TerrorRadius> _registeredTerrorRadii = new List<TerrorRadius>();

        // Runtime audio state - sleep tracker flow.
        private Coroutine _goodWakeupTransitionCoroutine;
        private bool _sleepTrackerAlarmIsGoodVariant;
        private bool _hasSleepTrackerAlarmVariant;
        private bool _goodWakeupTransitionRequested;
        private bool _goodWakeupHasBedroomSourceTransform;
        private Coroutine _tutorialHallwaySweepCoroutine;
        private Transform _tutorialHallwaySweepSourceTransform;
        private CharacterController _tutorialHallwayPlayerController;
        private Transform _tutorialHallwayPlayerTransform;
        private Vector3 _tutorialHallwayLastPlayerPosition;
        private bool _tutorialHallwayHasLastPlayerPosition;
        private bool _tutorialHallwayPlayerSpeedFloorActive;
        private PARAMETER_ID _bedroomWallClockActiveParameterId;
        private bool _hasBedroomWallClockActiveParameterId;
        private PARAMETER_ID _bedroomWallClockInspectingParameterId;
        private bool _hasBedroomWallClockInspectingParameterId;
        private NightmareWindPlane[] _cachedNightmareWindPlanes = Array.Empty<NightmareWindPlane>();

        // Runtime audio state - snapshot control.
        private bool _pauseSnapshotActive;
        #endregion

        #region Objects w/ Audio Scripts
        // Bedroom --> THHEDOORRR: DoorPassbyEmitter
        // Tutorial --> TheBuilding/Orb*: TutorialOrbAudioEmitter
        // Tutorial/Nightmare --> TheThingy: SpiderAudioEmitter
        #endregion


    //---------------------------------------//
        #region Lifecycle and Subscriptions
    //---------------------------------------//

        protected override void RegisterSubscriptions()
        {
            base.RegisterSubscriptions();

            // Mental-state driven audio parameters.
            TrackSubscription(
                () => EventBroadcaster.OnPlayerHealthStateChanged += OnPlayerMentalStateChanged,
                () => EventBroadcaster.OnPlayerHealthStateChanged -= OnPlayerMentalStateChanged);

            // Sleep tracker alarm state changes (active + good/bad variant).
            TrackSubscription(
                () => EventBroadcaster.OnSleepTrackerAudioStateChanged += OnSleepTrackerAudioStateChanged,
                () => EventBroadcaster.OnSleepTrackerAudioStateChanged -= OnSleepTrackerAudioStateChanged);
            TrackSubscription(
                () => EventBroadcaster.OnLetterSlide += OnLetterSlide,
                () => EventBroadcaster.OnLetterSlide -= OnLetterSlide);
            TrackSubscription(
                () => EventBroadcaster.OnLetterScribble += OnLetterScribble,
                () => EventBroadcaster.OnLetterScribble -= OnLetterScribble);
            TrackSubscription(
                () => EventBroadcaster.OnRequestScreenFade += OnRequestScreenFade,
                () => EventBroadcaster.OnRequestScreenFade -= OnRequestScreenFade);
            TrackSubscription(
                () => EventBroadcaster.OnRequestScreenFadeScreenSwap += OnRequestScreenFadeScreenSwap,
                () => EventBroadcaster.OnRequestScreenFadeScreenSwap -= OnRequestScreenFadeScreenSwap);
            TrackSubscription(
                () => EventBroadcaster.OnTutorialHallwayStretchStart += OnTutorialHallwayStretchStart,
                () => EventBroadcaster.OnTutorialHallwayStretchStart -= OnTutorialHallwayStretchStart);
            TrackSubscription(
                () => EventBroadcaster.OnTutorialHallwayStretchContracted += OnTutorialHallwayStretchContracted,
                () => EventBroadcaster.OnTutorialHallwayStretchContracted -= OnTutorialHallwayStretchContracted);

            // Scene/world transitions that affect persistent loops.
            TrackSubscription(
                () => EventBroadcaster.OnWorldLocationChangedEvent += OnWorldLocationChanged,
                () => EventBroadcaster.OnWorldLocationChangedEvent -= OnWorldLocationChanged);
            TrackSubscription(
                () => SceneManager.sceneLoaded += OnSceneLoaded,
                () => SceneManager.sceneLoaded -= OnSceneLoaded);
        }

        protected override void Awake()
        {
            base.Awake();

            BuildSfxMap();
            CachePlayerMovementBus();
            CacheSettingsBuses();
            CacheNightmareWindPlanes(SceneManager.GetActiveScene());
            AutoAttachLampAudioEmittersInScene(SceneManager.GetActiveScene());
            AutoAttachTutorialOrbAudioEmittersInScene(SceneManager.GetActiveScene());
            AutoAttachSpiderAudioEmittersInScene(SceneManager.GetActiveScene());
            AutoAttachNightmareRoofAudioEmittersInScene(SceneManager.GetActiveScene());
            ApplyBedroomAmbience();
            ApplyTutorialAmbience();
            ApplyNightmareWorldAmbience();
        }

        private void Update()
        {
            UpdateNightmareInteriorBlend();
            UpdateMentalAudioFrameParameters();
        }

        protected override void OnDestroy()
        {
            StopAndReleaseUiHover();
            StopAndReleaseHeartbeat();
            StopAndReleaseTerrorLoop();
            StopAndReleaseNightmareAmbience();
            StopAndReleaseSleepTrackerAlarm(true);
            StopAndReleaseGoodWakeupTransition(true);
            StopAndReleaseBedroomAmbience();
            StopAndReleaseBedroomWallClock(true);
            StopAndReleaseTutorialAmbience();
            StopAndReleaseNightmareInteriorExteriorAmbience();
            StopAndReleaseTutorialHallwayStretch(true);
            StopMainMenuMusic(true);
            SetPauseSnapshotEnabled(false);
            base.OnDestroy();
        }

        #endregion

    //-------------------------//
        #region Event Handlers
    //-------------------------//

          //--------------//
         //    Global    //
        //--------------//
        private void OnRequestScreenFade(Types.ScreenFadeData screenFadeData)
        {
            if (!_goodWakeupTransitionRequested)
            {
                return;
            }

            if (goodWakeupTransitionEvent.IsNull)
            {
                return;
            }

            if (_goodWakeupTransitionCoroutine != null)
            {
                StopCoroutine(_goodWakeupTransitionCoroutine);
            }

            _goodWakeupTransitionCoroutine = StartCoroutine(CrossfadeGoodWakeupTransition(screenFadeData));
        }

        private void OnWorldLocationChanged(Types.WorldLocation newLocation)
        {
            LogAudioState($"World location changed -> {newLocation}. Expected: nightmare stack in Nightmare, bedroom ambience in Bedroom.");
            if (newLocation != Types.WorldLocation.Nightmare)
            {
                _terrorSeverity = 0f;
                _terrorSourceTransform = null;
                _terrorRadiusIsActive = false;
            }

            if (newLocation != Types.WorldLocation.Bedroom)
            {
                StopAndReleaseSleepTrackerAlarm(true);
                StopAndReleaseGoodWakeupTransition(true);
                StopAndReleaseBedroomWallClock(true);
            }
            if (newLocation != Types.WorldLocation.Tutorial)
            {
                StopAndReleaseTutorialHallwayStretch(true);
            }
            RefreshMentalAudio();
            ApplyBedroomAmbience();
            ApplyTutorialAmbience();
            ApplyNightmareWorldAmbience();
        }

        private void OnRequestScreenFadeScreenSwap(Types.ScreenFadeSceneTransitionData screenFadeData)
        {
            if (!IsBedroomWorldLocation())
            {
                return;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Scene caching activates additive scenes briefly, which still triggers sceneLoaded.
            // Audio should only react to real single-scene transitions.
            if (mode == LoadSceneMode.Additive)
            {
                return;
            }

            if (!string.Equals(scene.name, "Bedroom", StringComparison.Ordinal))
            {
                StopAndReleaseBedroomWallClock(true);
            }

            CacheNightmareWindPlanes(scene);
            AutoAttachLampAudioEmittersInScene(scene);
            AutoAttachTutorialOrbAudioEmittersInScene(scene);
            AutoAttachSpiderAudioEmittersInScene(scene);
            AutoAttachNightmareRoofAudioEmittersInScene(scene);
            ApplyBedroomAmbience();
            ApplyTutorialAmbience();
            ApplyNightmareWorldAmbience();
        }

        protected override void OnGameStateChanged(Types.GameState newState)
        {
            base.OnGameStateChanged(newState);
            LogAudioState($"Game state changed -> {newState}. Expected: pause snapshot {(newState == Types.GameState.Paused ? "enabled" : "disabled")}.");
            SetPauseSnapshotEnabled(newState == Types.GameState.Paused);

            if (newState == Types.GameState.Paused)
            {
                StopFootstepsImmediate();
            }

            if (newState == Types.GameState.MainMenu)
            {
                PlayMainMenuMusicIfNeeded();
                StopAndReleaseSleepTrackerAlarm(true);
                StopAndReleaseGoodWakeupTransition(true);
                StopAndReleaseBedroomWallClock(true);
                StopAndReleaseTutorialHallwayStretch(true);
            }
            else
            {
                StopMainMenuMusic(true);
            }

            SetBedroomWallClockInspecting(newState == Types.GameState.Inspecting);
            ApplyBedroomAmbience();
            ApplyTutorialAmbience();
            ApplyNightmareWorldAmbience();
        }

        protected override void OnWorldClockTicked(int newHour)
        {
            base.OnWorldClockTicked(newHour);
            SetNightmareAmbienceWorldClock(newHour);
        }

          //---------------//
         //    Tutorial   //
        //---------------//
        private void OnTutorialHallwayStretchStart(Transform sourceTransform)
        {
            StartTutorialHallwayStretch(sourceTransform);
        }

        private void OnTutorialHallwayStretchContracted(Transform sourceTransform)
        {
            SetTutorialHallwayStretchContracted(sourceTransform);
        }

          //---------------//
         //    Bedroom    //
        //---------------//
        private void OnLetterSlide(Transform sourceTransform)
        {
            PlaySfx(SfxId.LetterSlide, sourceTransform);
        }

        private void OnLetterScribble(Transform sourceTransform)
        {
            if (GameStateManager.Instance == null
                || GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom)
            {
                return;
            }

            PlaySfx(SfxId.LetterScribble, sourceTransform);
        }

        public void StartBedroomWallClock(Transform sourceTransform)
        {
            if (GameStateManager.Instance == null
                || GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom)
            {
                if (_bedroomWallClockInstance.isValid())
                {
                    StopAndReleaseBedroomWallClock(true);
                }

                Debug.Log($"AudioManager: Ignoring bedroom wall clock start outside Bedroom. source={(sourceTransform != null ? sourceTransform.name : "null")}, world={(GameStateManager.Instance != null ? GameStateManager.Instance.GetCurrentWorldLocation().ToString() : "null")}");
                return;
            }

            EventReference eventReference = GetSfxEvent(SfxId.ClockTick);
            if (eventReference.IsNull)
            {
                Debug.LogWarning($"AudioManager: Missing FMOD EventReference for SfxId '{SfxId.ClockTick}'.");
                return;
            }

            if (_bedroomWallClockInstance.isValid())
            {
                if (sourceTransform != null && EventInstanceIs3D(_bedroomWallClockInstance))
                {
                    _bedroomWallClockInstance.set3DAttributes(RuntimeUtils.To3DAttributes(sourceTransform.position));
                }

                Debug.Log($"AudioManager: Reusing bedroom wall clock event. source={(sourceTransform != null ? sourceTransform.name : "null")}");
                SetBedroomWallClockActive(true);
                SetBedroomWallClockInspecting(GameStateManager.Instance != null
                    && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Inspecting);
                return;
            }

            _bedroomWallClockInstance = CreateEventInstance(eventReference, sourceTransform);
            CacheBedroomWallClockActiveParameterId();
            CacheBedroomWallClockInspectingParameterId();
            _bedroomWallClockInstance.start();
            Debug.Log($"AudioManager: Started bedroom wall clock event. source={(sourceTransform != null ? sourceTransform.name : "null")}");
            SetBedroomWallClockActive(true);
            SetBedroomWallClockInspecting(GameStateManager.Instance != null
                && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Inspecting);
        }

        private void OnSleepTrackerAudioStateChanged(bool isActive, bool isGoodWakeup, Transform sourceTransform)
        {
            if (GameStateManager.Instance == null)
            {
                StopAndReleaseSleepTrackerAlarm(true);
                StopAndReleaseGoodWakeupTransition(true);
                return;
            }

            Types.WorldLocation worldLocation = GameStateManager.Instance.GetCurrentWorldLocation();
            bool inBedroom = worldLocation == Types.WorldLocation.Bedroom;
            if (!inBedroom)
            {
                StopAndReleaseSleepTrackerAlarm(true);
                SetGoodWakeupTransitionActiveParameter(isActive);
                return;
            }

            if (!isActive)
            {
                StopAndReleaseSleepTrackerAlarm(true);
                StopAndReleaseGoodWakeupTransition(true);
                LogAudioState("Sleep tracker deactivated in Bedroom. Expected: active sleep tracker FMOD instances stop and unload.");
                return;
            }

            if (isGoodWakeup)
            {
                if (goodWakeupTransitionEvent.IsNull)
                {
                    Debug.LogWarning("AudioManager: Missing goodWakeupTransitionEvent reference for good wakeup alarm.");
                    return;
                }

                if (!_goodWakeupTransitionInstance.isValid())
                {
                    _goodWakeupTransitionInstance = CreateEventInstance(goodWakeupTransitionEvent, sourceTransform);
                    _goodWakeupTransitionInstance.start();
                }

                if (inBedroom && sourceTransform != null)
                {
                    _goodWakeupHasBedroomSourceTransform = true;
                    if (EventInstanceIs3D(_goodWakeupTransitionInstance))
                    {
                        _goodWakeupTransitionInstance.set3DAttributes(RuntimeUtils.To3DAttributes(sourceTransform.position));
                    }
                }

                SetGoodWakeupTransitionActiveParameter(isActive);
                StopAndReleaseSleepTrackerAlarm(true);
                return;
            }

            EventReference alarmEvent = GetSfxEvent(SfxId.AlarmBad);
            if (alarmEvent.IsNull)
            {
                Debug.LogWarning($"AudioManager: Missing FMOD EventReference for SfxId '{SfxId.AlarmBad}'.");
                return;
            }

            bool variantChanged = !_hasSleepTrackerAlarmVariant || _sleepTrackerAlarmIsGoodVariant != isGoodWakeup;
            if (variantChanged || !_sleepTrackerAlarmInstance.isValid())
            {
                StopAndReleaseSleepTrackerAlarm(true);
                _sleepTrackerAlarmInstance = CreateEventInstance(alarmEvent, sourceTransform);
                if (!string.IsNullOrWhiteSpace(sleepTrackerActiveParameter))
                {
                    _sleepTrackerAlarmInstance.setParameterByName(sleepTrackerActiveParameter, isActive ? 1f : 0f);
                }
                _sleepTrackerAlarmInstance.start();
                _sleepTrackerAlarmIsGoodVariant = isGoodWakeup;
                _hasSleepTrackerAlarmVariant = true;
                LogAudioState($"Sleep tracker alarm started ({(isGoodWakeup ? "good" : "bad")} wakeup).");
            }
            else if (sourceTransform != null && EventInstanceIs3D(_sleepTrackerAlarmInstance))
            {
                _sleepTrackerAlarmInstance.set3DAttributes(RuntimeUtils.To3DAttributes(sourceTransform.position));
            }

            if (_sleepTrackerAlarmInstance.isValid() && !string.IsNullOrWhiteSpace(sleepTrackerActiveParameter))
            {
                _sleepTrackerAlarmInstance.setParameterByName(sleepTrackerActiveParameter, isActive ? 1f : 0f);
            }
        }

          //----------------//
         //    Nightmare   //
        //----------------//
        private void OnPlayerMentalStateChanged(Types.PlayerMentalState newMentalState)
        {
            _mentalStateSeverity = GetNormalizedMentalHealth();
            LogAudioState($"Mental state changed -> {newMentalState} (normalized mental health {_mentalStateSeverity:0.00}). Expected: nightmare ambience/heartbeat parameters update.");
            RefreshMentalAudio();
        }

        #endregion


    //------------------------//
        #region Public API
    //-----------------------//


        #region Gameplay SFX

        public void RegisterTerrorRadius(TerrorRadius terrorRadius)
        {
            if (terrorRadius == null || _registeredTerrorRadii.Contains(terrorRadius))
            {
                return;
            }

            _registeredTerrorRadii.Add(terrorRadius);
        }

        public void UnregisterTerrorRadius(TerrorRadius terrorRadius)
        {
            if (terrorRadius == null)
            {
                return;
            }

            _registeredTerrorRadii.Remove(terrorRadius);
        }

        public void BeginGoodWakeupAlarmTransition()
        {
            _goodWakeupTransitionRequested = true;
            _goodWakeupHasBedroomSourceTransform = false;

            if (goodWakeupTransitionEvent.IsNull)
            {
                return;
            }

            if (!_goodWakeupTransitionInstance.isValid())
            {
                _goodWakeupTransitionInstance = CreateEventInstance(goodWakeupTransitionEvent);
                _goodWakeupTransitionInstance.start();
            }

            if (!string.IsNullOrWhiteSpace(sleepTrackerActiveParameter))
            {
                _goodWakeupTransitionInstance.setParameterByName(sleepTrackerActiveParameter, 1f);
            }

            if (!string.IsNullOrWhiteSpace(goodWakeupTransitionParameter))
            {
                _goodWakeupTransitionInstance.setParameterByName(goodWakeupTransitionParameter, 0f);
            }
        }

        public void PlayFootstep(string surfaceLabel, Transform fromTransform = null)
        {
            if (muteSFX) return;
            if (footstepPlayer.IsNull) return;

            // Use a labeled parameter to select the correct surface variation.
            EventInstance instance = CreateEventInstance(footstepPlayer, fromTransform);
            instance.setParameterByNameWithLabel("Surface", surfaceLabel);
            instance.start();
            instance.release();
        }

        public void StartTutorialHallwayStretch(Transform fromTransform = null)
        {
            if (muteSFX || tutorialHallwayStretchEvent.IsNull || !IsTutorialWorldLocation())
            {
                return;
            }

            if (!_tutorialHallwayStretchInstance.isValid())
            {
                _tutorialHallwayStretchInstance = CreateEventInstance(tutorialHallwayStretchEvent, fromTransform);
                _tutorialHallwayStretchInstance.start();
            }
            else if (fromTransform != null)
            {
                UpdateEventInstanceTransform(_tutorialHallwayStretchInstance, fromTransform);
            }

            _tutorialHallwaySweepSourceTransform = fromTransform;
            CacheTutorialHallwayPlayerMotionSource();
            _tutorialHallwayPlayerSpeedFloorActive = false;
            SetTutorialHallwayStretchStateLabel(tutorialHallwayStateStretchingLabel);
            SetTutorialHallwayRunPressure(0f);
            SetTutorialHallwayPlayerSpeed(0f);
            SetTutorialHallwayStretchProgress(0f, fromTransform);
            RestartTutorialHallwaySweep();
        }

        public void SetTutorialHallwayStretchContracted(Transform fromTransform = null)
        {
            if (!_tutorialHallwayStretchInstance.isValid())
            {
                return;
            }

            if (fromTransform != null)
            {
                _tutorialHallwaySweepSourceTransform = fromTransform;
            }

            if (fromTransform != null)
            {
                UpdateEventInstanceTransform(_tutorialHallwayStretchInstance, fromTransform);
            }

            SetTutorialHallwayStretchStateLabel(tutorialHallwayStateContractedLabel);
            SetTutorialHallwayRunPressure(1f);
        }

        public void StopTutorialHallwayStretch(bool immediate = false)
        {
            StopAndReleaseTutorialHallwayStretch(immediate);
        }
        #endregion

        #region External Event Instances

        public void UpdateEventInstanceTransform(EventInstance instance, Transform fromTransform)
        {
            if (!instance.isValid() || fromTransform == null)
            {
                return;
            }

            UpdateEventInstancePosition(instance, fromTransform.position);
        }

        public void UpdateEventInstancePosition(EventInstance instance, Vector3 worldPosition)
        {
            if (!instance.isValid())
            {
                return;
            }

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
        }

        public bool TryStartSfxEventInstance(EventReference eventReference, Vector3 worldPosition, out EventInstance instance)
        {
            instance = default;

            if (muteSFX || eventReference.IsNull)
            {
                return false;
            }

            instance = RuntimeManager.CreateInstance(eventReference);
            if (EventInstanceIs3D(instance))
            {
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
            }

            instance.start();
            return true;
        }

        public bool TryStartSfxEventInstance(
            EventReference eventReference,
            Vector3 worldPosition,
            bool randomizeTimelinePosition,
            out EventInstance instance)
        {
            instance = default;

            if (muteSFX || eventReference.IsNull)
            {
                return false;
            }

            instance = RuntimeManager.CreateInstance(eventReference);
            if (EventInstanceIs3D(instance))
            {
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
            }

            if (randomizeTimelinePosition
                && instance.isValid()
                && instance.getDescription(out EventDescription description) == FMOD.RESULT.OK
                && description.isValid()
                && description.getLength(out int eventLengthMs) == FMOD.RESULT.OK
                && eventLengthMs > 1)
            {
                int randomTimelinePositionMs = UnityEngine.Random.Range(0, eventLengthMs);
                instance.setTimelinePosition(randomTimelinePositionMs);
            }

            instance.start();
            return true;
        }

        public void StopAndReleaseEventInstance(ref EventInstance instance, bool immediate = false)
        {
            if (!instance.isValid())
            {
                return;
            }

            instance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
            instance = default;
        }

        public bool TryStartLampHumLoop(Transform fromTransform, bool isOn, out EventInstance instance)
        {
            instance = default;

            if (muteSFX || lampHumLoopEvent.IsNull)
            {
                return false;
            }

            instance = CreateEventInstance(lampHumLoopEvent, fromTransform);
            SetLampHumLoopEnabled(instance, isOn);
            instance.start();
            return true;
        }

        public void SetLampHumLoopEnabled(EventInstance instance, bool isOn)
        {
            if (!instance.isValid() || string.IsNullOrWhiteSpace(lampOnParameter))
            {
                return;
            }

            instance.setParameterByName(lampOnParameter, isOn ? 1f : 0f);
        }

        public bool TryStartTutorialDrawingStaticLoop(Transform fromTransform, out EventInstance instance)
        {
            instance = default;

            if (muteSFX || tutorialDrawingStaticLoopEvent.IsNull)
            {
                return false;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrWhiteSpace(tutorialDrawingAudioSceneName)
                && (!activeScene.IsValid() || !string.Equals(activeScene.name, tutorialDrawingAudioSceneName, StringComparison.Ordinal)))
            {
                return false;
            }

            instance = CreateEventInstance(tutorialDrawingStaticLoopEvent, fromTransform);
            instance.start();
            return true;
        }

        public bool TryStartSpiderLoop(
            Transform fromTransform,
            float rawIntensity,
            float rawDangerLevel,
            int stateValue,
            out EventInstance instance)
        {
            instance = default;

            if (muteSFX || spiderLoopEvent.IsNull)
            {
                return false;
            }

            instance = CreateEventInstance(spiderLoopEvent, fromTransform);
            SetSpiderLoopParameters(instance, rawIntensity, rawDangerLevel, stateValue);
            instance.start();
            return true;
        }

        public void UpdateSpiderLoop(
            EventInstance instance,
            Transform fromTransform,
            float rawIntensity,
            float rawDangerLevel,
            int stateValue)
        {
            if (!instance.isValid())
            {
                return;
            }

            if (fromTransform != null)
            {
                UpdateEventInstanceTransform(instance, fromTransform);
            }

            SetSpiderLoopParameters(instance, rawIntensity, rawDangerLevel, stateValue);
        }

        public void StopTutorialDrawingStaticLoopsImmediate()
        {
            if (tutorialDrawingStaticLoopEvent.IsNull)
            {
                return;
            }

            EventDescription description = RuntimeManager.GetEventDescription(tutorialDrawingStaticLoopEvent);
            if (!description.isValid() || description.getInstanceList(out EventInstance[] instances) != FMOD.RESULT.OK)
            {
                return;
            }

            for (int i = 0; i < instances.Length; i++)
            {
                if (!instances[i].isValid())
                {
                    continue;
                }

                instances[i].stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instances[i].release();
            }
        }
        #endregion

        #region Runtime Mix and Snapshot

        public void StopFootstepsImmediate()
        {
            if (_playerMovementBus.isValid())
            {
                _playerMovementBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
        }

        public void SetSfxVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_sfxBus.isValid())
            {
                _sfxBus.setVolume(clamped);
            }
            muteSFX = clamped <= 0.0001f;
        }
        public float GetSfxVolume()
        {
            float normalizedVolume = 1f;
            if (_sfxBus.isValid())
            {
                _sfxBus.getVolume(out normalizedVolume);
            }
            return normalizedVolume;
        }

        public void SetMasterVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_masterBus.isValid())
            {
                _masterBus.setVolume(clamped);
            }
        }

        public void SetMusicVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_musicBus.isValid())
            {
                _musicBus.setVolume(clamped);
            }
            muteMusic = clamped <= 0.0001f;
        }

        public void SetAmbienceVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            if (_ambienceBus.isValid())
            {
                _ambienceBus.setVolume(clamped);
            }
        }

        public void SetSfxMuted(bool isMuted)
        {
            muteSFX = isMuted;
            if (_sfxBus.isValid())
            {
                _sfxBus.setMute(isMuted);
            }
        }

        public void SetMusicMuted(bool isMuted)
        {
            muteMusic = isMuted;
            if (_musicBus.isValid())
            {
                _musicBus.setMute(isMuted);
            }
        }

        public void SetPauseSnapshotEnabled(bool enabled)
        {
            if (pauseSnapshotEvent.IsNull)
            {
                LogAudioState("Pause snapshot not started: pause snapshot event is null.");
                return;
            }

            if (enabled)
            {
                if (_pauseSnapshotActive)
                {
                    LogAudioState("Pause snapshot already active.");
                    return;
                }

                if (!_pauseSnapshotInstance.isValid())
                {
                    _pauseSnapshotInstance = RuntimeManager.CreateInstance(pauseSnapshotEvent);
                }
                _pauseSnapshotInstance.start();
                _pauseSnapshotActive = true;
                LogAudioState("Pause snapshot started. Expected: paused mix behavior now active.");
                return;
            }

            if (_pauseSnapshotInstance.isValid())
            {
                _pauseSnapshotInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _pauseSnapshotInstance.release();
                LogAudioState("Pause snapshot stopped.");
            }
            _pauseSnapshotActive = false;
        }

        public void TriggerMainMenuMusicTransition()
        {
            SetMainMenuMusicTransitionParameter(1f);
        }

        public void PlayLampBuzzOff(Transform fromTransform = null)
        {
            if (muteSFX || lampBuzzOffEvent.IsNull)
            {
                return;
            }

            PlayEvent(lampBuzzOffEvent, fromTransform);
        }
        #endregion

        #region Generic SFX Playback

        public void PlayParamSfx(SfxId sfxId, Transform fromTransform = null, params SfxParam[] parameters)
        {
            if (muteSFX) return;

            // Parameterized play path for events that need per-call data.
            EventReference eventReference = GetSfxEvent(sfxId);
            if (eventReference.IsNull)
            {
                return;
            }

            EventInstance instance = CreateEventInstance(eventReference, fromTransform);
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    instance.setParameterByName(parameters[i].name, parameters[i].value);
                }
            }
            instance.start();
            instance.release();
        }
        
        public void PlaySfx(SfxId sfxId, Transform fromTransform = null)
        {
            if (muteSFX) return;

            // Unparameterized SFX play path using the SfxId mapping.
            EventReference eventReference = GetSfxEvent(sfxId);
            if (!eventReference.IsNull)
            {
                PlayEvent(eventReference, fromTransform);
            }
            else
            {
                Debug.LogWarning($"AudioManager: Missing FMOD EventReference for SfxId '{sfxId}'.");
            }
        }

        public void PlayUiHoverSfx()
        {
            if (muteSFX)
            {
                return;
            }

            EventReference eventReference = GetSfxEvent(SfxId.UIHover);
            if (eventReference.IsNull)
            {
                Debug.LogWarning("AudioManager: Missing FMOD EventReference for SfxId 'UIHover'.");
                return;
            }

            if (!_uiHoverInstance.isValid())
            {
                _uiHoverInstance = CreateEventInstance(eventReference);
            }
            else
            {
                _uiHoverInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }

            _uiHoverInstance.start();
        }

        public void PlayDoorLockedSfx()
        {
            if (muteSFX)
            {
                return;
            }
            
            EventReference eventReference = GetSfxEvent(SfxId.DoorLocked);
            if (!_DoorLockedInstance.isValid())
            {
                _DoorLockedInstance = CreateEventInstance(eventReference);
            }
            else
            {
                _DoorLockedInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }

            _DoorLockedInstance.start();
        }

        public void PlayPlayerLanding(float downwardSpeed, float airborneTime, string surfaceLabel, Transform fromTransform = null)
        {
            if (muteSFX) return;

            EventReference eventReference = GetSfxEvent(SfxId.Landing);
            if (eventReference.IsNull)
            {
                Debug.LogWarning($"AudioManager: Missing FMOD EventReference for SfxId '{SfxId.Landing}'.");
                return;
            }

            float clampedDownwardSpeed = Mathf.Max(0f, downwardSpeed);
            float clampedAirborneTime = Mathf.Max(0f, airborneTime);
            float normalizedSpeed = landingFallSpeedMax > 0f
                ? Mathf.Clamp01(clampedDownwardSpeed / landingFallSpeedMax)
                : 0f;
            float normalizedAirTime = landingAirTimeMax > 0f
                ? Mathf.Clamp01(clampedAirborneTime / landingAirTimeMax)
                : 0f;
            float landingIntensity = Mathf.Clamp01(Mathf.Max(normalizedSpeed, normalizedAirTime));

            if (debugAudioLogs)
            {
                Debug.Log($"AudioManager: LandingIntensity={landingIntensity:0.000}");
            }

            EventInstance instance = CreateEventInstance(eventReference, fromTransform);
            if (!string.IsNullOrWhiteSpace(surfaceLabel) && !string.Equals(surfaceLabel, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                instance.setParameterByNameWithLabel("Surface", surfaceLabel);
            }
            if (!string.IsNullOrWhiteSpace(landingIntensityParameter))
            {
                instance.setParameterByName(landingIntensityParameter, landingIntensity);
            }

            instance.start();
            instance.release();
        }
        
        public void PlayPlayerJumping(float volume = 1, float deviation = 0.2f, Transform fromTransform = null)
        {
            StopFootstepsImmediate();
            PlaySfx(SfxId.Jump, fromTransform);
        }
        #endregion
        #endregion

    //-------------------------------//
        #region Sleep Tracker Audio
    //-------------------------------//

        private IEnumerator CrossfadeGoodWakeupTransition(Types.ScreenFadeData screenFadeData)
        {
            float fadeOutDuration = Mathf.Max(0f, screenFadeData.GetFadeOutDuration());
            float crossfadeDuration = Mathf.Clamp(
                fadeOutDuration * Mathf.Max(0f, goodWakeupCrossfadeFractionOfFadeOut),
                Mathf.Max(0.01f, goodWakeupCrossfadeMinSeconds),
                Mathf.Max(goodWakeupCrossfadeMinSeconds, goodWakeupCrossfadeMaxSeconds));
            float crossfadeStartDelay = Mathf.Max(0f, fadeOutDuration - crossfadeDuration);

            if (crossfadeStartDelay > 0f)
            {
                yield return new WaitForSeconds(crossfadeStartDelay);
            }

            if (!_goodWakeupTransitionInstance.isValid())
            {
                _goodWakeupTransitionCoroutine = null;
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(sleepTrackerActiveParameter))
            {
                _goodWakeupTransitionInstance.setParameterByName(sleepTrackerActiveParameter, 1f);
            }

            if (string.IsNullOrWhiteSpace(goodWakeupTransitionParameter))
            {
                _goodWakeupTransitionRequested = false;
                _goodWakeupTransitionCoroutine = null;
                yield break;
            }

            // Delay blend-to-world until the in-bedroom SleepTracker transform is known.
            while (!_goodWakeupHasBedroomSourceTransform)
            {
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / crossfadeDuration);
                _goodWakeupTransitionInstance.setParameterByName(goodWakeupTransitionParameter, alpha);

                yield return null;
            }

            _goodWakeupTransitionInstance.setParameterByName(goodWakeupTransitionParameter, 1f);

            _goodWakeupTransitionRequested = false;
            _goodWakeupTransitionCoroutine = null;
        }

        private void SetGoodWakeupTransitionActiveParameter(bool isActive)
        {
            if (string.IsNullOrWhiteSpace(sleepTrackerActiveParameter))
            {
                return;
            }

            float parameterValue = isActive ? 1f : 0f;
            if (_goodWakeupTransitionInstance.isValid())
            {
                _goodWakeupTransitionInstance.setParameterByName(sleepTrackerActiveParameter, parameterValue);
            }
        }
        #endregion

    //------------------------------//
        #region Mental Audio Stack
    //------------------------------//
        private void RefreshMentalAudio()
        {
            float terrorSeverityForAudio = IsNightmareWorldLocation() ? _terrorSeverity : 0f;
            ApplyTerrorLoop(terrorSeverityForAudio);
            ApplyHeartbeat();
        }

        private void UpdateMentalAudioFrameParameters()
        {
            _mentalStateSeverity = GetNormalizedMentalHealth();
            UpdateTerrorStateFromRegisteredRadii();

            if (IsNightmareWorldLocation() || _heartbeatInstance.isValid() || _terrorLoopInstance.isValid())
            {
                RefreshMentalAudio();
            }
        }

        private void UpdateTerrorStateFromRegisteredRadii()
        {
            if (!IsNightmareWorldLocation())
            {
                _terrorSeverity = 0f;
                _terrorSourceTransform = null;
                _terrorRadiusIsActive = false;
                return;
            }

            float bestTerrorSeverity = 0f;
            float bestDistanceToPlayer = float.MaxValue;
            Transform bestSourceTransform = null;
            bool foundActiveRadius = false;
            int bestRadiusIndex = -1;
            List<string> debugRadiusEntries = new List<string>();

            for (int i = _registeredTerrorRadii.Count - 1; i >= 0; i--)
            {
                TerrorRadius terrorRadius = _registeredTerrorRadii[i];
                if (terrorRadius == null)
                {
                    _registeredTerrorRadii.RemoveAt(i);
                    continue;
                }

                if (!terrorRadius.TryGetAudioTerrorState(
                        out float normalizedIntensity,
                        out Transform sourceTransform,
                        out float distanceToPlayer))
                {
                    continue;
                }

                foundActiveRadius = true;
                float clampedIntensity = Mathf.Clamp01(normalizedIntensity);
                debugRadiusEntries.Add($"[{i}] {terrorRadius.name}={clampedIntensity:0.000}");
                bool isStrongerSource = clampedIntensity > bestTerrorSeverity + 0.0001f;
                bool isTieButCloserSource = Mathf.Abs(clampedIntensity - bestTerrorSeverity) <= 0.0001f
                    && distanceToPlayer < bestDistanceToPlayer;

                if (!isStrongerSource && !isTieButCloserSource)
                {
                    continue;
                }

                bestTerrorSeverity = clampedIntensity;
                bestDistanceToPlayer = distanceToPlayer;
                bestSourceTransform = sourceTransform;
                bestRadiusIndex = i;
            }

            _terrorSeverity = bestTerrorSeverity;
            _terrorRadiusIsActive = foundActiveRadius;
            _terrorSourceTransform = foundActiveRadius ? bestSourceTransform : null;

            // Debug.Log(
            //     $"AudioManager: Terror radii considered = {debugRadiusEntries.Count}, bestIndex = {bestRadiusIndex}, bestValue = {_terrorSeverity:0.000}, values = {(debugRadiusEntries.Count > 0 ? string.Join(", ", debugRadiusEntries) : "none")}");
        }
        #endregion

    //-----------------------------------------//
        #region Persistent Music and Ambience
    //-----------------------------------------//

        private void PlayMainMenuMusicIfNeeded()
        {
            if (mainMenuMusicEvent.IsNull || muteMusic)
            {
                LogAudioState($"Main menu music not started. Reason: {(mainMenuMusicEvent.IsNull ? "event reference missing" : "music is muted")}.");
                return;
            }

            if (_mainMenuMusicInstance.isValid())
            {
                FMOD.RESULT stateResult = _mainMenuMusicInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
                if (stateResult == FMOD.RESULT.OK
                    && (playbackState == PLAYBACK_STATE.PLAYING || playbackState == PLAYBACK_STATE.STARTING))
                {
                    SetMainMenuMusicTransitionParameter(0f);
                    LogAudioState("Main menu music already playing.");
                    return;
                }

                _mainMenuMusicInstance.release();
            }

            _mainMenuMusicInstance = CreateEventInstance(mainMenuMusicEvent);
            SetMainMenuMusicTransitionParameter(0f);
            _mainMenuMusicInstance.start();
            LogAudioState("Main menu music started.");
        }

        private void StopMainMenuMusic(bool allowFadeout)
        {
            if (!_mainMenuMusicInstance.isValid())
            {
                return;
            }

            _mainMenuMusicInstance.stop(allowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            _mainMenuMusicInstance.release();
        }

        private void SetMainMenuMusicTransitionParameter(float value)
        {
            if (_mainMenuMusicInstance.isValid() && !string.IsNullOrWhiteSpace(mainMenuTransitionParameter))
            {
                _mainMenuMusicInstance.setParameterByName(mainMenuTransitionParameter, value);
            }
        }

        private void ApplyTutorialAmbience()
        {
            if (!ShouldPlayTutorialAmbience())
            {
                StopAndReleaseTutorialAmbience();
                return;
            }

            if (tutorialAmbLoopEvent.IsNull)
            {
                return;
            }

            if (_tutorialAmbienceInstance.isValid())
            {
                return;
            }

            _tutorialAmbienceInstance = CreateEventInstance(tutorialAmbLoopEvent);
            _tutorialAmbienceInstance.start();
            LogAudioState("Tutorial ambience started.");
        }

        private void ApplyBedroomAmbience()
        {
            if (!ShouldPlayBedroomAmbience())
            {
                StopAndReleaseBedroomAmbience();
                return;
            }

            if (bedroomAmbLoopEvent.IsNull)
            {
                return;
            }

            if (_bedroomAmbienceInstance.isValid())
            {
                return;
            }

            _bedroomAmbienceInstance = CreateEventInstance(bedroomAmbLoopEvent);
            _bedroomAmbienceInstance.start();
            LogAudioState("Bedroom ambience started.");
        }

        private void ApplyNightmareWorldAmbience()
        {
            ApplyNightmareAmbience();
            ApplyNightmareInteriorExteriorAmbience();
        }

        private void ApplyNightmareAmbience()
        {
            if (!IsNightmareWorldLocation() || IsMainMenuGameState())
            {
                StopAndReleaseNightmareAmbience();
                StopAndReleaseNightmareInteriorExteriorAmbience();
                return;
            }

            if (nightmareAmbLoopEvent.IsNull)
            {
                return;
            }

            if (!_nightmareAmbienceInstance.isValid())
            {
                _nightmareAmbienceInstance = CreateEventInstance(nightmareAmbLoopEvent);
                SetNightmareAmbienceWorldClock(GameStateManager.Instance != null
                    ? GameStateManager.Instance.GetCurrentWorldClockHour()
                    : 1);
                _nightmareAmbienceInstance.start();
                LogAudioState("Nightmare base ambience started.");
                return;
            }

            SetNightmareAmbienceWorldClock(GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentWorldClockHour()
                : 1);
        }

        private void ApplyNightmareInteriorExteriorAmbience()
        {
            if (!IsNightmareWorldLocation() || IsMainMenuGameState())
            {
                StopAndReleaseNightmareInteriorExteriorAmbience();
                return;
            }

            if (nightmareInteriorExteriorAmbLoopEvent.IsNull)
            {
                return;
            }

            if (!_nightmareInteriorExteriorAmbienceInstance.isValid())
            {
                _nightmareInteriorExteriorAmbienceInstance = CreateEventInstance(nightmareInteriorExteriorAmbLoopEvent);
                SetNightmareAmbienceWorldClock(GameStateManager.Instance != null
                    ? GameStateManager.Instance.GetCurrentWorldClockHour()
                    : 1);
                _nightmareInteriorExteriorAmbienceInstance.start();
                LogAudioState("Nightmare interior/exterior ambience started.");
            }

            SetNightmareAmbienceWorldClock(GameStateManager.Instance != null
                ? GameStateManager.Instance.GetCurrentWorldClockHour()
                : 1);
            UpdateNightmareInteriorBlend();
        }

        private void ApplyTerrorLoop(float terrorSeverity)
        {
            if (!IsNightmareWorldLocation())
            {
                StopAndReleaseTerrorLoop();
                return;
            }

            if (terrorLoopEvent.IsNull)
            {
                return;
            }

            bool shouldPlay = _terrorRadiusIsActive && terrorSeverity > 0.0001f && _terrorSourceTransform != null;
            if (!shouldPlay)
            {
                StopAndReleaseTerrorLoop();
                return;
            }

            if (!_terrorLoopIsPlaying)
            {
                _terrorLoopInstance = CreateEventInstance(terrorLoopEvent, _terrorSourceTransform);
                _terrorLoopInstance.start();
                _terrorLoopIsPlaying = true;
                LogAudioState("Terror loop started. Expected: audible 3D terror source in Nightmare.");
            }

            if (_terrorLoopInstance.isValid())
            {
                _terrorLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(_terrorSourceTransform.position));
            }

            if (!string.IsNullOrWhiteSpace(terrorDistortionParameter))
            {
                SetFmodParameter(_terrorLoopInstance, terrorDistortionParameter, terrorSeverity, terrorParameterIsGlobal);
            }

            if (!string.IsNullOrWhiteSpace(mentalHealthDistortionParameter))
            {
                SetFmodParameter(_terrorLoopInstance, mentalHealthDistortionParameter, _mentalStateSeverity, mentalHealthParameterIsGlobal);
            }
        }

        private void ApplyHeartbeat()
        {
            if (!IsNightmareWorldLocation())
            {
                StopAndReleaseHeartbeat();
                return;
            }

            if (heartbeatLoopEvent.IsNull)
            {
                return;
            }

            if (!_heartbeatInstance.isValid())
            {
                _heartbeatInstance = CreateEventInstance(heartbeatLoopEvent);
                _heartbeatInstance.start();
                LogAudioState("Heartbeat loop started. Expected: heartbeat audible in Nightmare.");
            }

            if (!string.IsNullOrWhiteSpace(heartbeatTerrorParameter))
            {
                SetFmodParameter(_heartbeatInstance, heartbeatTerrorParameter, _terrorSeverity, heartbeatTerrorParameterIsGlobal);
            }

            if (!string.IsNullOrWhiteSpace(heartbeatMentalHealthParameter))
            {
                SetFmodParameter(_heartbeatInstance, heartbeatMentalHealthParameter, _mentalStateSeverity, heartbeatMentalHealthParameterIsGlobal);
            }
        }
        #endregion

    //------------------------//
        #region FMOD Helpers
    //------------------------//

        private void SetFmodParameter(EventInstance instance, string parameterName, float value, bool isGlobal)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            if (isGlobal)
            {
                RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
                return;
            }

            if (instance.isValid())
            {
                instance.setParameterByName(parameterName, value);
            }
        }

        private float GetNormalizedMentalHealth()
        {
            if (PlayerStats.Instance == null)
            {
                return 0f;
            }

            Types.FPlayerStats playerStats = PlayerStats.Instance.GetPlayerStats();
            float maxMentalHealth = Mathf.Max(0.0001f, playerStats.GetMaxMentalHealth());
            float normalizedMentalHealth = Mathf.Clamp01(playerStats.GetCurrentMentalHealth() / maxMentalHealth);
            return 1f - normalizedMentalHealth;
        }

        private static bool IsNightmareWorldLocation()
        {
            return GameStateManager.Instance != null
                && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Nightmare;
        }

        private static bool IsTutorialWorldLocation()
        {
            return GameStateManager.Instance != null
                && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Tutorial;
        }

        private static bool IsBedroomWorldLocation()
        {
            return GameStateManager.Instance != null
                && GameStateManager.Instance.GetCurrentWorldLocation() == Types.WorldLocation.Bedroom;
        }

        private static bool IsMainMenuGameState()
        {
            return GameStateManager.Instance != null
                && GameStateManager.Instance.GetCurrentGameState() == Types.GameState.MainMenu;
        }

        private void CacheNightmareWindPlanes(Scene scene)
        {
            _cachedNightmareWindPlanes = Array.Empty<NightmareWindPlane>();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            List<NightmareWindPlane> planes = new List<NightmareWindPlane>();
            HashSet<NightmareWindPlane> uniquePlanes = new HashSet<NightmareWindPlane>();

            for (int i = 0; i < roots.Length; i++)
            {
                NightmareWindPlane[] rootPlanes = roots[i].GetComponentsInChildren<NightmareWindPlane>(true);
                for (int j = 0; j < rootPlanes.Length; j++)
                {
                    NightmareWindPlane rootPlane = rootPlanes[j];
                    if (rootPlane != null && uniquePlanes.Add(rootPlane))
                    {
                        planes.Add(rootPlane);
                    }
                }
            }

            _cachedNightmareWindPlanes = planes.ToArray();
        }

        private bool ShouldPlayBedroomAmbience()
        {
            if (GameStateManager.Instance == null)
            {
                return false;
            }

            if (GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Bedroom)
            {
                return false;
            }

            if (!bedroomAmbienceRequiresGameplay)
            {
                return true;
            }

            return GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay;
        }

        private bool ShouldPlayTutorialAmbience()
        {
            if (GameStateManager.Instance == null)
            {
                return false;
            }

            if (GameStateManager.Instance.GetCurrentWorldLocation() != Types.WorldLocation.Tutorial)
            {
                return false;
            }

            if (!tutorialAmbienceRequiresGameplay)
            {
                return true;
            }

            return GameStateManager.Instance.GetCurrentGameState() == Types.GameState.Gameplay;
        }

        private void UpdateNightmareInteriorBlend()
        {
            if (!_nightmareInteriorExteriorAmbienceInstance.isValid()
                || string.IsNullOrWhiteSpace(nightmareInteriorAmountParameter)
                || PlayerController.Instance == null)
            {
                return;
            }

            Vector3 playerPosition = PlayerController.Instance.transform.position;
            float defaultHalfWidth = 1.5f;
            float defaultBlendDepth = 4f;
            float defaultMaxInfluenceDistance = 20f;

            float clampedBlendDepth = Mathf.Max(0.01f, defaultBlendDepth);
            float clampedDoorHalfWidth = Mathf.Max(0.01f, defaultHalfWidth);
            float clampedMaxInfluenceDistance = Mathf.Max(0.01f, defaultMaxInfluenceDistance);
            float activeInteriorAmount = 1f;
            bool foundRelevantDoorway = false;
            NightmareWindPlane activeWindPlane = null;
            float closestObjectDistance = float.MaxValue;

            List<NightmareWindPlane> relevantDebugPlanes = null;
            List<float> debugSignedDepths = null;
            List<float> debugLateralOffsets = null;
            List<float> debugPlanarDistances = null;
            List<float> debugInteriorAmounts = null;
            List<float> debugHalfWidths = null;
            List<float> debugBlendDepths = null;
            List<float> debugMaxInfluenceDistances = null;

            if (_cachedNightmareWindPlanes == null || _cachedNightmareWindPlanes.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _cachedNightmareWindPlanes.Length; i++)
            {
                NightmareWindPlane windPlane = _cachedNightmareWindPlanes[i];
                if (windPlane == null || !windPlane.isActiveAndEnabled)
                {
                    continue;
                }

                float doorwayBlendDepth = windPlane.BlendDepthOverride > 0f
                    ? windPlane.BlendDepthOverride
                    : clampedBlendDepth;
                float doorwayHalfWidth = windPlane.HalfWidthOverride > 0f
                    ? windPlane.HalfWidthOverride
                    : clampedDoorHalfWidth;
                float doorwayMaxInfluenceDistance = windPlane.MaxInfluenceDistanceOverride > 0f
                    ? windPlane.MaxInfluenceDistanceOverride
                    : clampedMaxInfluenceDistance;

                Vector3 toPlayer = playerPosition - windPlane.transform.position;
                float objectDistance = toPlayer.magnitude;
                float planarDistance = Vector3.ProjectOnPlane(toPlayer, Vector3.up).magnitude;
                if (planarDistance > doorwayMaxInfluenceDistance)
                {
                    continue;
                }

                Vector3 outsideNormal = windPlane.GetOutsideNormalWorld().normalized;
                Vector3 lateralDirection = windPlane.GetLateralDirectionWorld().normalized;
                if (outsideNormal.sqrMagnitude <= 0.0001f || lateralDirection.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                foundRelevantDoorway = true;

                float signedDepth = Vector3.Dot(outsideNormal, toPlayer);
                float interiorAmount = 1f - Mathf.InverseLerp(-doorwayBlendDepth, doorwayBlendDepth, signedDepth);

                float lateralOffset = Mathf.Abs(Vector3.Dot(lateralDirection, toPlayer));
                if (lateralOffset > doorwayHalfWidth)
                {
                    interiorAmount = signedDepth <= 0f ? 1f : 0f;
                }

                interiorAmount = Mathf.Clamp(interiorAmount, windPlane.InteriorAmountFloor, windPlane.InteriorAmountCeiling);

                if (objectDistance < closestObjectDistance)
                {
                    closestObjectDistance = objectDistance;
                    activeInteriorAmount = interiorAmount;
                    activeWindPlane = windPlane;
                }

                if (windPlane.DebugOutputEnabled)
                {
                    relevantDebugPlanes ??= new List<NightmareWindPlane>();
                    debugSignedDepths ??= new List<float>();
                    debugLateralOffsets ??= new List<float>();
                    debugPlanarDistances ??= new List<float>();
                    debugInteriorAmounts ??= new List<float>();
                    debugHalfWidths ??= new List<float>();
                    debugBlendDepths ??= new List<float>();
                    debugMaxInfluenceDistances ??= new List<float>();

                    relevantDebugPlanes.Add(windPlane);
                    debugSignedDepths.Add(signedDepth);
                    debugLateralOffsets.Add(lateralOffset);
                    debugPlanarDistances.Add(planarDistance);
                    debugInteriorAmounts.Add(interiorAmount);
                    debugHalfWidths.Add(doorwayHalfWidth);
                    debugBlendDepths.Add(doorwayBlendDepth);
                    debugMaxInfluenceDistances.Add(doorwayMaxInfluenceDistance);
                }
            }

            if (!foundRelevantDoorway)
            {
                return;
            }

            if (relevantDebugPlanes != null)
            {
                for (int i = 0; i < relevantDebugPlanes.Count; i++)
                {
                    NightmareWindPlane debugPlane = relevantDebugPlanes[i];
                    debugPlane.LogDebugBlendState(
                        debugPlane == activeWindPlane,
                        debugSignedDepths[i],
                        debugLateralOffsets[i],
                        debugPlanarDistances[i],
                        debugInteriorAmounts[i],
                        debugHalfWidths[i],
                        debugBlendDepths[i],
                        debugMaxInfluenceDistances[i]);
                }
            }

            _nightmareInteriorExteriorAmbienceInstance.setParameterByName(nightmareInteriorAmountParameter, activeInteriorAmount);
        }

        private void SetTutorialHallwayStretchStateLabel(string stateLabel)
        {
            if (!_tutorialHallwayStretchInstance.isValid()
                || string.IsNullOrWhiteSpace(tutorialHallwayStretchStateParameter)
                || string.IsNullOrWhiteSpace(stateLabel))
            {
                return;
            }

            _tutorialHallwayStretchInstance.setParameterByNameWithLabel(tutorialHallwayStretchStateParameter, stateLabel);
        }

        private void RestartTutorialHallwaySweep()
        {
            StopTutorialHallwaySweep();
            if (!_tutorialHallwayStretchInstance.isValid())
            {
                return;
            }

            _tutorialHallwaySweepCoroutine = StartCoroutine(RunTutorialHallwaySweep());
        }

        private void StopTutorialHallwaySweep()
        {
            if (_tutorialHallwaySweepCoroutine == null)
            {
                return;
            }

            StopCoroutine(_tutorialHallwaySweepCoroutine);
            _tutorialHallwaySweepCoroutine = null;
        }

        private IEnumerator RunTutorialHallwaySweep()
        {
            float duration = Mathf.Max(0.01f, tutorialHallwaySweepDurationSeconds);
            float elapsed = 0f;
            SetTutorialHallwayStretchProgress(0f, _tutorialHallwaySweepSourceTransform);
            SetTutorialHallwayPlayerSpeed(0f);

            while (_tutorialHallwayStretchInstance.isValid() && elapsed < duration)
            {
                // Player speed (normalized 0..1) scales sweep time: 1 = realtime, 0.5 = half speed, 0 = paused.
                float speedCoefficient = UpdateTutorialHallwayPlayerSpeedFromMotionSource();
                elapsed += Time.deltaTime * speedCoefficient;
                float stretchProgress01 = Mathf.Clamp01(elapsed / duration);
                SetTutorialHallwayStretchProgress(stretchProgress01, _tutorialHallwaySweepSourceTransform);
                yield return null;
            }

            if (_tutorialHallwayStretchInstance.isValid())
            {
                _tutorialHallwayStretchInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _tutorialHallwayStretchInstance.release();
                _tutorialHallwayStretchInstance = default;
                _tutorialHallwaySweepSourceTransform = null;
                _tutorialHallwayPlayerController = null;
                _tutorialHallwayPlayerTransform = null;
                _tutorialHallwayHasLastPlayerPosition = false;
                _tutorialHallwayPlayerSpeedFloorActive = false;
                LogAudioState("Tutorial hallway stretch stopped (sweep duration complete).");
            }

            _tutorialHallwaySweepCoroutine = null;
        }

        private void SetTutorialHallwayStretchProgress(float stretchProgress01, Transform fromTransform = null)
        {
            if (!_tutorialHallwayStretchInstance.isValid())
            {
                return;
            }

            if (fromTransform != null)
            {
                _tutorialHallwaySweepSourceTransform = fromTransform;
                UpdateEventInstanceTransform(_tutorialHallwayStretchInstance, fromTransform);
            }

            if (string.IsNullOrWhiteSpace(tutorialHallwayStretchProgressParameter))
            {
                return;
            }

            _tutorialHallwayStretchInstance.setParameterByName(
                tutorialHallwayStretchProgressParameter,
                Mathf.Clamp01(stretchProgress01));
        }

        private void SetTutorialHallwayRunPressure(float runPressure01)
        {
            if (!_tutorialHallwayStretchInstance.isValid()
                || string.IsNullOrWhiteSpace(tutorialHallwayRunPressureParameter))
            {
                return;
            }

            _tutorialHallwayStretchInstance.setParameterByName(
                tutorialHallwayRunPressureParameter,
                Mathf.Clamp01(runPressure01));
        }

        private float UpdateTutorialHallwayPlayerSpeedFromMotionSource()
        {
            float speed = 0f;

            if (_tutorialHallwayPlayerController != null)
            {
                Vector3 velocity = _tutorialHallwayPlayerController.velocity;
                velocity.y = 0f;
                speed = velocity.magnitude;
            }
            else if (_tutorialHallwayPlayerTransform != null)
            {
                Vector3 currentPosition = _tutorialHallwayPlayerTransform.position;
                if (_tutorialHallwayHasLastPlayerPosition)
                {
                    float deltaTime = Mathf.Max(0.0001f, Time.deltaTime);
                    speed = Vector3.Distance(currentPosition, _tutorialHallwayLastPlayerPosition) / deltaTime;
                }

                _tutorialHallwayLastPlayerPosition = currentPosition;
                _tutorialHallwayHasLastPlayerPosition = true;
            }

            return SetTutorialHallwayPlayerSpeed(speed);
        }

        private float SetTutorialHallwayPlayerSpeed(float speedUnitsPerSecond)
        {
            if (!_tutorialHallwayStretchInstance.isValid())
            {
                return 0f;
            }

            float maxSpeed = Mathf.Max(0.01f, tutorialHallwayPlayerSpeedMax);
            float normalizedSpeed = Mathf.Clamp01(speedUnitsPerSecond / maxSpeed);
            float floorThreshold = Mathf.Clamp01(tutorialHallwayPlayerSpeedFloor);

            if (!_tutorialHallwayPlayerSpeedFloorActive && normalizedSpeed > floorThreshold)
            {
                _tutorialHallwayPlayerSpeedFloorActive = true;
            }

            float speedForFmod = normalizedSpeed;
            if (_tutorialHallwayPlayerSpeedFloorActive && normalizedSpeed < floorThreshold)
            {
                speedForFmod = floorThreshold;
            }

            if (!string.IsNullOrWhiteSpace(tutorialHallwayPlayerSpeedParameter))
            {
                _tutorialHallwayStretchInstance.setParameterByName(tutorialHallwayPlayerSpeedParameter, speedForFmod);
            }

            return normalizedSpeed;
        }

        private void CacheTutorialHallwayPlayerMotionSource()
        {
            _tutorialHallwayPlayerController = null;
            _tutorialHallwayPlayerTransform = null;
            _tutorialHallwayHasLastPlayerPosition = false;

            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null)
            {
                return;
            }

            _tutorialHallwayPlayerTransform = playerObject.transform;
            _tutorialHallwayPlayerController = playerObject.GetComponent<CharacterController>();
            if (_tutorialHallwayPlayerController == null)
            {
                _tutorialHallwayPlayerController = playerObject.GetComponentInParent<CharacterController>();
            }

            _tutorialHallwayLastPlayerPosition = _tutorialHallwayPlayerTransform.position;
            _tutorialHallwayHasLastPlayerPosition = true;
        }

        private void PlayEvent(EventReference eventReference, Transform fromTransform)
        {
            if (muteSFX) return;
            if (eventReference.IsNull) return;

            EventInstance instance = CreateEventInstance(eventReference, fromTransform);
            instance.start();
            instance.release();
        }

        private EventInstance CreateEventInstance(EventReference eventReference, Transform fromTransform = null)
        {
            // Use RuntimeManager to ensure correct FMOD instance tracking and virtualization.
            EventInstance instance = RuntimeManager.CreateInstance(eventReference);

            if (EventInstanceIs3D(instance))
            {
                Vector3 position = fromTransform != null
                    ? fromTransform.position
                    : Camera.main != null
                        ? Camera.main.transform.position
                        : Vector3.zero;

                instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            }
            return instance;
        }

        private static bool EventInstanceIs3D(EventInstance instance)
        {
            if (!instance.isValid())
            {
                return false;
            }

            FMOD.RESULT descriptionResult = instance.getDescription(out EventDescription description);
            if (descriptionResult != FMOD.RESULT.OK)
            {
                return false;
            }

            FMOD.RESULT is3DResult = description.is3D(out bool is3D);
            return is3DResult == FMOD.RESULT.OK && is3D;
        }
        #endregion

    //-------------------------------//
        #region Cleanup and Caching
    //-------------------------------//

        #region Cleanup
        private void StopAndReleaseHeartbeat()
        {
            if (_heartbeatInstance.isValid())
            {
                _heartbeatInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _heartbeatInstance.release();
                LogAudioState("Heartbeat loop stopped.");
            }
        }

        public void StopAndReleaseSleepTrackerAlarm(bool immediate)
        {
            if (_sleepTrackerAlarmInstance.isValid())
            {
                _sleepTrackerAlarmInstance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _sleepTrackerAlarmInstance.release();
                _sleepTrackerAlarmInstance = default;
                LogAudioState("Sleep tracker alarm stopped.");
            }

            _hasSleepTrackerAlarmVariant = false;
        }

        private void StopAndReleaseGoodWakeupTransition(bool immediate)
        {
            if (_goodWakeupTransitionCoroutine != null)
            {
                StopCoroutine(_goodWakeupTransitionCoroutine);
                _goodWakeupTransitionCoroutine = null;
            }

            if (_goodWakeupTransitionInstance.isValid())
            {
                _goodWakeupTransitionInstance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _goodWakeupTransitionInstance.release();
                _goodWakeupTransitionInstance = default;
            }

            _goodWakeupTransitionRequested = false;
            _goodWakeupHasBedroomSourceTransform = false;
        }

        private void StopAndReleaseTerrorLoop()
        {
            if (_terrorLoopInstance.isValid())
            {
                _terrorLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _terrorLoopInstance.release();
                LogAudioState("Terror loop stopped.");
            }
            _terrorLoopIsPlaying = false;
        }

        private void StopAndReleaseNightmareAmbience()
        {
            if (_nightmareAmbienceInstance.isValid())
            {
                _nightmareAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _nightmareAmbienceInstance.release();
                _nightmareAmbienceInstance = default;
                LogAudioState("Nightmare ambience stopped.");
            }
        }

        private void SetNightmareAmbienceWorldClock(int worldClockHour)
        {
            if (string.IsNullOrWhiteSpace(nightmareAmbWorldClockParameter))
            {
                return;
            }

            if (_nightmareAmbienceInstance.isValid())
            {
                _nightmareAmbienceInstance.setParameterByName(nightmareAmbWorldClockParameter, worldClockHour);
            }

            if (_nightmareInteriorExteriorAmbienceInstance.isValid())
            {
                _nightmareInteriorExteriorAmbienceInstance.setParameterByName(nightmareAmbWorldClockParameter, worldClockHour);
            }
        }

        private void StopAndReleaseNightmareInteriorExteriorAmbience()
        {
            if (_nightmareInteriorExteriorAmbienceInstance.isValid())
            {
                _nightmareInteriorExteriorAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _nightmareInteriorExteriorAmbienceInstance.release();
                _nightmareInteriorExteriorAmbienceInstance = default;
                LogAudioState("Nightmare interior/exterior ambience stopped.");
            }
        }

        private void StopAndReleaseBedroomAmbience()
        {
            if (_bedroomAmbienceInstance.isValid())
            {
                _bedroomAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _bedroomAmbienceInstance.release();
                _bedroomAmbienceInstance = default;
                LogAudioState("Bedroom ambience stopped.");
            }
        }

        private void StopAndReleaseBedroomWallClock(bool immediate)
        {
            if (_bedroomWallClockInstance.isValid())
            {
                LogAudioState($"Bedroom wall clock stopped via code fallback. immediate={immediate}.");
                _bedroomWallClockInstance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _bedroomWallClockInstance.release();
                _bedroomWallClockInstance = default;
            }

            _hasBedroomWallClockActiveParameterId = false;
            _bedroomWallClockActiveParameterId = default;
            _hasBedroomWallClockInspectingParameterId = false;
            _bedroomWallClockInspectingParameterId = default;
        }

        private void StopAndReleaseTutorialAmbience()
        {
            if (_tutorialAmbienceInstance.isValid())
            {
                _tutorialAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _tutorialAmbienceInstance.release();
                _tutorialAmbienceInstance = default;
                LogAudioState("Tutorial ambience stopped.");
            }
        }

        private void StopAndReleaseTutorialHallwayStretch(bool immediate)
        {
            StopTutorialHallwaySweep();
            _tutorialHallwaySweepSourceTransform = null;
            _tutorialHallwayPlayerController = null;
            _tutorialHallwayPlayerTransform = null;
            _tutorialHallwayHasLastPlayerPosition = false;
            _tutorialHallwayPlayerSpeedFloorActive = false;

            if (_tutorialHallwayStretchInstance.isValid())
            {
                _tutorialHallwayStretchInstance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _tutorialHallwayStretchInstance.release();
                _tutorialHallwayStretchInstance = default;
                LogAudioState("Tutorial hallway stretch stopped.");
            }
        }

        private void StopAndReleaseUiHover()
        {
            if (_uiHoverInstance.isValid())
            {
                _uiHoverInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _uiHoverInstance.release();
                _uiHoverInstance = default;
            }
        }
        #endregion

        #region Global Utilities

        private void LogAudioState(string message)
        {
            if (!debugAudioLogs)
            {
                return;
            }

            Debug.Log($"AudioManager: {message}");
        }

        private EventReference GetSfxEvent(SfxId sfxId)
        {
            // Lazy rebuild in case the inspector list changes at runtime.
            if (_sfxMap == null || _sfxMap.Count == 0)
            {
                BuildSfxMap();
            }

            return _sfxMap != null && _sfxMap.TryGetValue(sfxId, out EventReference evt) ? evt : default;
        }

        private void BuildSfxMap()
        {
            // Build the lookup table once from serialized entries.
            _sfxMap = new Dictionary<SfxId, EventReference>();
            if (sfxEvents == null)
            {
                return;
            }

            foreach (var entry in sfxEvents)
            {
                _sfxMap[entry.id] = entry.eventRef;
            }
        }

        private void CachePlayerMovementBus()
        {
            if (string.IsNullOrWhiteSpace(playerMovementBusPath))
            {
                return;
            }

            if (!_playerMovementBus.isValid())
            {
                _playerMovementBus = RuntimeManager.GetBus(playerMovementBusPath);
            }
        }

        private void CacheSettingsBuses()
        {
            if (!_masterBus.isValid() && !string.IsNullOrWhiteSpace(masterBusPath))
            {
                _masterBus = RuntimeManager.GetBus(masterBusPath);
            }

            if (!_sfxBus.isValid() && !string.IsNullOrWhiteSpace(sfxBusPath))
            {
                _sfxBus = RuntimeManager.GetBus(sfxBusPath);
            }

            if (!_musicBus.isValid() && !string.IsNullOrWhiteSpace(musicBusPath))
            {
                _musicBus = RuntimeManager.GetBus(musicBusPath);
            }

            if (!_ambienceBus.isValid() && !string.IsNullOrWhiteSpace(ambienceBusPath))
            {
                _ambienceBus = RuntimeManager.GetBus(ambienceBusPath);
            }
        }

        private void SetBedroomWallClockActive(bool isActive)
        {
            if (_bedroomWallClockInstance.isValid() && !string.IsNullOrWhiteSpace(bedroomWallClockActiveParameter))
            {
                float requestedValue = isActive ? 1f : 0f;
                if (!_hasBedroomWallClockActiveParameterId)
                {
                    CacheBedroomWallClockActiveParameterId();
                }

                if (_hasBedroomWallClockActiveParameterId)
                {
                    _bedroomWallClockInstance.setParameterByID(_bedroomWallClockActiveParameterId, requestedValue);
                }
                else
                {
                    _bedroomWallClockInstance.setParameterByName(bedroomWallClockActiveParameter, requestedValue);
                }
            }
        }

        private void SetBedroomWallClockInspecting(bool isInspecting)
        {
            if (_bedroomWallClockInstance.isValid() && !string.IsNullOrWhiteSpace(bedroomWallClockInspectingParameter))
            {
                float requestedValue = isInspecting ? 1f : 0f;
                if (!_hasBedroomWallClockInspectingParameterId)
                {
                    CacheBedroomWallClockInspectingParameterId();
                }

                if (_hasBedroomWallClockInspectingParameterId)
                {
                    _bedroomWallClockInstance.setParameterByID(_bedroomWallClockInspectingParameterId, requestedValue);
                }
                else
                {
                    _bedroomWallClockInstance.setParameterByName(bedroomWallClockInspectingParameter, requestedValue);
                }

                Debug.Log($"AudioManager: Bedroom wall clock '{bedroomWallClockInspectingParameter}' -> {requestedValue:0} (state={(GameStateManager.Instance != null ? GameStateManager.Instance.GetCurrentGameState().ToString() : "null")})");
            }
        }

        private void CacheBedroomWallClockActiveParameterId()
        {
            _hasBedroomWallClockActiveParameterId = false;
            _bedroomWallClockActiveParameterId = default;

            if (!_bedroomWallClockInstance.isValid() || string.IsNullOrWhiteSpace(bedroomWallClockActiveParameter))
            {
                return;
            }

            FMOD.RESULT descriptionResult = _bedroomWallClockInstance.getDescription(out EventDescription description);
            if (descriptionResult != FMOD.RESULT.OK)
            {
                return;
            }

            FMOD.RESULT parameterDescriptionResult = description.getParameterDescriptionByName(
                bedroomWallClockActiveParameter,
                out PARAMETER_DESCRIPTION parameterDescription);

            if (parameterDescriptionResult != FMOD.RESULT.OK)
            {
                return;
            }

            _bedroomWallClockActiveParameterId = parameterDescription.id;
            _hasBedroomWallClockActiveParameterId = true;
        }

        private void CacheBedroomWallClockInspectingParameterId()
        {
            _hasBedroomWallClockInspectingParameterId = false;
            _bedroomWallClockInspectingParameterId = default;

            if (!_bedroomWallClockInstance.isValid() || string.IsNullOrWhiteSpace(bedroomWallClockInspectingParameter))
            {
                return;
            }

            FMOD.RESULT descriptionResult = _bedroomWallClockInstance.getDescription(out EventDescription description);
            if (descriptionResult != FMOD.RESULT.OK)
            {
                return;
            }

            FMOD.RESULT parameterDescriptionResult = description.getParameterDescriptionByName(
                bedroomWallClockInspectingParameter,
                out PARAMETER_DESCRIPTION parameterDescription);

            if (parameterDescriptionResult != FMOD.RESULT.OK)
            {
                return;
            }

            _bedroomWallClockInspectingParameterId = parameterDescription.id;
            _hasBedroomWallClockInspectingParameterId = true;
        }

        #endregion

        #region Tutorial Utilities

        private void AutoAttachLampAudioEmittersInScene(Scene scene)
        {
            if (!autoAttachLampAudioOnSceneLoad || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(lampAudioAutoAttachSceneName)
                && !string.Equals(scene.name, lampAudioAutoAttachSceneName, StringComparison.Ordinal))
            {
                return;
            }

            int attachedCount = 0;
            int configuredCount = 0;
            HashSet<Transform> configuredRoots = new HashSet<Transform>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Light[] lights = roots[i].GetComponentsInChildren<Light>(true);
                for (int j = 0; j < lights.Length; j++)
                {
                    Light light = lights[j];
                    if (!IsLampCandidate(light))
                    {
                        continue;
                    }

                    Transform emitterRoot = GetLampEmitterRoot(light.transform);
                    if (!configuredRoots.Add(emitterRoot))
                    {
                        continue;
                    }

                    LampAudioEmitter emitter = emitterRoot.GetComponent<LampAudioEmitter>();
                    if (emitter == null)
                    {
                        emitter = emitterRoot.gameObject.AddComponent<LampAudioEmitter>();
                        attachedCount++;
                    }

                    bool playBuzzOnLightOff = IsLikelyFlickeringLamp(emitterRoot, light);
                    emitter.Configure(light, playBuzzOnLightOff);
                    configuredCount++;
                }
            }

            LogAudioState($"Lamp audio auto-attach in scene '{scene.name}': configured={configuredCount}, newlyAdded={attachedCount}.");
        }

        private void AutoAttachTutorialOrbAudioEmittersInScene(Scene scene)
        {
            if (!autoAttachTutorialOrbAudioOnSceneLoad || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(tutorialOrbAudioSceneName)
                && !string.Equals(scene.name, tutorialOrbAudioSceneName, StringComparison.Ordinal))
            {
                return;
            }

            List<Transform> orbTargets = new List<Transform>();
            GameObject[] roots = scene.GetRootGameObjects();
            HashSet<Transform> uniqueOrbTargets = new HashSet<Transform>();
            bool hasRootFilter = !string.IsNullOrWhiteSpace(tutorialOrbRootName);

            if (hasRootFilter)
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                    for (int j = 0; j < transforms.Length; j++)
                    {
                        Transform candidateRoot = transforms[j];
                        if (!string.Equals(candidateRoot.name, tutorialOrbRootName, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        CollectTutorialOrbCandidatesInHierarchy(candidateRoot, uniqueOrbTargets, orbTargets);
                    }
                }
            }
            else
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    CollectTutorialOrbCandidatesInHierarchy(roots[i].transform, uniqueOrbTargets, orbTargets);
                }
            }

            // Fallback: if the configured root name was not found, scan the whole scene for Orb* objects.
            if (orbTargets.Count == 0 && hasRootFilter)
            {
                for (int i = 0; i < roots.Length; i++)
                {
                    CollectTutorialOrbCandidatesInHierarchy(roots[i].transform, uniqueOrbTargets, orbTargets);
                }
            }

            List<EventReference> assignments = BuildTutorialOrbEventAssignments(orbTargets.Count);
            if (orbTargets.Count == 0 || assignments.Count == 0)
            {
                if (orbTargets.Count > 0)
                {
                    LogAudioState("Tutorial orb audio auto-attach skipped: no valid tutorialOrbEvents configured.");
                }
                return;
            }

            int attachedCount = 0;
            int configuredCount = 0;
            for (int i = 0; i < orbTargets.Count; i++)
            {
                Transform orbTransform = orbTargets[i];
                TutorialOrbAudioEmitter emitter = orbTransform.GetComponent<TutorialOrbAudioEmitter>();
                if (emitter == null)
                {
                    emitter = orbTransform.gameObject.AddComponent<TutorialOrbAudioEmitter>();
                    attachedCount++;
                }

                emitter.Configure(assignments[i]);
                configuredCount++;
            }

            LogAudioState($"Tutorial orb audio auto-attach in scene '{scene.name}': configured={configuredCount}, newlyAdded={attachedCount}, events={assignments.Count}.");
        }

        private void AutoAttachSpiderAudioEmittersInScene(Scene scene)
        {
            if (!autoAttachSpiderAudioOnSceneLoad || !scene.IsValid() || !scene.isLoaded || spiderLoopEvent.IsNull)
            {
                return;
            }

            if (!SceneMatchesConfiguredList(scene.name, spiderAudioSceneNames))
            {
                return;
            }

            int attachedCount = 0;
            int configuredCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform candidate = transforms[j];
                    if (!IsSpiderAnchorCandidate(candidate))
                    {
                        continue;
                    }

                    SpiderAudioEmitter emitter = candidate.GetComponent<SpiderAudioEmitter>();
                    if (emitter == null)
                    {
                        emitter = candidate.gameObject.AddComponent<SpiderAudioEmitter>();
                        attachedCount++;
                    }

                    AttractorAI attractorAI = candidate.GetComponentInParent<AttractorAI>();
                    Attractor attractor = ResolveSpiderAttractor(candidate, attractorAI);
                    emitter.Configure(candidate, attractor, attractorAI);
                    configuredCount++;
                }
            }

            LogAudioState($"Spider audio auto-attach in scene '{scene.name}': configured={configuredCount}, newlyAdded={attachedCount}.");
        }

        private void AutoAttachNightmareRoofAudioEmittersInScene(Scene scene)
        {
            if (!autoAttachNightmareRoofAudioOnSceneLoad
                || !scene.IsValid()
                || !scene.isLoaded
                || nightmareRoofLoopEvent.IsNull)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(nightmareRoofAudioSceneName)
                && !string.Equals(scene.name, nightmareRoofAudioSceneName, StringComparison.Ordinal))
            {
                return;
            }

            Transform roofsRoot = string.IsNullOrWhiteSpace(nightmareRoofRootName)
                ? FindTransformInSceneByName(scene, nightmareRoofParentName)
                : FindTransformInRootHierarchy(scene, nightmareRoofRootName, nightmareRoofParentName);
            if (roofsRoot == null)
            {
                return;
            }

            List<Transform> roofCandidates = new List<Transform>();
            for (int i = 0; i < roofsRoot.childCount; i++)
            {
                Transform candidate = roofsRoot.GetChild(i);
                if (candidate != null)
                {
                    roofCandidates.Add(candidate);
                }
            }

            List<Transform> selectedRoofs = SelectSpacedTransforms(
                roofCandidates,
                Mathf.Max(0, nightmareRoofEmitterTargetCount),
                Mathf.Max(0f, nightmareRoofMinimumSpacing));
            if (selectedRoofs.Count == 0)
            {
                return;
            }

            int attachedCount = 0;
            int configuredCount = 0;
            for (int i = 0; i < selectedRoofs.Count; i++)
            {
                Transform roofTransform = selectedRoofs[i];
                RoofRandomEmitter emitter = roofTransform.GetComponent<RoofRandomEmitter>();
                if (emitter == null)
                {
                    emitter = roofTransform.gameObject.AddComponent<RoofRandomEmitter>();
                    attachedCount++;
                }

                emitter.Configure(nightmareRoofLoopEvent);
                configuredCount++;
            }

            LogAudioState($"Nightmare roof audio auto-attach in scene '{scene.name}': configured={configuredCount}, newlyAdded={attachedCount}.");
        }

        private static Transform FindTransformInSceneByName(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindTransformInHierarchyByName(roots[i].transform, objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform FindTransformInRootHierarchy(Scene scene, string rootObjectName, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (!string.Equals(roots[i].name, rootObjectName, StringComparison.Ordinal))
                {
                    continue;
                }

                return FindTransformInHierarchyByName(roots[i].transform, objectName);
            }

            return null;
        }

        private static Transform FindTransformInHierarchyByName(Transform root, string objectName)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, objectName, StringComparison.Ordinal))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static List<Transform> SelectSpacedTransforms(List<Transform> candidates, int targetCount, float minimumSpacing)
        {
            List<Transform> selected = new List<Transform>();
            if (candidates == null || candidates.Count == 0 || targetCount <= 0)
            {
                return selected;
            }

            if (minimumSpacing <= 0f)
            {
                int takeCount = Mathf.Min(targetCount, candidates.Count);
                for (int i = 0; i < takeCount; i++)
                {
                    selected.Add(candidates[i]);
                }

                return selected;
            }

            List<Transform> shuffledCandidates = new List<Transform>(candidates);
            for (int i = shuffledCandidates.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                Transform temp = shuffledCandidates[i];
                shuffledCandidates[i] = shuffledCandidates[swapIndex];
                shuffledCandidates[swapIndex] = temp;
            }

            float minimumSpacingSqr = minimumSpacing * minimumSpacing;
            for (int i = 0; i < shuffledCandidates.Count && selected.Count < targetCount; i++)
            {
                Transform candidate = shuffledCandidates[i];
                bool isFarEnoughFromExisting = true;
                for (int j = 0; j < selected.Count; j++)
                {
                    if ((candidate.position - selected[j].position).sqrMagnitude < minimumSpacingSqr)
                    {
                        isFarEnoughFromExisting = false;
                        break;
                    }
                }

                if (isFarEnoughFromExisting)
                {
                    selected.Add(candidate);
                }
            }

            return selected;
        }

        private void CollectTutorialOrbCandidatesInHierarchy(
            Transform searchRoot,
            HashSet<Transform> uniqueOrbTargets,
            List<Transform> orbTargets)
        {
            if (searchRoot == null)
            {
                return;
            }

            Transform[] transforms = searchRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (!IsTutorialOrbCandidate(candidate) || !uniqueOrbTargets.Add(candidate))
                {
                    continue;
                }

                orbTargets.Add(candidate);
            }
        }

        private bool IsTutorialOrbCandidate(Transform candidate)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(tutorialOrbNamePrefix))
            {
                return false;
            }

            return candidate.name.StartsWith(tutorialOrbNamePrefix, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSpiderAnchorCandidate(Transform candidate)
        {
            return candidate != null
                   && !string.IsNullOrWhiteSpace(spiderAnchorName)
                   && string.Equals(candidate.name, spiderAnchorName, StringComparison.Ordinal);
        }

        private static Attractor ResolveSpiderAttractor(Transform spiderAnchor, AttractorAI attractorAI)
        {
            if (spiderAnchor == null)
            {
                return null;
            }

            Attractor attractor = spiderAnchor.GetComponentInParent<Attractor>();
            if (attractor != null)
            {
                return attractor;
            }

            Transform searchRoot = attractorAI != null ? attractorAI.transform : spiderAnchor.parent;
            if (searchRoot == null)
            {
                return null;
            }

            Attractor[] attractors = searchRoot.GetComponentsInChildren<Attractor>(true);
            for (int i = 0; i < attractors.Length; i++)
            {
                if (attractors[i] != null && attractors[i].attractorType == AttractorAI.AttractorType.self)
                {
                    return attractors[i];
                }
            }

            return attractors.Length > 0 ? attractors[0] : null;
        }

        private static bool SceneMatchesConfiguredList(string sceneName, string[] configuredSceneNames)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            if (configuredSceneNames == null || configuredSceneNames.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < configuredSceneNames.Length; i++)
            {
                if (string.Equals(sceneName, configuredSceneNames[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private List<EventReference> BuildTutorialOrbEventAssignments(int targetCount)
        {
            List<EventReference> assignments = new List<EventReference>(Mathf.Max(0, targetCount));
            if (targetCount <= 0)
            {
                return assignments;
            }

            List<EventReference> validEvents = GetValidTutorialOrbEvents();
            if (validEvents.Count == 0)
            {
                return assignments;
            }

            while (assignments.Count < targetCount)
            {
                ShuffleEventReferences(validEvents);
                int remaining = targetCount - assignments.Count;
                int takeCount = Mathf.Min(remaining, validEvents.Count);
                for (int i = 0; i < takeCount; i++)
                {
                    assignments.Add(validEvents[i]);
                }
            }
            return assignments;
        }

        private List<EventReference> GetValidTutorialOrbEvents()
        {
            List<EventReference> validEvents = new List<EventReference>();
            if (tutorialOrbEvents == null)
            {
                return validEvents;
            }

            for (int i = 0; i < tutorialOrbEvents.Length; i++)
            {
                if (!tutorialOrbEvents[i].IsNull)
                {
                    validEvents.Add(tutorialOrbEvents[i]);
                }
            }

            return validEvents;
        }

        private static void ShuffleEventReferences(List<EventReference> events)
        {
            for (int i = events.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                EventReference temp = events[i];
                events[i] = events[swapIndex];
                events[swapIndex] = temp;
            }
        }

        private void SetSpiderLoopParameters(
            EventInstance instance,
            float rawIntensity,
            float rawDangerLevel,
            int stateValue)
        {
            if (!instance.isValid())
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(spiderIntensityParameter))
            {
                instance.setParameterByName(spiderIntensityParameter, NormalizeSpiderValue(rawIntensity, spiderIntensityMax));
            }

            if (!string.IsNullOrWhiteSpace(spiderDangerParameter))
            {
                instance.setParameterByName(spiderDangerParameter, NormalizeSpiderValue(rawDangerLevel, spiderDangerMax));
            }

            if (!string.IsNullOrWhiteSpace(spiderStateParameter))
            {
                instance.setParameterByName(spiderStateParameter, stateValue);
            }
        }

        private static float NormalizeSpiderValue(float rawValue, float maxValue)
        {
            return Mathf.Clamp01(rawValue / Mathf.Max(0.01f, maxValue));
        }

        private static bool IsLampCandidate(Light light)
        {
            if (light == null)
            {
                return false;
            }

            Transform current = light.transform;
            while (current != null)
            {
                if (current.name.IndexOf("lamp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        private static Transform GetLampEmitterRoot(Transform lightTransform)
        {
            Transform bestMatch = null;
            Transform current = lightTransform;
            while (current != null)
            {
                if (current.name.IndexOf("lamp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestMatch = current;
                }
                current = current.parent;
            }

            return bestMatch != null ? bestMatch : lightTransform;
        }

        private static bool IsLikelyFlickeringLamp(Transform emitterRoot, Light light)
        {
            Animator animator = emitterRoot != null ? emitterRoot.GetComponent<Animator>() : null;
            if (animator == null && light != null)
            {
                animator = light.GetComponentInParent<Animator>();
            }

            return animator != null && animator.enabled && animator.runtimeAnimatorController != null;
        }

        #endregion
        #endregion
    }
}
