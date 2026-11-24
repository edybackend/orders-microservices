// PostToolUse hook: formats the file the agent just changed, so style never drifts.
import { execFileSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import path from 'node:path'

const input = JSON.parse(readFileSync(0, 'utf8'))
const filePath = input.tool_input?.file_path ?? ''
const root = process.env.CLAUDE_PROJECT_DIR ?? process.cwd()

const run = (cmd, args) => execFileSync(cmd, args, { cwd: root, stdio: ['ignore', 'ignore', 'pipe'] })

// dotnet format matches --include against paths relative to the solution; an absolute path silently matches nothing.
try {
  if (filePath.endsWith('.cs')) {
    run('dotnet', ['format', 'whitespace', 'OrdersMicroservices.slnx', '--include', path.relative(root, filePath)])
  } else if (filePath.endsWith('.go')) {
    run('gofmt', ['-w', filePath])
  }
} catch (error) {
  // Formatting problems are reported but never block the edit; the Stop hook enforces the gates.
  console.error(`Formatter failed for ${filePath}: ${error.stderr?.toString() ?? error.message}`)
}
