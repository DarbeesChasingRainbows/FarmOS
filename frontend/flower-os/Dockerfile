FROM denoland/deno:latest

ARG GIT_REVISION=1
ENV DENO_DEPLOYMENT_ID=${GIT_REVISION}

WORKDIR /app

# Copy shared module first (referenced by ../../shared/ from utils/)
COPY shared/ /app/shared/

# Copy flower-os app
COPY flower-os/ /app/flower-os/

WORKDIR /app/flower-os

# Cache the dependencies
RUN deno install

# Build the Fresh/Vite project — generates _fresh/
RUN deno task build
RUN deno cache _fresh/server.js

# Drop root privileges and serve the built project
USER deno
EXPOSE 8004
CMD ["serve", "-A", "--port=8004", "_fresh/server.js"]
