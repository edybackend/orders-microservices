# Engineering rules for AI agents

This file is loaded by Claude Code in every session. It defines how changes are made in this repository.
Hooks in `.claude/settings.json` enforce the parts that can be automated.

## Workflow
1. **Spec first.** A new behavior starts as a spec in `specs/NNN-name.md` (context, behavior, acceptance criteria, out of scope). Code follows the spec, not the other way around.
2. **Small steps.** One concern per commit, [Conventional Commits](https://www.conventionalcommits.org/) (`feat(orders): ...`, `fix(notifications): ...`).
3. **Definition of done** = the CI gates pass locally: `dotnet build` (warnings are errors), `dotnet test`, `dotnet format --verify-no-changes`, `go vet`, `go test -race`, `golangci-lint run`, `npm run lint && npm run build`. The Stop hook blocks finishing while the gates of the touched areas are red.

## Architecture boundaries (enforced by `tests/ArchitectureTests`)
- `Orders.Domain` depends on nothing. Business rules and invariants live here, never in endpoints or handlers.
- `Orders.Application` depends only on Domain and `Ecommerce.Contracts`. No EF Core, Dapper, RabbitMQ or ASP.NET Core.
- `Orders.Infrastructure` implements the ports declared in Application. `Orders.Api` only wires and maps HTTP.
- CQRS: writes go through the aggregate and EF Core; reads are SQL with Dapper returning DTOs.
- Services share **only** `contracts/events.md` (and `Ecommerce.Contracts` for .NET). Never reference another service's code.

## Messaging rules
- Every consumer is idempotent (processed-message table, unique business key or `INSERT ... ON CONFLICT DO NOTHING`).
- Acknowledge a message only after the state change is committed. Failures are rejected without requeue (they go to the DLQ).
- Changing an event is a contract change: update `contracts/events.md`, the C# record and the Go struct in the same commit.

## C# conventions
- Classes `sealed` by default; file-scoped namespaces; `TimeProvider` instead of `DateTime.Now`; `Guid.CreateVersion7()` for ids.
- No MediatR/AutoMapper: handlers are plain classes injected where they are used; mapping is explicit.
- Logging with `[LoggerMessage]` source generators.

## Go conventions
- `cmd/` + `internal/`, no `pkg/`. Small interfaces declared by the consumer.
- Errors are returned and wrapped with `%w`; no `panic` for control flow; `context.Context` is the first parameter of I/O functions.
- Table-driven tests; `go test -race` must pass.

## Never
- Edit generated files: EF migrations (`dotnet ef migrations add`), `package-lock.json`, `go.sum`. Blocked by hook.
- Read or write secrets (`.env`, keys). Blocked by permissions and hook.
- Disable an analyzer or a test to make the build pass. Fix the cause or explain the exception in `.editorconfig` with a comment.
- Add a dependency without a reason stated in the commit message.
