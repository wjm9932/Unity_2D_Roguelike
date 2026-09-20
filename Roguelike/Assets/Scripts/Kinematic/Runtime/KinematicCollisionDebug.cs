//using Kinematic.Data;
//using UnityEngine;

//namespace Kinematic.Runtime
//{
//    public sealed class KinematicCollisionDebug : MonoBehaviour
//    {
//        [Header("Circle A")]
//        [SerializeField] private Transform circleA;
//        [SerializeField] private Vector2 offsetA;
//        [SerializeField] private float radiusA = 1f;

//        [Header("Body B")]
//        [SerializeField] private Transform circleB;
//        [SerializeField] private Shape shapeB = Shape.Box;
//        [SerializeField] private Vector2 offsetB;
//        [SerializeField] private float radiusB = 1f;
//        [SerializeField] private Vector2 halfExtentsB = Vector2.one;

//        [Header("Simulation")]
//        [SerializeField] private bool applyMtvToA;

//        private bool hasContact;
//        private Vector2 separationNormal;
//        private float penetrationDepth;
//        private Vector2 separationMtv;

//        private KinematicBody bodyA;
//        private KinematicBody bodyB;

//        private void Awake()
//        {
//            if (circleA == null || circleB == null)
//            {
//                enabled = false;
//                return;
//            }

//            bodyA = new KinematicBody(
//                circleA,
//                ColliderInfo.CreateCircle(offsetA, radiusA),
//                false);

//            var colliderB = shapeB == Shape.Circle
//                ? ColliderInfo.CreateCircle(offsetB, radiusB)
//                : ColliderInfo.CreateBox(offsetB, halfExtentsB);

//            bodyB = new KinematicBody(circleB, colliderB, true);
//        }

//        private void Update()
//        {
//            hasContact = KinematicDetector.TryCollide(bodyA, bodyB, out var contact);

//            if (!hasContact)
//            {
//                separationNormal = Vector2.zero;
//                penetrationDepth = 0f;
//                separationMtv = Vector2.zero;
//                return;
//            }

//            separationNormal = contact.SeparationNormal;
//            penetrationDepth = contact.PenetrationDepth;
//            separationMtv = contact.SeparationMtv;

//            DrawContact(bodyA.WorldCenter, bodyB.WorldCenter, contact);

//            if (!applyMtvToA)
//            {
//                return;
//            }
//        }

//        private static void DrawContact(Vector2 centerA, Vector2 centerB, in Contact contact)
//        {
//            Debug.Log(contact);

//            Debug.DrawLine(
//                centerB,
//                centerB + contact.SeparationNormal,
//                Color.yellow,
//                0.1f,
//                false);

//            Debug.DrawLine(
//                centerA,
//                centerA + contact.SeparationMtv,
//                Color.red,
//                0.1f,
//                false);
//        }

//        private void OnDrawGizmos()
//        {
//            if (circleA != null)
//            {
//                var centerA = (Vector2)circleA.position + offsetA;
//                Gizmos.color = Color.cyan;
//                Gizmos.DrawWireSphere(centerA, radiusA);
//            }

//            if (circleB != null)
//            {
//                var centerB = (Vector2)circleB.position + offsetB;
//                Gizmos.color = Color.magenta;

//                if (shapeB == Shape.Circle)
//                {
//                    Gizmos.DrawWireSphere(centerB, radiusB);
//                }
//                else
//                {
//                    Gizmos.DrawWireCube(centerB, halfExtentsB * 2f);
//                }
//            }

//            if (!hasContact || circleA == null || circleB == null)
//            {
//                return;
//            }

//            var worldCenterA = (Vector2)circleA.position + offsetA;
//            var worldCenterB = (Vector2)circleB.position + offsetB;

//            Gizmos.color = Color.yellow;
//            //Gizmos.DrawLine(worldCenterB, worldCenterB + separationNormal);

//            Gizmos.color = Color.red;
//            //Gizmos.DrawLine(worldCenterA, worldCenterA + separationMtv);
//        }
//    }
//}
