package handler

import (
	"encoding/json"
	"fmt"
	"io"
	"log/slog"
	"net/http"
	"net/url"
	"strings"
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
	text := "Hello and welcome!\n\nThis is a mini app that allows you to play True/False quiz games either by yourself or with friends.\n\nYou can also directly create game rooms through this bot. Whether you create a room with a bot or through a mini app, just share the link with your friends and start playing with them!\n\nClick Start to launch an app\n\nHave fun!"
	return SendMsg{ChatId: msg.Chat.Id, Text: text}
}

func help(msg *telegramapi.Message) Responder {
	text := "Creating a game:\n\n/createGame <number of rounds> <round duration in seconds>\n\nYou can also just open the mini app and play there"
	return SendMsg{ChatId: msg.Chat.Id, Text: text}
}

func createGame(msg *telegramapi.Message, httpClient *http.Client, requestUrl, apiKey string) Responder {
	msgText := msg.Text
	cmdParts := strings.Fields(msgText)
	if len(cmdParts) != 3 {
		return SendMsg{ChatId: msg.Chat.Id, Text: "Incorrect arguments for this command\n\nExample:\n/createGame 10 5\n\nwhere 10 is the number of rounds you want and 5 is the maximum round duration"}
	}
	roundsNum, roundTimeout := cmdParts[1], cmdParts[2]
	createRoomUrl := fmt.Sprintf("%s/createRoom?roundsNum=%s&roundTimeout=%s", requestUrl, roundsNum, roundTimeout)
	request, err := http.NewRequest("GET", createRoomUrl, nil)
	if err != nil {
		slog.Error(err.Error())
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	userInitData := createUserInitData(msg)
	initDataHeader := fmt.Sprintf("bot %s %s", apiKey, userInitData)
	request.Header.Add("X-Telegram-InitData", initDataHeader)
	response, err := httpClient.Do(request)
	if err != nil {
		slog.Error(err.Error())
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	defer response.Body.Close()
	if response.StatusCode != 200 {
		slog.Error(fmt.Sprintf("createRoomUrl: %s, status code: %d", createRoomUrl, response.StatusCode))
		return SendMsg{ChatId: msg.Chat.Id, Text: "Couldn't create a room. Try again later"}
	}
	body, err := io.ReadAll(response.Body)
	if err != nil {
		slog.Error(err.Error())
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	fmt.Printf("%s\n", body)
	return SendMsg{ChatId: msg.Chat.Id, Text: string(body)}
}

func createUserInitData(msg *telegramapi.Message) string {
	userJsonString, err := json.Marshal(msg.From)
	if err != nil {
		slog.Error("marshallig of user init data failed: " + err.Error())
		return ""
	}
	values := url.Values{}
	values.Set("user", string(userJsonString))
	return values.Encode()
}
