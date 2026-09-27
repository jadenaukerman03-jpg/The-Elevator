using UnityEngine;

namespace TheElevator
{
    public sealed class SalvageItem : MonoBehaviour
    {
        public string Title { get; private set; }
        public float Mass { get; private set; }
        public int Value { get; private set; }
        int undamagedValue;
        public void ApplyCondition(float condition) { Value = Mathf.RoundToInt(undamagedValue * Mathf.Clamp(condition,0.25f,1)); }
        public bool IsBattery { get; private set; }
        public bool IsHeld { get; private set; }
        public Rigidbody Body { get; private set; }
        Collider shell;
        WorkerController carrier;
        float lastImpact;
        DescentGame game;

        public void Configure(DescentGame owner, string title, float mass, int value, bool battery, Vector3 size)
        {
            game = owner;
            Title = title;
            Mass = mass;
            Value = value;
            undamagedValue = value;
            IsBattery = battery;
            shell = gameObject.AddComponent<BoxCollider>();
            ((BoxCollider)shell).size = size;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = mass;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearDamping = 0.35f;
            Body.angularDamping = 0.65f;
            game.Items.Add(this);
        }

        public void PickUp(WorkerController worker)
        {
            carrier = worker;
            IsHeld = true;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.isKinematic = true;
            shell.enabled = false;
        }

        void FixedUpdate()
        {
            if (!IsHeld || !carrier) return;
            Vector3 chest = carrier.transform.position + Vector3.up * 1.15f;
            Vector3 forward = carrier.transform.forward;
            float distance = 1.15f;
            if (Physics.Raycast(chest, forward, out RaycastHit hit, distance + 0.55f, ~0, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0.35f, hit.distance - 0.55f);
            Body.MovePosition(chest + forward * distance);
            Body.MoveRotation(Quaternion.Slerp(Body.rotation, carrier.transform.rotation, 0.3f));
        }

        public void Release(bool toss)
        {
            if (!IsHeld) return;
            Vector3 velocity = carrier.transform.forward * (toss ? 8f : 1.2f) + Vector3.up * (toss ? 3f : 0.2f);
            IsHeld = false;
            carrier = null;
            shell.enabled = true;
            Body.isKinematic = false;
            Body.linearVelocity = velocity;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < 2.8f || Time.time - lastImpact < 0.5f || !game) return;
            lastImpact = Time.time;
            game.NoiseAt(transform.position, 0.65f);
            game.Sound.Play(110, 0.065f, 0.1f);
        }

        void OnDestroy() { if (game) game.Items.Remove(this); }
    }
}
