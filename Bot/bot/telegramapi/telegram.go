package telegramapi

type Status struct {
	Status bool `json:"ok"`
}

// this is what /getUpdates responds with
type UpdateResponse struct {
	Status
	Updates []Update `json:"result"`
}

// this is what /getMe responds with
type UserResponse struct {
	Status
	User User `json:"result"`
}

// TODO: find out whats the errors structure
type ErrorResponse struct {
}

// this is what /getMe responds with
type User struct {
	Id                      int64  `json:"id"`
	IsBot                   bool   `json:"is_bot"`
	FirstName               string `json:"first_name"`
	LastName                string `json:"last_name"`                   //optional
	Username                string `json:"username"`                    //optional
	LanguageCode            string `json:"language_code"`               //optional
	IsPremium               bool   `json:"is_premium"`                  //optional
	AddedToAttachmentMenu   bool   `json:"added_to_attachment_menu"`    //optional
	CanJoinGroups           bool   `json:"can_join_groups"`             //optional
	CanReadAllGroupMessages bool   `json:"can_read_all_group_messages"` //optional
	SupportsInlineQueries   bool   `json:"supports_inline_queries"`     //optional
	CanConnectToBusiness    bool   `json:"can_connect_to_business"`     //optional
	HasMainWebApp           bool   `json:"has_main_web_app"`            //optional
}

// for the purposes of this bot, I'm only interested in Id, Msg and CallbackQuery
// fields of the Update received from Telegram API
type Update struct {
	Id  int64   `json:"update_id"`
	Msg Message `json:"message"`
	// EditedMsg               Message                     `json:"edited_message"`
	// ChannelPost             Message                     `json:"channel_post"`
	// EditedChannelPost       Message                     `json:"edited_channel_post"`
	// BusinessConnection      BusinessConnection          `json:"business_connection"`
	// BusinessMsg             Message                     `json:"business_message"`
	// EditedBusinessMessage   Message                     `json:"edited_business_message"`
	// DeletedBusinessMessages BusinessMessagesDeleted     `json:"deleted_business_messages"`
	// MsgReaction             MessageReactionUpdated      `json:"message_reaction"`
	// MsgReactionCount        MessageReactionCountUpdated `json:"message_reaction_count"`
	// InlineQuery             InlineQuery                 `json:"inline_query"`
	// ChosenInlineResult      ChosenInlineResult          `json:"chosen_inline_result"`
	CallbackQuery CallbackQuery `json:"callback_query"`
	// ShippingQuery           ShippingQuery               `json:"shipping_query"`
	// PreCheckoutQuery        PreCheckoutQuery            `json:"pre_checkout_query"`
	// PurchasedPaidMedia      PaidMediaPurchased          `json:"purchased_paid_media"`
	// Poll                    Poll                        `json:"poll"`
	// PollAnswer              PollAnswer                  `json:"poll_answer"`
	// MyChatMember    ChatMemberUpdated `json:"my_chat_member"`
	ChatMember ChatMemberUpdated `json:"chat_member"`
	// ChatJoinRequest ChatJoinRequest   `json:"chat_join_request"`
	// ChatBoost               ChatBoost                   `json:"chat_boost"`
	// RemovedChatBoost        ChatBoostRemoved            `json:"removed_chat_boost"`
}

func (u Update) GetUpdateType() string {
	switch {
	case u.CallbackQuery.Id != "":
		return "callback"
	case u.Msg.Id != 0:
		return "message"
	default:
		return ""
	}
}

type ChatFullInfo struct {
	Id               int64         `json:"id"`
	Type             string        `json:"type"`
	Title            string        `json:"title"`
	Username         string        `json:"username"`
	FirstName        string        `json:"first_name"`
	LastName         string        `json:"last_name"`
	IsForum          bool          `json:"is_forum"`
	AccentColorId    int           `json:"accent_color_id"`
	MaxReactionCount int           `json:"max_reaction_count"`
	Photo            ChatPhoto     `json:"photo"`
	ActiveUsernames  []string      `json:"active_usernames"`
	Birthdate        Birthdate     `json:"birthdate"`
	BusinessIntro    BusinessIntro `json:"business_intro"`
}

type ChatMember struct {
}

type ChatInviteLink struct {
}

type ChatMemberUpdated struct {
	Chat                    Chat           `json:"chat"`
	From                    User           `json:"from"`
	Date                    int64          `json:"date"`
	OldChatMember           ChatMember     `json:"chat_member"`
	NewChatMember           ChatMember     `json:"new_chat_member"`
	InviteLink              ChatInviteLink `json:"invite_link"`
	ViaJoinRequest          bool           `json:"via_join_request"`
	ViaChatFolderInviteLink bool           `json:"via_chat_folder_invite_link"`
}

type Message struct {
	Id                   int64              `json:"message_id"`
	MsgThread            int64              `json:"message_thread_id"`
	From                 User               `json:"from"`
	SenderChat           Chat               `json:"sender_chat"`
	SenderBoostCount     int64              `json:"sender_boost_count"`
	SenderBusinessBot    User               `json:"sender_business_bot"`
	Date                 int64              `json:"date"`
	BusinessConnectionId string             `json:"business_connection_id"`
	Chat                 Chat               `json:"chat"`
	ForwardOrigin        MessageOrigin      `json:"forward_origin"`
	IsTopicMsg           bool               `json:"is_topic_message"`
	IsAutomaticForward   bool               `json:"is_automatic_forward"`
	ReplyToMessage       *Message           `json:"reply_to_message"`
	ExternalReply        ExternalReplyInfo  `json:"external_reply"`
	Quote                TextQuote          `json:"quote"`
	ReplyToStory         Story              `json:"reply_to_story"`
	ViaBot               User               `json:"via_bot"`
	EditDate             int64              `json:"edit_date"`
	HasProtectedContent  bool               `json:"has_protected_content"`
	Is_from_offline      bool               `json:"is_from_offline"`
	MediaGroupId         string             `json:"media_group_id"`
	AuthorSignature      string             `json:"author_signature"`
	Text                 string             `json:"text"`
	Entities             []MessageEntity    `json:"entities"`
	LinkPreviewOptions   LinkPreviewOptions `json:"link_preview_options"`
	EffectId             string             `json:"effect_id"`
	// Animation                    Animation                     `json:"animation"`
	// Audio                        Audio                         `json:"audio"`
	// Document                     Document                      `json:"document"`
	// PaidMedia                    PaidMediaInfo                 `json:"paid_media"`
	// Photo                        []PhotoSize                   `json:"photo"`
	// Sticker                      Sticker                       `json:"sticker"`
	// Story                        Story                         `json:"story"`
	// Video                        Video                         `json:"video"`
	// VideoNote                    VideoNote                     `json:"video_note"`
	// Voice                        Voice                         `json:"voice"`
	// Caption                      string                        `json:"caption"`
	// CaptionEntities              []MessageEntity               `json:"caption_entities"`
	// ShowCaptionAboveMedia bool `json:"show_caption_above_media"`
	// HasMediaSpoiler       bool `json:"has_media_spoiler"`
	// Contact                      Contact                       `json:"contact"`
	// Dice                         Dice                          `json:"dice"`
	// Game                         Game                          `json:"game"`
	// Poll                         Poll                          `json:"poll"`
	// Venue                        Venue                         `json:"venue"`
	// Location                     Location                      `json:"location"`
	// NewChatMembers []User `json:"new_chat_members"`
	// LeftChatMember User   `json:"left_chat_member"`
	// NewChatTitle   string `json:"new_chat_title"`
	// NewChatPhoto                 []PhotoSize                   `json:"new_chat_photo"`
	// DeleteChatPhoto       bool `json:"delete_chat_photo"`
	// GroupChatCreated      bool `json:"group_chat_created"`
	// SupergroupChatCreated bool `json:"supergroup_chat_created"`
	// ChannelChatCreated    bool `json:"channel_chat_created"`
	// MsgAutoDeleteTimerChanged    MessageAutoDeleteTimerChanged `json:"message_auto_delete_timer_changed"`
	// MigrateToChatId   int64 `json:"migrate_to_chat_id"`
	// MigrateFromChatId int64 `json:"migrate_from_chat_id"`
	// PinnedMsg                    MaybeInaccessibleMessage      `json:"pinned_message"`
	// Invoice                      Invoice                       `json:"invoice"`
	// SuccessfulPayment            SuccessfulPayment             `json:"successful_payment"`
	// RefundedPayment              RefundedPayment               `json:"refunded_payment"`
	// UsersShared                  UsersShared                   `json:"users_shared"`
	// ChatShared                   ChatShared                    `json:"chat_shared"`
	// ConnectedWebsite             ConnectedWebsite              `json:"connected_website"`
	// WriteAccessAllowed           WriteAccessAllowed            `json:"write_access_allowed"`
	// PassportData                 PassportData                  `json:"passport_data"`
	// ProximityAlertTriggered      ProximityAlertTriggered       `json:"proximity_alert_triggered"`
	// BoostAdded                   ChatBoostAdded                `json:"boost_added"`
	// ChatBackgroundSet            ChatBackground                `json:"chat_background_set"`
	// ForumTopicCreated            ForumTopicCreated             `json:"forum_topic_created"`
	// ForumTopicEdited             ForumTopicEdited              `json:"forum_topic_edited"`
	// ForumTopicClosed             ForumTopicClosed              `json:"forum_topic_closed"`
	// ForumTopicReopened           ForumTopicReopened            `json:"forum_topic_reopened"`
	// GeneralForumTopicHidden      GeneralForumTopicHidden       `json:"general_forum_topic_hidden"`
	// GeneralForumTopicUnhidden    GeneralForumTopicUnhidden     `json:"general_forum_topic_unhidden"`
	// GiveawayCreated              GiveawayCreated               `json:"giveaway_created"`
	// Giveaway                     Giveaway                      `json:"giveaway"`
	// GiveawayWinners              GiveawayWinners               `json:"giveaway_winners"`
	// GiveawayCompleted            GiveawayCompleted             `json:"giveaway_completed"`
	// VideoChatScheduled           VideoChatScheduled            `json:"video_chat_scheduled"`
	// VideoChatStarted             VideoChatStarted              `json:"video_chat_started"`
	// VideoChatEnded               VideoChatEnded                `json:"video_chat_ended"`
	// VideoChatParticipantsInvited VideoChatParticipantsInvited  `json:"video_chat_participants_invited"`
	// WebAppData                   WebAppData                    `json:"web_app_data"`
	ReplyMarkup InlineKeyboardMarkup `json:"reply_markup"`
}

type CallbackQuery struct {
	Id            string  `json:"id"`
	From          User    `json:"from"`
	Msg           Message `json:"message"` // this is sketchy, coz the real type of this field is MaybeInaccessibleMessage
	InlineMsgId   string  `json:"inline_message_id"`
	ChatInstance  string  `json:"chat_instance"`
	Data          string  `json:"data"`
	GameShortName string  `json:"game_short_name"`
}

// // TODO: this should be made in to a responder
// func (cq CallbackQuery) Answer(baseUrl string) {
// 	// this function needs to be called to stop buttons from
// 	// blinking after they're pressed by the user
// 	url := fmt.Sprintf("%s/answerCallbackQuery", baseUrl)
// 	data := fmt.Sprintf("{\"callback_query_id\":\"%s\"}", cq.Id)
// 	client := &http.Client{}
// 	res, _ := client.Post(url, "application/json", bytes.NewBufferString(data))
// 	// if error happens here, no big deal
// 	res.Body.Close()
// }

type MessageOrigin struct {
	Type string `json:"type"`
	Date int64  `json:"date"`
}

type MessageOriginUser struct {
	MessageOrigin
	SenderUser User `json:"sender_user"`
}

type MessageOriginHiddenUser struct {
	MessageOrigin
	SenderUsername string `json:"sender_user_name"`
}

type MessageOriginChat struct {
	MessageOrigin
	SenderChat      Chat   `json:"sender_chat"`
	AuthorSignature string `json:"author_signature"`
}

type MessageOriginChannel struct {
	MessageOrigin
	Chat            Chat   `json:"chat"`
	MsgId           int64  `json:"message_id"`
	AuthorSignature string `json:"author_signature"`
}

type InlineKeyboardMarkup struct {
	InlineKeyboard [][]InlineKeyboardButton `json:"inline_keyboard"`
}

type InlineKeyboardButton struct {
	Text                         string `json:"text"`
	Url                          string `json:"url"`
	CallbackData                 string `json:"callback_data"`
	SwitchInlineQuery            string `json:"switch_inline_query"`
	SwitchInlineQueryCurrentChat string `json:"switch_inline_query_current_chat"`
	SwitchInlineQueryChosenChat  string `json:"switch_inline_query_chosen_chat"`
	Pay                          bool   `json:"pay"`
	// CallbackGame CallbachGame `json:"callback_game"`
	// CopyText CopyTextButton `json:"copy_text"`
	// WebApp WebAppInfo `json:"web_app"`
	// LoginUrl LoginUrl `json:"login_url"`
}

type MessageReactionUpdated struct {
}
type MessageReactionCountUpdated struct {
}
type MessageEntity struct {
}
type ExternalReplyInfo struct {
}
type Animation struct {
}
type Audio struct {
}
type LinkPreviewOptions struct {
}
type TextQuote struct {
}
type InlineQuery struct {
}
type ChosenInlineResult struct {
}

type ShippingQuery struct {
}
type PreCheckoutQuery struct {
}
type Story struct {
	Chat Chat  `json:"chat"`
	Id   int64 `json:"id"`
}
type Poll struct {
}
type PollAnswer struct {
}
type ChatBoost struct {
}
type ChatBoostRemoved struct {
}
type ChatJoinRequest struct {
}

type PaidMediaPurchased struct {
}
type Game struct {
}
type PhotoSize struct {
}
type Document struct {
}
type PaidMedia struct {
	Type string `json:"type"`
}
type PaidMediaInfo struct {
	StarCount int64       `json:"star_count"`
	PaidMedia []PaidMedia `json:"paid_media"`
}
type VideoNote struct {
}
type Video struct {
}
type Sticker struct {
}
type Voice struct {
}
type Contact struct {
}
type Dice struct {
	Emoji string `json:"emoji"`
	Value int8   `json:"value"`
}
type Venue struct {
}
type Location struct {
}
type MessageAutoDeleteTimerChanged struct {
	MessageAutoDeleteTime int64 `json:"message_auto_delete_time"`
}

//	type InaccessibleMessage struct {
//		// Chat  Chat  `json:"chat"`
//		MsgId int64 `json:"message_id"`
//		Date  int64 `json:"date"`
//	}
//
//	type MaybeInaccessibleMessage struct {
//		Message
//		InaccessibleMessage
//	}
type Invoice struct {
}
type SuccessfulPayment struct {
}
type RefundedPayment struct {
}
type UsersShared struct {
}
type ChatShared struct {
}
type ConnectedWebsite struct {
}
type WriteAccessAllowed struct {
}
type PassportData struct {
}
type ProximityAlertTriggered struct {
}
type ChatBoostAdded struct {
	BoostCount int32 `json:"boost_count"`
}
type ChatBackground struct {
}
type ForumTopicCreated struct {
}
type ForumTopicEdited struct {
}
type ForumTopicClosed struct {
}
type ForumTopicReopened struct {
}
type GeneralForumTopicHidden struct {
}
type GeneralForumTopicUnhidden struct {
}
type GiveawayCreated struct {
}
type Giveaway struct {
}
type GiveawayWinners struct {
}
type GiveawayCompleted struct {
}
type VideoChatScheduled struct {
}
type VideoChatStarted struct {
}
type VideoChatEnded struct {
}
type VideoChatParticipantsInvited struct {
}
type WebAppData struct {
	Data       string `json:"data"`
	ButtonText string `json:"button_text"`
}
type ChatPhoto struct {
}
type Birthdate struct {
	Day   int8  `json:"day"`
	Month int8  `json:"month"`
	Year  int16 `json:"year"`
}
type BusinessIntro struct {
}

type BusinessConnection struct {
}

type BusinessMessagesDeleted struct {
	BusinessConnectionId string `json:"business_connection_id"`
	Chat                 Chat   `json:"chat"`
	MsgIds               []int  `json:"message_ids"`
}

type Chat struct {
	Id        int64  `json:"id"`
	Type      string `json:"type"`
	Title     string `json:"title"`
	Username  string `json:"username"`
	FirstName string `json:"first_name"`
	LastName  string `json:"last_name"`
	IsForum   string `json:"is_forum"`
}
