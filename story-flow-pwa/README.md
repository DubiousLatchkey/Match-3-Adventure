# Match-3 Story Flow PWA

Local browser editor for the Unity story flow in `Match-3-Adventure`.

## Run

```powershell
npm install
npm run dev -- --host 127.0.0.1
```

Open the shown localhost URL in Chrome or Edge. The File System Access API needs a secure browser context, and localhost qualifies.

## Workflow

1. Choose `Open Unity Project`.
2. Select the Unity project root folder, not `Assets`.
3. The app reads:
   - `Assets/Resources/Dialogues/**/*.txt`
   - `Assets/Resources/Combats/**/*.txt`
   - asset names from common `Assets/Resources` folders for dropdown suggestions
4. Drag graph nodes to organize the view.
5. Create new dialogue/combat beats from the graph toolbar.
6. Double-click a node, edit the dialogue or combat as line blocks, and save.
7. Delete unwanted beats from the editor header. The `.txt.meta` sidecar is deleted when present.

## Save Behavior

- Dialogue transitions are read from the reserved final command line: `|combat ...` or `|combatDirect ...`.
- Combat transitions are read from the reserved final setting line: `dialogue: ...`.
- Connecting graph nodes rewrites the source beat's reserved final transition line.
- The block editor serializes back to the same plain text format Unity already reads.
- Layout positions and placeholder asset names are saved to `story-flow.visuals.json` in the selected Unity project root.
- The editor reloads from disk after saves so the browser state stays true to the local folder.

## Character Assets

Character dropdowns use base character names inferred from `Assets/Resources/Characters/*Normal.*`.
Expression dropdowns for `setExpression` are inferred from sibling images such as `annAngry.png`,
`annHappy.png`, or `annShocked.png`. Custom typed values remain allowed for placeholder assets.

## Browser Support

Full folder read/write is intended for Chromium browsers such as Chrome and Edge. Browsers without `showDirectoryPicker` cannot use the direct Unity folder workflow.
