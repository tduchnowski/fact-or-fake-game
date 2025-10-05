FROM golang:latest AS build
WORKDIR /build
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -o fact-or-fake-bot ./cmd

FROM gcr.io/distroless/static
WORKDIR /app
COPY --from=build /build/fact-or-fake-bot .
COPY --from=build /build/config.yaml .
CMD ["/app/fact-or-fake-bot"]



