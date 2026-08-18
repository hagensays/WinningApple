# HSScanner

HSScanner is a read-only Windows drive/folder inventory tool for preparing deletion and retention reviews in office environments.

It is deliberately conservative:

- scans folders and file metadata only;
- does **not** delete, rename, move, copy, edit, or change attributes on source files;
- never writes to the scanned source tree as part of scanning;
- writes exports **only to the directory containing the running `HSScanner.exe`**;
- never uses `%TEMP%`, `%APPDATA%`, `%LOCALAPPDATA%`, or another user-profile storage location;
- never overwrites an existing export: a new filename is chosen instead;
- records access-denied and other read errors instead of trying to bypass them.

## Compatibility

- Windows desktop
- Target: **.NET Framework 4.7.2**
- UI: WPF
- CPU: AnyCPU
- No NuGet packages
- No Office/Excel interop
- No additional application runtime or SDK is required beyond the Windows/.NET Framework environment

The project is intentionally suitable for older Windows 10 Enterprise LTSC-style office machines. Building happens in GitHub Actions; the target PC only needs to run the compiled executable.

## What it scans

Choose an entire drive (for example `L:\`) or an individual root folder. HSScanner inventories every readable folder and file under that root, subject to the selected options.

The scan collects:

- folder hierarchy, level (`0`, `1`, `2`, ...), parent ID and paths;
- direct and recursive file counts;
- direct and recursive byte totals;
- file name, extension, relative/full path and size;
- creation, modification and last-access timestamps;
- file attributes;
- a configurable age/cutoff flag;
- read/access errors.

Reparse points are skipped by default to avoid loops or unexpected traversal into another tree.

## 11-year planning filter

The UI defaults to an **11-year** cutoff because the current business task is to prepare an old-data review. This is only a technical planning filter.

HSScanner does **not** decide what must legally be deleted and does not encode a legal retention rule. A file is marked `Älter als Stichtag = Ja` when the later of creation time and modification time is older than the selected cutoff date.

## Excel export

`Excel exportieren` creates a real `.xlsx` workbook next to `HSScanner.exe`. No Excel installation or COM automation is used to create it.

Workbook sheets:

1. **Übersicht** – source, scan time, cutoff, totals, status and safety note.
2. **Ordner** – full hierarchy with level/parent IDs, direct/recursive totals, cutoff counts and blank planning fields:
   - Prüfer / Verantwortlich
   - Entscheidung
   - Begründung / Notiz
   - Prüfdatum
   - Freigabe durch
   - Löschdatum
   - Unterschrift / Nachweis
3. **Dateien** – the complete file inventory. Very large inventories are automatically split over multiple `Dateien N` sheets before the Excel worksheet row limit is reached.
4. **Fehler** – unreadable paths and metadata failures.
5. **Hinweise** – definitions and interpretation notes for later reviewers.

Exports use timestamped names such as:

`HSScanner_Export_20260814_103000.xlsx`

If that filename already exists, HSScanner creates `_2`, `_3`, and so on. Existing files are never overwritten.

## Use

1. Put `HSScanner.exe` in the folder where exports are allowed to be written.
2. Start the EXE.
3. Pick the drive or root folder.
4. Set the cutoff years and scan options.
5. Click **Scan starten**.
6. Review the summary, folders, files and any read errors.
7. Click **Excel exportieren**.
8. Continue the human review/approval process in the generated workbook.

If HSScanner is started from Downloads, its export is written to Downloads. If it is started from another approved folder, exports are written there instead.

## Development

Read [`AGENTS.md`](AGENTS.md) before changing code. Every release change must follow the repository's version-branch → PR → CI → merge → automatic release → verification → cleanup workflow.
