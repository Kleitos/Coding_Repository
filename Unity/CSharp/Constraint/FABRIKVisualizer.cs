//using UnityEngine;

//[ExecuteAlways]
//public class FABRIKVisualizer : MonoBehaviour
//{
//    public FABRIKSolver solver;
//    public Color boneColor = Color.green;
//    public Color toNextColor = Color.cyan;
//    public Color upColor = Color.magenta;
//    public Color poleColor = Color.red;

//    void OnDrawGizmos()
//    {
//        if (solver == null || solver.joints == null || solver.joints.Length < 2) return;

//        for (int i = 0; i < solver.joints.Length - 1; i++)
//        {
//            Transform joint = solver.joints[i];
//            Transform next = solver.joints[i + 1];

//            // Bone line
//            Gizmos.color = boneColor;
//            Gizmos.DrawLine(joint.position, next.position);

//            // toNext vector (forward)
//            Vector3 toNext = (next.position - joint.position).normalized;
//            Gizmos.color = toNextColor;
//            Gizmos.DrawRay(joint.position, toNext * 0.3f);

//            // Up vector
//            Vector3 up = joint.up;
//            Gizmos.color = upColor;
//            Gizmos.DrawRay(joint.position, up * 0.2f);

//            // Pole vector direction
//            if (solver.pole != null)
//            {
//                Gizmos.color = poleColor;
//                Vector3 poleDir = (solver.pole.position - joint.position).normalized;
//                Gizmos.DrawRay(joint.position, poleDir * 0.25f);
//                Gizmos.DrawLine(joint.position, solver.pole.position);
//            }
//        }
//    }
//}
