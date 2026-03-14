# AGENTS.md

## Working mode
- Build working code, not just plans.
- Make reasonable assumptions and keep going.
- Prefer a complete MVP over partial overengineering.

## Stack
- C#
- .NET 8
- Logi Actions SDK
- SmartThings REST API

## Product priorities
1. Scenes
2. Devices
3. Live status polling
4. Settings/config
5. Tests and docs

## Rules
- Keep dependencies minimal.
- Keep architecture modular.
- Never hardcode device-specific assumptions when capability detection can be used.
- Handle API failures gracefully.
- Avoid blocking on OAuth; PAT-based local dev comes first.
- Write tests for mapping, formatting, polling, and auth failure cases.
- Update README as the project takes shape.

## Definition of done
- Solution builds
- Tests run
- Core scene and device flows exist
- Local config is documented
- Repository is understandable by another developer

One more thing: no AI IDE will reliably nail 100% of this in one shot. The smart play is:

paste the big prompt,

let it scaffold/build,

then give a second prompt like:
"Now fix all build errors, run tests, and finish only the scene flow first."

That usually lands better than forcing full washing machine + TV + folders + settings + tests all at once.
