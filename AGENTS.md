# LinkKeeper engineering constraints

Small reviewable PRs. Never push directly to main or merge without user instruction.
Do not mix the unrelated mobile app blocker into this product.
Only HTTP/HTTPS URLs; never download submitted URLs in this increment.
UTC timestamps; injectable TimeProvider; jobs=30 days, fun/art=permanent.
Never log message text, credentials, or private links. Never commit secrets.
No fabricated success: distinguish executed checks from planned checks.
Before changing behavior write a requirement-based test; convert discovered bugs into regressions.
Read the diff, question assumptions, and avoid unrelated refactors or speculative infrastructure.

Commands:
- dotnet build src/LinkKeeper.Cli -c Release
- dotnet test tests/LinkKeeper.Tests -c Release

Adapted for this project after reading Fabio Akita, not copied third-party skills:
https://ailair.akitaonrails.com/pt-br/workflow/
https://akitaonrails.com/2026/09/17/falando-um-pouco-sobre-minhas-skills-de-ia/
