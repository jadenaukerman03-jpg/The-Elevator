using System.Collections.Generic;
using UnityEngine;

namespace TheElevator
{
    // Going limp: every limb becomes a physics body joined to its parent, and the animation stops driving it.
    public sealed partial class BeanRig
    {
        public bool Ragdolled { get; private set; }
        // The pelvis body; carrying a body means dragging this and letting the limbs dangle.
        public Rigidbody Core { get; private set; }
        readonly List<Rigidbody> limbs = new List<Rigidbody>();

        public void Ragdoll(Vector3 impulse)
        {
            if (Ragdolled || !pelvis) return;
            Ragdolled = true;
            Core = Bone(pelvis, null, 9, Capsule(pelvis, new Vector3(0, .15f, 0), .2f, .62f));
            Rigidbody skull = Bone(head, Core, 3.5f, Sphere(head, HeadCenter, .31f));
            Soften(skull, 35, 30);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform shoulder = side < 0 ? shoulderL : shoulderR, elbow = side < 0 ? elbowL : elbowR;
                Transform hip = side < 0 ? hipL : hipR, knee = side < 0 ? kneeL : kneeR;
                Rigidbody upper = Bone(shoulder, Core, 1.2f, Capsule(shoulder, new Vector3(0, -UpperArm * .5f, 0), .06f, UpperArm + .12f));
                Soften(upper, 70, 80);
                Rigidbody lower = Bone(elbow, upper, 1, Capsule(elbow, new Vector3(0, -(Forearm + .1f) * .5f, 0), .06f, Forearm + .2f));
                Soften(lower, 80, 15);
                Rigidbody thigh = Bone(hip, Core, 2, Capsule(hip, new Vector3(0, -Thigh * .5f, 0), .075f, Thigh + .14f));
                Soften(thigh, 60, 40);
                Rigidbody shin = Bone(knee, thigh, 1.6f, Capsule(knee, new Vector3(0, -Shin * .5f - .03f, 0), .07f, Shin + .2f));
                Soften(shin, 80, 10);
            }
            Core.AddForce(impulse, ForceMode.VelocityChange);
            foreach (Rigidbody limb in limbs) limb.AddTorque(Random.insideUnitSphere * 2, ForceMode.VelocityChange);
        }

        // Carried bodies: the pelvis follows the carrier kinematically while the limbs keep swinging.
        public void Hold(bool held)
        {
            if (!Core) return;
            Core.isKinematic = held;
            if (!held) { Core.linearVelocity = Vector3.zero; Core.WakeUp(); }
        }

        Rigidbody Bone(Transform bone, Rigidbody parent, float mass, Collider shape)
        {
            bone.gameObject.layer = PhysicsLayers.Ragdolls;
            Rigidbody body = bone.gameObject.AddComponent<Rigidbody>();
            body.mass = mass; body.linearDamping = .05f; body.angularDamping = .8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            limbs.Add(body);
            if (parent)
            {
                CharacterJoint joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parent;
                joint.axis = Vector3.right; joint.swingAxis = Vector3.forward;
                joint.enableProjection = true;
            }
            return body;
        }

        static void Soften(Rigidbody body, float twist, float swing)
        {
            CharacterJoint joint = body.GetComponent<CharacterJoint>();
            if (!joint) return;
            joint.lowTwistLimit = new SoftJointLimit { limit = -twist };
            joint.highTwistLimit = new SoftJointLimit { limit = twist };
            joint.swing1Limit = new SoftJointLimit { limit = swing };
            joint.swing2Limit = new SoftJointLimit { limit = swing };
        }

        static Collider Capsule(Transform bone, Vector3 centre, float radius, float height)
        {
            CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = centre; capsule.radius = radius; capsule.height = height; capsule.direction = 1;
            return capsule;
        }

        static Collider Sphere(Transform bone, Vector3 centre, float radius)
        {
            SphereCollider sphere = bone.gameObject.AddComponent<SphereCollider>();
            sphere.center = centre; sphere.radius = radius;
            return sphere;
        }
    }
}
