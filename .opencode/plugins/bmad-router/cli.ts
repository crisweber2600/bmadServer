#!/usr/bin/env node
import { execSync, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, copyFileSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const PLUGIN_FILES = ['index.ts', 'router.ts', 'workflow.ts', 'types.ts', 'quota.ts', 'rules.ts', 'package.json'];

interface Args {
  targetDir: string | null;
  apiKey: string | null;
  global: boolean;
  help: boolean;
}

function parseArgs(args: string[]): Args {
  const result: Args = {
    targetDir: null,
    apiKey: null,
    global: false,
    help: false,
  };

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    switch (arg) {
      case '-k':
      case '--api-key':
        result.apiKey = args[++i] || null;
        break;
      case '-g':
      case '--global':
        result.global = true;
        break;
      case '-h':
      case '--help':
        result.help = true;
        break;
      default:
        if (!arg.startsWith('-')) {
          result.targetDir = arg;
        }
    }
  }

  return result;
}

function printUsage(): void {
  console.log(`
Usage: bmad-router [OPTIONS] <target-project-dir>

Install bmad-router plugin to an OpenCode project.

Options:
  -k, --api-key KEY    Set NOTDIAMOND_API_KEY (optional, can also use env var)
  -g, --global         Add API key to ~/.bashrc
  -h, --help           Show this help

Examples:
  npx bmad-router ~/myproject
  npx bmad-router -k sk-xxx ~/myproject
  npx bmad-router -k sk-xxx -g ~/myproject
`);
}

function findPackageManager(): 'bun' | 'npm' {
  try {
    execSync('bun --version', { stdio: 'ignore' });
    return 'bun';
  } catch {
    return 'npm';
  }
}

function installDependencies(dir: string): void {
  const pm = findPackageManager();
  console.log(`Installing dependencies with ${pm}...`);
  
  const result = spawnSync(pm, ['install'], {
    cwd: dir,
    stdio: 'inherit',
  });

  if (result.status !== 0) {
    console.warn(`Warning: ${pm} install failed. Run '${pm} install' in ${dir} manually.`);
  }
}

function updateOpencodeConfig(configPath: string): void {
  if (existsSync(configPath)) {
    const content = readFileSync(configPath, 'utf-8');
    if (content.includes('"bmad-router"')) {
      console.log('Plugin already registered in opencode.json');
      return;
    }

    try {
      const config = JSON.parse(content);
      if (Array.isArray(config.plugin)) {
        config.plugin.unshift('bmad-router');
      } else {
        config.plugin = ['bmad-router'];
      }
      writeFileSync(configPath, JSON.stringify(config, null, 2) + '\n');
      console.log('Added bmad-router to existing opencode.json');
    } catch {
      console.warn('Warning: Could not parse opencode.json. Add "bmad-router" to plugin array manually.');
    }
  } else {
    const config = {
      $schema: 'https://opencode.ai/config.json',
      plugin: ['bmad-router'],
    };
    writeFileSync(configPath, JSON.stringify(config, null, 2) + '\n');
    console.log('Created opencode.json');
  }
}

function setApiKey(targetDir: string, apiKey: string, global: boolean): void {
  if (global) {
    const bashrcPath = join(process.env.HOME || '~', '.bashrc');
    console.log(`Adding NOTDIAMOND_API_KEY to ${bashrcPath}...`);
    
    try {
      let content = existsSync(bashrcPath) ? readFileSync(bashrcPath, 'utf-8') : '';
      const exportLine = `export NOTDIAMOND_API_KEY="${apiKey}"`;
      
      if (content.includes('NOTDIAMOND_API_KEY')) {
        content = content.replace(/export NOTDIAMOND_API_KEY=.*/, exportLine);
      } else {
        content += `\n${exportLine}\n`;
      }
      
      writeFileSync(bashrcPath, content);
      console.log("Run 'source ~/.bashrc' or restart your shell to load the key.");
    } catch (err) {
      console.warn(`Warning: Could not update ~/.bashrc: ${err}`);
    }
  } else {
    const envPath = join(targetDir, '.env');
    console.log(`Adding NOTDIAMOND_API_KEY to ${envPath}...`);
    
    try {
      let content = existsSync(envPath) ? readFileSync(envPath, 'utf-8') : '';
      const line = `NOTDIAMOND_API_KEY=${apiKey}`;
      
      if (content.includes('NOTDIAMOND_API_KEY')) {
        content = content.replace(/NOTDIAMOND_API_KEY=.*/, line);
      } else {
        content += `${content && !content.endsWith('\n') ? '\n' : ''}${line}\n`;
      }
      
      writeFileSync(envPath, content);
    } catch (err) {
      console.warn(`Warning: Could not update .env: ${err}`);
    }
  }
}

function main(): void {
  const args = parseArgs(process.argv.slice(2));

  if (args.help) {
    printUsage();
    process.exit(0);
  }

  if (!args.targetDir) {
    console.error('Error: Target project directory required');
    printUsage();
    process.exit(1);
  }

  const targetDir = resolve(args.targetDir);

  if (!existsSync(targetDir)) {
    console.error(`Error: Directory does not exist: ${targetDir}`);
    process.exit(1);
  }

  console.log(`Installing bmad-router to: ${targetDir}`);

  const pluginDir = join(targetDir, '.opencode', 'plugins', 'bmad-router');
  mkdirSync(pluginDir, { recursive: true });

  for (const file of PLUGIN_FILES) {
    const src = join(__dirname, file);
    const dest = join(pluginDir, file);
    if (existsSync(src)) {
      copyFileSync(src, dest);
    }
  }

  installDependencies(pluginDir);

  const configPath = join(targetDir, 'opencode.json');
  updateOpencodeConfig(configPath);

  if (args.apiKey) {
    setApiKey(targetDir, args.apiKey, args.global);
  }

  console.log(`
bmad-router installed successfully!

Plugin location: ${pluginDir}
Config: ${configPath}
${args.apiKey ? `API key: Added to ${args.global ? '~/.bashrc' : join(targetDir, '.env')}` : 'API key: Not set (optional - plugin works without it)'}

Restart OpenCode to activate the plugin.
`);
}

main();
