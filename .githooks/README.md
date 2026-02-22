# Git Hooks

This repo uses:
- a `pre-commit` guard to block staging/committing private or non-public materials
- a `pre-push` guard to block direct pushes to `main`/`master` (branch + PR workflow)

Enable it locally:

```bash
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit
chmod +x .githooks/pre-push
```

The hook blocks commits if staged paths include:
- private directories (`cv/`, `portfolio/`, `research/`, `private/`, etc.)
- sensitive filename patterns (`interview`, `cv-studio`, `saragossa`, `job-research`, `recruiter`)

The push guard blocks direct pushes to:
- `refs/heads/main`
- `refs/heads/master`

Override is available but discouraged:
```bash
ALLOW_MAIN_PUSH=1 git push ...
```
