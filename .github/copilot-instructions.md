# Copilot Instructions for R2.ShopNet

## Big Picture Architecture
- **Service-Oriented Solution**: Organized into `src/Services`, `src/Framework`, `src/Gateway`, and `src/Web`. Each service (e.g., Catalog, Identity) is a distinct boundary with its own data and logic.
- **Framework Layer**: Shared code lives in `src/Framework`, including base entities, error/result patterns, and GUID v7 support (see `R2.ShopNet.Framework.Common`).
- **Specs-Driven Development**: All major changes are spec-driven. See `openspec/AGENTS.md` and `openspec/specs/` for requirements, scenarios, and technical decisions. Always review `proposal.md`, `tasks.md`, and `design.md` before implementing.

## Developer Workflows
- **Build**: Use the VS Code task `build-apphost` or run `dotnet build R2.ShopNet.sln -c Debug` from the root.
- **Testing**: Tests are under `tests/`. Use standard .NET test runners. Check for service-specific test projects.
- **Specs**: For new features or changes, follow the OpenSpec workflow:
  1. Create a unique verb-led change ID under `openspec/changes/`
  2. Scaffold `proposal.md`, `tasks.md`, and spec deltas
  3. Validate with `openspec validate <id> --strict`
  4. Do not implement until proposal is approved

## Project-Specific Conventions
- **Entity IDs**: All entities use GUID Version 7 (RFC 9562) via `GuidGenerator.NewGuidV7()`
- **Error Handling**: Use predefined error types (NotFound, Validation, Conflict, etc.) from the Framework
- **Result Pattern**: Return results using the Framework's result types
- **Specs Format**: Requirements must have at least one scenario. Use `## ADDED|MODIFIED|REMOVED Requirements` in spec deltas.
- **File References**: Use `file.ts:42` format for code locations in documentation/specs

## Integration Points & Dependencies
- **Admin Dashboard**: `temp-tailadmin/` contains an Angular + Tailwind CSS dashboard template. See its README for setup and usage.
- **External Services**: Service discovery, identity, and persistence are handled via Framework and Gateway layers. See respective directories for implementation details.
- **OpenSpec**: All cross-component changes must be reflected in specs and proposals. Specs are the source of truth.

## Key Files & Directories
- `openspec/AGENTS.md`, `openspec/project.md`: Spec-driven development instructions and conventions
- `src/Framework/R2.ShopNet.Framework.Common/README.md`: Core framework patterns and examples
- `temp-tailadmin/README.md`: Admin dashboard setup and usage
- `docs/`: Implementation guides, design patterns, troubleshooting, and migration notes

## Example Patterns
- **GUID v7 Usage**:
  ```csharp
  var id = GuidGenerator.NewGuidV7();
  bool isV7 = GuidGenerator.IsGuidV7(id);
  ```
- **Spec Delta Example**:
  ```markdown
  ## ADDED Requirements
  ### Requirement: Two-Factor Authentication
  #### Scenario: User provides second factor during login
  ```

---

For unclear or missing conventions, review `openspec/AGENTS.md` and ask for clarification before proceeding.
