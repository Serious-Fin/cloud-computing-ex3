FROM node:24-bookworm-slim
WORKDIR /app
RUN npm install --global pnpm@11.19.0
COPY worker/package.json worker/pnpm-lock.yaml ./
RUN pnpm install --prod --frozen-lockfile
COPY worker/src/ ./src/
USER node
CMD ["node", "src/run-job.js"]
