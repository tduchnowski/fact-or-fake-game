package handler

import (
	"FactOrFake.Bot/telegramapi"
	"bytes"
	"encoding/json"
	"fmt"
	"log/slog"
	"net/http"
)

type Responder interface {
	Respond(string) // can be sending a message, editing existing msg etc.
}

type Responders []Responder

// this makes a list of Responders also a Responder
func (r Responders) Respond(baseUrl string) {
	for _, responder := range r {
		responder.Respond(baseUrl)
	}
}

// TODO: return an error if smth bad happens
func postRequest(baseUrl string, method string, r Responder) {
	data, err := json.Marshal(r)
	if err != nil {
		slog.Error(fmt.Sprintf("postRequest: couldn't unmarshal responder %+v, error: %s", r, err.Error()))
	}
	url := fmt.Sprintf("%s/%s", baseUrl, method)
	client := &http.Client{}
	res, err := client.Post(url, "application/json", bytes.NewBuffer(data))
	if err != nil {
		slog.Error(fmt.Sprintf("postRequest failed for url: %s, data: %s, error: %s", url, data, err.Error()))
	}
	defer res.Body.Close()
	// TODO: define a new type for getting Telegram errors
	var j interface{}
	err = json.NewDecoder(res.Body).Decode(&j)
	if err != nil {
		slog.Error(fmt.Sprintf("postRequest: couldn't serialize a response from Telegram: %s", err.Error()))
	}
}

type SendMsg struct {
	ChatId      int64                             `json:"chat_id"`
	Text        string                            `json:"text"`
	ParseMode   *string                           `json:"parse_mode,omitempty"`   // pointer for omitempty to skip it if its not present
	ReplyMarkup *telegramapi.InlineKeyboardMarkup `json:"reply_markup,omitempty"` // pointer for omitempty to skip it if its not present
}

func (sm SendMsg) Respond(baseUrl string) {
	postRequest(baseUrl, "sendMessage", sm)
}

// TODO: there should be a way to embed SendMsg into this, its just one field more
type EditMsg struct {
	ChatId      int64                             `json:"chat_id"`
	Text        string                            `json:"text"`
	ReplyMarkup *telegramapi.InlineKeyboardMarkup `json:"reply_markup,omitempty"` // has to be a pointer for json serializer to skip it if empty
	MsgId       int64                             `json:"message_id"`             // id of a message to edit
}

func (em EditMsg) Respond(baseUrl string) {
	postRequest(baseUrl, "editMessageText", em)
}

type DeleteMsg struct {
	ChatId int64 `json:"chat_id"`
	MsgId  int64 `json:"message_id"`
}

func (dm DeleteMsg) Respond(baseUrl string) {
	postRequest(baseUrl, "deleteMessage", dm)
}

type AnswerCallback struct {
	CallbackQueryId string `json:"callback_query_id"`
}

func (ac AnswerCallback) Respond(baseUrl string) {
	postRequest(baseUrl, "answerCallbackQuery", ac)
}
