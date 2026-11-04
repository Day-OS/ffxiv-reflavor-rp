# ReflavorRP: your first roleplay session

![ReflavorRP](Data/readme-title.svg)

This guide uses **Legaia Lindblum** as your character and **Raih'li Tia** as your conversation partner. Replace those names and the example world with real ones when sending a Tell.

## 1. Set up your chat

Follow the [installation instructions](README.md#installing), then open `/reflavor`.

The **Disable plugin** button at the top pauses all outgoing conversion, accents and incoming styling. Click **Enable plugin** to resume with your saved settings. You can still open the window and change settings while the plugin is paused.

| Tab | Setting | What it does |
| --- | --- | --- |
| Outgoing | Conversion enabled | Formats messages you send. |
| Outgoing | Replace the /me emote | Makes `/me` write an action instead of playing the game's emote animation. |
| Outgoing | Replace /s with RP shout | Makes `/s` narrate a shout. Turn off to leave `/s` as the game's native Say shorthand. |
| Incoming | GTA World style chat | Restyles Say, custom emotes, eligible Shout/Yell and both directions of Tell. |
| Incoming | Darker text for distant players | Dims nearby players' Say and emote lines according to distance. |
| Incoming | Highlight my character's name | Highlights mentions of your first and last name separately in gold. |
| Accents | Active accents | Applies selected speech transformations in order. Leave empty to start without an accent. |

**Enable Echo in your chat tab's filters.** Restyled messages appear as local Echo lines, including Tells. Nothing is sent to other players by incoming styling.

**Other chat plugins are incompatible**, including Chat 2 and ChatProximity. Do not combine them with ReflavorRP.

## 2. Read the colors

![Color guide: light speech, purple actions, grey OOC and orange Tell prefixes](docs/images/chat-colors.svg)

| Color | Meaning |
| --- | --- |
| Light grey `#E6E6E6` | Spoken dialogue. |
| Purple `#C2A2DA` | Actions and narration. |
| Grey `#AFAFAF` | Out of character, or OOC. |
| Orange `#FFA64D` | The Tell contact prefix. |
| Gold `#FFD166` | Your character's name and custom important words mentioned in a message. |

The previews below illustrate text and colors against a dark background. They do not reproduce the game's exact font, timestamps or UI. Names in images are static; eligible names in game retain their native player links.

## 3. Speak, act, or combine both

Use `/say` for dialogue, `/me` for actions, and `*asterisks*` for actions inside a spoken message. For dialogue inside an emote, use straight double quotes.

Inside double quotes, asterisks are emphasis, not an action. For example, `/say "I *really* mean it" *waves*` keeps `*really*` in the speech color and shows `waves.` as a purple action. Keep emphasis inside the quotes and actions outside them.

| Case | Type this | FFXIV command sent |
| --- | --- | --- |
| Simple dialogue | `/say hello there` | `/say "Hello there."` |
| Dialogue with an action | `/say hello *waves*` | `/say "Hello." waves.` |
| Action only | `/me waves` | `/em waves.` |
| Action with dialogue | `/me smiles. "good morning"` | `/em smiles. "Good morning."` |
| Action written with asterisks | `/say *waves*` | `/em waves.` |
| Several actions between dialogue | `/say hello *waves* *smiles* take a seat` | `/say "Hello." waves. smiles. "Take a seat."` |
| OOC | `/b one moment` | `/say (( one moment ))` |

With incoming styling enabled, those messages look like this:

![Public chat examples showing dialogue, actions, mixed narration and OOC](docs/images/public-chat.svg)

The game adds your character's name to native `/em` messages. **Write `/me waves`, without adding your own name.** `/em` and `/ame` also work; `/me` requires its replacement setting to be enabled.

With the Say channel active, you can type `hello there` without `/say`. Bare text in other active channels is left alone. For consistent formatting in another channel, use its explicit prefix as shown below.

### Why did someone's message become OOC?

Incoming Say without quotes or marked actions is treated as OOC. For example, a received Say body of `brb` becomes `(( Raih'li Tia: brb ))`.

Your outgoing `/say brb` becomes quoted speech automatically. Use `/b brb` when you mean OOC. Plain incoming Tell is treated as speech; use `(( ... ))` for OOC there.

## 4. Shout or whisper as part of a scene

| Type this | FFXIV command sent |
| --- | --- |
| `/s help me` | `/em shouts, "Help me!"` |
| `/s *slams the table* stop` | `/em slams the table. Shouts, "Stop!"` |
| `/low come here` | `/em whispers, "Come here."` |
| `/w Rai did you hear that?` | `/em whispers to Rai, "Did you hear that?"` |

**`/low` and `/w` describe a whisper in a local emote. They do not send a private message.** Use `/tell` for privacy.

Likewise, ReflavorRP's `/s` describes shouting through `/em`; it does not send to the Shout channel. This overrides FFXIV's usual `/s` shorthand for Say while conversion and **Replace /s with RP shout** are enabled. Turn that setting off under **Outgoing** to leave `/s` untouched. Use `/say` when you want formatted dialogue with the replacement disabled.

The generated words `shouts`, `whispers` and `whispers to` can be changed under **Outgoing → Verbs**.

Actual incoming Shout/Yell uses a `Name shouts:` header, with actions in purple, when the sender is loaded nearby and has the Role-playing status. Other Shout/Yell messages keep their native appearance.

## 5. Have a private conversation with Tell

Put the recipient before your message or RP command:

```text
/tell Raih'li Tia@Balmung hello
/tell Raih'li Tia@Balmung /me waves
/tell Raih'li Tia@Balmung hello! *She coughs*
/tell Raih'li Tia@Balmung /b one moment
```

The first sends `"Hello."`. The second sends `Legaia Lindblum waves.`. The third sends `"Hello!" She coughs.`. The last sends `(( one moment ))`. All four stay in Tell.

### Follow the contact and the author

The orange `[TELL:Raih'li Tia]` identifies the **contact**, in both directions. The name after it identifies **who spoke**:

![Tell conversation with incoming and outgoing speech, emotes and OOC](docs/images/tell-chat.svg)

The illustrated conversation uses these message bodies:

| Direction | Body sent or received |
| --- | --- |
| Incoming from Raih'li | `"Welcome!"` |
| Outgoing from you | `"A menu, please."` |
| Incoming emote | `Raih'li Tia slides over a menu. "Here you go."` |
| Outgoing emote | `Legaia Lindblum nods. "Thank you."` |
| Outgoing OOC | `(( one moment ))` |

For the outgoing emote, type `/tell Raih'li Tia@Balmung /me nods. "thank you"`; the formatter supplies your name.

Tell narration must already contain a name when one is needed. Incoming styling never invents an author inside an emote: a received `*waves*` becomes `[TELL:Raih'li Tia] * waves`, not `* Raih'li Tia waves`.

The contact remains clickable when the original message supplies a native player link. A name merely written in narration is ordinary text. The visible world suffix is removed from linked display names.

### Reply quickly

```text
/reply thank you
/reply /me nods
/reply /b back in a minute
```

`/r` is an alias for `/reply`. FFXIV chooses the reply recipient normally.

## 6. Use Party, linkshells and other channels

Put the channel first. The RP command formats the body while the channel prefix keeps its destination.

| Type this | Sent as |
| --- | --- |
| `/party /me waves` | `/party Legaia Lindblum waves.` |
| `/party hello everyone` | `/party "Hello everyone."` |
| `/l1 /say on my way` | `/l1 "On my way."` |
| `/cwl1 /b five minutes` | `/cwl1 (( five minutes ))` |
| `/fc /me raises a glass` | `/fc Legaia Lindblum raises a glass.` |
| `/alliance /say ready` | `/alliance "Ready."` |

Supported routes include Party (`/party`, `/p`), Tell (`/tell`, `/t`), Reply (`/reply`, `/r`), Free Company (`/freecompany`, `/fc`), Alliance (`/alliance`, `/a`), linkshells (`/linkshell`, `/l`) and cross-world linkshells (`/cwlinkshell`, `/cwl`), including numbered linkshells 1–8.

These channels have no native custom-emote layout, so routed actions include your character's name in the message body. Incoming Party, FC, Alliance and linkshell messages keep their native appearance; they do not receive the public-chat styling shown above.

**Selecting Party and then typing a bare `/me` does not route the emote to Party.** Use `/party /me ...` explicitly. A channel command without a body is left alone so you can still select channels normally.

## 7. Try an accent

Open **Accents**, select a template under **Active accents**, and type in the preview field before sending anything.

For example, **Uwufication** changes spoken `"Sorry, I really love this nice place."` to `"Sowwy, I weawwy wuv this nyice pwace."`.

Accents affect speech, including quoted dialogue inside `/me`. They leave actions, narration and `/b` unchanged. You can combine accents in order, remove an active accent, or use **Copy active accent** to make an editable copy of a template.

## 8. Understand distance and messages kept unchanged

With distance fading enabled, Say and custom emotes from loaded players get darker as they move away:

| Distance | Brightness |
| --- | --- |
| Under 4 yalms | 100% |
| 4 to under 8 | 87% |
| 8 to under 12 | 74% |
| 12 to under 16 | 61% |
| 16 or more | 48% |

Your own lines remain at full brightness. Tell, Shout and Yell never fade. Players who cannot be found among the loaded characters are not distance-faded.

Messages containing item links, map links or auto-translate are kept in their native format to preserve those features. This is expected.

## Chat bubbles

Enable **Incoming → Chat bubbles → Alternative chat bubbles** for overhead messages with character nameplates: white for speech, purple for actions and grey for OOC. Overlapping bubbles stack in conversation order, with older messages above newer ones.

Longer messages stay longer. Lower **Bubble reading speed (characters/s)** to give yourself more reading time. The side bar shows the active bubble's remaining time; timers slow down while bubbles are offscreen or hidden behind walls.

Custom bubbles temporarily replace the game's native bubbles. Your previous native-bubble setting is restored when you disable them or unload the plugin.

## Offscreen speech direction

Under **Incoming → Speech direction**, enable **Indicate speech outside the camera view** for a soft purple glow on the left or right screen edge when another loaded player speaks outside your camera view. Look toward the speaker to dismiss the glow. A speaker directly behind you can light both sides.

Customize **Indicator color**, **Indicator width** (20–600 scaled pixels), **Indicator height** (10–100% of the screen) and **Indicator opacity** (0–100%). Defaults are purple, 180 pixels wide, 60% height and 65% opacity. Width is capped at half the screen. Distance and fade timing still reduce the chosen opacity during normal use.

Click **Preview indicator (4 seconds)** to show both screen edges without a message or sound. The preview works even with the indicator disabled and reflects appearance changes immediately; click again to restart it.

The cue lasts up to four seconds, fades in and out, and gets weaker with distance from your character. **Speech indicator range (yalms)** sets where it fades to zero (default: **30**, adjustable from **5 to 100**). Multiple speakers on the same side do not stack the brightness.

This option is off by default and requires **GTA World style chat**. It works with either native or alternative chat bubbles. It only responds to public dialogue processed by the plugin, including quoted speech inside emotes; Tell, OOC, your own messages and actions without speech do not trigger it.

## Speech sounds

Under **Incoming → Speech sounds**, enable **Play a quiet sound for speech** to play a short voice sound (about **220 ms**) for another player's public dialogue or an incoming Tell with speech, including dialogue inside emotes. **This works whether the speaker is visible or offscreen, and does not require the visual speech direction indicator.** OOC, your own speech (including outgoing Tell) and actions without dialogue do not trigger it. It requires **GTA World style chat**, but works with either native or alternative bubbles.

Public speech uses left/right stereo direction relative to your camera and volume falling with distance from your character. **Incoming Tell plays centered, without distance attenuation**, so it works even when the sender is elsewhere or on another world; it does not trigger a directional screen glow. **Speech sound volume** defaults to **100%**, adjustable from **0 to 200%**. **Public speech sound range (yalms)** defaults to **30**, adjustable from **5 to 100**, independently of the visual indicator's range. FFXIV master/system sound volume and mute rules still apply. Each message gets one attempt when it arrives, and a global **1.5-second interval** shared by public speech and Tell prevents bursts of overlapping notifications. Sound is off by default and is never transmitted to other players. The sound asset is licensed for non-commercial use under **CC BY-NC-SA 3.0**; see its [attribution and license](Data/Audio/ATTRIBUTION.md).

## Player nameplates by distance

Under **Incoming → Player nameplates**, enable **Hide distant players' names and titles** and choose a distance from **5 to 100 yalms** (default: **20**). The distance is applied when you release the slider or finish editing its value. Other players' native overhead names, titles and FC tags are hidden beyond that distance and return when they come closer, according to your native display settings.

This option is off by default. It leaves your own name, NPCs, enemies, HP bars and chat bubbles unchanged. It works independently of incoming chat styling. Disabling the option, pausing ReflavorRP or unloading the plugin restores the game's normal nameplate display without changing your saved game settings.

## Important words and name mentions

Under **Incoming → Important words**, enable **Highlight my character's name** to highlight your first and last name independently. For a character named Legaia Lindblum, both `Legaia` and `Lindblum` are recognized, regardless of capitalization.

Add custom words or phrases to the box, one per line:

```text
Dancing Daisy
menu
Rai
```

Matches use gold (`#FFD166`) in message bodies, including dialogue, narration and OOC. The surrounding speech or action color resumes after each match. Sender labels and Tell contact prefixes keep their existing colors and player links. Highlights only affect your local styled chat, not outgoing messages or overhead bubbles.

Matches also work inside words: `Rai` highlights that part of `Raih`, and `menu` highlights that part of `menus`. This applies to character names and custom entries. Phrases match the text you entered, and punctuation such as `C++` is treated literally. If phrases overlap, the longer match at the same position takes priority. Distance fading also applies to highlights. Turn off the name option or remove custom entries to stop those highlights.

Enable **Play a sound on highlights** in the same section to hear the game's `<se.1>` notification when a received message contains a name or custom term. It plays once per matching message, even with several matches. Your own messages stay silent. This option is off by default and only applies to messages processed by incoming styling.

## Quick fixes

| What happened? | What to check |
| --- | --- |
| Styled messages disappeared | Enable Echo in the current chat tab. |
| A Tell appears in an unexpected tab | Restyled Tells are local Echo lines; the tab's Echo filter controls visibility. |
| `/me` played an animation | Enable **Replace the /me emote**. |
| A whisper was visible to nearby players | `/low` and `/w` are local narration. Use `/tell` for private messages. |
| Party or linkshell text was not converted | Include the channel prefix in the message, such as `/party /me waves`. |
| A Say message became grey OOC | Incoming unquoted Say with no marked action is treated as OOC. Use quotes for dialogue. |
| A linked message was not restyled | Unsupported payloads keep their native appearance. |
| A split post has speech and actions swapped | Unmatched straight quotes are ambiguous. Closing curly quotes (`”`) can identify a continuation that starts in speech. |

For settings, installation and technical details, see the [README](README.md).
