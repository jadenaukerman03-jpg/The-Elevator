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
        public float Stamina { get; private set; } = 1f;
        public int Health { get; private set; } = 3;
        public bool InCabin { get { return DescentGame.InCabin(transform.position); } }
        public string Prompt { get; private set; }
        public bool MovingFast { get; private set; }
        DescentGame game;
        CharacterController motor;
        float yaw, pitch = 17f, vertical, damageCooldown, lastStep;
        bool firstPerson = true;
        ObjectiveTerminal terminal;
        Light flashlight;
        bool flashlightOn = true;
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
            View = new GameObject("Worker camera").AddComponent<Camera>();
            View.tag = "MainCamera";
            View.nearClipPlane = 0.08f;
            View.farClipPlane = 100;
            View.fieldOfView = 68;
            View.backgroundColor = Workshop.Ink;
            View.clearFlags = CameraClearFlags.SolidColor;
            View.gameObject.AddComponent<AudioListener>();
            flashlight = View.gameObject.AddComponent<Light>();
            flashlight.type = LightType.Spot; flashlight.range = 24; flashlight.spotAngle = 72;
            flashlight.intensity = 3; flashlight.color = new Color(1,0.94f,0.80f); flashlight.shadows = LightShadows.Hard;
            Teleport(new Vector3(0, 0.08f, -6.5f));
        }

        public void Teleport(Vector3 position)
        {
            motor.enabled = false;
            transform.position = position;
            motor.enabled = true;
            vertical = 0;
            knockback = Vector3.zero;
        }

        void Update()
        {
            if (!game || !game.ControlsActive) return;
            damageCooldown -= Time.deltaTime;
            yaw += Input.GetAxisRaw("Mouse X") * 2.1f;
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 1.8f, -35, 65);
            if (Input.GetKeyDown(KeyCode.L)) flashlightOn = !flashlightOn;
            if (Input.GetKeyDown(KeyCode.V) && (Application.isEditor || Debug.isDebugBuild))
            {
                firstPerson = !firstPerson;
                Model.gameObject.SetActive(!firstPerson);
            }
            float horizontal = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(horizontal, 0, forward), 1);
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * input;
            MovingFast = Input.GetKey(KeyCode.LeftShift) && Stamina > 0.08f && input.sqrMagnitude > 0.1f;
            Stamina = Mathf.Clamp01(Stamina + Time.deltaTime * (MovingFast ? -0.23f : 0.17f));
            float speed = MovingFast ? 6.8f : 4.1f;
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
                if (Input.GetKeyDown(KeyCode.Space) && Stamina > 0.15f)
                {
                    vertical = Held ? 4.5f : 6f;
                    Stamina = Mathf.Max(0, Stamina - 0.12f);
                }
            }
            vertical -= 18f * Time.deltaTime;
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
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (game.CurrentOffice && game.CurrentOffice.InteractPressed()) { }
                else if (Held) Drop(false);
                else if (terminal) terminal.Use(game);
                else if (Target) { Held = Target; Held.PickUp(this); game.Sound.Play(470, 0.06f, 0.08f); }
            }
            if (Input.GetKeyDown(KeyCode.Q) && Held) Drop(true);
            if (Input.GetKeyDown(KeyCode.F) && Held && Held.IsBattery && InCabin) game.UseBattery();
            if (Input.GetKeyDown(KeyCode.R) && InCabin) game.RequestDeparture();
            if (transform.position.y < -8) game.Finish(false, "The facility has no basement for this basement.");
        }

        void UpdateTarget()
        {
            Target = null;
            terminal = null;
            Prompt = "";
            if (Held)
            {
                Prompt = "E  DROP     Q  THROW" + (Held.IsBattery && InCabin ? "     F  CONNECT BATTERY" : "");
                return;
            }
            Ray ray = View.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.SphereCast(ray, 0.18f, out RaycastHit hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                TheElevator.Office.OfficeDoor officeDoor=hit.collider.GetComponentInParent<TheElevator.Office.OfficeDoor>();
                if(officeDoor&&hit.distance<2.8f){Prompt=officeDoor.Unlocked?"ACCESS GRANTED":"E  SWIPE DEPARTMENT BADGE";return;}
                TheElevator.Office.OfficeCargo officeCargo=hit.collider.GetComponentInParent<TheElevator.Office.OfficeCargo>();
                if(officeCargo&&officeCargo.Mandatory&&hit.distance<2.8f){Prompt=officeCargo.Mounted?"E  DISCONNECT POWER / ENGAGE DOLLY":"E  TAKE DOLLY HANDLES     Q  RELEASE";return;}
                ObjectiveTerminal objective = hit.collider.GetComponentInParent<ObjectiveTerminal>();
                if (objective && Vector3.Distance(transform.position + Vector3.up, objective.transform.position + Vector3.up) < 2.7f)
                {
                    terminal = objective;
                    Prompt = objective.Complete ? "SURVEY RECORDED" : "E  RECORD REMOTE SURVEY";
                    return;
                }
                SalvageItem item = hit.collider.GetComponentInParent<SalvageItem>();
                if (item && Vector3.Distance(transform.position + Vector3.up, item.transform.position) < 2.7f)
                {
                    Target = item;
                    Prompt = "E  PICK UP " + item.Title.ToUpper() + "  /  " + item.Mass + " KG  /  $" + item.Value;
                }
            }
            if (Prompt == "" && InCabin) Prompt = "R  REQUEST DEPARTURE";
        }

        public void Drop(bool toss)
        {
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

        public void Hurt(Vector3 source)
        {
            if (damageCooldown > 0 || !game.ControlsActive) return;
            damageCooldown = 2;
            Health--;
            Vector3 away = transform.position - source;
            away.y = 0;
            knockback = away.normalized * 8;
            Drop(true);
            game.Sound.Play(65, 0.25f, 0.18f);
            game.Notify("Workplace incident. Return to the lift!");
            if (Health <= 0) game.Finish(false, "Three workplace incidents. Your shift has been terminated.");
        }

        public void Recover() { Health = Mathf.Min(3, Health + 1); Stamina = 1; }

        void LateUpdate()
        {
            if (!game || !View) return;
            if (game.Phase == DescentGame.RunPhase.Briefing || game.Phase == DescentGame.RunPhase.Generating)
            {
                Model.gameObject.SetActive(true);
                flashlight.enabled = false;
                View.rect = new Rect(0.45f, 0, 0.55f, 1);
                Model.Animate(0, false, Time.unscaledDeltaTime);
                View.transform.position = new Vector3(3.1f, 2.25f, -2.7f);
                View.transform.LookAt(transform.position + Vector3.up * 1.05f);
                return;
            }
            Model.gameObject.SetActive(!firstPerson);
            flashlight.enabled = flashlightOn;
            View.rect = new Rect(0, 0, 1, 1);
            if (game.Paused) return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 pivot = transform.position + Vector3.up * (firstPerson ? 1.7f : 1.45f);
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
        }
    }
}
