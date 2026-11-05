# Paint Mark — WIP archive

Retired from active gameplay at the user's request. These text files are outside Assets and are not compiled by Unity.

- EntityPaintMarkController.cs.txt: original stack accumulation, faction changes, natural/active decay, armor reduction, initialization and events.
- BasicBullet.TryApplyPaintMark.cs.txt: original bullet-to-mark integration.

The live EntityPaintMarkController keeps its existing script GUID and an inert API so serialized prefab references do not become Missing Script. It has no Update/decay loop and never adds, removes or displays marks. Mark armor deltas and consumable mark recovery return zero. Legacy SO data is hidden and disabled; no Inspector switch can re-enable this archived feature. Debug Monitor and item details no longer display mark values. Health, infection, status effects, environment paint and visibility are not retired with this feature.

Restoring this feature requires deliberate source integration, not toggling a serialized field. No build, Unity Refresh, automatic test or Play Mode run was performed for this change.
