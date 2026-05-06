FROM node:20-alpine AS build
WORKDIR /app

COPY Frontend/admin/package*.json ./
RUN npm ci

COPY Frontend/admin/ ./

ARG VITE_API_URL=http://localhost:8500
ARG VITE_WALL_ORIGIN=http://localhost:8502
ENV VITE_API_URL=${VITE_API_URL}
ENV VITE_WALL_ORIGIN=${VITE_WALL_ORIGIN}

RUN npm run build

FROM nginx:1.27-alpine AS runtime

RUN rm -f /etc/nginx/conf.d/default.conf
RUN printf "server {\n\
  listen 80;\n\
  server_name _;\n\
  root /usr/share/nginx/html;\n\
  index index.html;\n\
\n\
  location / {\n\
    try_files \$uri \$uri/ /index.html;\n\
  }\n\
}\n" > /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
