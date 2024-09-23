FROM nginx:stable-alpine as base


FROM base AS dev
COPY reverse-proxy-dev.conf /etc/nginx/conf.d/default.conf
EXPOSE 80

FROM base AS prod
COPY reverse-proxy-prod.conf /etc/nginx/conf.d/default.conf
EXPOSE 80

FROM base
COPY reverse-proxy-prod.conf /etc/nginx/conf.d/default.conf
EXPOSE 80