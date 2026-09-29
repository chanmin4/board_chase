# Script hierarchy refactor tools

These scripts make the `Assets/Scripts` responsibility hierarchy reproducible without changing runtime behavior.

## 1. Move scripts into the responsibility hierarchy

Preview the migration:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\Tools\ScriptRefactor\Invoke-ScriptHierarchyMigration.ps1 `
  -DryRun
```

Apply the migration:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\Tools\ScriptRefactor\Invoke-ScriptHierarchyMigration.ps1
```

The migration:

- moves every existing `.cs` together with its `.cs.meta`;
- creates only missing destination folders and their folder metas;
- stops when a destination conflicts with another asset;
- verifies script count, GUID ownership, and SHA-256 source hashes before and after moving;
- is idempotent: an already completed move is reported as `Already moved`.

The script does not rename declared types, namespaces, public APIs, serialized fields, or runtime logic.

## 2. Add role headers

Preview header insertion:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\Tools\ScriptRefactor\Add-ScriptRoleHeaders.ps1 `
  -DryRun
```

Add missing headers:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\Tools\ScriptRefactor\Add-ScriptRoleHeaders.ps1
```

The header tool preserves each file's BOM, newline style, and all original source bytes following the inserted comment. Existing role headers are intentionally skipped. If a script is moved after headers have been added, update its `Role hierarchy` and `Execution boundary` fields to match the new path.

## 3. Verify the result

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\Tools\ScriptRefactor\Test-ScriptRefactorIntegrity.ps1
```

The verifier checks:

- every `.cs` has an adjacent `.cs.meta`;
- every script GUID is readable and unique;
- every script has all three role-header fields;
- the header role hierarchy matches the current folder;
- the recorded execution boundary matches the Unity assembly boundary;
- both StateMachine asmdefs still exist.

All scripts accept an optional absolute `-ProjectRoot` for validating a disposable project copy.

## WIP caveat

`Assets/Scripts/WIP` is an organizational quarantine, not a compile exclusion. Runtime files below WIP remain in `Assembly-CSharp` unless an assembly boundary explicitly excludes them. Active production code must not acquire new dependencies on WIP types.

These tools perform file-level validation only. Unity import, compilation, PlayMode, and builds remain separate verification steps.
