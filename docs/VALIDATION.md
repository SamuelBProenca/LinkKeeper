# Validation of foundation increment

SDK 10.0.402. Build succeeded: zero warnings, zero errors.
xUnit: 23 passed, zero failed, zero skipped.
Manual CLI: vacancy alias and seven-day UTC expiry returned validation-only JSON.
Staged whitespace check passed.

The first run in the mounted workspace produced empty dependency DLL files and
failed assembly loading. A clean copy under /tmp compiled and passed all 23 cases;
no test was removed or weakened. Generated files are excluded from the delivery.

GitHub branch creation returned 403 Resource not accessible by integration.
No remote branch, commit, PR, merge, or CI run was created. Repository public read
access does not establish write access for this integration.

CI is defined but has not run on GitHub. It pins checkout/setup-dotnet to resolved
v4 commit SHAs. Production libraries use no third-party packages. Tests use xUnit.

Next step: grant the installed GitHub integration repository access, then publish
the local foundation branch and open a PR to main. Do not merge automatically.
