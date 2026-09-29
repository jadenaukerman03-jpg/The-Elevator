using System.Collections.Generic;
using UnityEngine;
namespace TheElevator.Office
{
    // Temper, speech, violence and health.
    //
    // Anger is a score that starts at 1. The face shows it up to 5 (1 calm ... 5 furious), but offences keep adding
    // past 5. A bump adds 1. Taking an award or another thing off their desk puts them at 3 at least; taking their
    // computer, throwing something at them or foaming them puts them at 5 at least; repeating those adds 2 or 5 more.
    // Anger never cools on its own, with one exception: a furious employee who loses sight of the player for a while
    // settles to 4 (on edge). At 20 they snap: they hunt the player for good and swing at them, and some pull a weapon.
    public sealed partial class OfficeEmployee
    {
        public const float SnapPoint = 20, MaxHealth = 200, LoseInterest = 10;
        public float Anger { get; private set; } = 1;
        public int AngerLevel { get { return Mathf.Clamp((int)Anger, 1, 5); } }
        public bool Snapped { get { return Anger >= SnapPoint; } }
        public Grievance Complaint { get; private set; }
        public float Health { get; private set; } = MaxHealth;
        public bool Dead { get; private set; }
        public Arms Weapon { get; private set; }
        string lastLine;
        float lastSawPlayer = -999, lastFoamOffence = -10, nextSpotted, nextSwing, nextShot, swingUntil, fallen;
        int burst;
        Transform weapon;
        bool armsBusy;

        static float Weight(Grievance g)
        {
            switch (g)
            {
                case Grievance.Bump: case Grievance.Shove: return 1;
                case Grievance.Award: case Grievance.Property: return 2;
                case Grievance.Blast: case Grievance.None: case Grievance.Noise: return 0;
                default: return 5;
            }
        }
        static float Minimum(Grievance g)
        {
            switch (g)
            {
                case Grievance.Award: case Grievance.Property: return 3;
                case Grievance.Computer: case Grievance.Thrown: case Grievance.Foam: case Grievance.Theft: return 5;
                default: return 0;
            }
        }

        // Something the player did to this employee. opening: what they say first, instead of a random reaction.
        public void Offend(Grievance grievance, string opening = null)
        {
            if (Dead) return;
            OfficeDialogue.Tier before = OfficeDialogue.TierFor(Anger);
            bool wasSnapped = Snapped;
            float minimum = Minimum(grievance);
            Anger = Anger < minimum ? minimum : Anger + Weight(grievance);
            Complaint = grievance;
            attentionUntil = Time.time + 5;
            SawPlayer();
            if (Office.Game) { LastObservedPosition = Office.Game.Player.transform.position; Office.Questioner = this; }
            // Past the snapping point every new offence is another chance they reach for something in their desk.
            if (Snapped && Weapon == Arms.None && Office.Game)
            {
                double roll = random.NextDouble();
                if (roll < .02) { Arm(Arms.Bazooka); Say(OfficeDialogue.Pick(OfficeDialogue.DrawBazooka, random, ref lastLine)); return; }
                if (roll < .12) { Arm(Arms.Rifle); Say(OfficeDialogue.Pick(OfficeDialogue.DrawRifle, random, ref lastLine)); return; }
            }
            if (opening != null) Say(opening);
            else if (!Speaking || OfficeDialogue.TierFor(Anger) != before || (Snapped && !wasSnapped)) Say(OfficeDialogue.Reaction(grievance, Anger, random, ref lastLine));
        }

        public void Bump() { Offend(Grievance.Bump); }

        // Hit by extinguisher foam. Holding the spray on someone counts as a fresh offence every 1.5 seconds.
        // Foam in the face blinds them until they wipe it off.
        float blindUntil;
        bool wiping;
        public bool Blinded { get { return Time.time < blindUntil; } }
        public void Foamed(bool face)
        {
            if (Dead) return;
            if (face) blindUntil = Mathf.Min(Time.time + 8, Mathf.Max(blindUntil, Time.time + 2) + .3f);
            if (Time.time - lastFoamOffence >= 1.5f) { lastFoamOffence = Time.time; Offend(Grievance.Foam); }
        }

        // Struck by something the player threw.
        public void HitByThrown(Vector3 at)
        {
            if (Dead) return;
            Offend(Grievance.Thrown);
            Damage(10, at, 2);
        }

        // Caught in a rocket blast.
        public void Blasted(float damage, Vector3 centre, float knock)
        {
            Damage(damage, centre, knock);
            if (!Dead && !Speaking) Say(OfficeDialogue.Reaction(Grievance.Blast, Anger, random, ref lastLine));
        }

        public void Damage(float amount, Vector3 from, float knock)
        {
            if (Dead || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            Vector3 away = transform.position - from; away.y = 0;
            if (knock > 0 && away.sqrMagnitude > 1e-4f && !Robot.Seated) Impulse(away.normalized * knock);
            if (Health <= 0) { Die(); return; }
            if (!Speaking) Say(OfficeDialogue.Pick(OfficeDialogue.Hurt, random, ref lastLine));
        }

        // A sudden shove (explosions, getting hit): they stagger off in that direction.
        public void Impulse(Vector3 velocity) { velocity.y = 0; push = velocity; windUntil = Time.time + .05f; }

        void Die()
        {
            Dead = true; Health = 0; State = "Knocked out";
            Chasing = false; route.Clear(); Partner = null; phrases.Clear(); Speech = null;
            if (voice) voice.Stop();
            motor.enabled = false;
            // Limp: no typing or reaching pose left in the arms.
            Robot.Seated = false; Robot.Reaching = false; Robot.Talking = false; Robot.Speed = 0; Robot.Activity = OfficeTask.Reading;
            if (weapon)
            {
                // The weapon clatters to the floor.
                weapon.SetParent(Office.transform, true);
                Rigidbody body = weapon.gameObject.AddComponent<Rigidbody>(); body.mass = 4;
                BoxCollider box = weapon.gameObject.AddComponent<BoxCollider>(); box.size = new Vector3(.12f, .15f, 1f);
                weapon = null;
            }
            if (Cup) { Destroy(Cup.gameObject); Cup = null; }
        }

        // Keeled over: tip backwards onto the floor over half a second, then lie still.
        void Fallen()
        {
            if (fallen >= 1) return;
            fallen = Mathf.Min(1, fallen + Time.deltaTime / .5f);
            float t = Mathf.SmoothStep(0, 1, fallen);
            Robot.transform.localRotation = Quaternion.Euler(-88 * t, 0, 0);
            Robot.transform.localPosition = Vector3.up * .22f * t;
            Robot.Animate(Time.deltaTime);
        }

        // ---- Seeing the player, cooling off ----
        void SawPlayer() { lastSawPlayer = Time.time; }

        void Temper()
        {
            // Furious (but not snapped): lose sight of the player long enough and they settle to on edge, never lower.
            if (AngerLevel >= 5 && !Snapped && Time.time - lastSawPlayer > LoseInterest)
            {
                Anger = 4;
                Say(OfficeDialogue.Pick(OfficeDialogue.OnEdge, random, ref lastLine));
                nextSpotted = Time.time + 20;
            }
        }

        // Test hook: pretend the player was last seen this many seconds ago, then apply the cooling rule.
        public void LastSawPlayerAgo(float seconds) { lastSawPlayer = Time.time - seconds; Temper(); }
        public void ResetTemperForValidation() { Anger = 1; Complaint = Grievance.None; if (Weapon != Arms.None) Arm(Arms.None); }
        public void ArmForValidation(Arms arms) { Anger = Mathf.Max(Anger, SnapPoint); Arm(arms); }

        void Arm(Arms arms)
        {
            if (weapon) Destroy(weapon.gameObject);
            Weapon = arms;
            weapon = arms == Arms.None ? null : OfficeWeapons.Build(arms, Office.Kit.A, Office.transform);
            nextShot = Time.time + 1;
        }

        // Weapons ride in the right hand, pointed at the player while aiming.
        void PoseWeapon()
        {
            if (!weapon || !Robot.RightHand) return;
            Vector3 aim = Office.Game ? Office.Game.Player.transform.position + Vector3.up * 1.1f : transform.position + transform.forward * 5;
            Vector3 hand = Robot.RightHand.position + (Weapon == Arms.Bazooka ? Vector3.up * .08f : Vector3.zero);
            Vector3 toward = Chasing ? aim - hand : transform.forward;
            weapon.SetPositionAndRotation(hand, Quaternion.LookRotation(toward.sqrMagnitude > 1e-4f ? toward : transform.forward));
        }

        // ---- Furious: go after the player, yelling. Snapped: attack. ----
        public bool Chasing { get; private set; }
        float nextChasePlan;
        readonly List<Vector3> chaseRoute = new List<Vector3>();
        void UpdateChase(float distance)
        {
            WorkerController player = Office.Game.Player;
            bool recentlySeen = Time.time - lastSawPlayer <= LoseInterest;
            bool want = AngerLevel >= 5 && player && !player.InCabin && !player.Down && (Snapped || (recentlySeen && distance < 40));
            if (!want)
            {
                if (armsBusy) { armsBusy = false; Robot.Reaching = Cup; }
                if (!Chasing) return;
                // Gave up: go back to where they belong.
                Chasing = false; route.Clear(); nextTask = Time.time + 5;
                if (Station && !AtStation && !TryTravel(Station)) route.Add(Station.transform.position);
                return;
            }
            if (!Chasing)
            {
                Chasing = true; Partner = null; nextChasePlan = 0; Robot.Seated = false; motor.enabled = true;
                if (CoffeeStage >= 0) { CoffeeStage = -1; if (Station && Station.Coffee) Station.Coffee.SetOpen(0); if (stream) Destroy(stream.gameObject); stream = null; }
            }
            State = Snapped ? "Out for revenge" : "Confronting the intruder";
            attentionUntil = Mathf.Max(attentionUntil, Time.time + .5f);
            Vector3 target = Snapped || Time.time - lastSawPlayer < 1 ? player.transform.position : LastObservedPosition;
            if (!Speaking && Time.time >= nextRant)
                Say(Snapped && Weapon != Arms.None && random.NextDouble() < .5 ? OfficeDialogue.Pick(OfficeDialogue.Firing, random, ref lastLine) : OfficeDialogue.Rant(Complaint, Anger, random, ref lastLine));
            if (Blinded || Pushed) return;
            Vector3 chest = player.transform.position + Vector3.up * 1.1f;
            bool clear = !Physics.Linecast(transform.position + Vector3.up * 1.3f, chest, ~((1 << 2) | (1 << 8)), QueryTriggerInteraction.Ignore);
            if (Snapped) Attack(player, distance, clear, chest);
            float keep = Weapon == Arms.Rifle ? 7 : Weapon == Arms.Bazooka ? 9 : Snapped ? 1.1f : 1.5f;
            if (distance <= keep && (clear || Weapon == Arms.None))
            {
                route.Clear(); Robot.Seated = false;
                Vector3 toward = player.transform.position - transform.position; toward.y = 0;
                if (toward.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toward), 300 * Time.deltaTime);
                return;
            }
            if (Time.time < nextChasePlan) return;
            nextChasePlan = Time.time + 1.2f;
            if (OfficeNavigation.Build(this, Office.Map.NearestRoom(target).Id, target, chaseRoute)) { route.Clear(); route.AddRange(chaseRoute); bestWaypointDistance = float.MaxValue; }
        }

        void Attack(WorkerController player, float distance, bool clear, Vector3 chest)
        {
            // Arms: aim the weapon, or throw a punch.
            bool aiming = Weapon != Arms.None && clear && distance < 30;
            bool swinging = Time.time < swingUntil;
            armsBusy = aiming || swinging;
            if (armsBusy) { Robot.Reaching = true; Robot.ReachTarget = chest; }
            else Robot.Reaching = Cup;
            if (Weapon == Arms.None)
            {
                if (distance > 1.35f || Time.time < nextSwing) return;
                nextSwing = Time.time + 1.4f; swingUntil = Time.time + .3f;
                // Half the time a shove, half the time a punch.
                bool punch = random.NextDouble() < .55;
                player.Damage(punch ? OfficeWeapons.PunchDamage : 0, transform.position, punch ? 5 : 8);
                OfficeWeapons.Punch(player.transform.position + Vector3.up);
                if (random.NextDouble() < .5) Say(OfficeDialogue.Pick(OfficeDialogue.Swing, random, ref lastLine));
                return;
            }
            Transform muzzle = OfficeWeapons.Muzzle(weapon);
            if (!aiming || !muzzle || Time.time < nextShot) return;
            if (Weapon == Arms.Rifle)
            {
                // Short bursts with pauses in between.
                if (burst <= 0) burst = 3 + random.Next(3);
                OfficeWeapons.FireRifle(muzzle.position, chest, player, random);
                burst--;
                nextShot = Time.time + (burst > 0 ? .11f : 1.1f + (float)random.NextDouble() * 1.1f);
            }
            else if (distance > 5)
            {
                OfficeWeapons.FireRocket(this, muzzle.position, chest, random);
                nextShot = Time.time + 4.5f + (float)random.NextDouble() * 1.5f;
            }
        }

        // ---- Speech: one phrase at a time in a bubble over their head, voiced in the made-up office language ----
        public string Speech { get; private set; }
        public float SpeechUntil { get; private set; }
        public float PhraseUntil { get; private set; }
        public OfficeVoice.Tone SpeechTone { get; private set; }
        public bool Speaking { get { return !string.IsNullOrEmpty(Speech) && (Time.time < PhraseUntil || phrases.Count > 0); } }
        public bool PhraseShowing { get { return !string.IsNullOrEmpty(Speech) && Time.time < PhraseUntil; } }
        // Each employee has their own natural voice pitch, from deep to high.
        public float Voice { get { return 105 + EmployeeId * 53 % 13 * 10.5f; } }
        const float PhraseGap = .3f;
        readonly Queue<string> phrases = new Queue<string>();
        AudioSource voice;
        AudioClip spoken;
        float nextRant;

        public void Say(string line)
        {
            if (Dead || string.IsNullOrEmpty(line)) return;
            phrases.Clear();
            foreach (string phrase in OfficeDialogue.Phrases(line)) phrases.Enqueue(phrase);
            if (phrases.Count == 0) return;
            SpeechTone = OfficeVoice.ToneFor(AngerLevel);
            float total = 0;
            foreach (string phrase in phrases) total += OfficeVoice.Duration(phrase, SpeechTone) + PhraseGap;
            SpeechUntil = Time.time + total;
            nextRant = SpeechUntil + 1 + (float)random.NextDouble() * 1.5f;
            NextPhrase();
        }

        void NextPhrase()
        {
            Speech = phrases.Dequeue();
            PhraseUntil = Time.time + OfficeVoice.Duration(Speech, SpeechTone);
            if (!Office.Game) return;
            if (!voice)
            {
                voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 1; voice.dopplerLevel = 0;
                voice.rolloffMode = AudioRolloffMode.Linear; voice.minDistance = 1.5f;
            }
            voice.maxDistance = SpeechTone == OfficeVoice.Tone.Yelling ? 30 : 16;
            voice.volume = SpeechTone == OfficeVoice.Tone.Calm ? .6f : SpeechTone == OfficeVoice.Tone.Upset ? .8f : 1f;
            voice.pitch = 1;
            voice.Stop();
            if (spoken) Destroy(spoken);
            spoken = OfficeVoice.Speak(Speech, SpeechTone, Voice, EmployeeId);
            voice.clip = spoken;
            voice.Play();
        }

        void UpdateSpeech()
        {
            if (phrases.Count > 0 && Time.time >= PhraseUntil + PhraseGap) NextPhrase();
        }
    }
}
