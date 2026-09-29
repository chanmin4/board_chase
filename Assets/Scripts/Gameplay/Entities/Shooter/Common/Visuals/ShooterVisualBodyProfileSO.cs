/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Stores shared shooter body and socket poses without touching gameplay colliders.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShooterVisualBodyProfile",
    menuName = "Game/Visual Customization/Shooter Body Profile")]
public sealed class ShooterVisualBodyProfileSO : ScriptableObject
{
    [Header("Body Visual Only")]
    [Tooltip("Applied only to ShooterVisualRig.Body Visual Root. Keep gameplay colliders outside that root.")]
    [SerializeField] private ShooterVisualLocalTransform _bodyVisualTransform = new ShooterVisualLocalTransform(
        Vector3.zero,
        Vector3.zero,
        Vector3.one);

    [Header("Socket Poses")]
    [Tooltip("Only sockets listed here override the base Visual_Shooter pose.")]
    [SerializeField] private List<ShooterVisualSocketPose> _socketPoses = new List<ShooterVisualSocketPose>();

    public ShooterVisualLocalTransform BodyVisualTransform => _bodyVisualTransform;
    public IReadOnlyList<ShooterVisualSocketPose> SocketPoses => _socketPoses;

    // Purpose: Updates the body visual transform state.
    public void SetBodyVisualTransform(ShooterVisualLocalTransform value)
    {
        _bodyVisualTransform = value;
    }

    // Purpose: Updates the socket pose state.
    public void SetSocketPose(
        ShooterVisualSocketId socket,
        ShooterVisualLocalTransform value)
    {
        for (int i = 0; i < _socketPoses.Count; i++)
        {
            if (_socketPoses[i].socket != socket)
                continue;

            _socketPoses[i] = new ShooterVisualSocketPose
            {
                socket = socket,
                localTransform = value
            };
            return;
        }

        _socketPoses.Add(new ShooterVisualSocketPose
        {
            socket = socket,
            localTransform = value
        });
    }
}
