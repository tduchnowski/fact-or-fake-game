package main

import (
	"log/slog"
)

func main() {
	bot, err := createBot()
	if err != nil {
		slog.Error(err.Error())
		slog.Info("bot exiting")
		return
	}
	slog.Info("Bot ready to fetch updates")
	bot.Start(60)
}
