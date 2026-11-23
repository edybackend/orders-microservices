// Stop hook: the agent cannot finish while the quality gates of the areas it changed are red.
// Mirrors CI: .NET build + tests, Go vet + tests, web lint + build.
import { execSync } from 'node:child_process'
import { readFileSync } from 'node:fs'

const input = JSON.parse(readFileSync(0, 'utf8'))
if (input.stop_hook_active) process.exit(0) // already blocked once: avoid an endless loop

const root = process.env.CLAUDE_PROJECT_DIR ?? process.cwd()
const sh = (command, cwd = root) => execSync(command, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] })

const changed = sh('git status --porcelain').split('\n').filter(Boolean).map((line) => line.slice(3))
const touched = (prefixes) => changed.some((file) => prefixes.some((p) => file.startsWith(p)))

const gates = []
if (touched(['src/Orders', 'src/Payments', 'src/BuildingBlocks', 'tests', 'Directory.'])) {
  gates.push(['dotnet build', root], ['dotnet test --no-build', root])
}
if (touched(['src/notifications'])) {
  gates.push(['go vet ./...', `${root}/src/notifications`], ['go test ./...', `${root}/src/notifications`])
}
if (touched(['web'])) {
  gates.push(['npm run lint', `${root}/web`], ['npm run build', `${root}/web`])
}

for (const [command, cwd] of gates) {
  try {
    sh(command, cwd)
  } catch (error) {
    const output = `${error.stdout ?? ''}${error.stderr ?? ''}`.split('\n').slice(-40).join('\n')
    console.error(`Quality gate failed: \`${command}\`. Fix it before finishing.\n${output}`)
    process.exit(2)
  }
}
