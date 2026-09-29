using UnityEngine;
using TheElevator.Generation;

namespace TheElevator
{
    public sealed class WorkerController : MonoBehaviour
    {
        public WorkerModel Model { get; private set; }
        public SalvageItem Held { get; private set; }
        public SalvageItem Target { get; private set; }
        public Camera View { get; private set; }
        // Keep unlimited until the user requests normal stamina again.
        [SerializeField] bool infiniteStamina = true;
        float stamina = 1f;
        public float Stamina { get { return infiniteStamina ? 1f : stamina; } private set { stamina = value; } }

        public bool InCabin { get { return DescentGame.InCabin(transform.position); } }
        public string Prompt { get; private set; }
        public bool MovingFast { get; private set; }
        public bool ReadingNotebook { get; set; }
        public bool PreviewAvatar { get; set; }
        public bool CanInteract { get; private set; }
        public float MotionSpeed { get { return motor?new Vector2(motor.velocity.x,motor.velocity.z).magnitude:0; } }
        ElevatorButton elevatorButton;
        FieldNotebook notebook;
        public bool Crouched { get; private set; }
        public bool FirstPerson { get { return firstPerson; } }
        public FirstPersonHands Hands { get; private set; }
        public float ThrowCharge { get; private set; }
        public bool ChargingThrow { get; private set; }
        // Intended walking velocity this frame (what the player is steering, before collisions).
        public Vector3 Motion { get; private set; }
        public TheElevator.Office.OfficeSeat Seat { get; private set; }
        // Extinguisher foam on the face (0 clear, 1 covered). It smears your view and wears off over a few seconds.
        public float FaceFoam { get; private set; }
        // Health: coworkers who snap can hurt you. At zero you are out: you spectate a teammate who is still
        // standing, or, with nobody left, the shift ends.
        public const float MaxHealth = 100;
        public float Health { get; private set; } = MaxHealth;
        public bool Down { get; private set; }
        public WorkerController Spectating { get; private set; }
        float hurtFlash;
        static Texture2D foamBlot;
        // Seated view: a little lower than standing, so sitting reads as sitting.
        public const float SeatedEyeHeight=1.22f;
        TheElevator.Office.OfficeSeat seatTarget;
        TheElevator.Office.OfficeTaskPoint seatStation;
        Vector3 standPosition;
        Quaternion standRotation;
        float sitBlend;
        bool standingUp;
        // Camera sits at the avatar's own eye line, the same height as every NPC's eyes.
        public const float EyeHeight=1.34f, CrouchEyeHeight=.9f;
        float eyeHeight=EyeHeight;
        DescentGame game;
        CharacterController motor;
        float yaw, pitch = 0f, vertical, damageCooldown, lastStep;
        bool firstPerson = true;
        ObjectiveTerminal terminal;
        Light flashlight;
        bool flashlightOn = false;
        Vector3 knockback;

        public void Initialize(DescentGame owner, Workshop workshop)
        {
            game = owner;
            gameObject.layer = 2; // Camera / interaction rays ignore the worker collider.
            motor = gameObject.AddComponent<CharacterController>();
            motor.height = 1.9f;
            motor.radius = 0.38f;
            motor.center = new Vector3(0, 0.95f, 0);
            motor.stepOffset = 0.28f;
            Model = new GameObject("Employee 004").AddComponent<WorkerModel>();
            Model.transform.SetParent(transform, false);
            Model.Build(workshop, 0);
            Hands = new GameObject("First person hands").AddComponent<FirstPersonHands>();
            Hands.Initialize(this);
            View = new GameObject("Worker camera").AddComponent<Camera>();
            View.tag = "MainCamera";
            View.nearClipPlane = 0.08f;
            View.farClipPlane = 100;
            View.fieldOfView = 60;
            View.backgroundColor = Workshop.Ink;
            View.clearFlags = CameraClearFlags.SolidColor;
            View.gameObject.AddComponent<AudioListener>();
            flashlight = View.gameObject.AddComponent<Light>();
            flashlight.type = LightType.Spot; flashlight.range = 24; flashlight.spotAngle = 72;
            flashlight.intensity = .6f; flashlight.color = new Color(1,0.94f,0.80f); flashlight.shadows = LightShadows.Soft;
            Teleport(new Vector3(0, 0.08f, -6.5f));
        }

        public void Teleport(Vector3 position)
        {
            if (Seat) LeaveSeat();
            motor.enabled = false;
            transform.position = position;
            motor.enabled = true;
            vertical = 0;
            knockback = Vector3.zero;
        }

        void Update()
        {
            if (!game || !game.ControlsActive)
            {
                ChargingThrow=false;ThrowCharge=0;
                if(game&&!game.Paused&&game.Phase==DescentGame.RunPhase.Transit)
                {yaw+=Input.GetAxisRaw("Mouse X")*2.1f;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.8f,-35,65);}
                return;
            }
            if(ReadingNotebook||Down)return;
            damageCooldown -= Time.deltaTime;
            FaceFoam = Mathf.MoveTowards(FaceFoam, 0, Time.deltaTime / 7f);
            yaw += Input.GetAxisRaw("Mouse X") * 2.1f;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 1.8f, -65, 75);
            if (Seat) { UpdateSeated(); return; }
            if (Input.GetKeyDown(KeyCode.L)) flashlightOn = !flashlightOn;
            if (Input.GetKeyDown(KeyCode.V) && (Application.isEditor || Debug.isDebugBuild))
            {
                firstPerson = !firstPerson;
                Model.SetView(firstPerson,Crouched);
            }
            float horizontal = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(horizontal, 0, forward), 1);
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * input;
            FireExtinguisher extinguisher = Held ? Held.GetComponent<FireExtinguisher>() : null;
            SetCrouched(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C));
            MovingFast = !Crouched && Input.GetKey(KeyCode.LeftShift) && Stamina > 0.08f && input.sqrMagnitude > 0.1f;
            Stamina = infiniteStamina ? 1f : Mathf.Clamp01(Stamina + Time.deltaTime * (MovingFast ? -0.23f : 0.17f));
            float speed = Crouched ? 1.85f : MovingFast ? 6.8f : 4.1f;
            if (Held) speed *= RunRules.CarrySpeed(Held.Mass);
            if (game.CurrentOffice && game.CurrentOffice.Transported) speed *= 0.48f;
            bool wet = game.CurrentMap ? game.CurrentMap.IsWet(transform.position) : game.FloorIndex == 1 && transform.position.z > 3f;
            if (wet) speed *= 0.72f;
            if (firstPerson || Held) transform.rotation = Quaternion.Euler(0, yaw, 0);
            else if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 14);
            if (motor.isGrounded)
            {
                vertical = -2f;
                if (!Crouched && Input.GetKeyDown(KeyCode.Space) && Stamina > 0.15f)
                {
                    vertical = Held ? 4.5f : 6f;
                    if (!infiniteStamina) Stamina = Mathf.Max(0, Stamina - 0.12f);
                }
            }
            vertical -= 18f * Time.deltaTime;
            Motion = direction * speed;
            motor.Move((direction * speed + Vector3.up * vertical + knockback) * Time.deltaTime);
            knockback = Vector3.Lerp(knockback, Vector3.zero, Time.deltaTime * 5);
            Model.Animate(direction.magnitude * speed, Held, Time.deltaTime);
            if (direction.sqrMagnitude > 0.1f && Time.time - lastStep > (MovingFast ? 0.3f : 0.46f))
            {
                lastStep = Time.time;
                game.Sound.Play(wet && !InCabin ? 160 : 85, 0.035f, 0.035f);
                if (MovingFast) game.NoiseAt(transform.position, 0.8f);
            }
            UpdateTarget();
            // With a usable item in hand the mouse button uses it; E still interacts.
            if (Input.GetKeyDown(KeyCode.E) || (CanInteract && !extinguisher && Input.GetMouseButtonDown(0)))
            {
                if(elevatorButton){elevatorButton.Press();}
                else if(notebook){notebook.Open();}
                else if (game.CurrentOffice && game.CurrentOffice.InteractPressed()) { }
                else if (Held) Drop(false);
                else if (terminal) terminal.Use(game);
                else if (Target) PickUp(Target);
                else if (seatTarget) Sit(seatTarget);
            }
            if(Input.GetKeyDown(KeyCode.Q)&&Held)BeginThrowCharge();
            if(ChargingThrow&&Held&&Input.GetKey(KeyCode.Q))AdvanceThrowCharge(Time.deltaTime);
            if(ChargingThrow&&Input.GetKeyUp(KeyCode.Q))ReleaseChargedThrow();
            if (Input.GetKeyDown(KeyCode.F) && Held && Held.IsBattery && InCabin) game.UseBattery();
            extinguisher = Held ? Held.GetComponent<FireExtinguisher>() : null;
            if (extinguisher) extinguisher.Operate(this, !ChargingThrow && Input.GetMouseButton(0), Time.deltaTime);

            if (transform.position.y < -8) game.Finish(false, "The facility has no basement for this basement.");
        }

        void UpdateTarget()
        {
            Target = null;CanInteract=false;elevatorButton=null;notebook=null;seatTarget=null;
            terminal = null;
            Prompt = "";
            if (Held)
            {
                Prompt = Held.Title;
            }
            Ray ray = View.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            bool looking = Physics.Raycast(ray, out RaycastHit hit, 2.8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (looking)
            {
                ElevatorButton floorButton=hit.collider.GetComponentInParent<ElevatorButton>();
                if(floorButton){if(floorButton.Available){elevatorButton=floorButton;CanInteract=true;}return;}
                notebook=Held?null:hit.collider.GetComponentInParent<FieldNotebook>();if(notebook){CanInteract=true;Prompt="Field notebook";return;}
                TheElevator.Office.OfficeKeycard keycard=hit.collider.GetComponentInParent<TheElevator.Office.OfficeKeycard>();
                if(keycard&&hit.distance<2.8f){CanInteract=true;Prompt="Reception access card";return;}
                TheElevator.Office.OfficeDoor officeDoor=hit.collider.GetComponentInParent<TheElevator.Office.OfficeDoor>();
                if(officeDoor&&hit.distance<2.8f){CanInteract=true;Prompt="";return;}
                TheElevator.Office.OfficeCargo officeCargo=hit.collider.GetComponentInParent<TheElevator.Office.OfficeCargo>();
                if(officeCargo&&officeCargo.Mandatory&&hit.distance<2.8f){CanInteract=!Held;Prompt=officeCargo.Item.Title;return;}
                ObjectiveTerminal objective = hit.collider.GetComponentInParent<ObjectiveTerminal>();
                if (objective && Vector3.Distance(transform.position + Vector3.up, objective.transform.position + Vector3.up) < 2.7f)
                {
                    terminal = objective;
                    CanInteract=!objective.Complete;Prompt="Survey terminal";
                    return;
                }
                SalvageItem item = hit.collider.GetComponentInParent<SalvageItem>();
                if (item && Vector3.Distance(transform.position + Vector3.up, item.transform.position) < 2.7f)
                {
                    Target = Held?null:item;
                    CanInteract=!Held&&!item.IsHeld;Prompt = item.Title;
                }
            }
            // Nothing else under the crosshair: a free seat you are looking at can be sat on.
            if (!Held && !Target && game.CurrentOffice)
            {
                seatTarget = TheElevator.Office.OfficeSeat.Aimed(View.transform.position, looking ? hit.point : ray.GetPoint(2.2f), game.CurrentOffice);
                if (seatTarget) { CanInteract = true; Prompt = "Sit down"; }
            }

        }

        public bool PickUp(SalvageItem item)
        {
            if(!item||Held||item.IsHeld||!game.ControlsActive||Vector3.Distance(transform.position+Vector3.up,item.transform.position)>2.8f)return false;
            var cargo=item.GetComponent<TheElevator.Office.OfficeCargo>();if(cargo&&cargo.Mandatory)return false;
            ChargingThrow=false;ThrowCharge=0;Held=item;Held.PickUp(this);game.Sound.Play(470,.06f,.08f);return true;
        }
        public bool SetCrouched(bool crouch)
        {
            if(!motor)return false;
            if(!crouch&&Crouched&&Physics.CheckCapsule(transform.position+Vector3.up*1.12f,transform.position+Vector3.up*1.51f,.37f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return false;
            Crouched=crouch;motor.height=crouch?1.10f:1.9f;motor.center=Vector3.up*(motor.height*.5f);
            return true;
        }
        public void BeginThrowCharge(){if(Held&&game.ControlsActive){ChargingThrow=true;ThrowCharge=0;}}
        public void AdvanceThrowCharge(float dt){if(ChargingThrow&&Held)ThrowCharge=Mathf.Clamp01(ThrowCharge+Mathf.Max(0,dt)/1.6f);}
        public void ReleaseChargedThrow()
        {
            if(ChargingThrow&&Held){Held.Release(ThrowCharge>=.12f,ThrowCharge);Held=null;}
            ChargingThrow=false;ThrowCharge=0;
        }
        public void Drop(bool toss)
        {
            ChargingThrow=false;ThrowCharge=0;
            if (!Held) return;
            Held.Release(toss);
            Held = null;
        }

        public void ConsumeHeld()
        {
            if (!Held) return;
            GameObject consumed = Held.gameObject;
            Held = null;
            Destroy(consumed);
        }

        public void Knock(Vector3 source)
        {
            if (damageCooldown > 0 || !game.ControlsActive) return;
            damageCooldown = 2;

            Vector3 away = transform.position - source;
            away.y = 0;
            knockback = away.normalized * 8;
            Drop(true);
            game.Sound.Play(65, 0.25f, 0.18f);
            game.Notify("Ow. Watch where you're going.");

        }

        public void Recover() { Stamina = 1; }

        // Anything that shoves the player steadily (extinguisher foam) pushes them along like a strong wind.
        public void Push(Vector3 wind)
        {
            if (Seat) return;
            wind.y = 0;
            knockback = Vector3.MoveTowards(knockback, wind, Time.deltaTime * 30);
        }

        public void Foam(float amount) { FaceFoam = Mathf.Clamp01(FaceFoam + amount); }

        // Punches, bullets and blasts. The lift is safe: nothing hurts you inside it.
        public void Damage(float amount, Vector3 from, float knock)
        {
            if (Down || !game || !game.ControlsActive || InCabin) return;
            Vector3 away = transform.position - from; away.y = 0;
            if (knock > 0 && away.sqrMagnitude > 1e-4f) knockback = away.normalized * knock;
            if (amount <= 0) return;
            if (Seat) LeaveSeat();
            Health = Mathf.Max(0, Health - amount);
            hurtFlash = Mathf.Clamp01(hurtFlash + .35f + amount / 60f);
            game.Sound.Play(70, .18f, .16f);
            if (Health <= 0) GoDown();
        }

        // Points the view at a world position (cutscenes, tests).
        public void FaceTowards(Vector3 point)
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight, delta = point - eye;
            yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(-Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg, -65, 75);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        public void HealForValidation() { Health = MaxHealth; hurtFlash = 0; }

        void GoDown()
        {
            Down = true;
            Drop(true);
            ChargingThrow = false; ThrowCharge = 0; Motion = Vector3.zero;
            foreach (WorkerController other in FindObjectsByType<WorkerController>(FindObjectsSortMode.None))
                if (other != this && !other.Down) { Spectating = other; break; }
            if (Spectating) game.Notify("You're out cold. Spectating a teammate.");
            else game.Finish(false, "You were knocked out by an angry coworker.");
        }

        // Soft white blots over the view plus a milky wash: the screen equivalent of foam on your face.
        void DrawFaceFoam()
        {
            if (FaceFoam <= .01f || Event.current.type != EventType.Repaint) return;
            if (!foamBlot)
            {
                const int size = 64;
                foamBlot = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Foam blot", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x - size * .5f + .5f, y - size * .5f + .5f).magnitude / (size * .5f);
                    foamBlot.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(1, 0, Mathf.InverseLerp(.35f, 1, d))));
                }
                foamBlot.Apply();
            }
            Color old = GUI.color;
            GUI.color = new Color(.97f, .98f, 1f, FaceFoam * .45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            float h = Screen.height;
            Vector3[] blots = { new Vector3(.18f, .25f, .75f), new Vector3(.72f, .2f, .6f), new Vector3(.45f, .55f, .9f), new Vector3(.1f, .75f, .7f), new Vector3(.85f, .7f, .8f), new Vector3(.55f, .9f, .6f), new Vector3(.32f, .05f, .5f) };
            for (int i = 0; i < blots.Length; i++)
            {
                float coverage = Mathf.Clamp01(FaceFoam * 1.6f - i * .12f);
                if (coverage <= 0) continue;
                float size = blots[i].z * h * (.6f + coverage * .4f);
                GUI.color = new Color(.98f, .99f, 1f, coverage * .92f);
                GUI.DrawTexture(new Rect(blots[i].x * Screen.width - size * .5f, blots[i].y * h - size * .5f, size, size), foamBlot);
            }
            GUI.color = old;
        }

        public bool Sit(TheElevator.Office.OfficeSeat seat)
        {
            if (!seat || Held || Seat || !seat.Free(game.CurrentOffice)) return false;
            SetCrouched(false);
            ChargingThrow = false; ThrowCharge = 0; Motion = Vector3.zero; knockback = Vector3.zero;
            Seat = seat; seat.Sitter = this; sitBlend = 0; standingUp = false;
            standPosition = transform.position; standRotation = transform.rotation;
            // The employee who works here waits until the seat is free again.
            seatStation = seat.Station(game.CurrentOffice);
            if (seatStation) seatStation.ReservedByPlayer = true;
            motor.enabled = false;
            game.Sound.Play(140, .08f, .05f);
            return true;
        }

        public void StandUp() { if (Seat) standingUp = true; }

        // Seated: turn around to look (within reason); E, Space or any movement key stands back up.
        void UpdateSeated()
        {
            Prompt = "E  STAND UP"; CanInteract = false; Target = null; seatTarget = null; Motion = Vector3.zero;
            bool leave = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D);
            if (leave && sitBlend > .5f) standingUp = true;
            sitBlend = Mathf.MoveTowards(sitBlend, standingUp ? 0 : 1, Time.deltaTime / .55f);
            float t = Mathf.SmoothStep(0, 1, sitBlend);
            transform.SetPositionAndRotation(Vector3.Lerp(standPosition, Seat.transform.position, t), Quaternion.Slerp(standRotation, Seat.transform.rotation, t));
            float facing = Seat.transform.eulerAngles.y;
            yaw = facing + Mathf.Clamp(Mathf.DeltaAngle(facing, yaw), -120, 120);
            Model.Animate(0, false, Time.deltaTime, !standingUp);
            if (standingUp && sitBlend <= 0) LeaveSeat();
        }

        void LeaveSeat()
        {
            if (seatStation) seatStation.ReservedByPlayer = false;
            if (Seat) { Seat.Sitter = null; transform.position = standPosition; }
            Seat = null; seatStation = null; standingUp = false; sitBlend = 0;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            motor.enabled = true;
        }

        void OnGUI()
        {
            if(!game||!game.ControlsActive||ReadingNotebook||PreviewAvatar)return;
            DrawFaceFoam();
            if(hurtFlash>.01f&&Event.current.type==EventType.Repaint)
            {
                // A red flash when you get hurt.
                Color old=GUI.color;GUI.color=new Color(.85f,.05f,.05f,hurtFlash*.45f);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            }
            if(!Held)return;
            // Throw strength and item usage share one ring around the cursor.
            if(ChargingThrow){CursorGauge.Draw(ThrowCharge,Color.Lerp(new Color(1,.85f,.3f),new Color(1,.3f,.12f),ThrowCharge));return;}
            FireExtinguisher extinguisher=Held.GetComponent<FireExtinguisher>();
            if(extinguisher&&extinguisher.Used>0)CursorGauge.Draw(extinguisher.Used,extinguisher.Empty?new Color(.95f,.32f,.25f):new Color(.93f,.97f,1f,extinguisher.Spraying?1:.7f));
        }
        void OnDestroy(){if(Hands)Destroy(Hands.gameObject);}
        void LateUpdate()
        {
            if (!game || !View) return;
            if (game.Phase == DescentGame.RunPhase.Briefing || game.Phase == DescentGame.RunPhase.Generating)
            {
                if(Hands)Hands.gameObject.SetActive(false);
                Model.SetView(false,false);
                flashlight.enabled = false;
                View.rect = new Rect(0.45f, 0, 0.55f, 1);
                Model.Animate(0, false, Time.unscaledDeltaTime);
                View.transform.position = new Vector3(3.1f, 2.25f, -2.7f);
                View.transform.LookAt(transform.position + Vector3.up * 1.05f);
                return;
            }
            if (PreviewAvatar)
            {
                // Outfit settings mid-shift: face the player's own body on the right of the screen.
                if(Hands)Hands.gameObject.SetActive(false);
                Model.SetView(false,false);
                View.rect = new Rect(0.45f, 0, 0.55f, 1);
                Model.Animate(0, false, Time.unscaledDeltaTime);
                View.transform.position = transform.TransformPoint(new Vector3(0.9f, 1.55f, 2.9f));
                View.transform.LookAt(transform.position + Vector3.up * 1.0f);
                return;
            }
            Model.SetView(firstPerson,Crouched);
            flashlight.enabled = flashlightOn;
            View.rect = new Rect(0, 0, 1, 1);
            if (game.Paused) return;
            hurtFlash = Mathf.MoveTowards(hurtFlash, 0, Time.deltaTime * 1.2f);
            if (Down)
            {
                // Out cold: watch a teammate who is still on their feet over their shoulder.
                if (Hands) Hands.gameObject.SetActive(false);
                if (Spectating && !Spectating.Down)
                {
                    Transform target = Spectating.transform;
                    View.transform.position = target.position + Vector3.up * 1.9f - target.forward * 3.2f;
                    View.transform.LookAt(target.position + Vector3.up * 1.2f);
                }
                return;
            }
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            if(Seat)
            {
                // Dip forward-and-down through the middle of the sit, then settle at the seated eye line.
                float t=Mathf.SmoothStep(0,1,sitBlend);
                eyeHeight=Mathf.Lerp(EyeHeight,SeatedEyeHeight,t)-Mathf.Sin(sitBlend*Mathf.PI)*.08f;
            }
            else eyeHeight=Mathf.MoveTowards(eyeHeight,Crouched?CrouchEyeHeight:EyeHeight,Time.deltaTime*4);
            Vector3 pivot = transform.position + Vector3.up * (firstPerson ? eyeHeight : Crouched?.75f:1.2f);
            Vector3 position = pivot;
            if (!firstPerson)
            {
                Vector3 back = rotation * Vector3.back;
                float distance = 4.2f;
                if (Physics.SphereCast(pivot, 0.18f, back, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    distance = Mathf.Max(0.12f, hit.distance - 0.08f);
                position += back * distance;
            }
            View.transform.SetPositionAndRotation(position, rotation);
            FireExtinguisher held = Held ? Held.GetComponent<FireExtinguisher>() : null;
            if (held && Held.IsHeld) held.PoseHeld(View.transform);
            if(Hands)Hands.Present(Held,firstPerson&&game.ControlsActive&&!ReadingNotebook,Time.deltaTime);
        }
    }
}
