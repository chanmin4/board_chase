/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Converts the player's X-key backpack command into an atomic world drop.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerBackpackDropInput : MonoBehaviour
{
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;
    [SerializeField, Min(0f)] private float _dropForwardOffset = 0.8f;
    [SerializeField] private float _dropUpOffset = 0.05f;

    // Purpose: Subscribes to the shared, rebindable backpack drop action.
    private void OnEnable()
    {
        ResolveInputReader();

        if (_inputReader != null)
            _inputReader.DropBackpackEvent += TryDropEquippedBackpack;
    }

    // Purpose: Releases the shared backpack-drop action subscription.
    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.DropBackpackEvent -= TryDropEquippedBackpack;
    }

    // Purpose: Preserves the old X-key behavior only when an old prefab has no InputReader reference.
    private void Update()
    {
        if (_inputReader == null)
        {
            ResolveInputReader();

            if (_inputReader != null)
            {
                _inputReader.DropBackpackEvent += TryDropEquippedBackpack;
                return;
            }
        }

        if (_inputReader == null && Keyboard.current != null &&
            Keyboard.current.xKey.wasPressedThisFrame)
            TryDropEquippedBackpack();
    }

    // Purpose: Detaches the worn backpack, spawns it at the player feet, and restores it on any spawn failure.
    private void TryDropEquippedBackpack()
    {
        ResolveRefs();

        if (_inventoryRuntime == null)
        {
            RaiseMessage("Backpack drop failed: player inventory runtime is missing.");
            return;
        }

        if (!_inventoryRuntime.TryDetachEquippedBackpack(
                out BackpackItemSO backpack,
                out List<PlayerInventoryItemStack> cargo,
                out string detachMessage))
        {
            RaiseMessage(detachMessage);
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        Vector3 dropPosition =
            transform.position +
            forward * _dropForwardOffset +
            Vector3.up * _dropUpOffset;

        if (WorldItemPickupSpawner.TrySpawnBackpack(
                backpack,
                cargo,
                dropPosition,
                Quaternion.identity,
                out WorldItemPickup pickup) && pickup != null)
        {
            RaiseMessage($"{backpack.DisplayName} dropped. Press E to pick it up.");
            return;
        }

        if (_inventoryRuntime.TryEquipBackpackFromWorld(backpack, cargo, out string restoreMessage))
        {
            RaiseMessage("Could not drop backpack. It was restored.");
            Debug.LogWarning(
                $"[PlayerBackpackDropInput] World drop failed and the backpack was restored. details={restoreMessage}",
                this);
            return;
        }

        RaiseMessage("CRITICAL: backpack drop failed and restore also failed. Check Console.");
        Debug.LogError(
            $"[PlayerBackpackDropInput] CRITICAL backpack restore failure. backpack={backpack.name}, cargoSlots={cargo.Count}",
            this);
    }

    // Purpose: Resolves required player-local dependencies when the prefab is instantiated.
    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
            _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);
    }

    // Purpose: Resolves the shared input asset from the gameplay overlay for old player prefabs.
    private void ResolveInputReader()
    {
        if (_inputReader != null)
            return;

        UIOverlayManager overlay = FindFirstObjectByType<UIOverlayManager>(
            FindObjectsInactive.Include);
        _inputReader = overlay != null ? overlay.InputReader : null;
    }

    // Purpose: Sends the result through the standard in-game message channel.
    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[Backpack] {message}", this);
    }
}
