# Git Hooks Setup

This repository uses git hooks to ensure code quality. To set up the hooks:

## Method 1: Automated (Recommended)

```bash
# Configure git to use the hooks from .githooks directory
git config core.hooksPath .githooks
```

## Method 2: Manual

```bash
# Copy the hooks to your local .git/hooks directory
cp .githooks/pre-commit .git/hooks/
chmod +x .git/hooks/pre-commit
```

## Available Hooks

### Pre-commit Hook
The pre-commit hook performs the following checks:

1. **YAML Validation** - Validates syntax of all staged `.yml` and `.yaml` files
2. **Markdown Link Check** - Checks for broken links in staged `.md` files
3. **Build Validation** - Builds the solution in both Debug and Release configurations

If any check fails, the commit will be blocked until the issues are fixed.