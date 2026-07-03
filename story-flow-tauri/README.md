# Match-3 Story Flow Desktop

Native desktop shell for the Unity story flow editor in `Match-3-Adventure`.

This folder is a sibling migration of `story-flow-pwa`. The original browser/Vite editor is intentionally left intact as a fallback while this Tauri version moves folder access and file I/O out of the browser.

## Run

Install JavaScript dependencies:

```powershell
npm install
```

Install Rust through rustup before running the native shell:

```powershell
winget install Rustlang.Rustup
```

Then restart the terminal and run:

```powershell
npm run tauri:dev
```

The Vite-only frontend can still be typechecked and bundled with:

```powershell
npm run build
```

## Workflow

1. Choose `Open Unity Project`.
2. Select the Unity project root folder, not `Assets`.
3. The app reads and writes:
   - `Assets/Resources/Dialogues/**/*.txt`
   - `Assets/Resources/Combats/**/*.txt`
   - `Assets/Resources/Data/GearStops/**/*.json`
   - `Assets/Resources/Data/story_flow.json`
   - `story-flow.visuals.json`
4. Drag graph nodes to organize the view.
5. Create new dialogue/combat/gear-stop beats from the graph toolbar.
6. Double-click a node, edit the dialogue or combat as line blocks, and save.
7. Delete unwanted beats from the editor header. The `.txt.meta` or `.json.meta` sidecar is deleted when present.

## Native Boundary

- Folder selection uses Tauri's native dialog plugin.
- File reads, writes, recursive listings, and deletes go through Rust commands in `src-tauri/src/lib.rs`.
- The selected project root is passed to commands as an absolute path, and command paths are constrained to relative paths inside that root.
- The UI code stays close to the PWA version so fixes can be ported between both tools while they coexist.

## Build Output

Generated output is ignored:

- `node_modules/`
- `dist/`
- `src-tauri/target/`

Do not commit generated desktop bundles unless explicitly requested.
