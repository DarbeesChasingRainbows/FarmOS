FROM denoland/deno:latest

ARG GIT_REVISION=1
ENV DENO_DEPLOYMENT_ID=${GIT_REVISION}

WORKDIR /app

# Copy shared module first (referenced by ../../shared/ from utils/)
COPY shared/ /app/shared/

# Copy apiary-os app
COPY apiary-os/ /app/apiary-os/

WORKDIR /app/apiary-os

# Cache the dependencies
RUN deno install

# Build the Fresh/Vite project — generates _fresh/
RUN deno task build
RUN deno cache _fresh/server.js

# Drop root privileges and serve the built project
USER deno
EXPOSE 8001
CMD ["serve", "-A", "--port=8001", "_fresh/server.js"]
