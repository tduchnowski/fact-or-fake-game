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

type GameApiResponse struct {
	Ok      bool   `json:"ok"`
	Content string `json:"content"`
}

type CommandHandler struct {
	serverApiHttpClient *http.Client
	apiServerUrl        string
	apiKey              string
	miniAppLink         string
}

func CreateCommandHandler(apiServerUrl, apiKey, miniAppLink string) (CommandHandler, error) {
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
		miniAppLink:         miniAppLink,
	}, nil
}

func (cmdHandler CommandHandler) GetResponder(msg *telegramapi.Message) Responder {
	username := msg.Chat.Username
	slog.Info(fmt.Sprintf("user activity -- command: %s, user: %s", msg.Text, username))
	var resp Responder
	switch msg.Text {
	case "/start":
		resp = start(msg)
	case "/help":
		resp = help(msg)
	}
	if strings.HasPrefix(msg.Text, "/creategame") {
		resp = createGame(msg, cmdHandler.serverApiHttpClient, cmdHandler.apiServerUrl, cmdHandler.apiKey, cmdHandler.miniAppLink)
	}
	if resp != nil {
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

func createGame(msg *telegramapi.Message, httpClient *http.Client, requestUrl, apiKey, miniAppLink string) Responder {
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
	request.Header.Add("X-Telegram-Initdata", initDataHeader)
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
		slog.Error(fmt.Sprintf("couldn't read the response when creating a room. %s", err.Error()))
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	var apiResponse GameApiResponse
	err = json.Unmarshal([]byte(body), &apiResponse)
	if err != nil {
		slog.Error(fmt.Sprintf("couldn't unmarshal apiResponse for body = %s. %s", body, err.Error()))
		return SendMsg{ChatId: msg.Chat.Id, Text: "Internal server error. Please try again later"}
	}
	miniAppDirectLink := fmt.Sprintf("Share this link with people so they can play with you:\n%s?startapp=%s", miniAppLink, apiResponse.Content)
	return SendMsg{ChatId: msg.Chat.Id, Text: miniAppDirectLink}
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
