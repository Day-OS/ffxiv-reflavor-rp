# ReflavorRP

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for FINAL FANTASY XIV that lets you roleplay the way you would on SA-MP or GTA World, while everyone else still sees normal FFXIV RP.

It does three things:

1. **Outgoing:** you type `/me`, `/s`, `/low`, `/w`, `/b` and `*actions*` like on GTA World, and it's sent as FFXIV style `/say` and `/em` lines.
2. **Incoming:** chat you receive is shown GTA World style (`Name says:`, purple `* Name` emotes, grey OOC, darker text for people further away). This only changes what **you** see.
3. **Accents:** regex based find and replace on your speech, with a couple of built-in templates.

## Commands

`/reflavor` opens the settings window. `/reflavorrp` and `/textrp` do the same.

## Outgoing: what you type vs. what gets sent

Speech gets quotes, a capital letter and ending punctuation. `*actions*` are kept as narration.

| You type | Sent as | Others see |
| --- | --- | --- |
| `hello there` (Say channel active) | `/say "Hello there."` | Legaia: "Hello there." |
| `/say oi *laughs*` | `/say "Oi." laughs.` | Legaia: "Oi." laughs. |
| `*coughs*` | `/em coughs.` | Legaia coughs. |
| `/me sighs. "yeah, right"` | `/em sighs. "Yeah, right."` | Legaia sighs. "Yeah, right." |
| `/em` / `/ame` | same as `/me` | |
| `/s help me` | `/em shouts, "Help me!"` | Legaia shouts, "Help me!" |
| `/s *slams the table* stop` | `/em slams the table. Shouts, "Stop!"` | Legaia slams the table. Shouts, "Stop!" |
| `/low come here` | `/em whispers, "Come here."` | Legaia whispers, "Come here." |
| `/w Ziero did you just?` | `/em whispers to Ziero, "Did you just?"` | Legaia whispers to Ziero, "Did you just?" |
| `/b brb` | `/say (( brb ))` | Legaia: (( brb )) |

The verbs (`shouts`, `whispers`, `whispers to`) can be changed in the settings. Typing text without a slash only gets converted while the Say channel is active; Party, FC, linkshells etc. are left alone.

## Incoming: how chat looks to you

With everything turned on, this is roughly what the chat log turns into. Speech uses your normal text color, emotes are purple and OOC is grey:

```math
\begin{array}{l}
\textsf{Legaia says: Do you seek adventure?} \\
\textsf{Legaia says: Hey. }{\color{#C2A2DA}\textsf{*she laughs.*}} \\
{\color{#C2A2DA}\textsf{* Anastasia smiles. }}\textsf{"Not a thing to apologize for."}{\color{#C2A2DA}\textsf{ She leans back.}} \\
\textsf{Ziero shouts: Help!} \\
{\color{#9A9A9A}\textsf{(( Legaia: brb ))}}
\end{array}
```

| Received | Shown as |
| --- | --- |
| `Legaia: "Do you seek adventure?"` | Legaia says: Do you seek adventure? |
| `Legaia: "Oi." ri.` / `Legaia: *waves* hey` | speech plus the action in purple between `*` |
| `Anastasia smiles. "Not a thing to apologize for."` (emote) | `* Anastasia …` in purple, the quoted part in normal color |
| `Legaia: brb` (no quotes, no `*actions*`) | `(( Legaia: brb ))` in grey, treated as OOC |
| `Ziero: Help!` (shout / yell) | Ziero shouts: Help! |

Some other details:

- The world name is dropped from names (`Legaia Lindblum` instead of `Legaia Lindblum ⬥Balmung`). The name stays clickable.
- Any kind of quote counts (`"` `“` `”` `„` `«` `»` …), including mismatched ones and posts split over several messages.
- **Distance fade:** the further away the speaker, the darker the line, like GTA World. Your own lines are never faded.

  | Distance (yalms) | < 4 | 4–8 | 8–12 | 12–16 | 16+ |
  | --- | --- | --- | --- | --- | --- |
  | Brightness | 100% | 87% | 74% | 61% | 48% |

## Accents

Accents only touch what you **say**: quoted speech, `/s`, `/low`, `/w`. Actions, narration and `/b` are never changed.

An accent is a list of rules (regex pattern → replacement) applied in order. Patterns are case insensitive and the replacement keeps the case of what it replaced, so `Sorry` becomes `Thorry` and `STOP` becomes `THTOP`. Use `$1`, `$2` for capture groups.

Two templates are built in:

| Template | Rules | Example |
| --- | --- | --- |
| Frontal lisp | `s(?!h)` → `th`, `z` → `th`, `c(?=[eiy])` → `th` | "Thorry, I really love thith nithe plathe." |
| Uwufication | `[rl]` → `w`, `n([aeiou])` → `ny$1`, `ove` → `uv` | "Sowwy, I weawwy wuv this nyice pwace." |

Templates can't be edited, but **Copy active accent** turns one into your own accent that you can change. Custom accents are saved in the plugin config.

## Compatibility and known issues

- **Chat 2 and other chat plugins.** With *Rewrite full lines* on, the original line is hidden and a new one is printed in the **Echo** channel (the only way to get `Name says:`). Plugins that draw their own chat window may show duplicates or miss lines, and chat tabs that filter by channel will treat these lines as Echo. Turn *Rewrite full lines* off and the plugin only recolors and cleans up the original message instead.
- **ChatProximity.** Both plugins fade text by distance. On Say, Emote, Shout and Yell this plugin's colors win; other channels are left to ChatProximity. If they fight, turn one of them off.
- **Item links, auto-translate, map links.** Messages containing those are not restyled, since rebuilding them would break the links.
- **`/s` and `/me`.** In FFXIV `/s` is short for `/say`; with conversion on it means *shout* instead. `/me` normally plays an emote animation; the *Replace the /me emote* setting decides which one you get.
- **Macros** go through the same conversion as typed commands.
- **Split posts with straight quotes.** The middle part of a long post that only closes a quote (`...and that's why." she said`) can't be told apart from one that opens it, so speech and action get swapped for that part. Curly quotes (`”`) are detected correctly.
- **Distance fade** only works for players currently loaded around you; anyone else is shown at full brightness.
- **Everyone else sees normal chat.** Incoming styling is local only.

## Installing

ReflavorRP isn't in the official plugin list, so it's installed from a custom repository:

1. In game, open `/xlsettings` → **Experimental**.
2. Under **Custom Plugin Repositories**, add this URL, tick **Enabled** and save:
   ```
   https://raw.githubusercontent.com/Day-OS/ffxiv-sampstyled-rp/master/repo.json
   ```
3. Open `/xlplugins`, search for **ReflavorRP** and install it.

## Releasing

Push a version tag and the [release workflow](.github/workflows/release.yml) does the rest:

```
git tag v0.1.0
git push origin v0.1.0
```

It builds the plugin with that version, creates a GitHub release with `latest.zip`, and updates `repo.json` on `master` so installed copies pick up the update.

## Building

You need XIVLauncher with Dalamud (run the game with it at least once) and the .NET 10 SDK.

1. Open `ReflavorRP.slnx` in Visual Studio, Rider or VS Code, or run `dotnet build` in the repo folder.
2. The plugin ends up in `ReflavorRP/bin/x64/Debug/ReflavorRP.dll`.
3. In game, open `/xlsettings` → **Experimental** and add the full path to that DLL under **Dev Plugin Locations**.
4. Open `/xlplugins` → **Dev Tools** → **Installed Dev Plugins** and enable ReflavorRP.

Watch out for the output folder: building with the `x64` platform writes to `bin/x64/Debug`, building as `Any CPU` writes to `bin/Debug`. Point Dalamud at the one your IDE actually builds, or rebuilds won't show up in game.

## Project layout

| File | What it does |
| --- | --- |
| `Plugin.cs` | Entry point, commands, and the hook on the game's command execution. Dalamud's command API only receives commands the game doesn't know, so overriding `/me` and `/say` needs a hook. |
| `RpFormatter.cs` | Outgoing conversion (GTA World input → FFXIV RP line). |
| `ChatStyler.cs` | Incoming chat restyling and distance fade. |
| `Accent.cs` | Accent rules and the built-in templates. |
| `Configuration.cs` | Saved settings. |
| `Windows/ConfigWindow.cs` | Settings window. |

## Credits and license

The project skeleton (project file, build setup, plugin entry point layout) started from goatcorp's [SamplePlugin](https://github.com/goatcorp/SamplePlugin) template. The chat conversion, chat styling and accent code is ReflavorRP's own.

Licensed under the [Mozilla Public License 2.0](LICENSE.md).
