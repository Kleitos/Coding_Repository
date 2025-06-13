using UnityEngine;

public class FABRIKRestPoseController : MonoBehaviour
{
    public FABRIKSolver ikSolver;
    public Transform restTarget;
    public Transform restPole;

    void Start()
    {
        if (ikSolver == null) return;

        if (restTarget != null)
            ikSolver.target.position = restTarget.position;

        if (ikSolver.pole != null && restPole != null)
            ikSolver.pole.position = restPole.position;
    }
}
