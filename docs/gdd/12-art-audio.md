# 12. Art and audio

> **In this chapter:** The look and sound of Squishy Bump: mood board, characters, palette, level targets, hazard readability, interface mockups and audio.

All visual material in this chapter is AI-generated or produced procedurally by code that an AI assistant (Claude) wrote, and it is declared asset by asset in Appendix C. It defines the target look for implementation; final in-engine art may be rebuilt from these references.

## 12.1 Visual direction and mood board

Squishy Bump looks like a toy store after closing time: warm shelf lights, wooden toy tables, play mats and building blocks, seen through a slightly shallow depth of field so the background stays soft. The six fighters are glossy squishy jelly fruits with a visible highlight, a slightly translucent rim and a darker base, and they squash and stretch on every landing and hit. Arena floors stay calm and nearly empty so that fruits and warnings are always the most readable things on screen. There is no blood and no weapon; impact is shown only through jelly squash, speed lines and impact rings.

*Figure 11: Key art: six jelly fruits bumping on a play mat in the toy store (AI image)*

*Figure 12: Mood board: characters, toy store, jelly material, play mat, cannons, ice, emotion and impact*

## 12.2 Characters and weight classes

Every fighter is a round jelly fruit with dot eyes, blush and stubby arms. Each fruit has its own body colour and one recognisable top feature (stem, leaf, crown or calyx), so its colour and silhouette identify the player. The weight class is shown by size: Heavy fruits are 1.2× and Light fruits 0.9× the Normal radius, and a class badge appears next to the name in the lobby and on the HUD. The 3D prototypes use the radii of Section 6.1 exactly; leaves and stems are visual only and never part of the collider .

| Fruit | Class | Body | Silhouette cue |
|---|---|---|---|
| Watermelon | Heavy | #5CC46E | Wide oval body with dark wavy stripes, short stem |
| Pineapple | Heavy | #FFC93C | Tall body with a diamond pattern and a crown of five leaves |
| Apple | Normal | #EF4B4B | Round body, top dimple, brown stem and one leaf |
| Mangosteen | Normal | #7A4FA3 | Deep purple body with a four-lobed green calyx |
| Peach | Light | #FFA3B5 | Soft pink body with a cleft line and a leaf |
| Blueberry | Light | #4D7CDB | Blue body with a small star-shaped crown and light speckles |

*Figure 13: Fruit line-up by weight class, true radius ratio 1.2 : 1.0 : 0.9*

*Figure 14: Expression sheet: neutral, happy, dizzy (high Wobble), panic when falling, victory*

*Figure 15: Low-poly 3D prototypes of the six fruits, exported as .glb in metres (Blender, procedural)*

## 12.3 Palette, type and style guide

| Role | Hex | Rule |
|---|---|---|
| Fruit colours | Six hues | #5CC46E #FFC93C #EF4B4B #7A4FA3 #FFA3B5 #4D7CDB; each fruit's colour is also its player colour (name tag, scoreboard dot) |
| Toy-store wood | #C68B59 | Toy tables, shelves and cannon carriages |
| Store backdrop | #CDB4DB → #FFC8DD, #FFE8D6 | Pastel wall gradient and cream paper behind every arena; always out of focus |
| L1 ring bands | #CDB4DB #A8E6CF #FFE8D6 #BDE0FE | Outer to inner; calm pastels so they never read as warnings |
| Ice | #BDE0FE | Ice tiles and air gusts only |
| Warning | #FFB703 | Reserved for telegraphs; never decoration |
| Danger | #E63946 | Reserved for imminent impact (flashing 2 Hz) |
| Ink | #1F2340 | Text and outlines |

The Apple's red (#EF4B4B) is close to the danger red, so danger signals always combine the flashing red with a shape (ring with a cross, crack lines, edge stripes) and never appear as a solid red blob on the floor. Type: Baloo 2 for all interface and diagram text (it supports Vietnamese diacritics, so both language builds share one layout) and Fredoka for the logo and large numbers such as the clock and ranks. Both fonts use the SIL Open Font License. AI images never contain text; every label is added afterwards in SVG. UI outlines are #1F2340, 8 px on a 256 px icon, with round joins, 22–34 px panel corners and a solid 8 px button shadow without blur.

*Figure 16: Palette and style guide sheet*

## 12.4 Level visual targets

Each level is a different play area of the same toy store and has a key art image that sets its mood and lighting: the play mat on the front table (L1), the spinning display stand (L2), the toy-soldier corner with its cannons (L3), the freezer of ice lollies (L4) and the top-shelf showcase (L5). The scaled top-down layout sheets drawn from the numbers in Chapters 6, 7 and 9 (grid 1 m, scale bar 5 m, six spawn points) are shown with each arena in Section 9.2.

*L1 Training Ring · L2 Moving Ground*

*L3 Cannon Court · L4 Frost Ring*

*L5 Chaos Core · L1 3D prototype with the four ring bands (Blender)*

Figure note: the generator used for each key art (AI image or Blender render) is listed in Appendix C.

## 12.5 Hazard readability

- Every hazard telegraphs at least 1 s ahead in amber #FFB703: glowing cannon, floor cone, flashing ring seam, reversal arrows on the turntable, amber stripes on the obstacle boxes.
- Just before impact the signal switches to red #E63946 flashing at 2 Hz: breaking ice tile, dropping ring, fireball landing spot, the clock in the last 10 s.
- Amber and red are never used as decoration on an arena floor; the L1 ring bands use calm pastel colours.
- Signals never rely on colour alone: each one pairs colour with a shape (cone, ring with a cross, crack lines, stripes) for colour-blind players.

*Figure 17: Hazard identification board H01–H07 (IDs as in Chapter 7)*

## 12.6 Interface visual mockups

These mockups give the visual target for the interface specified in Chapter 11. The HUD shows the player's fruit and class badge, lives (Rank) or fruits left (Survival), the match clock, the session scoreboard, a hazard banner, Wobble counters above fruits, the dash cooldown and key hints.

*HUD, Rank rule · HUD, Survival rule*

*Main menu · Mode and rule select*

*3D lobby with the host desk · Match result and podium*

*Figure 18: Screen flow*

*Heavy · Normal · Light · Lives · Respawn · Dash · Jump · Wobble*

*Clock · Fruits left · Crown · Ring drop · Air cannon · Fire cannon · Ice crack*

## 12.7 Audio direction

Sounds are soft and toy-like, squishy rather than hard, and every gameplay event has a sound that matches its visual cue. The effects below exist as procedural placeholders (44.1 kHz mono WAV) so they can be wired into Unity immediately and replaced later.

| File | Event | Direction for the final sound |
|---|---|---|
| sfx_bump | Light contact between two fruits | Short soft bump; volume follows impulse |
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

Still to produce: UI sounds (hover, select, back, join, ready, 3-2-1-GO countdown), lobby footsteps and fruit-pick pop, air-gust and fireball loops, a last-10-seconds tick, a dizzy cue at high Wobble, turntable and pusher-block loops and an ice-slide loop. Music will come from CC0 or CC-BY sources or be composed by the team, and every file will be credited in Appendix C.

## 12.8 Known gaps

- Most arena and character images are scripted Blender renders rather than painted concept art; they are accurate in geometry and colour but plainer than final art (Appendix C).
- Mockup numbers (clock, points, cooldowns, chosen fruits, lobby state) are illustrative examples, not test results.
- AI images are references only; in-engine models follow the radii and colours of Sections 6.1 and 12.2.
- Music and the remaining UI sounds are not produced yet (Section 12.7).
