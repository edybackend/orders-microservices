# Build context: src/notifications.
FROM golang:1.27 AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build -trimpath -ldflags="-s -w" -o /out/notifications ./cmd/notifications \
    && mkdir -p /out/data

# Static binary on a distroless image: no shell, no package manager, non-root.
FROM gcr.io/distroless/static-debian12:nonroot
WORKDIR /app
COPY --from=build /out/notifications ./notifications
COPY --from=build --chown=nonroot:nonroot /out/data ./data
ENV DB_PATH=/app/data/notifications.db \
    HTTP_ADDR=:8081
EXPOSE 8081
USER nonroot
ENTRYPOINT ["/app/notifications"]
