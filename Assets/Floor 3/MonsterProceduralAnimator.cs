using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterProceduralAnimator : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float stepsPerSecond = 2.8f;
    [SerializeField, Range(0f, 1f)] private float limbSwing = 0.65f;
    [SerializeField, Min(0f)] private float hipBob = 0.08f;

    private Transform hips;
    private Transform leftArm;
    private Transform leftForeArm;
    private Transform rightArm;
    private Transform rightForeArm;
    private Transform leftUpLeg;
    private Transform leftLeg;
    private Transform rightUpLeg;
    private Transform rightLeg;
    private Quaternion[] bindRotations;
    private Transform[] animatedBones;
    private Vector3 hipsBindPosition;
    [SerializeField] private bool running;

    public void SetRunning(bool value)
    {
        running = value;
    }

    private void Awake()
    {
        hips = FindBone("mixamorig:Hips");
        leftArm = FindBone("mixamorig:LeftArm");
        leftForeArm = FindBone("mixamorig:LeftForeArm");
        rightArm = FindBone("mixamorig:RightArm");
        rightForeArm = FindBone("mixamorig:RightForeArm");
        leftUpLeg = FindBone("mixamorig:LeftUpLeg");
        leftLeg = FindBone("mixamorig:LeftLeg");
        rightUpLeg = FindBone("mixamorig:RightUpLeg");
        rightLeg = FindBone("mixamorig:RightLeg");

        animatedBones = new[] { leftArm, rightArm, leftUpLeg, rightUpLeg, leftLeg, rightLeg };
        bindRotations = new Quaternion[animatedBones.Length];
        for (int index = 0; index < animatedBones.Length; index++)
        {
            if (animatedBones[index] != null)
                bindRotations[index] = animatedBones[index].localRotation;
        }

        if (hips != null)
            hipsBindPosition = hips.localPosition;
    }

    private void LateUpdate()
    {
        for (int index = 0; index < animatedBones.Length; index++)
        {
            if (animatedBones[index] != null)
                animatedBones[index].localRotation = bindRotations[index];
        }

        float phase = running
            ? Mathf.Sin(Time.time * stepsPerSecond * Mathf.PI * 2f) * limbSwing
            : 0f;

        AimBone(leftArm, leftForeArm, new Vector3(-0.08f, -1f, -phase));
        AimBone(rightArm, rightForeArm, new Vector3(0.08f, -1f, phase));

        if (!running)
        {
            if (hips != null)
                hips.localPosition = hipsBindPosition;
            return;
        }

        AimBone(leftUpLeg, leftLeg, new Vector3(-0.03f, -1f, phase * 0.75f));
        AimBone(rightUpLeg, rightLeg, new Vector3(0.03f, -1f, -phase * 0.75f));

        if (hips != null)
            hips.localPosition = hipsBindPosition + Vector3.up * (Mathf.Abs(phase) * hipBob);
    }

    private void AimBone(Transform bone, Transform child, Vector3 localDirection)
    {
        if (bone == null || child == null)
            return;

        Vector3 currentDirection = child.position - bone.position;
        Vector3 targetDirection = transform.TransformDirection(localDirection.normalized);
        if (currentDirection.sqrMagnitude > 0.0001f)
            bone.rotation = Quaternion.FromToRotation(currentDirection, targetDirection) * bone.rotation;
    }

    private Transform FindBone(string boneName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == boneName)
                return child;
        }

        return null;
    }
}
