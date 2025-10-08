# Fact Or Fake Game

A solo or multiplayer quiz game for Telegram.
https://t.me/FactOrFakeGameBot

## Overiview

The whole application consists of the following components:

### Bot

Handles interaction with an app through a Telegram bot and has three commands:
- /start - just a greeting and info about the app
- /help - explains the commands
- /creategame <number of rounds> <round duration> - sends a request to backend server for game creation and returns a link that launches a mini app

the bot also has a Play button which directly launches the mini app.

### Frontend

A Blazor WASM project for UI. It's what users see when launching the mini app. Users can choose to play either by themselves (with unlimited time for answers but the can make 5 mistakes after which the game is over and the result screen is shown) or with others. If they choose to play online, they can either join an existing room via room code or create a game which gives them a room code or a link they can share.

### Backend

Backend consists of ASP.NET application that communicates with a Postgres DB (for questions and storing user activity info) and Redis (for synchronizing rooms).
It has one controller for http requests (retrieving questions and creating rooms) and a SignalR service handling socket communication and broadcasting messages about a game state and players. Thanks to synchronization with Redis it works even when there are many replicas of the server.
The backend is also responsible for validating InitData string injected by Telegram and thus authenticating users. It doesn't work if someone connects through anything else than Telegram.

## Deployment

All of it, except for the frontend, is deployed on a server in a Kubernetes cluster. The backend is exposed to the public web through a tunnel. Frontend is hosted separately.
