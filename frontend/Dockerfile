# Build with the repository root as context.
FROM node:24-alpine
WORKDIR /app
COPY --chown=node:node frontend/index.html frontend/styles.css frontend/app.js frontend/server.mjs ./
ENV NODE_ENV=production
ENV PORT=8080
USER node
EXPOSE 8080
CMD ["node", "server.mjs"]
