# Readability and contribution rules

Prefer code a maintainer can understand on the first reading.

- Use descriptive names, simple control flow, and cohesive methods.
- Prefer explicit types when they clarify intent; use `var` when the type is obvious.
- Use braces for control-flow blocks and four-space C# indentation.
- Avoid clever one-liners, unnecessary generic abstractions, reflection, and premature frameworks.
- Keep runtime-specific types outside Model and Modules.
- Modules may reference Model but must not reference other modules.
- Extend the model only when a capability demonstrates a need.
- Preserve meaningful error context and avoid silent failure.
- Add focused tests for contract changes and behavioral fixes.
- Run `dotnet build`, `dotnet format --verify-no-changes`, and `git diff --check` before review.

Compiler and reliability warnings are errors; stylistic preferences are suggestions except consistent formatting, unused imports, namespace style, and braces.
