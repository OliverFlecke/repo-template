# Repository Template

This repository contains a template for creating new repositories for
structuring web-based projects and code spanning multiple applications. It is
an opinionated approach, using my preferred tools and languages to solve
different tasks. It is a moving target, which I plan to evolve and adapt
with new tools.

## Directory Structure

Overall, each service is contained in a separate directory.

## Development

Tools are primarily managed through [mise](https://mise.jdx.dev/).
To install all required tools, run:

```sh
mise install
```

Versions are primarily specified to their latest major version, using the
`mise.lock` to specify exact versions that has been verified to work.

### Hooks

To enforce consistent code style, linting, formatting, and commit message
structure, [hk](https://hk.jdx.dev/) is used for pre-commit hooks.
It is optional, but strongly recommended.

The following commands are available to run manually:

```sh
# Check all changed files for issues
hk check

# Fix all issues in changed files, if possible
hk fix
```
