FROM denoland/deno:latest

ARG GIT_REVISION=1
ENV DENO_DEPLOYMENT_ID=${GIT_REVISION}

WORKDIR /app

# Cache the dependencies first to speed up future rebuilds
COPY deno.json deno.lock ./
RUN deno install

# Copy your raw source code
COPY . .

# Build the Fresh/Vite project — generates _fresh/
RUN deno task build
RUN deno cache _fresh/server.js

# Drop root privileges and serve the built project
USER deno
EXPOSE 8000
CMD ["serve", "-A", "_fresh/server.js"]