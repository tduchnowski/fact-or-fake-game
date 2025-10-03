package main

import (
	"errors"
	"log/slog"

	"github.com/spf13/viper"
)

type Config struct {
	GameApiUrl string `mapstructure:"GAME_API_URL"`
	TgApiUrl   string `mapstructure:"TG_API_URL"`
	BotToken   string `mapstructure:"BOT_TOKEN"`
	ApiKey     string `mapstructure:"API_KEY"`
}

func LoadConfig() (*Config, error) {
	viper.SetConfigFile("./config.yaml")
	viper.AutomaticEnv()
	err := viper.ReadInConfig()
	if err != nil {
		return nil, err
	}
	var config Config
	err = viper.Unmarshal(&config)
	if err != nil {
		slog.Error("couldn't load the config file into a struct: " + err.Error())
		return nil, err
	}
	if config.BotToken == "" || config.GameApiUrl == "" || config.TgApiUrl == "" {
		return nil, errors.New("one or more config values are empty")
	}
	return &config, nil
}
