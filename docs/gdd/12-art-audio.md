# 12. Art and audio

> **In this chapter:** The current look and sound of Squishy Bump: characters, Creative palette, level targets, hazard readability, UX/UI screens and audio.

All 3D character models, the STAR CATCHER claw-machine lobby, all five L1–L5 maps and the UX/UI / Console screens shown in this chapter are final Creative team assets integrated in Unity (SB-75–SB-78); all earlier concept art has been removed. Full provenance is listed in Appendix C.

## 12.1 Visual direction

Squishy Bump presents a bright, playful toy-store world after closing time: six plump, glossy squishy animals with distinctive headgear (leaf sprout, strawberry beret, witch hat, nightcap, duckling hat, frog hood) gather inside the pastel-purple STAR CATCHER claw machine and compete across five themed toy-set arenas (inflatable tropical pool, circus music box, ocean fishing board, Christmas snow village and LEGO brick castle). Arena play surfaces keep strong figure-ground contrast so fighters and telegraphs remain the clearest elements on screen; there is no blood or weapon, only jelly squash, speed trails and impact rings.

*Figure 11: Key art v1.4 (`bg_start.png`): Creative's six squishy animal fighters (Bear, Pig, Cat, Chick, Frog, Penguin) on display stands.*

## 12.2 Characters and weight classes

Every fighter is a plump squishy animal toy (SB-76) with a distinct head accessory readable at third-person camera distance, pairing silhouette with colour. Weight class is shown by physical size from Section 6.1 (Heavy r = 0.60 m = 1.2× Normal; Light r = 0.45 m = 0.9× Normal), floating class cards in the lobby (`card_heavy/normal/light.png`) and 3D-rendered UI avatars (`avatar_0.png`–`avatar_5.png`) on the HUD. Fighters move upright facing their direction of travel with jelly squash-and-stretch instead of rolling. Each arena applies a visual-only fighter model scale: **L1 0.95×, L2 0.80×, L3 0.60×, L4 0.90×, L5 0.85×** (physics sphere collider radii remain unchanged).

| Fighter | Class | Colour | Silhouette cue & asset files |
|---|---|---|---|
| Bear | Heavy | `#B19659` | Round brown bear, cream belly, tiny green sprout on top · `Bear.glb` / `avatar_0.png` |
| Pig | Heavy | `#F9B8BE` | Plump pink pig, snout, strawberry beret with leaf & small daisy · `Pig.glb` / `avatar_1.png` |
| Cat | Normal | `#AA77D6` | Lavender cat wearing a purple witch hat with gold buckle · `Cat.glb` / `avatar_2.png` |
| Penguin | Normal | `#A4BFF2` | Sky-blue penguin wearing a dark-blue nightcap with white pom-pom · `Penguin.glb` / `avatar_3.png` |
| Chick | Light | `#EABD71` | Small pale-yellow chick topped with a tiny yellow duckling hat · `Chick.glb` / `avatar_4.png` |
| Frog | Light | `#87E57A` | Small mint-green frog in a big-eyed frog hood, cheeky tongue out · `Frog.glb` / `avatar_5.png` |

## 12.3 Palette, type and style guide

| Role | Hex | Rule |
|---|---|---|
| Fighter colours | Six hues | `#B19659` (Bear) `#F9B8BE` (Pig) `#AA77D6` (Cat) `#A4BFF2` (Penguin) `#EABD71` (Chick) `#87E57A` (Frog); used for name tags and scoreboard dots |
| Primary UI (Creative) | `#410E9E` · `#FD5FB0` · `#3ED5D0` | Three core UX/UI kit colours (deep purple outlines/frames, pink star/badge accents, cyan-mint button fills and sliders) |
| STAR CATCHER Lobby | Pastel purple · cyan-mint | STAR CATCHER claw machine (`Lobby.glb`) and Console UI (`console_lobby`, `console_rooms`, `card_heavy/normal/light`) |
| L1 ring bands | #CDB4DB #A8E6CF #FFE8D6 #BDE0FE | Outer to inner; calm pastels so they never read as warnings |
| Ice | #BDE0FE | Ice tiles and air gusts only |
| Warning | #FFB703 | Reserved for telegraphs; never decoration |
| Danger | #E63946 | Reserved for imminent impact (flashing 2 Hz) |
| Ink | #1F2340 | Text and outlines |

All six animal colours use calm pastel tones distinct from danger red (`#E63946`); imminent danger signals always pair 2 Hz red flashing with a shape (cross ring, cracks, edge stripes) for colour-blind readability. Creative's UX/UI kit (SB-74–SB-77) uses `#410E9E`, `#FD5FB0` and `#3ED5D0` together with the Sleeping display font (paired with Baloo 2 for full Vietnamese diacritic support).

*Figure 15: Official Creative UI colour palette (`#410E9E`, `#FD5FB0`, `#3ED5D0`).*

## 12.4 Level visual targets

Each level is a distinct toy-set arena built from Creative's 3D map pack (SB-78):
- **L1 Training Ring** (`Map1Demo.glb`, `Vong1–4.glb`, `PhaoTron1–3.glb`, `PhaoUnicorn1–3.glb`): four-ring inflatable-pool island · fighter visual scale **0.95×**.
- **L2 Moving Ground** (`L2.glb`): spinning circus music box on a toy-room wooden floor · fighter visual scale **0.80×**.
- **L3 Cannon Court** (`Map3.glb`): ocean fishing-game board on blue foam tiles · fighter visual scale **0.60×**.
- **L4 Frost Ring** (`map 4.glb`, `cuc bang 2.glb`): Christmas snow-village frozen pond (61 hex ice tiles) · fighter visual scale **0.90×**.
- **L5 Chaos Core** (`L5.glb`): LEGO-style brick castle courtyard with 61 tiles and rim cannons · fighter visual scale **0.85×**.

Scaled physics top-down layouts remain in Section 9.2; full asset paths are listed in Appendix C.

## 12.5 Hazard readability

- Every hazard telegraphs at least 1 s ahead in amber #FFB703: glowing cannon, floor cone, flashing ring seam, reversal arrows on the turntable, amber stripes on the obstacle boxes.
- Just before impact the signal switches to red #E63946 flashing at 2 Hz: breaking ice tile, dropping ring, fireball landing spot, the clock in the last 10 s.
- Amber and red are never used as decoration on an arena floor; the L1 ring bands use calm pastel colours.
- Signals never rely on colour alone: each one pairs colour with a shape (cone, ring with a cross, crack lines, stripes) for colour-blind players.

*Figure 17: Hazard identification board H01–H07 (IDs as in Chapter 7)*

## 12.6 Interface visual mockups

These mockups give the visual target for the interface specified in Chapter 11. The HUD shows the player's fighter and class badge, lives (Rank) or fighters left (Survival), the match clock, the session scoreboard, a hazard banner, Wobble counters above fighters, the dash cooldown and key hints.

*HUD, Rank rule · HUD, Survival rule*

*Main menu · Mode and rule select*

*3D lobby with the host console · Match result and podium*

*Figure 18: Screen flow*

*Heavy · Normal · Light · Lives · Respawn · Dash · Jump · Wobble*

*Clock · Fighters left · Crown · Ring drop · Air cannon · Fire cannon · Ice crack*

## 12.7 Audio direction

Sounds are soft and toy-like, squishy rather than hard, and every gameplay event has a sound that matches its visual cue. The effects below exist as procedural placeholders (44.1 kHz mono WAV) so they can be wired into Unity immediately and replaced later.

| File | Event | Direction for the final sound |
|---|---|---|
| sfx_bump | Light contact between two fighters | Short soft bump; volume follows impulse |
| sfx_squish_bump | Strong hit or dash hit | Soft jelly squelch; pitch rises with impulse and falls with the target's class |
| sfx_dash | Dash | Short whoosh synced with the trail |
| sfx_jump | Jump | Playful boing |
| sfx_cannon | Cannon charge and fire | Charge length matches the 1.0 s telegraph |
| sfx_ice_crack | Ice tile cracks and breaks | Split into crack and shatter |
| sfx_ring_drop | L1 ring warning and drop | Rising creak for 3 s, then a soft thump |
| sfx_obstacle_whoosh | An obstacle box approaches (L2) | Low whoosh that gets louder within 2 m |
| sfx_pusher_horn | A pusher block leaves its dock (L2) | Short toy horn, 1.0 s before departure |
| sfx_fall_off | Ring-out | Falling whistle with a distant poof |
| sfx_respawn | Rank respawn | Bright pop with a sparkle while invulnerable |
| sfx_win | Match win | 2–3 s jingle for the result screen |

| Music track | Mood | Length |
|---|---|---|
| Main menu | Relaxed toy-box music: music-box, wooden percussion and soft synths | 60–90 s loop |
| 3D lobby | Light and bouncy store ambience | 45–60 s loop |
| Match L1–L2 | Energetic, about 120 BPM | 90–120 s loop |
| Match L3–L5 | Driving, about 140 BPM | 90–120 s loop |
| Last 10 seconds | Extra drum layer on the match track | 10 s layer |
| Result and podium | Celebration | 10–15 s stinger |

Still to produce: UI sounds (hover, select, back, join, ready, 3-2-1-GO countdown), lobby footsteps and fighter-pick pop, air-gust and fireball loops, a last-10-seconds tick, a dizzy cue at high Wobble, turntable and pusher-block loops and an ice-slide loop. Music will come from CC0 or CC-BY sources or be composed by the team, and every file will be credited in Appendix C.

## 12.8 Known gaps

- Creative's 3D animal fighters (SB-76), STAR CATCHER claw-machine lobby (SB-76), UX/UI & Lobby Console kits (SB-75, SB-77) and L1–L5 environments (SB-78) have fully replaced the earlier concept art and are integrated in Unity.
- HUD, round score, Find Server, Lobby Console and WINNER captures in Section 12.6 show Creative's live UI assets rendered in Unity with internal test data.
- Per-map visual fighter scales (L1 0.95×, L2 0.80×, L3 0.60×, L4 0.90×, L5 0.85×) apply strictly to 3D presentation meshes; collision and balance calculations always use the standard radii 0.60 / 0.50 / 0.45 m in Section 6.1.
- Music and the remaining UI sounds are not produced yet (Section 12.7).
