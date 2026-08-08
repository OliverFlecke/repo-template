# Frontend App

This directory contains the frontend app for the repository. It is built with
[NextJs](https://nextjs.org/) and [React](https://reactjs.org/), using
[TypeScript](https://www.typescriptlang.org/).

## Development

Run the development server:

```sh
pnpm dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser.

## Component library

The shared UI components live in `ui/`, styled with CSS Modules and the
design tokens in `app/globals.css`. Browse them with
[Ladle](https://ladle.dev/):

```sh
pnpm story:dev
```

## End-to-end tests

End-to-end tests use [Playwright](https://playwright.dev/) and live in `e2e/`.
They run against a real dev server, which Playwright starts automatically on
port 3000 (this port is fixed, since it's the origin registered as an allowed
Auth0 callback URL).

Before the first run, generate the OpenAPI client that the app imports at
build time (see `build:api` below) and install the Playwright browsers:

```sh
pnpm build:api
pnpm exec playwright install chromium
```

Then run the tests:

```sh
pnpm test:e2e        # headless run
pnpm test:e2e:ui     # interactive UI mode, useful for writing new tests
pnpm test:e2e:report # view the HTML report from the last run
```

The login test drives the real Auth0 hosted login page end to end, so it
needs network access and will break if the login UI or its flow changes. It
requires credentials for a real user in the Auth0 tenant, provided via the
`E2E_LOGIN_EMAIL` / `E2E_LOGIN_PASSWORD` env vars (never commit real
credentials). Locally, copy `.env.e2e.example` to `.env.e2e.local` (gitignored)
and fill it in; the test is skipped if these aren't set.

To generate a new test, run `pnpm exec playwright codegen https://localhost:3000`
(with the dev server running) to record interactions and produce a starting
point for a spec file in `e2e/`.

## Docker

There are two Dockerfiles for this frontend, depending on whether the `export`
mode is used or not. By default, this frontend is limited to the `export` mode
to enable it to be served as static files by any web server. If more advanced
features of NextJs are needed, you can switch to `standalone` mode.

The `Dockerfile` is used to build and run the frontend project with a full
NextJs server. `Dockerfile.export` is simpler, only building the project and
serving it with [nginx](https://nginx.org/).
