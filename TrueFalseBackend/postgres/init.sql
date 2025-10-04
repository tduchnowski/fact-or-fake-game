CREATE TABLE questions (
	id SERIAL PRIMARY KEY,
	text VARCHAR(255) NOT NULL,
	answer BOOLEAN NOT NULL,
	category VARCHAR(20),
	explanation VARCHAR(400)
);

CREATE INDEX category_index ON questions (category);

COPY questions (text, answer, category, explanation) FROM '/tmp/questions.csv' WITH (FORMAT csv, HEADER true, DELIMITER '|');

CREATE TABLE users (
	username VARCHAR(32) PRIMARY KEY UNIQUE,
	time_first_activity TIMESTAMP NOT NULL DEFAULT now(),
	time_last_activity TIMESTAMP,
	rooms_created INTEGER NOT NULL DEFAULT 0,
	games_played INTEGER NOT NULL DEFAULT 0
)
