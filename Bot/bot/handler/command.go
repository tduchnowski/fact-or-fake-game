package handler

import (
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"time"

	"FactOrFake.Bot/telegramapi"
)

type CommandHandler struct {
	serverApiHttpClient *http.Client
	apiServerUrl        string
	apiKey              string
}

func CreateCommandHandler(apiServerUrl, apiKey string) (CommandHandler, error) {
	client := http.Client{
		Timeout: 5 * time.Second,
		Transport: &http.Transport{
			MaxIdleConns:        100,
			MaxIdleConnsPerHost: 10,
			IdleConnTimeout:     60 * time.Second,
		},
	}
	return CommandHandler{
		serverApiHttpClient: &client,
		apiServerUrl:        apiServerUrl,
		apiKey:              apiKey,
	}, nil
}

func (cmdHandler CommandHandler) GetResponder(msg *telegramapi.Message) Responder {
	var resp Responder
	switch msg.Text {
	case "/start":
		resp = start(msg)
	case "/help":
		resp = help(msg)
	case "/createGame":
		resp = createGame(msg, cmdHandler.serverApiHttpClient, cmdHandler.apiServerUrl, cmdHandler.apiKey)
	}
	if resp != nil {
		username := msg.Chat.Username
		slog.Info(fmt.Sprintf("user activity -- command: %s, user: %s", msg.Text, username))
		return resp
	}
	return SendMsg{}
}

func start(msg *telegramapi.Message) Responder {
	text := "Hello and Welcome!\n\nThis is an game app that allows you to play True/False quiz games either by yourself or with friends.\n\nYou can also directly create game rooms through this bot. Whether you create a room with a bot or through a mini app, just share the link with your friends and start playing with them!\n\nClick Start to launch an app\n\nHave fun!"
	return SendMsg{ChatId: msg.Chat.Id, Text: text}
}

func help(msg *telegramapi.Message) Responder {
	text := "help"
	return SendMsg{ChatId: msg.Chat.Id, Text: text}
}

func createGame(msg *telegramapi.Message, httpClient *http.Client, requestUrl, apiKey string) Responder {
	roundsNum := "10"
	roundTimeout := "10"
	createRoomUrl := fmt.Sprintf("%s/createRoom?roundsNum=%s&roundTimeout=%s", requestUrl, roundsNum, roundTimeout)
	request, err := http.NewRequest("GET", createRoomUrl, nil)
	if err != nil {
		slog.Error(err.Error())
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	request.Header.Add("X-Telegram-InitData", "bot "+apiKey)
	response, err := httpClient.Do(request)
	if err != nil {
		slog.Error(err.Error())
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	defer response.Body.Close()
	if response.StatusCode != 200 {
		slog.Error(fmt.Sprintf("createRoomUrl: %s, status code: %d", createRoomUrl, response.StatusCode))
	}
	body, err := io.ReadAll(response.Body)
	if err != nil {
		slog.Error(err.Error())
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	fmt.Printf("%s\n", body)
	return SendMsg{ChatId: msg.Chat.Id, Text: string(body)}
}
