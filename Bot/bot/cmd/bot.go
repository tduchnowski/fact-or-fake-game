package main

import (
	"FactOrFake.Bot/handler"
	"FactOrFake.Bot/telegramapi"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"time"
)

type Bot struct {
	telegramapi.User
	token      string
	baseUrl    string
	cmdHandler handler.CommandHandler
}

func (b *Bot) addCmdHandler(cmdHandler handler.CommandHandler) {
	b.cmdHandler = cmdHandler
}

func (b Bot) Start(timeout int) {
	lastId := int64(0)
	for {
		client := http.Client{Timeout: time.Duration(timeout) * time.Second}
		urlQuery := fmt.Sprintf("%s/getUpdates?timeout=%d&offset=%d", b.baseUrl, timeout, lastId+1)
		slog.Info("fetching updates from Telegram")
		res, clientErr := client.Get(urlQuery)
		if clientErr != nil {
			slog.Error("error during update fetching" + clientErr.Error())
			time.Sleep(5 * time.Second)
			continue
		}
		if res.StatusCode != 200 {
			slog.Error(fmt.Sprintf("telegram responded with status: %s", res.Status))
			time.Sleep(5 * time.Second)
			continue
		}
		body, readErr := io.ReadAll(res.Body)
		if readErr != nil {
			slog.Error(readErr.Error())
			time.Sleep(5 * time.Second)
			continue
		}
		lastId = b.handleUpdateResponse(body)
		res.Body.Close()
	}
}

func (b Bot) handleUpdateResponse(updateBody []byte) int64 {
	var ur telegramapi.UpdateResponse
	err := json.Unmarshal(updateBody, &ur)
	if err != nil {
		slog.Error("handleUpdateResponse - " + err.Error())
		return 0
	}
	slog.Info(fmt.Sprintf("received %d updates", len(ur.Updates)))
	var lastUpdateId int64
	for _, update := range ur.Updates {
		go func() {
			var reply handler.Responder
			switch update.GetUpdateType() {
			case "message":
				reply = b.cmdHandler.GetResponder(&update.Msg)
			default:
				reply = handler.SendMsg{}
			}
			reply.Respond(b.baseUrl)
		}()
		lastUpdateId = update.Id
	}
	return lastUpdateId
}

// verifies token and then creates Bot struct and returns it
func createBot() (*Bot, error) {
	config, err := LoadConfig()
	if err != nil {
		return nil, err
	}
	baseUrl := config.TgApiUrl + config.BotToken
	getMeUrl := baseUrl + "/getMe"
	res, err := http.Get(getMeUrl)
	if err != nil {
		return &Bot{}, err
	}
	defer res.Body.Close()
	if res.StatusCode != 200 {
		return &Bot{}, errors.New("/getMe request: " + res.Status)
	}
	body, err := io.ReadAll(res.Body)
	var ur telegramapi.UserResponse
	err = json.Unmarshal(body, &ur)
	if err != nil {
		return &Bot{}, err
	}
	bot := Bot{
		User:    ur.User,
		token:   config.BotToken,
		baseUrl: baseUrl,
	}
	cmdHandler, err := handler.CreateCommandHandler(config.GameApiUrl, config.ApiKey, config.MiniAppDirectLink)
	if err != nil {
		slog.Error("couldn't create the bot: " + err.Error())
		return nil, err
	}
	bot.addCmdHandler(cmdHandler)
	return &bot, nil
}
