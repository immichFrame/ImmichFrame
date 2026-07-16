FROM golang:1.23-alpine AS build
RUN apk add --no-cache build-base sqlite-dev
WORKDIR /src
COPY go.mod go.sum* ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=1 go build -trimpath -ldflags='-s -w' -o /out/immichframe-transcoder ./cmd/immichframe-transcoder

FROM alpine:3.21
RUN apk add --no-cache ca-certificates ffmpeg
COPY --from=build /out/immichframe-transcoder /usr/local/bin/immichframe-transcoder
USER 1000:1000
EXPOSE 8090
ENTRYPOINT ["immichframe-transcoder"]
