using System;
using System.Collections;
using System.Collections.Generic;
using TheElevator.Generation;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheElevator.Office;

namespace TheElevator
{
    public sealed class DescentGame : MonoBehaviour
    {
        public enum RunPhase { Briefing, Exploring, Closing, Transit, Won, Lost, Generating }
        [Header("Map generation")]
        public bool UseProceduralFloors = true;
        public MapSize MapScale = MapSize.Small;
        public FloorGenerationProfile ProfileOverride;
        public FloorContentCatalog ContentCatalog;
        public bool UseManualSeed = false;
        public int ManualSeed = 104729;
        public GeneratedFloor CurrentMap { get; private set; }
        public OfficeFloor CurrentOffice { get { return CurrentMap ? CurrentMap.GetComponent<OfficeFloor>() : null; } }
        public float GenerationProgress { get; private set; }
        public int ActiveSeed { get; private set; }
        public readonly List<Custodian> Guards = new List<Custodian>();
        public RunPhase Phase { get; private set; }
        public WorkerController Player { get; private set; }
        public PrototypeSound Sound { get; private set; }
        public Custodian Guard { get; set; }
        public readonly List<SalvageItem> Items = new List<SalvageItem>();
        public int FloorIndex { get; private set; }
        public float Power { get; private set; } = RunRules.StartingPower;
        public float Clock { get; private set; } = RunRules.FloorSeconds;
        public float ClosingProgress { get; private set; }
        public float Load { get; private set; }
        public int CargoValue { get; private set; }
        public bool Paused { get; private set; }
        public string Outcome { get; private set; }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        public bool ControlsActive { get { return !Paused && (Phase == RunPhase.Exploring || Phase == RunPhase.Closing); } }
        Workshop workshop;
        FacilityBuilder builder;
        Transform floor;
        float doorClosed = 1, transitClock;
        int seed, lastAlarm = -1;
        int destinationFloor = 2;
        readonly HashSet<int> completedFloors = new HashSet<int>();
        public int HighestUnlocked { get; private set; } = 1;
        int retainedBadgeLevel=1, retainedBadgeDepartment=-1;

        void Awake()
        {
            Time.timeScale = 1;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 45;
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            // Builds render at the monitor's native resolution; a remembered small window would be upscaled and blurry.
            if (!Application.isEditor && !Application.isBatchMode && (Screen.width < Screen.currentResolution.width || Screen.height < Screen.currentResolution.height))
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f,.52f,.50f);
            RenderSettings.fog = false;
            RenderSettings.fogColor = new Color(0.085f, 0.14f, 0.15f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            seed = UseManualSeed ? ManualSeed : Guid.NewGuid().GetHashCode();
            if (!ContentCatalog) ContentCatalog = Resources.Load<FloorContentCatalog>("Generation/MorrowOffice");
            workshop = new Workshop();
            builder = new FacilityBuilder(this, workshop);
            Light fill = new GameObject("Facility fill").AddComponent<Light>();
            fill.transform.SetParent(transform);
            fill.type = LightType.Directional;
            fill.color = new Color(1,.92f,.80f);
            fill.intensity = .55f;
            fill.shadows = LightShadows.Soft;
            fill.transform.rotation = Quaternion.Euler(55, -30, 0);
            builder.Cabin(transform);
            Player = new GameObject("Player").AddComponent<WorkerController>();
            Player.transform.SetParent(transform);
            Player.Initialize(this, workshop);
            Sound = gameObject.AddComponent<PrototypeSound>();
            Sound.Initialize();
            gameObject.AddComponent<DescentHUD>().Initialize(this);
            gameObject.AddComponent<GenerationDebugPanel>().Initialize(this);
            if (UseProceduralFloors) StartCoroutine(GenerateFloor(true, null));
            else { floor = builder.Floor(0, seed); Phase = RunPhase.Briefing; }
            SetCursor(false);
            RecountCargo();
        }

        public static bool InCabin(Vector3 point)
        {
            return point.x > -4.25f && point.x < 4.25f && point.z > -9.8f && point.z < -2.65f
                && point.y > -0.5f && point.y < 3.7f;
        }

        public void Begin()
        {
            if (Phase != RunPhase.Briefing) return;
            Phase = RunPhase.Exploring;
            SetCursor(true);
            Notify(CurrentOffice ? "Morrow Systems. The access card is on the reception counter. Read the field notebook in the lift." : "Collect valuables. Bring a power cell back and press F to connect it.");
            Sound.Play(660, 0.22f, 0.13f);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !Player.ReadingNotebook && Phase != RunPhase.Generating && Phase != RunPhase.Briefing && Phase != RunPhase.Won && Phase != RunPhase.Lost)
                SetPaused(!Paused);
            if (Paused || Phase == RunPhase.Briefing || Phase == RunPhase.Won || Phase == RunPhase.Lost) return;
            RecountCargo();
            UpdateFloorUnlock();
            builder.Display.text = "FLOOR " + (FloorIndex + 1).ToString("00");
            if (Phase == RunPhase.Exploring)
            {
                Clock = Mathf.Max(0, Clock - Time.deltaTime);
                doorClosed = Mathf.MoveTowards(doorClosed, 0, Time.deltaTime * 0.8f);
                if (Clock <= 20 && Mathf.CeilToInt(Clock) != lastAlarm)
                {
                    lastAlarm = Mathf.CeilToInt(Clock);
                    Sound.Play(520, 0.11f, 0.12f);
                }
                if (Clock <= 0) { destinationFloor=Mathf.Min(50,FloorIndex+2);StartClosing(); }
            }
            else if (Phase == RunPhase.Closing)
            {
                ClosingProgress = Mathf.Clamp01(ClosingProgress + Time.deltaTime / RunRules.DoorSeconds(Load));
                doorClosed = ClosingProgress;
                if (ClosingProgress >= 1) Depart();
            }
            else if (Phase == RunPhase.Transit)
            {
                doorClosed = 1;
                transitClock -= Time.deltaTime;
                if (transitClock <= 0) Arrive();
            }
            builder.SetDoors(doorClosed);
        }

        public MapRecipe CreateRecipe(int requestedSeed)
        {
            FloorGenerationProfile profile = ProfileOverride ? ProfileOverride : Resources.Load<FloorGenerationProfile>("Generation/" + MapScale);
            MapRecipe recipe = MapRecipe.Default(requestedSeed, MapScale);
            if (profile) recipe.Settings = JsonUtility.FromJson<GenerationSettings>(JsonUtility.ToJson(profile.Settings));
            if (ContentCatalog)
            {
                recipe.ThemeId = ContentCatalog.ThemeId; recipe.ThemeRevision = ContentCatalog.ThemeRevision;
                // Snapshot all settings/content; later inspector edits cannot mutate a running manifest.
                recipe.Modules = ContentCatalog.Modules;
                recipe.ThemePayload = ContentCatalog.ThemePayload;
                recipe.Districts = ContentCatalog.Districts;
                recipe = JsonUtility.FromJson<MapRecipe>(JsonUtility.ToJson(recipe));
            }
            return recipe;
        }

        IEnumerator GenerateFloor(bool briefing, GenerationRecord replay)
        {
            System.Diagnostics.Stopwatch generationTimer=System.Diagnostics.Stopwatch.StartNew();
            Phase = RunPhase.Generating; GenerationProgress = 0; doorClosed = 1; builder.SetDoors(1);
            MapRecipe recipe = replay != null ? replay.Recipe : CreateRecipe(unchecked(seed + FloorIndex * 104729));
            ActiveSeed = recipe.Seed;
            MapManifest manifest = null;
            string failure = null;
            try
            {
                if (ContentCatalog && (recipe.ThemeId != ContentCatalog.ThemeId || recipe.ThemeRevision != ContentCatalog.ThemeRevision))
                    throw new InvalidOperationException("Saved floor uses a different content catalog revision.");
                manifest = new MacroLayoutGenerator().Generate(recipe);
                if (replay != null && !replay.Matches(manifest)) throw new InvalidOperationException("Replay fingerprint mismatch. Generation aborted.");
            }
            catch (Exception e) { failure = e.Message; }
            if (failure != null)
            {
                try { GenerationRecordStore.LogFailure(recipe, failure); } catch (Exception e) { Debug.LogWarning(e.Message); }
                Debug.LogError("Floor generation failed: " + failure); Finish(false, failure); yield break;
            }
            Transform generated = workshop.Group("Generated facility / seed " + recipe.Seed, transform, Vector3.zero);
            generated.gameObject.SetActive(false);
            CurrentMap = generated.gameObject.AddComponent<GeneratedFloor>(); CurrentMap.Initialize(manifest);
            floor = generated;
            IEnumerator construction = new FloorGeometryBuilder(workshop, ContentCatalog).Build(CurrentMap, value => GenerationProgress = value, PopulateSocket);
            // Coroutine exceptions must not strand the lift in Generating forever.
            while (true)
            {
                bool more = false; object yielded = null;
                try { more = construction.MoveNext(); if (more) yielded = construction.Current; }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null)
                {
                    try { GenerationRecordStore.LogFailure(recipe, failure); } catch (Exception e) { Debug.LogWarning(e.Message); }
                    Destroy(generated.gameObject); CurrentMap = null;
                    Debug.LogError(failure); Finish(false, "Geometry construction failed. See the generation log."); yield break;
                }
                if (!more) break;
                yield return yielded;
            }
            if (CurrentOffice)
            {
                try { CurrentOffice.Assemble(this); if(retainedBadgeLevel>1)CurrentOffice.GiveBadge(retainedBadgeLevel,retainedBadgeDepartment); }
                catch (Exception e) { failure = e.ToString(); }
                if(failure!=null){Debug.LogError(failure);Finish(false,"Office validation failed; see log.");yield break;}
            }
            generated.gameObject.SetActive(true);
            if(CurrentOffice)CurrentOffice.GenerationMilliseconds=generationTimer.ElapsedMilliseconds;
            Physics.SyncTransforms();
            Clock = recipe.Settings.ExplorationSeconds;
            ClosingProgress = 0; lastAlarm = -1;
            Phase = briefing ? RunPhase.Briefing : RunPhase.Exploring;
            Player.Recover();
            try { GenerationRecordStore.Save(manifest); } catch (Exception e) { Debug.LogWarning("Could not save generation record: " + e.Message); }
            Debug.Log("MAP READY seed=" + recipe.Seed + " profile=" + recipe.Settings.ProfileId + " version=" + recipe.GenerationVersion
                + " rooms=" + manifest.Rooms.Count + " hash=" + manifest.StructureHash);
            if (!briefing) { SetCursor(true); Notify("Floor ready. " + recipe.Settings.ObjectiveCount + " remote survey terminals await. Remember your route home."); }
        }

        void PopulateSocket(GenerationSocket socket)
        {
            if (CurrentOffice && (socket.Kind==SocketKind.Enemy || socket.Kind==SocketKind.Reward)) return;
            if (socket.Kind == SocketKind.Supply || socket.Kind == SocketKind.Reward)
            {
                string kind = socket.Kind == SocketKind.Supply ? "battery" : (socket.ContentSeed & 3) == 0 ? "safe" : (socket.ContentSeed & 3) == 1 ? "artifact" : "case";
                builder.Item(floor, floor.InverseTransformPoint(socket.transform.position + Vector3.up), kind, FloorIndex);
            }
            else if (socket.Kind == SocketKind.Enemy)
            {
                Transform root = workshop.Group("Custodian / " + socket.StableId, floor, floor.InverseTransformPoint(socket.transform.position + Vector3.up * 0.1f));
                Custodian guard = root.gameObject.AddComponent<Custodian>(); guard.Initialize(this, workshop, true); Guards.Add(guard);
            }
            // Hazard, event and restricted-zone sockets are extension points; nothing is placed on the floor for them.
        }

        public void RegenerateMap(int requestedSeed, MapSize size, bool replayPrevious)
        {
            if (!UseProceduralFloors || Phase == RunPhase.Generating || Phase == RunPhase.Closing || Phase == RunPhase.Transit) return;
            if (!Player.InCabin) { Notify("Return to the elevator before regenerating."); return; }
            GenerationRecord record = null;
            if (replayPrevious)
            {
                try { record = GenerationRecordStore.Load(); }
                catch (Exception e) { Notify(e.Message); return; }
            }
            bool briefing = Phase == RunPhase.Briefing;
            Player.Drop(false);
            foreach (SalvageItem item in new List<SalvageItem>(Items)) if (IsCargo(item)) item.transform.SetParent(transform, true);
            if (floor) { floor.gameObject.SetActive(false); Destroy(floor.gameObject); }
            Guards.Clear(); Guard = null; CurrentMap = null;
            seed = unchecked(requestedSeed - FloorIndex * 104729); MapScale = size;
            if (Paused) SetPaused(false);
            StartCoroutine(GenerateFloor(briefing, record));
        }

        bool IsCargo(SalvageItem item)
        {
            return item && (InCabin(item.transform.position) || (item.IsHeld && Player.InCabin));
        }

        public void RecountCargo()
        {
            Load = Player && Player.InCabin ? RunRules.WorkerMass : 0;
            CargoValue = 0;
            foreach (SalvageItem item in Items)
            {
                if (!IsCargo(item)) continue;
                Load += item.Mass;
                CargoValue += item.Value;
            }
        }

        void UpdateFloorUnlock()
        {
            if(CurrentOffice && CurrentOffice.RequiredRecovered){completedFloors.Add(FloorIndex+1);HighestUnlocked=Mathf.Max(HighestUnlocked,Mathf.Min(50,FloorIndex+2));}
        }
        public bool CanSelectFloor(int number)
        {
            UpdateFloorUnlock();
            return number>=1 && number<=HighestUnlocked && number!=FloorIndex+1 && Phase==RunPhase.Exploring && Player && Player.InCabin && (number<FloorIndex+1 || completedFloors.Contains(FloorIndex+1) || !CurrentOffice || CurrentOffice.RequiredRecovered);
        }
        public void SelectFloor(int number)
        {
            if(!CanSelectFloor(number))return;
            RecountCargo();if(!RunRules.CanDepart(Power,Load)){Notify("Lift power insufficient. Connect a battery or unload cargo.");return;}
            destinationFloor=number;StartClosing();
        }
        public void RequestDeparture()
        {
            if (Phase != RunPhase.Exploring || !Player.InCabin) return;
            if (CurrentOffice && !CurrentOffice.RequiredRecovered) { Notify("Contract incomplete. The mandatory office asset must be inside the lift."); return; }
            RecountCargo();
            if (!RunRules.CanDepart(Power, Load))
            {
                Notify("Not enough power. Connect a cell with F, or unload cargo.");
                Sound.Play(120, 0.18f, 0.12f);
                return;
            }
            destinationFloor=Mathf.Min(50,FloorIndex+2);
            StartClosing();
        }

        void StartClosing()
        {
            Phase = RunPhase.Closing;
            ClosingProgress = 0;
            Notify("DOORS CLOSING. Get all the way inside the elevator.");
            Sound.Play(330, 0.4f, 0.18f);
        }

        void Depart()
        {
            if (!Player.InCabin) { Finish(false, "The elevator departed without you. Attendance is mandatory."); return; }
            if (destinationFloor>FloorIndex+1 && !completedFloors.Contains(FloorIndex+1) && CurrentOffice && !CurrentOffice.RequiredRecovered) { Finish(false,"Contract failed: optional cargo cannot replace the mandatory asset."); return; }
            if (CurrentOffice)
            {
                CurrentOffice.Transported = null;
                retainedBadgeLevel=CurrentOffice.Plan.Config.BadgesExpireOnDeparture?1:CurrentOffice.BadgeLevel;
                retainedBadgeDepartment=CurrentOffice.Plan.Config.BadgesExpireOnDeparture?-1:CurrentOffice.BadgeDepartment;
            }
            RecountCargo();
            if (!RunRules.CanDepart(Power, Load)) { Finish(false, "Power failure. Carry fewer valuables or connect more batteries."); return; }
            Power -= RunRules.DepartureCost(Load);
            if (Player.Held)
            {
                SalvageItem held = Player.Held;
                Player.Drop(false);
                Vector3 position = held.transform.position;
                position.x = Mathf.Clamp(position.x, -3.5f, 3.5f);
                position.z = Mathf.Clamp(position.z, -9, -3.5f);
                held.transform.position = position;
                held.Body.linearVelocity = Vector3.zero;
            }
            // Reparent recovered objects BEFORE destroying the departing floor.
            foreach (SalvageItem item in new List<SalvageItem>(Items))
            {
                if (!IsCargo(item)) continue;
                item.transform.SetParent(transform, true);
                item.Body.linearVelocity = Vector3.zero;
                item.Body.angularVelocity = Vector3.zero;
            }
            Guard = null;
            Guards.Clear();
            floor.gameObject.SetActive(false);
            Destroy(floor.gameObject);
            Phase = RunPhase.Transit;
            transitClock = 3.5f;
            Sound.Play(90, 0.8f, 0.12f);
            Notify("Descending. Please keep all regrets inside the cabin.");
        }

        void Arrive()
        {
            FloorIndex = Mathf.Clamp(destinationFloor - 1,0,49);
            if (UseProceduralFloors) { CurrentMap = null; StartCoroutine(GenerateFloor(false, null)); return; }
            floor = builder.Floor(FloorIndex, seed);
            Clock = RunRules.FloorSeconds;
            ClosingProgress = 0;
            lastAlarm = -1;
            Player.Recover();
            Phase = RunPhase.Exploring;
            Notify(FloorIndex == 1 ? "Flooded laboratory. Mind the specimen tanks."
                : "Human Resources. Walk quietly. The custodian hears sprinting and impacts.");
            Sound.Play(740, 0.25f, 0.13f);
        }

        public void UseBattery()
        {
            if (!ControlsActive || !Player.InCabin || !Player.Held || !Player.Held.IsBattery) return;
            if (Power >= 99.9f) { Notify("Battery already full. Save that cell for the next stop."); return; }
            Power = RunRules.Recharge(Power);
            Items.Remove(Player.Held);
            Player.ConsumeHeld();
            RecountCargo();
            Notify("Cell connected. Lift power: " + Mathf.CeilToInt(Power) + "%.");
            Sound.Play(880, 0.2f, 0.14f);
        }

        public void NoiseAt(Vector3 position, float strength)
        {
            if (CurrentOffice) CurrentOffice.HearNoise(position,strength);
            if (Guard) Guard.Hear(position, strength);
            foreach (Custodian guard in Guards) if (guard) guard.Hear(position, strength);
        }
        public void Notify(string text) { Notice = text; NoticeUntil = Time.unscaledTime + 5.5f; }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0 : 1;
            SetCursor(!paused);
        }

        public void Finish(bool success, string reason)
        {
            if (Phase == RunPhase.Won || Phase == RunPhase.Lost) return;
            Phase = success ? RunPhase.Won : RunPhase.Lost;
            Outcome = reason;
            Time.timeScale = 0;
            SetCursor(false);
        }

        public void Restart()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Quit()
        {
            Time.timeScale = 1;
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        void SetCursor(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
        void OnApplicationFocus(bool focus)
        {
            if (!focus && ControlsActive) SetPaused(true);
        }
        void OnDestroy()
        {
            Time.timeScale = 1;
            SetCursor(false);
            if (Player && Player.View) Destroy(Player.View.gameObject);
            if (workshop != null) workshop.Dispose();
        }
    }
}
