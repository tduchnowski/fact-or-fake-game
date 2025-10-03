FROM golang:latest

COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN mkdir ./bot-logs
RUN go build -o lingo-cards-bot ./cmd
CMD ["./lingo-cards-bot"]

