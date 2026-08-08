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

## Docker

There are two Dockerfiles for this frontend, depending on whether the `export`
mode is used or not. By default, this frontend is limited to the `export` mode
to enable it to be served as static files by any web server. If more advanced
features of NextJs are needed, you can switch to `standalone` mode.

The `Dockerfile` is used to build and run the frontend project with a full
NextJs server. `Dockerfile.export` is simpler, only building the project and
serving it with [nginx](https://nginx.org/).
