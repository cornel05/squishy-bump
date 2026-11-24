# Fruit scale preview

| Level | Model scale | Review |
| --- | ---: | --- |
| L1 Training Ring | 0.95× | Approved from stills |
| L2 Moving Ground | 0.80× | Approved |
| L3 Cannon Court | 0.60× | Approved |
| L4 Frost Ring | 0.90× | Trial |
| L5 Chaos Core | 0.85× | Trial |

## Play Mode

Open `Assets/_Project/Scenes/Boot.unity` and enter Play Mode. From the repo
root, run the command below. Use `PlayL2`, `PlayL3`, `PlayL4`, or `PlayL5`.

```powershell
unity command --project-path . script run --file _scratch/fruit-scale-preview/PlayPreview.cs --entry FruitScalePlayPreview.PlayL4
```

The script scales visuals in memory. The official scenes, fighter colliders,
and GDD stay as committed in the game project. On L5, LEGO bricks follow
their nearest H06 tile, so the visible brick edges can differ from collider
edges.

Creative source assets remain local under `Assets/_Scratch/`. Play Mode
reads `CreativeL2L5/L2.glb`, `CreativeMap3/Map3.glb`, both the original and
revised `map 4.glb`, `CreativeMap4Revised/cuc bang 2.glb`, and
`CreativeL2L5/L5.glb`. Keep those local assets available when using the
checkpoint on another machine.

`Capture.cs` and `Compose.ps1` reproduce the earlier size comparisons.
Those stills predate the final L2/L3 approvals and the Play Mode Creative art
updates.
