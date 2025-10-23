# RDMP Default Branch Migration: develop → main

## Overview

**Date:** 2025-10-23
**Status:** Completed
**Previous Default:** `develop`
**New Default:** `main`

This document describes the migration of RDMP's default branch from `develop` to `main` to align with industry standards and the other 8 repositories in this workspace.

## Rationale

1. **Industry Standard:** `main` is now the standard default branch name across GitHub and the software industry
2. **Workspace Consistency:** 8/9 repositories in this workspace already use `main`
3. **Simplified Workflow:** Eliminates the need for a separate development branch
4. **Alignment with GitHub:** GitHub defaults to `main` for new repositories

## Changes Made

### 1. GitHub Configuration Files

#### `.github/dependabot.yml`
**Changed:**
- Line 12: `target-branch: develop` → `target-branch: main`

**Impact:** Dependabot will now create PRs targeting `main` branch

#### `.github/workflows/build.yml`
**Changed:**
- Line 230: `base-branch: 'develop'` → `base-branch: 'main'`

**Impact:** Markdown link checker will use `main` as the base branch for comparison

### 2. Documentation Updates

#### `README.md`
**Changed:**
- Line 3: Coverage badge URL parameter changed from `?branch=develop` to `?branch=main`

**Impact:** Coverage badge will show stats for `main` branch

#### `CHANGELOG.md`
**Changed:**
- Bottom of file: Unreleased comparison link changed from `compare/v8.1.0...develop` to `compare/v8.1.0...main`

**Impact:** "Unreleased" changelog link will compare against `main` branch

#### `NoteForNewDevelopers.md`
**Changed:**
- Line 157: Release process now references `main` instead of `develop`
- Line 183: Removed reference to merging `develop` into `main` (no longer needed)
- Simplified update testing instructions

**Impact:** Developer documentation now reflects single-branch workflow

#### `.github/pull_request_template.md`
**Changed:**
- Line 21: Test Plan link changed from `blob/develop/` to `blob/main/`

**Impact:** PR template links to correct branch

### 3. Plugin Documentation

#### `Plugins/RdmpDicom/Documentation/NlpPlugin.md`
**Changed:**
- Line 15: Cohort building plugins link updated to use `main`
- Line 40: Query caching link updated to use `main`
- Lines 60-61: Glossary reference links updated to use `main`

**Impact:** Plugin documentation links point to correct branch

#### `Plugins/RdmpDicom/CHANGELOG.md`
**Changed:**
- Unreleased comparison link changed to use `main`

**Impact:** Plugin changelog links work correctly

## GitHub Repository Settings

The following changes need to be made in GitHub repository settings (requires admin access):

1. **Navigate to:** Settings → General → Default branch
2. **Change from:** `develop` → `main`
3. **Update branch protection rules** if any exist on `develop` to apply to `main` instead

## For Contributors

### If You Have Local Clones

#### Option 1: Update Existing Clone
```bash
# Navigate to your RDMP directory
cd /path/to/RDMP

# Fetch latest from remote
git fetch origin

# If you're currently on develop branch
git checkout develop
git pull origin develop

# Create main branch from develop (if it doesn't exist remotely yet)
# Skip this if main already exists on remote
git checkout -b main
git push -u origin main

# Update your local default branch
git branch -m develop main
git fetch origin
git branch -u origin/main main
git remote set-head origin -a

# Delete old develop branch (optional, after confirming main is working)
git branch -d develop  # Delete local
git push origin --delete develop  # Delete remote (only after GitHub default is changed)
```

#### Option 2: Fresh Clone (Recommended)
```bash
# Delete old clone
rm -rf /path/to/RDMP

# Clone fresh copy (will automatically use new default branch)
git clone https://github.com/jas88/RDMP.git
cd RDMP
```

### For Active Feature Branches

If you have active feature branches based on `develop`:

```bash
# Update your feature branch to track main
git checkout your-feature-branch
git rebase main  # or: git merge main
git push --force-with-lease  # Only if branch is already pushed
```

### Creating New PRs

- **Old way:** PRs targeted `develop` branch
- **New way:** PRs now target `main` branch
- **GitHub will automatically update:** existing open PRs to target `main` after the default branch change

## CI/CD Impact

### GitHub Actions
- All workflows triggered by `push` events will now run on `main` branch
- Existing workflows on `develop` branch will complete but not trigger on new commits

### Dependabot
- New Dependabot PRs will target `main`
- Existing open Dependabot PRs targeting `develop` can be closed and will be recreated targeting `main`

## Release Process Changes

### Old Process
1. Merge feature branches into `develop`
2. Test on `develop`
3. Tag release on `develop`
4. Merge `develop` into `main` after release
5. `main` contained only released code

### New Process
1. Merge feature branches into `main`
2. Test on `main`
3. Tag release on `main`
4. `main` contains both development and released code
5. Tags identify release points

**Benefits:**
- Simpler workflow (one less merge)
- Faster releases
- Clearer history
- Aligns with modern Git practices

## Branch Protection Recommendations

Configure branch protection for `main` with the following rules:

- ✅ Require a pull request before merging
- ✅ Require approvals (at least 1)
- ✅ Require status checks to pass before merging
  - Build and test workflows
  - CodeQL analysis
- ✅ Require conversation resolution before merging
- ✅ Require linear history (optional)
- ✅ Do not allow bypassing the above settings

## Timeline

1. ✅ **2025-10-23:** Documentation and configuration files updated
2. 🔄 **Next:** Update GitHub repository default branch setting (requires admin)
3. 📢 **After change:** Notify all contributors via GitHub issue/discussion
4. 🗑️ **After 30 days:** Delete `develop` branch from remote (after confirming all workflows work)

## Verification Checklist

After the migration is complete:

- [ ] GitHub repository default branch shows `main`
- [ ] New PRs default to `main` as target
- [ ] CI workflows trigger on push to `main`
- [ ] Dependabot creates PRs targeting `main`
- [ ] Coverage badges show data for `main` branch
- [ ] Documentation links work correctly
- [ ] Contributors have updated their local clones
- [ ] Branch protection rules applied to `main`

## Rollback Plan

If critical issues arise:

1. Change GitHub default branch back to `develop`
2. Revert documentation changes
3. Create hotfix branch from last known good state
4. Investigate issues before attempting migration again

## Support

If you encounter issues with the migration:

1. Check this guide first
2. Review the [Git documentation](https://git-scm.com/doc)
3. Open an issue in the repository with:
   - Your current branch state (`git status`)
   - The error message or unexpected behavior
   - Steps you've already tried

## References

- [GitHub: Renaming default branch](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-branches-in-your-repository/renaming-a-branch)
- [BRANCH-PR-ANALYSIS.md](../../docs/BRANCH-PR-ANALYSIS.md) - Full branch health analysis that identified this need
- [CODING-STANDARDS.md](../../docs/CODING-STANDARDS.md) - Workspace coding standards requiring `main` as default

---

**Questions?** Contact repository maintainers or open a discussion.
