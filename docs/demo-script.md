# A02 demo and code walkthrough

Source: [GDD 14.5](gdd/14-validation-rubric-map.md#145-evidence-for-assignment-02). Track rehearsal in #51. #47 gates the final demo build.

## Setup

Use the verified Windows build from #47 on two PCs on the same LAN. Keep both displays visible. Record the build version and commit, confirm UDP 7777 is reachable, and assign Duy to host and Nhật to join. Keep the build, credits, change log, test record, and playtest report ready for questions.

## Run of show

| Time | Demonstrate | Watch for | Readiness |
|---|---|---|---|
| 0:00–0:45 | Duy hosts a LAN room. Nhật joins by IP. Both pick fruits and ready up in the 3D lobby. | Both screens show the same roster and ready state before the host starts. | Waiting on Nhật's 3D lobby (#23). |
| 0:45–1:30 | On L3 Cannon Court, enter the same wind cone with a Heavy fruit and a Light fruit on comparable footing. | Show the warning, then compare how far both fruits move under the gust. | Rehearse on L3. |
| 1:30–2:15 | In Rank, knock a fighter with a life remaining off the arena. | Show the life decrement, credited score and safe respawn on both screens. | Waiting on Nhật's Rank and match-event sync (#25, #22), then respawn wiring (#26). |
| 2:15–2:45 | Show a bot moving out of the cannon cone during the warning. | Keep the bot and cone on screen through the shot. | Waiting on Nhật's bot behavior (#38). |
| 2:45–5:15 | Follow one full match through its last elimination or time limit. Show the result, then return to the lobby. | Compare final ranks on both screens and show that the flow can start again. | Rehearse with the final lobby and match-event sync (#23, #22). |

## Code walkthrough

1. Duy opens one of his physics, hazard or presentation commits. Explain the GDD rule, trace the code in the scene, and show the test or capture used to verify it.
2. Nhật opens one of his match, bot or network-session commits. Explain the host decision, what the client sees, and how the test exercises a failure or edge case.
3. Each person identifies the AI-assisted work in their example and explains what they reviewed, changed, and verified. Use [CREDITS.md](../CREDITS.md) and exact commits or PRs during rehearsal.
